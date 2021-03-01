using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraMove : MonoBehaviour
{
    public Transform mainCameraTransform;
    public float minY = 10f;
    public float maxY = 60f;
    
    public float scrollSpeed = 1f;
    public float rotateXSpeed = 1f;
    public float rotateYSpeed = 1f;
    private float _moveSpeed = 1f;
    public bool operate = true;

    public static Vector3 Position;
    public static Quaternion Rotation;
    
    private bool _isRotate;
    private bool _isMove;
    private Vector3 _traStart;
    private Vector3 _mouseStart;
    private bool _isDown;
    private Transform _transform;
    private World _world;
    private Scene _scene;

    private void Awake()
    {
        _world = GameObject.Find("World").GetComponent<World>();
        _scene = _world.scene;
    }

    private void Update()
    {
        _transform = transform;
        Position = _transform.position;
        Rotation = _transform.rotation;
        var height = - mainCameraTransform.localPosition.z;
        _moveSpeed = 0.1f + height * 0.01f;
        
        if (!operate) 
            return;
        if (_isRotate && Input.GetMouseButtonUp(2))
            _isRotate = false;
        if (_isMove && Input.GetMouseButtonUp(1))
            _isMove = false;
        
        if (!Input.GetMouseButton(2))
            _isRotate = false;
        if (!Input.GetMouseButton(1))
            _isMove = false;
        if (_isRotate && _isMove)
            return;
        
        if (Input.mousePosition.y >= Screen.height + 1 || 
            Input.mousePosition.y <= - 1 || 
            Input.mousePosition.x >= Screen.width + 1 || 
            Input.mousePosition.x <= - 1)
            return;

        if (_isRotate && !_isMove) 
        {
            var offset = Input.mousePosition - _mouseStart;
            
            // whether the lens is facing down
            if (_isDown)
            {   var rot = _traStart + new Vector3(offset.y * 0.3f * rotateYSpeed, -offset.x * 0.3f * rotateXSpeed, 0);
                rot.x = Mathf.Clamp(rot.x, 0f, 90f);
                _transform.rotation = Quaternion.Euler(rot);
            }
            else
            {
                var rotNotDown = _traStart + new Vector3(-offset.y * 0.3f * rotateYSpeed, offset.x * 0.3f * rotateXSpeed, 0);
                rotNotDown.x = Mathf.Clamp(rotNotDown.x, 0f, 90f); 
                _transform.rotation = Quaternion.Euler(rotNotDown);
            }
        }

        else if (Input.GetMouseButtonDown(2) && !_isMove)
        {
            _isRotate = true;
            _mouseStart = Input.mousePosition;
            _traStart = _transform.rotation.eulerAngles;
            _isDown = _transform.up.y < -0.0001f;
        }

        if (_isMove && !_isRotate)
        {
            var offset = Input.mousePosition - _mouseStart;
            var sceneRotationY = transform.rotation.eulerAngles.y;
            var sceneForward = Quaternion.Euler(0, sceneRotationY, 0) * Vector3.forward;
            var sceneRight = Quaternion.Euler(0, sceneRotationY, 0) * Vector3.right;
            var position = _traStart + _moveSpeed * -offset.y * 0.1f * sceneForward +
                                             _moveSpeed * -offset.x * 0.1f * sceneRight;

            _transform.position = new Vector3(Mathf.Clamp(position.x, 0,_scene.Width), position.y, Mathf.Clamp(position.z, 0,_scene.Height));
        }

        else if (Input.GetMouseButtonDown(1) && !_isRotate)
        {
            _isMove = true;
            _mouseStart = Input.mousePosition;
            _traStart = _transform.position;
        }
        
        // scroll
        if (World.MouseOverUi)
            return;
        
        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Math.Abs(scroll) < 0.0001f)
            return;

        var cameraFromMount = mainCameraTransform.localPosition;
        cameraFromMount.z += scrollSpeed * scroll * 10f * Mathf.Sqrt(height);
        cameraFromMount.z = - Mathf.Clamp(- cameraFromMount.z, minY, maxY);
        mainCameraTransform.localPosition = cameraFromMount;
        
    }

    public void ResetRotation()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
    }
}
using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class Draggable : MonoBehaviour
{
    public bool dragged;

    private bool _mouseOver;
    public bool MouseOver => !World.MouseOverUi && _mouseOver;

    private float _timeHoldingMouse;
    
    private void OnMouseDrag()
    {
        _timeHoldingMouse += Time.deltaTime;
        if (_timeHoldingMouse < 0.25f) 
            return;
        
        GetComponent<Collider>().enabled = false;
        dragged = true;
    }
    
    private void OnMouseEnter()
    {
        _mouseOver = true;
    }
    
    private void OnMouseExit()
    {
        _mouseOver = false;
    }
    
    private void OnMouseUp()
    {
        _timeHoldingMouse = 0;
        
        GetComponent<Collider>().enabled = true;
        dragged = false;
    }
}
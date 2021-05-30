using System;
using System.Collections.Generic;
using System.Linq;
using BardoUI;
using OutlineEffect.OutlineEffect;
using UnityEngine;
using UnityEngine.UI;

public class Token : MonoBehaviour
{
    public MeshRenderer baseMeshRenderer;
    public MeshCollider baseMeshCollider;
    public Canvas bodyCanvas;
    public Image bodyImage;
    public Canvas nameCanvas;
    public Text nameText;
    public Canvas barsCanvas;
    public GameObject healthBar;
    public Image healthImage;
    public Text healthText;
    public GameObject staminaBar;
    public Image staminaImage;
    public Text staminaText;
    public GameObject manaBar;
    public Image manaImage;
    public Text manaText;
    public Draggable draggableBase;
    public OutlineMesh outlineSelected;
    public OutlineMesh outlineFocused;

    private static readonly string[] Permissions =
    {
        "NAME", "POSITION", "VISION", "CONTROL", "HEALTH", "STAMINA", "MANA"
    };

    public Tile tile;
    public List<Tile> tilesInVision;
    public List<Tile> tilesInLight;
    public List<Aura> auras;

    public bool dirty;
    public bool focused;

    public string id;
    public bool hasLabel;
    public string label;
    public bool hasInitiative;
    public float initiative;
    public bool isStatic;
    public bool hasBase;
    public float baseSize = 1f;
    public Color baseColor;
    public float baseAlfa;
    public bool hasBody;
    public float bodySize = 1f;
    public Color bodyColor;
    public float bodyAlfa = 1f;
    public string bodyResource = "";
    public bool hasHealth;
    public float health;
    public float maxHealth = 100;
    public bool hasStamina;
    public float stamina;
    public float maxStamina = 100;
    public bool hasMana;
    public float mana;
    public float maxMana = 100;
    public bool hasVision;
    public bool hasShadowVision;
    public float shadowVisionRange;
    public bool hasDarkVision;
    public bool hasLight;
    public float lightRange;

    public PropertyHolder properties = new PropertyHolder();

    public bool sharedName;
    public bool sharedPosition;
    public bool sharedVision;
    public bool sharedControl;
    public bool sharedHealth;
    public bool sharedStamina;
    public bool sharedMana;

    private Vector2 Offset
    {
        get
        {
            var pos = transform.position;
            return new Vector2(pos.x - tile.x, pos.z - tile.y);
        }
    }

    public float rotation;

    private Camera _mainCamera;
    private bool _inMovement;
    private Tile _cachedTile;
    private Vector3 _cachedPosition;
    private bool _resetVision;

    private void Awake()
    {
        _mainCamera = Camera.main;
        id = World.NewId();
        baseMeshCollider.gameObject.SetActive(false);
        bodyCanvas.gameObject.SetActive(false);
        nameCanvas.gameObject.SetActive(false);
        healthBar.gameObject.SetActive(false);
        staminaBar.gameObject.SetActive(false);
        manaBar.gameObject.SetActive(false);
    }

    private void FixTile()
    {
        if (!ReferenceEquals(tile, null)) return;

        var position = transform.position;
        var scene = GameObject.Find("World").GetComponent<World>().scene;
        tile = scene.Tile(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
        transform.parent = tile.chunk.scene.tokensParent;
        tile.chunk.scene.tokens.Add(this);
        _cachedPosition = new Vector3(tile.x, tile.altitude * World.U, tile.y);
        dirty = true;
    }

    private void Update()
    {
        FixTile();

        if (dirty) Refresh();

        UpdateSelection();
        UpdateUi();
        UpdatePosition();
        UpdateVision();
        UpdateStaging();
        UpdateOutline();
    }

    private void Refresh()
    {
        transform.localEulerAngles = new Vector3(0, rotation, 0);
        // token
        var baseTransform = baseMeshRenderer.transform;
        baseTransform.gameObject.SetActive(hasBase);
        var bodyTransform = bodyCanvas.transform;
        bodyTransform.gameObject.SetActive(hasBody);
        nameCanvas.gameObject.SetActive(hasLabel);
        healthBar.gameObject.SetActive(hasHealth);
        staminaBar.gameObject.SetActive(hasStamina);
        manaBar.gameObject.SetActive(hasMana);
        baseTransform.localScale = new Vector3(baseSize, baseTransform.localScale.y, baseSize);
        var baseColorAll = new Color(baseColor.r, baseColor.g, baseColor.b, baseAlfa);
        baseMeshRenderer.material.color = baseColorAll;
        var bodyColorAll = new Color(bodyColor.r, bodyColor.g, bodyColor.b, bodyAlfa);
        bodyImage.color = bodyColorAll;
        if (bodyResource != "")
        {
            var bodySprite = bodyImage.sprite = World.GetResource<Sprite>(bodyResource);
            bodyImage.rectTransform.sizeDelta = new Vector2(bodySprite.rect.width, bodySprite.rect.height);
            bodyTransform.localScale = Vector3.one * bodySize / bodySprite.rect.height;
        }

        // UI
        healthImage.fillAmount = health / maxHealth;
        healthText.text = $"{health} / {maxHealth}";
        staminaImage.fillAmount = stamina / maxStamina;
        staminaText.text = $"{stamina} / {maxStamina}";
        manaImage.fillAmount = mana / maxMana;
        manaText.text = $"{mana} / {maxMana}";
        nameText.text = label;

        RefreshPermissions();
        RefreshVision();

        dirty = false;
    }

    public void RefreshPermissions()
    {
        if (World.playerIsMaster && !World.sharingPlayerVision)
        {
            sharedName = true;
            sharedPosition = true;
            sharedVision = true;
            sharedControl = true;
            sharedHealth = true;
            sharedStamina = true;
            sharedMana = true;
            return;
        }

        sharedName = false;
        sharedPosition = false;
        sharedVision = false;
        sharedControl = false;
        sharedHealth = false;
        sharedStamina = false;
        sharedMana = false;

        foreach (var permission in Permissions)
        {
            var property = World.instance.scene.properties.FirstOrDefault(x => x.name == permission);
            if (property == null) continue;

            var tokens = property.values;
            if (!tokens.Contains(id)) continue;

            switch (permission)
            {
                case "NAME":
                    sharedName = true;
                    break;
                case "POSITION":
                    sharedPosition = true;
                    break;
                case "VISION":
                    sharedVision = true;
                    break;
                case "CONTROL":
                    sharedControl = true;
                    break;
                case "HEALTH":
                    sharedHealth = true;
                    break;
                case "STAMINA":
                    sharedStamina = true;
                    break;
                case "MANA":
                    sharedMana = true;
                    break;
            }
        }
    }

    private void RefreshVision()
    {
        _resetVision = true;
        tile.chunk.scene.RefreshVision();
    }

    private void UpdateSelection()
    {
        if (LeftClicked)
        {
            var scene = tile.chunk.scene;
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                scene.selectedTokens.Clear();
            if (scene.selectedTokens.Contains(this))
                scene.selectedTokens.Remove(this);
            else
                scene.selectedTokens.Add(this);
        }
        else if (RightClicked)
        {
            var scene = tile.chunk.scene;
            scene.selectedTokens.Clear();
            scene.selectedTokens.Add(this);
            NavigationPanel.instance.ShowTokenEditWindow();
        }

    }

    private void UpdateUi()
    {
        nameCanvas.gameObject.SetActive(hasLabel && sharedName && tile.explored);
        healthBar.SetActive(hasHealth && sharedHealth && tile.explored);
        staminaBar.SetActive(hasStamina && sharedStamina && tile.explored);
        manaBar.SetActive(hasMana && sharedMana && tile.explored);

        var barsCanvasTransform = barsCanvas.transform;
        var nameCanvasTransform = nameCanvas.transform;
        var bodyCanvasTransform = bodyCanvas.transform;
        var mainCameraTransform = _mainCamera.transform;
        var mainCameraPosition = mainCameraTransform.position;
        var mainCameraRotation = mainCameraTransform.rotation;
        barsCanvasTransform.LookAt(mainCameraPosition);
        nameCanvasTransform.LookAt(mainCameraPosition);
        bodyCanvasTransform.LookAt(mainCameraPosition);
        barsCanvasTransform.rotation = mainCameraRotation;
        nameCanvasTransform.rotation = mainCameraRotation;
        bodyCanvasTransform.rotation = mainCameraRotation;
        var cameraForward = mainCameraTransform.forward;
        var cameraUp = mainCameraTransform.up;
        var screenForward = new Vector3(cameraForward.x, 0, cameraForward.z);
        screenForward = screenForward.magnitude < 0.001f ? cameraUp : screenForward.normalized;
        if (!hasBody)
        {
            barsCanvasTransform.localPosition = baseSize / 2 * screenForward + World.U * Vector3.up;
            nameCanvasTransform.localPosition = -baseSize / 2 * screenForward;
        }
        else
        {
            bodyCanvasTransform.localPosition = nameCanvasTransform.localPosition =
                -Mathf.Sin(mainCameraRotation.eulerAngles.x * Mathf.Deg2Rad) * baseSize / 2 * screenForward +
                World.U * Vector3.up;
            barsCanvasTransform.localPosition = bodyCanvasTransform.localPosition + bodySize * cameraUp;
        }
    }

    private void UpdatePosition()
    {
        if (!sharedControl)
            return;

        if (Dragged)
        {
            _inMovement = true;
            baseMeshRenderer.material.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseAlfa / 2);
            bodyImage.color = new Color(bodyColor.r, bodyColor.g, bodyColor.b, bodyAlfa / 2);

            var scene = tile.chunk.scene;
            var mouseTile = scene.mouseTile;
            if (!mouseTile || !mouseTile.walkable || !mouseTile.explored) return;

            tile = mouseTile;
            ColliderEnabled = false;
            _cachedPosition = new Vector3(scene.mousePosition.x, tile.altitude * World.U, scene.mousePosition.z);

            if (Input.GetKey(KeyCode.LeftAlt))
                transform.position = _cachedPosition;
            else
                transform.position = new Vector3(Mathf.Round(_cachedPosition.x * 2) / 2,
                    tile.altitude * World.U, Mathf.Round(_cachedPosition.z * 2) / 2);

            if (tile == _cachedTile) return;

            _cachedTile = tile;
            RefreshVision();
        }
        else if (_inMovement)
        {
            _inMovement = false;
            ColliderEnabled = true;
            baseMeshRenderer.material.color = baseColor;
            bodyImage.color = bodyColor;

            if (Input.GetKey(KeyCode.LeftAlt))
                transform.position = new Vector3(_cachedPosition.x, tile.altitude * World.U, _cachedPosition.z);
            else
                transform.position = new Vector3(Mathf.Round(_cachedPosition.x * 2) / 2,
                    tile.altitude * World.U, Mathf.Round(_cachedPosition.z * 2) / 2);

            World.instance.RegisterAction(new Action
            {
                name = World.ActionNames.ChangeToken,
                scene = World.instance.scene.id,
                tokens = new List<Model> {Serializable()}
            });
        }
    }

    private void UpdateVision()
    {
        if (!_resetVision)
            return;
        _resetVision = false;

        var scene = tile.chunk.scene;
        tilesInVision.Clear();
        if (hasVision && (sharedControl || sharedVision))
            tilesInVision.AddRange(scene.TilesInRange(tile, 50));
        else if (!hasVision && sharedControl)
            tilesInVision.Add(tile);

        tilesInLight.Clear();
        if (hasLight)
            tilesInLight.AddRange(scene.TilesInRange(tile, lightRange));
        else if (hasVision && sharedControl)
            tilesInLight.Add(tile);
    }

    private void UpdateStaging()
    {
        if (!isStatic && !tile.revealed)
        {
            baseMeshRenderer.gameObject.SetActive(false);
            bodyCanvas.gameObject.SetActive(false);
            return;
        }

        if (sharedPosition)
        {
            baseMeshRenderer.gameObject.SetActive(hasBase && (tile.explored || sharedControl));
            bodyCanvas.gameObject.SetActive(hasBody && (tile.explored || sharedControl));
        }
        else
        {
            baseMeshRenderer.gameObject.SetActive(false);
            bodyCanvas.gameObject.SetActive(false);
            return;
        }

        if (Dragged) return;

        var tl = tile.revealed ? 1 : 0.5f;
        baseMeshRenderer.material.color = new Color(baseColor.r * tl, baseColor.g * tl, baseColor.b * tl, baseAlfa);
        bodyImage.color = new Color(bodyColor.r * tl, bodyColor.g * tl, bodyColor.b * tl, bodyAlfa);
    }

    private void UpdateOutline()
    {
        var focus = focused || MouseOver || Dragged;
        outlineSelected.enabled = tile.chunk.scene.selectedTokens.Contains(this) && !focus;
        outlineFocused.enabled = focus;
    }

    private bool Dragged => draggableBase.dragged;

    private bool MouseOver => draggableBase.MouseOver;

    private bool LeftClicked => MouseOver && Input.GetMouseButtonDown(0);
    private bool RightClicked => MouseOver && Input.GetMouseButtonDown(1);

    private bool ColliderEnabled
    {
        set => baseMeshCollider.enabled = value;
    }


    #region Serialization

    [Serializable]
    public class Model
    {
        public string id;
        public bool hasLabel;
        public string label;
        public bool hasInitiative;
        public float initiative;
        public bool isStatic;
        public bool hasBase;
        public float baseSize;
        public Color baseColor;
        public bool hasBody;
        public float bodySize;
        public Color bodyColor;
        public string bodyResource;
        public bool hasHealth;
        public float health;
        public float maxHealth;
        public bool hasStamina;
        public float stamina;
        public float maxStamina;
        public bool hasMana;
        public float mana;
        public float maxMana;
        public bool hasVision;
        public bool hasShadowVision;
        public float shadowVisionRange;
        public bool hasDarkVision;
        public bool hasLight;
        public float lightRange;

        public Vector2Int position;
        public Vector2 offset;
        public float rotation;
    }

    public Model Serializable()
    {
        return new Model
        {
            id = id,
            hasLabel = hasLabel,
            label = label,
            hasInitiative = hasInitiative,
            initiative = initiative,
            isStatic = isStatic,
            hasBase = hasBase,
            baseSize = baseSize,
            baseColor = baseColor,
            hasBody = hasBody,
            bodySize = bodySize,
            bodyColor = bodyColor,
            bodyResource = bodyResource,
            hasHealth = hasHealth,
            health = health,
            maxHealth = maxHealth,
            hasStamina = hasStamina,
            stamina = stamina,
            maxStamina = maxStamina,
            hasMana = hasMana,
            mana = mana,
            maxMana = maxMana,
            hasVision = hasVision,
            hasShadowVision = hasShadowVision,
            shadowVisionRange = shadowVisionRange,
            hasDarkVision = hasDarkVision,
            hasLight = hasLight,
            lightRange = lightRange,

            position = new Vector2Int(tile.x, tile.y),
            offset = Offset,
            rotation = rotation,
        };
    }

    public void Deserialize(Model model)
    {
        tile = World.instance.scene.Tile(model.position.x, model.position.y);
        id = model.id;
        hasLabel = model.hasLabel;
        label = model.label;
        hasInitiative = model.hasInitiative;
        initiative = model.initiative;
        isStatic = model.isStatic;
        hasBase = model.hasBase;
        baseSize = model.baseSize;
        baseColor = model.baseColor;
        hasBody = model.hasBody;
        bodySize = model.bodySize;
        bodyColor = model.bodyColor;
        bodyResource = model.bodyResource;
        hasHealth = model.hasHealth;
        health = model.health;
        maxHealth = model.maxHealth;
        hasStamina = model.hasStamina;
        stamina = model.stamina;
        maxStamina = model.maxStamina;
        hasMana = model.hasMana;
        mana = model.mana;
        maxMana = model.maxMana;
        hasVision = model.hasVision;
        hasShadowVision = model.hasShadowVision;
        shadowVisionRange = model.shadowVisionRange;
        hasDarkVision = model.hasDarkVision;
        hasLight = model.hasLight;
        lightRange = model.lightRange;

        transform.position = new Vector3(tile.x + model.offset.x, tile.altitude, tile.y + model.offset.y);

        Refresh();
    }

    #endregion
}
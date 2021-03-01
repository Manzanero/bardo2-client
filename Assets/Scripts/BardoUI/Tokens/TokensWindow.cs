using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace BardoUI.Tokens
{
    public class TokensWindow : MonoBehaviour
    {
        public Transform propertyItemsParent;
        public GameObject propertyItemPrefab;
        public Transform selectedItemsParent;
        public GameObject selectedItemPrefab;
        
        public Text currentTileText;
        public Button newTokenButton;
        public Button addButton;
        public Button clearButton;
        public Button saveButton;
        public Button cloneButton;
        public Button loadButton;
        public Button deleteButton;
        
        public bool dirtyTiles;
        public bool dirtyTokens;
        public bool dirtyProperties;
        
        private World _world;
        private Scene _scene;
        private Tile _cacheSelectTile;
        private readonly List<TokenPropertyItem> _tokenPropertiesItems = new List<TokenPropertyItem>();
        private const string DefaultText = "-";

        private void Awake()
        {
            foreach (Transform child in propertyItemsParent) Destroy(child.gameObject);
            foreach (Transform child in selectedItemsParent) Destroy(child.gameObject);
        }

        private void Start()
        {
            _world = World.instance;
            _scene = _world.scene;
            newTokenButton.onClick.AddListener(NewToken);
            addButton.onClick.AddListener(Add);
            clearButton.onClick.AddListener(Clear);
            saveButton.onClick.AddListener(Save);
            cloneButton.onClick.AddListener(Clone);
            loadButton.onClick.AddListener(Load);
            deleteButton.onClick.AddListener(Delete);

            _scene.selectedTiles.CollectionChanged += SelectedTilesCollectionChanged;
            _scene.selectedTokens.CollectionChanged += SelectedTokensCollectionChanged;

            dirtyProperties = true;
        }

        private void SelectedTilesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => dirtyTiles = true;
        private void SelectedTokensCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => dirtyTokens = true;

        private void Update()
        {
            if (dirtyTiles)
            {
                var tiles = _scene.selectedTiles;
                if (tiles.Any())
                {
                    _cacheSelectTile = tiles[0];
                    currentTileText.text = $"x: {tiles[0].x}, y: {tiles[0].y}";
                }
                else
                {
                    currentTileText.text = DefaultText;
                }
                
                dirtyTiles = false;
            }

            if (dirtyTokens)
            {
                
                foreach (Transform child in selectedItemsParent) Destroy(child.gameObject);
                foreach (var token in _scene.selectedTokens)
                {
                    var item = Instantiate(selectedItemPrefab, selectedItemsParent).GetComponent<TokenSelectedItem>();
                    item.token = token;
                    item.tokenName.text = token.label;
                    item.tokenId.text = token.id;
                    item.deselectButton.onClick.AddListener(delegate { _scene.selectedTokens.Remove(token); });
                }
                
                dirtyTokens = false;
                dirtyProperties = true;
            }

            if (dirtyProperties)
            {
                _tokenPropertiesItems.Clear();
                foreach (Transform child in propertyItemsParent) Destroy(child.gameObject);

                foreach (var property in _world.tokenProperties)
                {
                    var item = Instantiate(propertyItemPrefab, propertyItemsParent).GetComponent<TokenPropertyItem>();
                    _tokenPropertiesItems.Add(item);
                    item.label = property.label;
                    item.id = property.id;
                    item.type = property.type;
                    item.Control = property.control;
                    
                    if (_scene.selectedTokens.Count != 1) continue;

                    var token = _scene.selectedTokens[0];
                    item.Values = property.id switch
                    {
                        "showLabel" => ToStrings(token.hasLabel),
                        "label" => ToStrings(token.label),
                        "initiative" => ToStrings(token.initiative),
                        "isStatic" => ToStrings(token.isStatic),
                        "hasBase" => ToStrings(token.hasBase),
                        "baseSize" => ToStrings(token.baseSize),
                        "baseColor" => ToStrings(token.baseColor),
                        "baseAlfa" => ToStrings(Mathf.Round(token.baseAlfa * 100)),
                        "hasBody" => ToStrings(token.hasBody),
                        "bodySize" => ToStrings(token.bodySize),
                        "bodyColor" => ToStrings(token.bodyColor),
                        "bodyAlfa" => ToStrings(Mathf.Round(token.bodyAlfa * 100)),
                        "bodyResource" => ToStrings(token.bodyResource),
                        "health" => ToStrings(token.health, token.maxHealth),
                        "stamina" => ToStrings(token.stamina, token.maxStamina),
                        "mana" => ToStrings(token.mana, token.maxMana),
                        "vision" => ToStrings(token.hasVision),
                        "light" => ToStrings(token.lightRange),
                        _ => item.Values
                    };
                    item.Changes = false;
                }
                    
                dirtyProperties = false;
            }
        }

        private static string[] ToStrings(string value) => new[] {value};
        private static string[] ToStrings(bool value) => new[] {value ? "true" : "false"};
        private static string[] ToStrings(float value, float max) => new [] {value.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture)};
        private static string[] ToStrings(float value) => new[] {value.ToString(CultureInfo.InvariantCulture)};
        private static string[] ToStrings(Color value) => new []
        {
            Mathf.Round(value.r * 100).ToString(CultureInfo.InvariantCulture),
            Mathf.Round(value.g * 100).ToString(CultureInfo.InvariantCulture),
            Mathf.Round(value.b * 100).ToString(CultureInfo.InvariantCulture)
        };
        
        private void NewToken()
        {
            var tile = _cacheSelectTile;
            if (!tile)
                return;
            
            var token = Instantiate(_world.scene.tokenPrefab, _world.scene.tokensParent).GetComponent<Token>();
            token.transform.position = new Vector3(tile.x, tile.altitude, tile.y);
            _scene.tokens.Add(token);
            _scene.selectedTokens.Clear();
            _scene.selectedTokens.Add(token);
            token.tile = tile;
            token.id = World.NewId();
            token.label = "New Token";
            token.hasBase = true;
            token.baseSize = 1f;
            token.baseColor = Color.white;
            token.baseAlfa = 0.7f;
            token.hasBody = false;
            token.bodySize = 1f;
            token.bodyColor = Color.white;
            token.bodyAlfa = 1f;
            token.hasHealth = false;
            token.hasStamina = false;
            token.hasMana = false;
            token.dirty = true;

            // _world.RegisterAction(new Action {
            //     name = GameMaster.ActionNames.ChangeToken,
            //     map = map.id,
            //     entities = map.selectedEntities.Select(x => x.Serialize()).ToList()
            // });
        }
        
        private void Add()
        {
        }
        
        private void Clear()
        {
            dirtyProperties = true;
        }
        
        private void Save()
        {
            var changedItems = _tokenPropertiesItems.Where(item => item.Changes).ToList();
            foreach (var token in _scene.selectedTokens)
            {
                foreach (var item in changedItems)
                {
                    switch (item.id)
                    {
                        case "showLabel": token.hasLabel = item.ToBoolean(); break;
                        case "label": token.label = item.ToText(); break;
                        case "initiative": token.initiative = item.ToNumeric(); break;
                        case "isStatic": token.isStatic = item.ToBoolean(); break;
                        case "hasBase": token.hasBase = item.ToBoolean(); break;
                        case "baseSize": token.baseSize = item.ToNumeric(); break;
                        case "baseColor": token.baseColor = item.ToColor(); break;
                        case "baseAlfa": token.baseAlfa = Mathf.Clamp(item.ToNumeric() / 100, 0, 1); break;
                        case "hasBody": token.hasBody = item.ToBoolean(); break;
                        case "bodySize": token.bodySize = item.ToNumeric(); break;
                        case "bodyColor": token.bodyColor = item.ToColor(); break;
                        case "bodyAlfa": token.bodyAlfa = item.ToNumeric() / 100; break;
                        case "bodyResource": token.bodyResource = item.ToText(); break;
                        case "health": var health = item.ToBar(); token.health = health[0]; token.maxHealth = health[1]; break;
                        case "stamina": var stamina = item.ToBar(); token.stamina = stamina[0]; token.maxStamina = stamina[1]; break;
                        case "mana": var mana = item.ToBar(); token.mana = mana[0]; token.maxMana = mana[1]; break;
                        case "vision": token.hasVision = item.ToBoolean(); break;
                        case "light": token.lightRange = item.ToNumeric(); break;
                    }
                }
                token.dirty = true;
                dirtyTokens = true;
            }
            
            // var controlledIds = _tokenPropertiesItems.Where(item => item.Control).Select(item => item.id).ToList();
            // foreach (var property in _world.tokenProperties) property.control = controlledIds.Contains(property.id);
            
            // _world.RegisterAction(new Action {
            //     name = GameMaster.ActionNames.ChangeToken,
            //     map = map.id,
            //     entities = map.selectedEntities.Select(x => x.Serialize()).ToList()
            // });
        }
        
        private void Clone()
        {
            var tempSelected = new List<Token>();
            tempSelected.AddRange(_scene.selectedTokens);
            _scene.selectedTokens.Clear();
            foreach (var selected in tempSelected)
            {
                var token = Instantiate(_world.scene.tokenPrefab, _world.scene.tokensParent).GetComponent<Token>();
                token.transform.position = selected.transform.position;
                _scene.tokens.Add(token);
                _scene.selectedTokens.Add(token);
                token.tile = selected.tile;
                token.id = World.NewId();
                token.label = selected.label;
                token.hasBase = selected.hasBase;
                token.baseSize = selected.baseSize;
                token.baseColor = selected.baseColor;
                token.baseAlfa = selected.baseAlfa;
                token.hasBody = selected.hasBody;
                token.bodySize = selected.bodySize;
                token.bodyColor = selected.bodyColor;
                token.bodyAlfa = selected.bodyAlfa;
                token.bodyResource = selected.bodyResource;
                token.hasHealth = selected.hasHealth;
                token.hasStamina = selected.hasStamina;
                token.hasMana = selected.hasMana;
                
                token.dirty = true;
            }
        }
        
        private void Load()
        {
        }
        
        private void Delete()
        {
            foreach (var token in _scene.selectedTokens)
            {
                _scene.tokens.Remove(token);
                Destroy(token.gameObject);
            }
            
            // _world.RegisterAction(new Action {
            //     name = GameMaster.ActionNames.DeleteToken,
            //     map = map.id,
            //     entities = map.selectedEntities.Select(x => x.Serialize()).ToList()
            // });
            
            _scene.selectedTokens.Clear();
        }
    }
}
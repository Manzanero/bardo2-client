using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
        public Scrollbar scrollbar;
        public Button addButton;
        public AddPropertyWindow addPropertyWindow;
        public Button removeButton;
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
            addPropertyWindow.gameObject.SetActive(false);
        }

        private void Start()
        {
            _world = World.instance;
            _scene = _world.scene;
            newTokenButton.onClick.AddListener(NewToken);
            addButton.onClick.AddListener(Add);
            removeButton.onClick.AddListener(Remove);
            clearButton.onClick.AddListener(Clear);
            saveButton.onClick.AddListener(Save);
            cloneButton.onClick.AddListener(Clone);
            loadButton.onClick.AddListener(Load);
            deleteButton.onClick.AddListener(Delete);

            _scene.selectedTiles.CollectionChanged += (sender, e) => dirtyTiles = true;
            _scene.selectedTokens.CollectionChanged += (sender, e) => dirtyTokens = true;

            dirtyProperties = true;
        }

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

                foreach (var key in _world.tokenPropertiesInfo.Keys)
                {
                    var item = Instantiate(propertyItemPrefab, propertyItemsParent).GetComponent<TokenPropertyItem>();
                    _tokenPropertiesItems.Add(item);
                    item.label = World.Humanize(key);
                    item.key = key;
                    item.type = _world.tokenPropertiesInfo[key].type;
                    item.master = true;
                    item.extra = _world.tokenPropertiesInfo[key].extra;
                    item.Control = _world.tokenPropertiesInfo[key].control;

                    if (_scene.selectedTokens.Count != 1) continue;

                    var token = _scene.selectedTokens[0];
                    switch (key)
                    {
                        case "showLabel":
                            item.Values = BooleanToStrings(token.hasLabel);
                            break;
                        case "label":
                            item.Values = TextToStrings(token.label);
                            break;
                        case "initiative":
                            item.Values = NumericToStrings(token.initiative);
                            break;
                        case "isStatic":
                            item.Values = BooleanToStrings(token.isStatic);
                            break;
                        case "hasBase":
                            item.Values = BooleanToStrings(token.hasBase);
                            break;
                        case "baseSize":
                            item.Values = NumericToStrings(token.baseSize);
                            break;
                        case "baseColor":
                            item.Values = ColorToStrings(token.baseColor);
                            break;
                        case "baseAlfa":
                            item.Values = PercentageToStrings(token.baseAlfa);
                            break;
                        case "hasBody":
                            item.Values = BooleanToStrings(token.hasBody);
                            break;
                        case "bodySize":
                            item.Values = NumericToStrings(token.bodySize);
                            break;
                        case "bodyColor":
                            item.Values = ColorToStrings(token.bodyColor);
                            break;
                        case "bodyAlfa":
                            item.Values = PercentageToStrings(token.bodyAlfa);
                            break;
                        case "bodyResource":
                            item.Values = TextToStrings(token.bodyResource);
                            break;
                        case "health":
                            item.Values = BarToStrings(token.health, token.maxHealth);
                            break;
                        case "stamina":
                            item.Values = BarToStrings(token.stamina, token.maxStamina);
                            break;
                        case "mana":
                            item.Values = BarToStrings(token.mana, token.maxMana);
                            break;
                        case "vision":
                            item.Values = BooleanToStrings(token.hasVision);
                            break;
                        case "light":
                            item.Values = NumericToStrings(token.lightRange);
                            break;
                        default:
                            if (!token.properties.Has(item.key)) continue;
                            item.Values = token.properties.Get(item.key).values.ToArray();
                            break;
                    }

                    item.Changes = false;
                }

                dirtyProperties = false;
            }
        }

        private static string[] TextToStrings(string value) => new[] {value};
        private static string[] BooleanToStrings(bool value) => new[] {value ? "true" : "false"};

        private static string[] BarToStrings(float value, float max) => new[]
        {
            value.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture)
        };

        private static string[] NumericToStrings(float value) => new[] {value.ToString(CultureInfo.InvariantCulture)};

        private static string[] ColorToStrings(Color value) => new[]
        {
            Mathf.Round(value.r * 100).ToString(CultureInfo.InvariantCulture),
            Mathf.Round(value.g * 100).ToString(CultureInfo.InvariantCulture),
            Mathf.Round(value.b * 100).ToString(CultureInfo.InvariantCulture)
        };

        private static string[] PercentageToStrings(float value) => new[]
        {
            Mathf.Round(value * 100).ToString(CultureInfo.InvariantCulture)
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

            _world.RegisterAction(new Action {
                name = World.ActionNames.ChangeToken,
                scene = World.instance.scene.id,
                tokens = World.instance.scene.selectedTokens.Select(x => x.Serializable()).ToList()
            });
        }

        private void Add()
        {
            addPropertyWindow.gameObject.SetActive(true);
        }

        private void Remove()
        {
            var removeItems = _tokenPropertiesItems.Where(item => item.Remove).ToList();
            foreach (var item in removeItems)
            {
                _world.tokenPropertiesInfo.Remove(item.key);
                foreach (var token in _scene.tokens) token.properties.Remove(item.key);
            }
            
            dirtyProperties = true;
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
                    switch (item.key)
                    {
                        case "showLabel":
                            token.hasLabel = item.ToBoolean();
                            break;
                        case "label":
                            token.label = item.ToText();
                            break;
                        case "initiative":
                            token.initiative = item.ToNumeric();
                            break;
                        case "isStatic":
                            token.isStatic = item.ToBoolean();
                            break;
                        case "hasBase":
                            token.hasBase = item.ToBoolean();
                            break;
                        case "baseSize":
                            token.baseSize = item.ToNumeric();
                            break;
                        case "baseColor":
                            token.baseColor = item.ToColor();
                            break;
                        case "baseAlfa":
                            token.baseAlfa = Mathf.Clamp(item.ToPercentage(), 0, 1);
                            break;
                        case "hasBody":
                            token.hasBody = item.ToBoolean();
                            break;
                        case "bodySize":
                            token.bodySize = item.ToNumeric();
                            break;
                        case "bodyColor":
                            token.bodyColor = item.ToColor();
                            break;
                        case "bodyAlfa":
                            token.bodyAlfa = Mathf.Clamp(item.ToPercentage(), 0, 1);
                            break;
                        case "bodyResource":
                            token.bodyResource = item.ToText();
                            break;
                        case "health":
                            var health = item.ToBar();
                            token.health = health[0];
                            token.maxHealth = health[1];
                            break;
                        case "stamina":
                            var stamina = item.ToBar();
                            token.stamina = stamina[0];
                            token.maxStamina = stamina[1];
                            break;
                        case "mana":
                            var mana = item.ToBar();
                            token.mana = mana[0];
                            token.maxMana = mana[1];
                            break;
                        case "vision":
                            token.hasVision = item.ToBoolean();
                            break;
                        case "light":
                            token.lightRange = item.ToNumeric();
                            break;
                        default:
                            token.properties.Set(item.key, item.Values);
                            break;
                    }
                }

                token.dirty = true;
                dirtyTokens = true;
            }
            
            dirtyProperties = true;

            _world.RegisterAction(new Action {
                name = World.ActionNames.ChangeToken,
                scene = _scene.id,
                tokens = _scene.selectedTokens.Select(x => x.Serializable()).ToList()
            });
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
                token.properties = selected.properties;

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

            _world.RegisterAction(new Action {
                name = World.ActionNames.DeleteToken,
                scene = _scene.id,
                tokens = _scene.selectedTokens.Select(x => x.Serializable()).ToList()
            });

            _scene.selectedTokens.Clear();
        }

        public void ScrollToLast() => StartCoroutine(ScrollToLastCoroutine());

        private IEnumerator ScrollToLastCoroutine()
        {
            yield return new WaitForEndOfFrame();
            scrollbar.value = 0f;
            yield return new WaitForEndOfFrame();
            scrollbar.value = 0f;
        }
    }
}
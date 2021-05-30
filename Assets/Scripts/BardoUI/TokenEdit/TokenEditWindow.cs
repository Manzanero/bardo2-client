using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BardoUI.Tokens;
using UnityEngine;
using UnityEngine.UI;

namespace BardoUI.TokenEdit
{
    public class TokenEditWindow : MonoBehaviour
    {
        public Transform propertyItemsParent;
        public GameObject propertyItemPrefab;

        public Button clearButton;
        public Button saveButton;

        public bool dirtyProperties;

        private World _world;
        private Scene _scene;
        private readonly List<TokenPropertyItem> _tokenPropertiesItems = new List<TokenPropertyItem>();

        private void Awake()
        {
            foreach (Transform child in propertyItemsParent) Destroy(child.gameObject);
        }

        private void Start()
        {
            _world = World.instance;
            _scene = _world.scene;
            clearButton.onClick.AddListener(Clear);
            saveButton.onClick.AddListener(Save);

            dirtyProperties = true;
        }

        private void Update()
        {
            if (dirtyProperties)
            {
                _tokenPropertiesItems.Clear();
                foreach (Transform child in propertyItemsParent) Destroy(child.gameObject);

                foreach (var key in _world.tokenPropertiesInfo.Keys)
                {
                    if (!_world.tokenPropertiesInfo[key].control) continue;
                    
                    var item = Instantiate(propertyItemPrefab, propertyItemsParent).GetComponent<TokenPropertyItem>();
                    _tokenPropertiesItems.Add(item);
                    item.label = World.Humanize(key);
                    item.key = key;
                    item.type = _world.tokenPropertiesInfo[key].type;
                    item.master = false;
                    item.extra = false;

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
            }

            dirtyProperties = true;
            
            _world.RegisterAction(new Action {
                name = World.ActionNames.ChangeToken,
                scene = _scene.id,
                tokens = _scene.selectedTokens.Select(x => x.Serializable()).ToList()
            });
        }
    }
}
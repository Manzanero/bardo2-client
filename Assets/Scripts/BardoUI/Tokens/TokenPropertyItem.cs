using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BardoUI.Tokens
{
    public class TokenPropertyItem : MonoBehaviour
    {
        public Text propertyName;

        public Button newTokenButton;
        
        public GameObject textGo;
        public InputField textField;

        public GameObject numGo;
        public InputField numField;

        public GameObject maxGo;
        public InputField valField;
        public InputField maxField;

        public GameObject boolGo;
        public Toggle trueField;
        public Toggle falseField;

        public GameObject colorGo;
        public InputField rField;
        public InputField gField;
        public InputField bField;
        
        public Toggle changesField;
        public Toggle controlField;

        public string id;
        public string label;
        public int type;

        private void Awake()
        {
            propertyName.text = "";
            textGo.SetActive(false);
            numGo.SetActive(false);
            maxGo.SetActive(false);
            boolGo.SetActive(false);
            colorGo.SetActive(false);
        }

        private void OnValueChanged(bool x) => changesField.isOn = true;
        private void OnValueChanged(string x) => changesField.isOn = true;
        private void OnControlChanged(bool x)
        {
            foreach (var property in World.instance.tokenProperties.Where(property => property.id == id))
                property.control = Control;
        }


        private void Start()
        {
            if (id == null) throw new ArgumentOutOfRangeException();
            if (label == null) throw new ArgumentOutOfRangeException();
            propertyName.text = label;
            switch (type)
            {
                case TokenPropertiesTypes.TEXT: 
                    textGo.SetActive(true); 
                    textField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case TokenPropertiesTypes.NUMERIC: 
                    numGo.SetActive(true); 
                    numField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case TokenPropertiesTypes.BAR: 
                    maxGo.SetActive(true); 
                    valField.onValueChanged.AddListener(OnValueChanged);
                    maxField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case TokenPropertiesTypes.BOOLEAN: 
                    boolGo.SetActive(true); 
                    trueField.onValueChanged.AddListener(OnValueChanged);
                    falseField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case TokenPropertiesTypes.COLOR: 
                    colorGo.SetActive(true); 
                    rField.onValueChanged.AddListener(OnValueChanged);
                    gField.onValueChanged.AddListener(OnValueChanged);
                    bField.onValueChanged.AddListener(OnValueChanged);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            controlField.onValueChanged.AddListener(OnControlChanged);
            if (Control) controlField.gameObject.SetActive(true);
            
            if (!textGo.activeSelf) Destroy(textGo);
            if (!numGo.activeSelf) Destroy(numGo);
            if (!maxGo.activeSelf) Destroy(maxGo);
            if (!boolGo.activeSelf) Destroy(boolGo);
            if (!colorGo.activeSelf) Destroy(colorGo);
        }

        public bool Changes
        {
            get => changesField.isOn;
            set => changesField.isOn = value;
        }

        public bool Control
        {
            get => controlField.isOn;
            set => controlField.isOn = value;
        }
        
        public string ToText() => type == TokenPropertiesTypes.TEXT ? Values[0] : 
            throw new Exception($"Property '{label}' is not type 'text'. " +
                                $"It is '{TokenPropertiesTypes.ToString(type)}'");
        public float ToNumeric() => type == TokenPropertiesTypes.NUMERIC ? 
            Values[0] == "" ? 0 : float.Parse(Values[0]) : 
            throw new Exception($"Property '{label}' is not type 'numeric'. " +
                                $"It is '{TokenPropertiesTypes.ToString(type)}'");
        public float[] ToBar() => type == TokenPropertiesTypes.BAR ? 
            new []{Values[0] == "" ? 0 : float.Parse(Values[0]), Values[1] == "" ? 0 : float.Parse(Values[1])} : 
            throw new Exception($"Property '{label}' is not type 'bar'. " +
                                $"It is '{TokenPropertiesTypes.ToString(type)}'");
        public bool ToBoolean() => type == TokenPropertiesTypes.BOOLEAN ? Values[0] == "true" : 
            throw new Exception($"Property '{label}' is not type 'boolean'. " +
                                $"It is '{TokenPropertiesTypes.ToString(type)}'");
        public Color ToColor() => type == TokenPropertiesTypes.COLOR ? 
            new Color(Values[0] == "" ? 100 : Mathf.Clamp(float.Parse(Values[0]), 0, 100), 
                Values[1] == "" ? 100 : Mathf.Clamp(float.Parse(Values[1]), 0, 100), 
                Values[0] == "" ? 100 : Mathf.Clamp(float.Parse(Values[2]), 0, 100)) / 100 : 
            throw new Exception($"Property '{label}' is not type 'color'. " +
                                $"It is '{TokenPropertiesTypes.ToString(type)}'");

        public string[] Values
        {
            get
            {
                return type switch
                {
                    TokenPropertiesTypes.TEXT => new[] {textField.text},
                    TokenPropertiesTypes.NUMERIC => new[] {numField.text},
                    TokenPropertiesTypes.BAR => new[] {valField.text, maxField.text},
                    TokenPropertiesTypes.BOOLEAN => new[] {trueField.isOn ? "true" : falseField.isOn ? "false" : "null"},
                    TokenPropertiesTypes.COLOR => new[] {rField.text, gField.text, bField.text},
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            set
            {
                switch (type)
                {
                    case TokenPropertiesTypes.TEXT:
                        textField.text = value[0];
                        break;
                    case TokenPropertiesTypes.NUMERIC:
                        numField.text = value[0];
                        break;
                    case TokenPropertiesTypes.BAR:
                        valField.text = value[0];
                        maxField.text = value[1];
                        break;
                    case TokenPropertiesTypes.BOOLEAN:
                        trueField.isOn = value[0] switch
                        {
                            "true" => trueField.isOn = true,
                            "false" => falseField.isOn = true,
                            _ => trueField.isOn = falseField.isOn = false
                        };
                        break;
                    case TokenPropertiesTypes.COLOR:
                        rField.text = value[0];
                        gField.text = value[1];
                        bField.text = value[2];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}

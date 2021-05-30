using System;
using UnityEngine;
using UnityEngine.UI;

namespace BardoUI.Tokens
{
    public class TokenPropertyItem : MonoBehaviour
    {
        public Text propertyName;

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

        public GameObject percentGo;
        public InputField percentField;

        public Toggle removeField;
        public Toggle changesField;
        public Toggle controlField;

        public string key;
        public string label;
        public int type;
        public bool master;
        public bool extra;

        private void Awake()
        {
            propertyName.text = "";
            textGo.SetActive(false);
            numGo.SetActive(false);
            maxGo.SetActive(false);
            boolGo.SetActive(false);
            colorGo.SetActive(false);
            
            removeField.gameObject.SetActive(false);
            controlField.gameObject.SetActive(false);
        }

        private void OnValueChanged(bool x) => changesField.isOn = true;
        private void OnValueChanged(string x) => changesField.isOn = true;

        private void OnControlChanged(bool x) =>
            PropertyHolder.GetInfo(World.instance.tokenPropertiesInfo, key).control = Control;


        private void Start()
        {
            if (key == null) throw new ArgumentOutOfRangeException();
            if (label == null) throw new ArgumentOutOfRangeException();
            propertyName.text = label;
            switch (type)
            {
                case Property.Types.Text:
                    textGo.SetActive(true);
                    textField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case Property.Types.Numeric:
                    numGo.SetActive(true);
                    numField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case Property.Types.Bar:
                    maxGo.SetActive(true);
                    valField.onValueChanged.AddListener(OnValueChanged);
                    maxField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case Property.Types.Boolean:
                    boolGo.SetActive(true);
                    trueField.onValueChanged.AddListener(OnValueChanged);
                    falseField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case Property.Types.Color:
                    colorGo.SetActive(true);
                    rField.onValueChanged.AddListener(OnValueChanged);
                    gField.onValueChanged.AddListener(OnValueChanged);
                    bField.onValueChanged.AddListener(OnValueChanged);
                    break;
                case Property.Types.Percentage:
                    percentGo.SetActive(true);
                    percentField.onValueChanged.AddListener(OnValueChanged);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            removeField.gameObject.SetActive(extra);
            controlField.gameObject.SetActive(master);
            controlField.onValueChanged.AddListener(OnControlChanged);

            if (!textGo.activeSelf) Destroy(textGo);
            if (!numGo.activeSelf) Destroy(numGo);
            if (!maxGo.activeSelf) Destroy(maxGo);
            if (!boolGo.activeSelf) Destroy(boolGo);
            if (!colorGo.activeSelf) Destroy(colorGo);
            if (!percentGo.activeSelf) Destroy(percentGo);
        }

        public bool Changes
        {
            get => changesField.isOn;
            set => changesField.isOn = value;
        }

        public bool Remove
        {
            get => removeField.isOn;
            set => removeField.isOn = value;
        }

        public bool Control
        {
            get => controlField.isOn;
            set => controlField.isOn = value;
        }

        public string ToText() => type == Property.Types.Text
            ? Values[0]
            : throw new Exception($"Property '{label}' is not type 'text'. " +
                                  $"It is '{Property.Types.ToString(type)}'");

        public float ToNumeric() => type == Property.Types.Numeric
            ? Values[0] == "" ? 0 : float.Parse(Values[0])
            : throw new Exception($"Property '{label}' is not type 'numeric'. " +
                                  $"It is '{Property.Types.ToString(type)}'");

        public float[] ToBar() => type == Property.Types.Bar
            ? new[] {Values[0] == "" ? 0 : float.Parse(Values[0]), Values[1] == "" ? 0 : float.Parse(Values[1])}
            : throw new Exception($"Property '{label}' is not type 'bar'. " +
                                  $"It is '{Property.Types.ToString(type)}'");

        public bool ToBoolean() => type == Property.Types.Boolean
            ? Values[0] == "true"
            : throw new Exception($"Property '{label}' is not type 'boolean'. " +
                                  $"It is '{Property.Types.ToString(type)}'");

        public Color ToColor() => type == Property.Types.Color
            ? new Color(Values[0] == "" ? 100 : Mathf.Clamp(float.Parse(Values[0]), 0, 100),
                Values[1] == "" ? 100 : Mathf.Clamp(float.Parse(Values[1]), 0, 100),
                Values[0] == "" ? 100 : Mathf.Clamp(float.Parse(Values[2]), 0, 100)) / 100
            : throw new Exception($"Property '{label}' is not type 'color'. " +
                                  $"It is '{Property.Types.ToString(type)}'");
        
        public float ToPercentage() => type == Property.Types.Percentage
            ? Values[0] == "" ? 0 : float.Parse(Values[0]) / 100
            : throw new Exception($"Property '{label}' is not type 'percentage'. " +
                                  $"It is '{Property.Types.ToString(type)}'");
        
        public string[] Values
        {
            get
            {
                return type switch
                {
                    Property.Types.Text => new[] {textField.text},
                    Property.Types.Numeric => new[] {numField.text},
                    Property.Types.Bar => new[] {valField.text, maxField.text},
                    Property.Types.Boolean => new[] {trueField.isOn ? "true" : falseField.isOn ? "false" : "null"},
                    Property.Types.Color => new[] {rField.text, gField.text, bField.text},
                    Property.Types.Percentage => new[] {percentField.text},
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            set
            {
                switch (type)
                {
                    case Property.Types.Text:
                        textField.text = value[0];
                        break;
                    case Property.Types.Numeric:
                        numField.text = value[0];
                        break;
                    case Property.Types.Bar:
                        valField.text = value[0];
                        maxField.text = value[1];
                        break;
                    case Property.Types.Boolean:
                        trueField.isOn = value[0] switch
                        {
                            "true" => trueField.isOn = true,
                            "false" => falseField.isOn = true,
                            _ => trueField.isOn = falseField.isOn = false
                        };
                        break;
                    case Property.Types.Color:
                        rField.text = value[0];
                        gField.text = value[1];
                        bField.text = value[2];
                        break;
                    case Property.Types.Percentage:
                        percentField.text = value[0];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

namespace BardoUI.Tokens
{
    public class AddPropertyWindow : MonoBehaviour
    {
        public TokensWindow parentWindow;
        public InputField newPropertyInput;
        public Toggle typeTextToggle;
        public Toggle typeNumericToggle;
        public Toggle typeBarToggle;
        public Toggle typeBooleanToggle;
        public Toggle typeColorToggle;
        public Toggle typePercentageToggle;
        public Button createButton;

        private void Start()
        {
            createButton.onClick.AddListener(Create);
        }

        private void Create()
        {
            var text = newPropertyInput.text;
            if (text == "") return;
                
            var tokenProperties = World.instance.tokenPropertiesInfo;
            if (PropertyHolder.HasInfo(tokenProperties, text)) return;

            var type = 0;
            if (typeTextToggle.isOn) type = 0;
            else if (typeNumericToggle.isOn) type = 1;
            else if (typeBarToggle.isOn) type = 2;
            else if (typeBooleanToggle.isOn) type = 3;
            else if (typeColorToggle.isOn) type = 4;
            else if (typePercentageToggle.isOn) type = 5;

            World.instance.tokenPropertiesInfo.Add(text,new Property.Info {type = type, extra = true});
            newPropertyInput.text = "";
            parentWindow.ScrollToLast();
            parentWindow.dirtyProperties = true;
            gameObject.SetActive(false);
        }
    }
}

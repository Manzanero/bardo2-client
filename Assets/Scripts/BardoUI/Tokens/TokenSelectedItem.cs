using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BardoUI.Tokens
{
    public class TokenSelectedItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Text tokenName;
        public Text tokenId;
        public Button deselectButton;

        public Token token;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ReferenceEquals(token, null)) return;
            token.focused = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ReferenceEquals(token, null)) return;
            token.focused = false;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(token, null)) return;
            token.focused = false;
        }
    }
}

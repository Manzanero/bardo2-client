using BardoUI.Permissions;
using BardoUI.TokenEdit;
using BardoUI.Tokens;
using UnityEngine;
using UnityEngine.UI;

namespace BardoUI
{
    public class NavigationPanel : MonoBehaviour
    {
    
        public TokensWindow tokensWindow;
        public Button tokensButton;
        public Button tokensButtonPressed;
    
        public PermissionsWindow permissionsWindow;
        public Button permissionsButton;
        public Button permissionsButtonPressed;

        public TokenEditWindow tokenEditWindow;
        
        public static NavigationPanel instance;

        private void Awake()
        {
            instance = this;
            tokensWindow.gameObject.SetActive(false);
            tokensButton.onClick.AddListener(delegate
            {
                tokensWindow.transform.position = new Vector3(204, Screen.height - 28, 0);
                tokensWindow.gameObject.SetActive(true);
                tokensButton.gameObject.SetActive(false);
                tokensButtonPressed.gameObject.SetActive(true);
            });
            tokensButtonPressed.onClick.AddListener(delegate
            {
                tokensWindow.gameObject.SetActive(false);
                tokensButton.gameObject.SetActive(true);
                tokensButtonPressed.gameObject.SetActive(false);
            });
        
            permissionsWindow.gameObject.SetActive(false);
            permissionsButton.onClick.AddListener(delegate
            {
                permissionsWindow.transform.position = new Vector3(204, Screen.height - 28, 0);
                permissionsWindow.gameObject.SetActive(true);
                permissionsButton.gameObject.SetActive(false);
                permissionsButtonPressed.gameObject.SetActive(true);
            });
            permissionsButtonPressed.onClick.AddListener(delegate
            {
                permissionsWindow.gameObject.SetActive(false);
                permissionsButton.gameObject.SetActive(true);
                permissionsButtonPressed.gameObject.SetActive(false);
            });
            
            // Token Edit Window
            tokenEditWindow.gameObject.SetActive(false);
        }

        public void ShowTokenEditWindow()
        {
            tokenEditWindow.gameObject.SetActive(true);
            tokenEditWindow.dirtyProperties = true;
            tokenEditWindow.transform.position = new Vector3(204, Screen.height - 28, 0);
        }
    }
}

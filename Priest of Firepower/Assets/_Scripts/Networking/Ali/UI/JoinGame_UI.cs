using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ServerAli
{
    public class JoinGame_UI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputFieldServerIP;
        [SerializeField] private TMP_InputField _inputFieldUserName;
        [SerializeField] private Button _joinGameButton;

        public Client_TCP clientTCP;
        public MenuManager menuManager;

        private void OnEnable()
        {
            if (clientTCP != null)
            {
                _inputFieldUserName.onValueChanged.AddListener(newName => { clientTCP._socketName = newName; });
                _inputFieldServerIP.onValueChanged.AddListener(newServerIP => { clientTCP.serverIP = newServerIP; });
                _joinGameButton.onClick.AddListener(OnJoinGamePressed);
            }
        }

        private void OnDisable()
        {
            if (clientTCP != null)
            {
                _inputFieldUserName.onValueChanged.RemoveListener(newName => { clientTCP._socketName = newName; });
                _inputFieldServerIP.onValueChanged.RemoveListener(newServerIP => { clientTCP.serverIP = newServerIP; });
                _joinGameButton.onClick.RemoveListener(OnJoinGamePressed);
            }
        }

        void OnJoinGamePressed()
        {
            clientTCP.InitServerConnection();
            DontDestroyOnLoad(clientTCP);
            SceneManager.LoadScene("Ali_lobby", LoadSceneMode.Single);
        }
    }
}

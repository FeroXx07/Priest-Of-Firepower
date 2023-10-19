using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ServerAli
{
    public class CreateGame_UI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _textServerIP;
        [SerializeField] private TMP_InputField _inputFieldServerName;
        [SerializeField] private Button _createGameButton;

        public Server_TCP serverTCP;
        public MenuManager menuManager;

        private void Awake()
        {
            SetIPText("server_ip");
        }

        private void OnEnable()
        {
            if (serverTCP != null)
            {
                serverTCP.onRemoteIPAssignated += SetIPText;
                _inputFieldServerName.onValueChanged.AddListener(newName => serverTCP._socketName = newName);
                _createGameButton.onClick.AddListener(OnCreateGamePressed);
            }
        }

        void OnDisable()
        {
            if (serverTCP != null)
            {
                serverTCP.onRemoteIPAssignated -= SetIPText;
                _inputFieldServerName.onValueChanged.RemoveListener(newName => serverTCP._socketName = newName);
                _createGameButton.onClick.RemoveListener(OnCreateGamePressed);
            }
        }

        void SetIPText(string newIP)
        {
            _textServerIP.text = $"Your server IP is <color=green>{newIP}</color> with envy";
        }

        void OnCreateGamePressed()
        {
            serverTCP.TriggerCreateGame();
            DontDestroyOnLoad(serverTCP);
            SceneManager.LoadScene("Ali_lobby", LoadSceneMode.Single);
        }
    }
}
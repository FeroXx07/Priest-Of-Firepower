using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.Networking
{
    public class NetworkManagerHUD : MonoBehaviour
    {
        [SerializeField] Button hostBtn;
        [SerializeField] TMP_InputField ipInputField;
        private void Start()
        {
            hostBtn.onClick.AddListener(HostGame);
            NetworkManager.Instance.OnClientConnected += Lobby;
            ipInputField.onEndEdit.AddListener(ConnectToServer);
        }
        void HostGame()
        {
            NetworkManager.Instance.StartHost();
        }

        //if client connected send to lobby
        void Lobby()
        {
            SceneManager.LoadScene(1);
            Debug.Log("Going to lobby ...");
        }

        private void ConnectToServer(string ip)
        {
            IPAddress adddress = IPAddress.Parse(ip);
            if (adddress != null)
            {
                NetworkManager.Instance.StartClient();
                NetworkManager.Instance.ConnectClient(adddress);
            }
            else
            {
                Debug.LogError(ip + " address is not valid ...");
            }
        }
    }
}

using System;
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

        private void OnEnable()
        {
            hostBtn.onClick.AddListener(HostGame);
            NetworkManager.Instance.OnClientConnected += Lobby;
            ipInputField.onEndEdit.AddListener(ConnectToServer);
        }

        private void OnDisable()
        {
            hostBtn.onClick.RemoveListener(HostGame);
            NetworkManager.Instance.OnClientConnected -= Lobby;
            ipInputField.onEndEdit.RemoveListener(ConnectToServer);
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
            NetworkManager manager = NetworkManager.Instance;

            if (IPAddress.TryParse(ip, out manager.serverAdress))
            {
                if (manager.isServerOnSameMachine)
                {
                    manager.serverAdress = IPAddress.Parse("127.0.0.1");
                }
                manager.StartClient();
            }
            else
            {
                Debug.LogError(ip + " address is not valid ...");
            }
        }
    }
}

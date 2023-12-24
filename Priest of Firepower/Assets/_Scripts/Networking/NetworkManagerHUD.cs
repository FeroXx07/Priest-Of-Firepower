using System;
using System.Net;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.Networking
{
    public class NetworkManagerHUD : MonoBehaviour
    {
        [SerializeField] private string nextSceneToLoad = "Game_Networking_Test";
        [SerializeField] private Button hostBtn;
        [SerializeField] private Button returnBtn;

        [Header("Join Game")]
        [SerializeField] private TMP_InputField ipInputField;
        [SerializeField] private TMP_Text ipTextfield;
        [Header("Username")]
        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_Text priestName;


        private bool isNameSet = false;
        private void OnEnable()
        {
            isNameSet = false;

            NetworkManager.Instance.OnClientConnected += Lobby;

            hostBtn.onClick.AddListener(HostGame);
            returnBtn.onClick.AddListener(GameManager.Instance.ReturnToMainMenu);

            ipInputField.onEndEdit.AddListener(ConnectToServer);
            userNameInputField.onEndEdit.AddListener(SetPriestName);
        }

        private void OnDisable()
        {
            NetworkManager.Instance.OnClientConnected -= Lobby;

            hostBtn.onClick.RemoveListener(HostGame);
            returnBtn.onClick.RemoveListener(GameManager.Instance.ReturnToMainMenu);
           
            ipInputField.onEndEdit.RemoveListener(ConnectToServer);
            userNameInputField.onEndEdit.RemoveListener(SetPriestName);
        }
        
        void HostGame()
        {
            if (!isNameSet)
            {
                priestName.color = Color.red;
                priestName.text = "Name can't be empty";
                return;
            }
            NetworkManager.Instance.StartHost();
        }

        //if client connected send to lobby
        void Lobby()
        {
            SceneManager.LoadScene(nextSceneToLoad);
            Debug.Log("NetworkManagerHUD: Going to lobby ...");
        }

        private void ConnectToServer(string ip)
        {

            if (!isNameSet)
            {
                priestName.color = Color.red;
                priestName.text = "Name can't be empty";
                return;
            }
            
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
                ipInputField.text = "Invalid IP address";
                ipTextfield.color = Color.red;
                Debug.LogError(ip + " address is not valid ...");
            }
        }

        void SetPriestName(string name)
        {
            if (ValidateName(name))
            {
                NetworkManager.Instance.PlayerName = name;
                isNameSet = true;
                priestName.text = name;
                priestName.color = Color.white;
            }
        }

        bool ValidateName(string nameToValidate)
        {
            bool validation = true;

            if (nameToValidate == "")
            {
                validation = false;
                isNameSet = false;
                priestName.color = Color.red;
                priestName.text = "Name can't be empty";
            }

            if (nameToValidate.Length > 10)
            {
                validation = false;
                isNameSet = false;
                priestName.color = Color.red;
                priestName.text = "Name too long";
            }
        
            return validation;
        }
    }
}

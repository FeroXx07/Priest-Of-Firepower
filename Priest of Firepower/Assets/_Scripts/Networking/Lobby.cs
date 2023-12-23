using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;

namespace _Scripts.Networking
{
    public class Lobby : NetworkBehaviour
    {
        enum LobbyAction
        {
            NONE,
            UPDATE_LIST,
            REMOVE_PLAYER,
            START_GAME,
            LEAVE_GAME,
            MSSAGE
        }

        [Header("Host elements")]
        [SerializeField] private Button startGameBtn;
        [SerializeField] private string sceneToLoadOnGameStart;
        [Header("Lobby info")]
        [SerializeField] private GameObject clientUiPrefab;
        [SerializeField] private Transform listHolder;
        [Header("Lobby info")]
        [SerializeField] private TMP_InputField inputMessage;
        [SerializeField] private GameObject mesagePrefab;
        [SerializeField] private Transform msgContainer;
        [SerializeField] private List<GameObject> mesageObjectList;

        [SerializeField] private TMP_Text ipAddress;
        private List<GameObject> playerList = new List<GameObject>();
        public override void Awake()
        {
            // init network variable
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            NetworkVariableList.ForEach(var => var.SetTracker(BITTracker));
            
            NetworkManager.Instance.OnClientConnected += OnClientConnected;
            NetworkManager.Instance.OnClientDisconnected += OnClientDisconnected;

            inputMessage.onSubmit.AddListener(OnSendMSG);
        
            //enable the start button if host other wise don't
            if (NetworkManager.Instance.IsHost())
            {
                startGameBtn.gameObject.SetActive(true);
                startGameBtn.onClick.AddListener(StartGame);
            }
            else
            {
                startGameBtn.gameObject.SetActive(false);
            }

            ClearMessages();
        }
        private void Start()
        {
            
            //clear the list transform
            foreach (Transform t in listHolder)
            {
                Destroy(t.gameObject);
            }
    
            //If host update the client list cuz the event is triggered before entering the lobby
            OnClientConnected();

            //Set the Ip where is connected to
            ipAddress.text = NetworkManager.Instance.serverAdress.ToString();

        }

        public override void OnEnable()
        {
            base.OnEnable();
        }
        public override void OnDisable()
        {
            base.OnDisable();
            NetworkManager.Instance.OnClientConnected -= OnClientConnected;
            NetworkManager.Instance.OnClientDisconnected -= OnClientDisconnected;
            startGameBtn.onClick.RemoveListener(StartGame);
            inputMessage.onSubmit.RemoveListener(OnSendMSG);
        }

        private void ClearMessages()
        {
            foreach(Transform t in msgContainer)
            {
                Destroy(t.gameObject);
            }
        }
        
        void StartGame()
        {       
            if (!NetworkManager.Instance.IsHost()) return;
            
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            
            writer.Write((int)LobbyAction.START_GAME);
            SendInput(stream, true);
            OnStartGame();
        }

        void OnStartGame()
        {
            GameManager.Instance.StartGame(sceneToLoadOnGameStart);
        }

        public void OnClientConnected()
        {
            if (!NetworkManager.Instance.IsHost()) return;

            List<ClientData> clients = NetworkManager.Instance.GetServer().GetClients();
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            
            writer.Write((int)LobbyAction.UPDATE_LIST);
            writer.Write(clients.Count);
            foreach (ClientData client in clients)
            {
                writer.Write(client.userName);
            }

            SendInput(stream, true);
            //as host just update the new list when a client is connected
            UpdatePlayerList(NetworkManager.Instance.GetServer().GetClients());
        }

        public void OnClientDisconnected()
        {
            if (!NetworkManager.Instance.IsHost()) return;
            
            List<ClientData> clients = NetworkManager.Instance.GetServer().GetClients();
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            
            writer.Write((int)LobbyAction.UPDATE_LIST);
            writer.Write(clients.Count);
            foreach (ClientData client in clients)
            {
                writer.Write(client.userName);
            }
              
            SendInput(stream,true);
            //as host just update the new list when a client is connected
            UpdatePlayerList(NetworkManager.Instance.GetServer().GetClients());
        }

        void UpdatePlayerList(List<ClientData> newPlayerList)
        {
            //clear the list transform
            foreach (Transform t in listHolder)
            {
                Destroy(t.gameObject);
            }
            //clear all the items in the holder
            playerList.Clear();
            
            foreach (ClientData p in newPlayerList)
            {
                GameObject go = Instantiate(clientUiPrefab, listHolder);
                
                //set the player name
                go.GetComponentInChildren<TMP_Text>().text = p.userName;
                
                //enable or disable the kick button
                if (NetworkManager.Instance.IsHost())
                {
                    go.GetComponentInChildren<Button>().onClick.AddListener(()=>NetworkManager.Instance.RemoveClient(p));
                    go.GetComponentInChildren<Button>().gameObject.SetActive(!p.isHost);
                }
                else
                {
                    go.GetComponentInChildren<Button>().gameObject.SetActive(false);
                }
                playerList.Add(go);
            }
        }

        void OnSendMSG(string msg)
        {
            inputMessage.text = ""; // clear the inputfield
            string message = NetworkManager.Instance.PlayerName +": " + msg;
            
            //instantiate the message in the current machine
            GameObject msgObj = Instantiate(mesagePrefab, msgContainer);
            msgObj.GetComponent<TMP_Text>().text = message; 

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            
            //Send the action,text and the sender id. Server will replicate the msg 
            writer.Write((int)LobbyAction.MSSAGE);
            writer.Write(message);
            writer.Write(NetworkManager.Instance.getId);

            SendInput(stream, true);
        }
        #region write
        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream,
            ReplicationAction action)
        {
            base.WriteReplicationPacket(outputMemoryStream, action);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }

        #endregion

        #region  read

        public override void ReceiveInputFromClient(InputHeader header, BinaryReader reader)
        {
            switch ((LobbyAction)reader.ReadInt32())
            {
                case LobbyAction.MSSAGE:
                    {
                        ServerMsgRecieved(reader);
                    }
                    break;
                case LobbyAction.NONE:
                    Debug.Log("Lobby: Action None");
                    break;
            }
        }

        public override void ReceiveInputFromServer(InputHeader header, BinaryReader reader)
        {
            switch ((LobbyAction)reader.ReadInt32())
            {
                case LobbyAction.UPDATE_LIST:
                    ReadPlayerList(reader);
                    break;
                case LobbyAction.START_GAME:
                    OnStartGame();
                    break;
                case LobbyAction.MSSAGE:
                    ClientMsgRecieved(reader);
                    break;
                case LobbyAction.NONE:
                    Debug.Log("Lobby: Action None");
                    break;
            }
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long currentPosition = 0)
        {

            return true;
        }

        void ClientMsgRecieved(BinaryReader reader)
        {
            string msg = reader.ReadString();
            UInt64 id = reader.ReadUInt64();
            //if the mesage replicated by the server is mine then skip it
            if(id == NetworkManager.Instance.getId) return;
            Debug.Log("Msg received: " + msg);
            GameObject msgObj = Instantiate(mesagePrefab,msgContainer);
            msgObj.GetComponent<TMP_Text>().text = msg;
        }
        void ServerMsgRecieved(BinaryReader reader)
        {
            //message recieve from antoher client
            string msg = reader.ReadString();
            UInt64 clientId = reader.ReadUInt64();
            Debug.Log("Msg received: " + msg);
            GameObject msgObj = Instantiate(mesagePrefab, msgContainer);
            msgObj.GetComponent<TMP_Text>().text = msg;
           
            //re send the message to everyone
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
           
            writer.Write((int)LobbyAction.MSSAGE);
            writer.Write(msg);
            writer.Write(clientId);
           
            SendInput(stream,true);
        }
        //client have to read the new clients names and whatever is needed then updatePlayer list
        void ReadPlayerList(BinaryReader reader)
        {
            int Count = reader.ReadInt32();
            List<ClientData> newPlayerList = new List<ClientData>();
            for (int i = 0; i < Count; i++)
            {
                ClientData client = new ClientData();
                client.userName= reader.ReadString();
                newPlayerList.Add(client);
            }
            UpdatePlayerList(newPlayerList);
        }

        protected override void InitNetworkVariablesList()
        {

        }
        #endregion
    }
}
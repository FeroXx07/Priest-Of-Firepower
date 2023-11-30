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
            LEAVE_GAME
        }

        private LobbyAction _lobbyAction;
        [Header("Host elements")]
        [SerializeField] private Button startGameBtn;
        [SerializeField] private string sceneToLoadOnGameStart;
        [Header("Lobby info")]
        [SerializeField] private GameObject clientUiPrefab;
        [SerializeField] private Transform listHolder;
        
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
        
            //enable the start button if host otherwise not
            if (NetworkManager.Instance.IsHost())
            {
                startGameBtn.gameObject.SetActive(true);
                startGameBtn.onClick.AddListener(StartGame);
            }
            else
            {
                startGameBtn.gameObject.SetActive(false);
            }

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
        }

        protected override void InitNetworkVariablesList()
        {
            
        }
        
        void StartGame()
        {
       
            if (!NetworkManager.Instance.IsHost()) return;
            
            _lobbyAction = LobbyAction.START_GAME;
            SendReplicationData(ReplicationAction.UPDATE);
            OnStartGame();
        }

        void OnStartGame()
        {
            GameManager.Instance.StartGame(sceneToLoadOnGameStart);
        }
        public override void ListenToMessages(ulong senderId, string message, long timeStamp)
        {
            if (NetworkManager.Instance.IsClient())
            {
                
            }
        }

        public void OnClientConnected()
        {
            if (!NetworkManager.Instance.IsHost()) return;
            
            _lobbyAction = LobbyAction.UPDATE_LIST;
            SendReplicationData(ReplicationAction.UPDATE);
            //as host just update the new list when a client is connected
            UpdatePlayerList(NetworkManager.Instance.GetServer().GetClients());
        }

        public void OnClientDisconnected()
        {
            if (!NetworkManager.Instance.IsHost()) return;
            
            _lobbyAction = LobbyAction.UPDATE_LIST;
            SendReplicationData(ReplicationAction.UPDATE);
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
        #region write
        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream,
            ReplicationAction action)
        {
            base.WriteReplicationPacket(outputMemoryStream, action);
            
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            writer.BaseStream.Position = outputMemoryStream.Length;
            bool ret = true;
            
            // write lobby actions
            writer.Write((int)_lobbyAction);
            if (NetworkManager.Instance.IsHost())
            {
                switch (_lobbyAction)
                {
                    case LobbyAction.UPDATE_LIST:
                        WritePlayersList(writer);
                        break;
                    case LobbyAction.START_GAME:
                        //nothing, just send the start game action
                        break;
                    case LobbyAction.NONE:
                        if (showDebugInfo) Debug.Log("Lobby: Action None");
                        ret = false;
                        break;
                }
            }
            //Set the lobby action to none avoid any posible error writing corrupted data
            _lobbyAction = LobbyAction.NONE;
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }
        
        void WritePlayersList(BinaryWriter writer)
        {
            if (NetworkManager.Instance.IsHost())
            {
                List<ClientData> clients = NetworkManager.Instance.GetServer().GetClients();
                writer.Write(clients.Count);
                foreach (ClientData client in clients)
                {
                    writer.Write(client.userName);
                }
            }    
        }

        #endregion

        #region  read
        public override bool ReadReplicationPacket(BinaryReader reader, long currentPosition = 0)
        {
            base.ReadReplicationPacket(reader, currentPosition);
            if (NetworkManager.Instance.IsClient())
            {
                _lobbyAction = (LobbyAction)reader.ReadInt32();
                switch (_lobbyAction)
                {
                    case LobbyAction.UPDATE_LIST:
                        ReadPlayerList(reader,currentPosition);
                        break;
                    case LobbyAction.START_GAME:
                        OnStartGame();
                        break;
                    case LobbyAction.NONE:
                        Debug.Log("Lobby: Action None");
                        break;
                }
            }

            //Set the lobby action to none avoid any posible error reading corrupted data
            _lobbyAction = LobbyAction.NONE;
            return false;
        }
        //client have to read the new clients names and whatever is needed then updatePlayer list
        void ReadPlayerList(BinaryReader reader, long currentPosition = 0)
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
        #endregion
    }
}
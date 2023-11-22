using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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
        [SerializeField] private GameObject clientUiPrefab;
        [SerializeField] private Transform listHolder;
        [SerializeField] private Button startGameBtn;
        [SerializeField] private TMP_Text ipAddress;
        private List<GameObject> playerList = new List<GameObject>();

        public override void Awake()
        {
            base.Awake();
            NetworkManager.Instance.OnClientConnected += OnClientConnected;
        }

        private void Start()
        {
            
            //clear the list transform
            foreach (Transform t in listHolder)
            {
                Destroy(t.gameObject);
            }
    
            //If host update the client list cuz the event is triggered before entering the lobby
            if(NetworkManager.Instance.IsHost())
                OnClientConnected();

            //Set the Ip where is connected to
            ipAddress.text = NetworkManager.Instance.serverAdress.ToString();

        }

        private void OnDisable()
        {
            NetworkManager.Instance.OnClientConnected -= OnClientConnected;
        }

        protected override void InitNetworkVariablesList()
        {
          
        }

        protected override bool WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            // Serialize
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            writer.Write((int)action);
            writer.Write((int)_lobbyAction);
            if (NetworkManager.Instance.IsHost())
            {
                switch (_lobbyAction)
                {
                    case LobbyAction.UPDATE_LIST:
                        WritePlayersList(writer);
                        break;
                    case LobbyAction.NONE:
                        Debug.Log("Lobby: Action None");
                        break;
                }
            }
            //Set the lobby action to none avoid any posible error writing corrupted data
            _lobbyAction = LobbyAction.NONE;
            return false;
        }

        public void OnClientConnected()
        {
            _lobbyAction = LobbyAction.UPDATE_LIST;
            SendReplicationData(ReplicationAction.UPDATE);
        }
        
        public override bool ReadReplicationPacket(BinaryReader reader, long currentPosition = 0)
        {
            _lobbyAction = (LobbyAction)reader.ReadInt32();
            switch (_lobbyAction)
            {
                case LobbyAction.UPDATE_LIST:
                    ReadPlayerList(reader,currentPosition);
                    break;
                case LobbyAction.NONE:
                    Debug.Log("Lobby: Action None");
                    break;
            }
            //Set the lobby action to none avoid any posible error reading corrupted data
            _lobbyAction = LobbyAction.NONE;
            return false;
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

        void ReadPlayerList(BinaryReader reader, long currentPosition = 0)
        {
            int Count = reader.ReadInt32();
            List<string> newPlayerList = new List<string>();
            for (int i = 0; i < Count; i++)
            {
                string name = reader.ReadString();
                newPlayerList.Add(name);
            }

            foreach (GameObject p in playerList)
            {
                Destroy(p);
            }

            foreach (string p in newPlayerList)
            {
                GameObject go = Instantiate(clientUiPrefab, listHolder);
                //set the player name
                go.GetComponentInChildren<TMP_Text>().text = p;
                //enable or disable the kick button
                if (NetworkManager.Instance.IsHost())
                {
                    go.GetComponentInChildren<Button>().enabled = true;
                }
                else
                {
                    go.GetComponentInChildren<Button>().enabled = false;
                }
            }
        }
    }
}
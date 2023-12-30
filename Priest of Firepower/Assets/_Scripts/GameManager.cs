using System.Collections.Generic;
using System.IO;
using _Scripts.Enemies;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Player;
using _Scripts.UI.Points;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public enum GameState
    {
        IN_GAME,
        GAME_OVER,
        LOBBY,
        RETURN_TO_LOBBY
    }
    public class GameManager : NetworkBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // If the instance doesn't exist, create it
                    instance = new GameObject("GameManager").AddComponent<GameManager>();
                }
                return instance;
            }
        }
        [SerializeField] GameObject uiCanvas;
        [SerializeField] EnemyManager enemySpawnManager;
        [SerializeField] RoundSystem roundSystem;
        private List<GameObject> playersList = new List<GameObject>();
        private GameState state = GameState.LOBBY;
        public override void Awake()
        {
            base.Awake();
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            DontDestroyOnLoad(this);
            instance = this;
        }

        public override void Update()
        {
            base.Update();
            
            if (roundSystem == null || enemySpawnManager == null)
                return;
            
            roundSystem.RoundFinished(enemySpawnManager);
            
            
        }
        
        public void StartGame(string sceneToLoad)
        {
            SceneManager.LoadScene(sceneToLoad);
            SceneManager.sceneLoaded += SpawnPlayers;
            SceneManager.sceneLoaded += InitGame;
        }
        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void ReturnToLobby()
        {
            SceneManager.LoadScene("Lobby");
            foreach (GameObject player in playersList)
            {
                RemovePlayer(player.name);
            }
        }

        void SpawnPlayers(Scene arg0, LoadSceneMode loadSceneMode)
        {
            if (NetworkManager.Instance.IsHost())
            {
               playersList = NetworkManager.Instance.SpawnPlayers();
            }
        }

        public void RemovePlayer(string playerName)
        {
            if(!isHost) return;
            GameObject player = playersList.Find(player => player.name == playerName);

            if (player != null)
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(player.GetComponent<Player.Player>().GetName());
                writer.Write(player.GetComponent<NetworkObject>().GetNetworkId());
                ReplicationHeader header = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY,
                    stream.ToArray().Length);
    
                NetworkObject nObj = player.GetComponent<NetworkObject>();
                NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(nObj, header, stream);

                playersList.Remove(player.gameObject);
            }
        }

        public void InitGame(Scene arg0, LoadSceneMode loadSceneMode)
        {

            state = GameState.IN_GAME;
            if (uiCanvas == null)
            {
                uiCanvas = FindObjectOfType<UIPoints>(true).gameObject;
            }
            
            uiCanvas.SetActive(true);

            if (roundSystem == null)
            {
                roundSystem = FindObjectOfType<RoundSystem>(true);
            }
            
            if (enemySpawnManager == null)
            {
                enemySpawnManager = FindObjectOfType<EnemyManager>(true);
            }
            
            roundSystem.OnRoundBegin += enemySpawnManager.SpawnEnemies;
            roundSystem.StartRound();
        }

        public void CheckGameOver()
        {
            bool isGameOver = true;
            foreach (GameObject player in playersList)
            {
                Player.Player p = player.GetComponent<Player.Player>();
                if (p.state != PlayerState.DEAD)
                {
                    isGameOver = false;
                    break;
                }
            }
            
            if(isGameOver)
                OnGameOver();
        }
        void OnGameOver()
        {
            state = GameState.GAME_OVER;
            SendReplicationData(ReplicationAction.UPDATE);
        }
        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);

            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            if (showDebugInfo) Debug.Log("Game manager sending data");
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            state = (GameState)reader.ReadInt32();

            switch (state)
            {
                case GameState.RETURN_TO_LOBBY:
                    ReturnToLobby();
                    break;
                case GameState.GAME_OVER:
                    //display gameover canvas
                    break;
            }

            return true;
        }

        protected override void InitNetworkVariablesList()
        {
        }
    }
}



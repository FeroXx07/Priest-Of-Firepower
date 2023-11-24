using _Scripts.Enemies;
using _Scripts.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public class GameManager : GenericSingleton<GameManager>
    {
        [SerializeField] EnemyManager enemySpawnManager;

        [SerializeField]RoundSystem roundSystem;
        public void StartGame(string sceneToLoad)
        {
            SceneManager.LoadScene(sceneToLoad);
            SceneManager.sceneLoaded += SpawnPlayers;
        }
        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        void SpawnPlayers(Scene arg0, LoadSceneMode loadSceneMode)
        {
            if (NetworkManager.Instance.IsHost())
            {
                NetworkManager.Instance.InstantiatePlayer();
            }
        }
        private void Start()
        {
            //roundSystem.OnRoundBegin += enemySpawnManager.SpawnEnemies;
            //roundSystem.StartRound();
        }


        private void Update()
        {
           // roundSystem.RoundFinished(enemySpawnManager);
        }



    }
}



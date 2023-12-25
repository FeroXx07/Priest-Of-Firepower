using _Scripts.Enemies;
using _Scripts.Networking;
using _Scripts.UI.Points;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public class GameManager : GenericSingleton<GameManager>
    {
        [SerializeField] GameObject uiCanvas;
        [SerializeField] EnemyManager enemySpawnManager;
        [SerializeField] RoundSystem roundSystem;
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
                NetworkManager.Instance.SpawnPlayers();
            }
        }

        public void StartGame()
        {
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
        
        private void Update()
        {
            if (roundSystem == null || enemySpawnManager == null)
                return;
            
            roundSystem.RoundFinished(enemySpawnManager);
        }
    }
}



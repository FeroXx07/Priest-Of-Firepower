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

        private void Start()
        {
            roundSystem.OnRoundBegin += enemySpawnManager.SpawnEnemies;
            roundSystem.StartRound();
        }


        private void Update()
        {
            roundSystem.RoundFinished(enemySpawnManager);
        }

        public void StartGame(string sceneToLoad)
        {
            SceneManager.LoadScene(sceneToLoad);
            NetworkManager.Instance.InstantiatePlayer();
        }
    }
}



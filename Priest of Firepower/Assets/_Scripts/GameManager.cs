using _Scripts.Enemies;
using UnityEngine;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
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
    }
}



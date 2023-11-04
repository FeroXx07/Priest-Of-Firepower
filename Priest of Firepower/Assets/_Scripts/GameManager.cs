using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] EnemySpawnManager enemySpawnManager;
    int currentRound = 1;
    private void Start()
    {
        enemySpawnManager.SpawnEnemies(currentRound);
    }
}

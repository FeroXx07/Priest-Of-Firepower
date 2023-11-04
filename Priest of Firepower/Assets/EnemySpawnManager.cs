using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] Dictionary<string, List<GameObject>> enemies;
    List<Transform> spawnPoints = new List<Transform>();
    [SerializeField]
    EnemyPoolManager enemyPoolManager;

    [SerializeField] AnimationCurve enemyCountProgression = new AnimationCurve();

    [SerializeField]
    float spawnFrequency = 0.5f;

    public void SpawnEnemies(int round)
    {
        StartCoroutine(SpawnRate(round));
    }

    IEnumerator SpawnRate(int round)
    {

        int nEnemies = GetNumberOfEnemies(round);

        for (int i = 0; i < nEnemies; i++)
        {
            Transform p = GetRadomSpawnPoint();
            SpawnEnemy(p.position);
            yield return new WaitForSeconds(spawnFrequency);
        }

    }

    GameObject SpawnEnemy(Vector3 spawnPosition)
    {
        //get enemy 
        // TODO add probability
        List<EnemyPoolData> pools = enemyPoolManager.GetPoolData();

        int enemyType = Random.Range(0, pools.Count );
        GameObject enemyPrefab = pools[enemyType].prefab;

        Debug.Log("Count  " + pools.Count);
        Debug.Log("type " + enemyType);
        Debug.Log("Spawning " + enemyPrefab.name);

       
        GameObject enemy =  enemyPoolManager.GetFromPool(enemyPrefab.GetHashCode());

        enemy.GetComponent<Transform>().position = spawnPosition;

        return enemy;
    }

    int GetNumberOfEnemies(int round)
    {
        //increase number of enemies 
        //TODO improve function
       // return Mathf.RoundToInt(Mathf.Sqrt(Mathf.Exp(round)));
        return  Mathf.FloorToInt( enemyCountProgression.Evaluate(round));
    }

    Transform GetRadomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }
    public void AddSpawnpoint(Transform spawnPoint)
    {
        spawnPoints.Add(spawnPoint);
    }
}

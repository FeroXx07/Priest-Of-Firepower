using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    List<Transform> spawnPoints = new List<Transform>();
    public List<GameObject> enemiesPrefabs = new List<GameObject>();
    public int numToInit = 5;

    [SerializeField] AnimationCurve enemyCountProgression = new AnimationCurve();
    [SerializeField] float spawnFrequency = 0.5f;

    private void Awake()
    {
    }

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
        // TODO add probability
        int enemyType = Random.Range(0, enemiesPrefabs.Count - 1);
        GameObject enemyPrefab = enemiesPrefabs[enemyType];
        GameObject enemy = PoolManager.Instance.Pull(enemyPrefab, spawnPosition);

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

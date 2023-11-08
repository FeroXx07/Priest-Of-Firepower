using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

public class EnemyManager : GenericSingleton<EnemyManager>
{
    List<Transform> spawnPoints = new List<Transform>();
    public List<GameObject> enemiesPrefabs = new List<GameObject>();
    public int numToInit = 5;

    [SerializeField] AnimationCurve enemyCountProgression = new AnimationCurve();
    [SerializeField] float spawnFrequency = 0.5f;
    [SerializeField] List<Enemy> enemiesAlive = new List<Enemy>();

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
        GameObject polledObj = PoolManager.Instance.Pull(enemyPrefab, spawnPosition);

        if (polledObj.TryGetComponent(out Enemy enemy))
            AddEnemyToList(enemy);
        
        return polledObj;
    }

    void AddEnemyToList(Enemy enemy)
    {
        enemiesAlive.Add(enemy);
        enemy.onDeath.AddListener(RemoveEnemyFromList);
    }

    void RemoveEnemyFromList(Enemy enemy)
    {
        enemiesAlive.Remove(enemy);
        enemy.onDeath.RemoveListener(RemoveEnemyFromList);
    }

    public void KillAllEnemies()
    {
        GameObject bombObject = new GameObject("NUKE");
        NuclearBomb nuke = bombObject.AddComponent<NuclearBomb>();

        nuke.Damage = 100000;

        foreach (Enemy enemy in enemiesAlive.ToArray())
        {
            if (enemy.TryGetComponent(out HealthSystem healthSystem))
            {
                healthSystem.TakeDamage(nuke, Vector3.zero, gameObject);
            }
        }

        nuke.RaiseDamageDealthEvent(gameObject);
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

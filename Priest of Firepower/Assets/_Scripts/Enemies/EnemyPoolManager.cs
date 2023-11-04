using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class EnemyPoolData
{
    public GameObject prefab;
    public int poolSize;
}
public class EnemyPoolManager : MonoBehaviour
{
    [SerializeField]
    List<EnemyPoolData> enemyPoolData = new List<EnemyPoolData>();
    [SerializeField]
    private Dictionary<int, List<GameObject>> enemyPools = new Dictionary<int, List<GameObject>>();


    public float poolSizeIncreaseFactor = 1.1f; // 10% increase factor


    private void Awake()
    {
        Debug.Log("Generating Enemy Pools ...");
        GeneratePools();
    }

    // Create an enemy pool for a specific type with a given size.
    private void GeneratePools()
    {
       foreach(EnemyPoolData poolData in enemyPoolData)
       {
            int prefabHashCode = poolData.prefab.GetHashCode();

            if (!enemyPools.ContainsKey(prefabHashCode))
            {
                enemyPools[prefabHashCode] = new List<GameObject>();
            }

            for (int i = 0; i < poolData.poolSize; i++)
            {
                GameObject enemy = InstantiateEnemy(poolData.prefab);
                enemyPools[prefabHashCode].Add(enemy);
            }
       }
    }

    // Instantiate an enemy of a specific type.
    private GameObject InstantiateEnemy(GameObject enemyPrefab)
    {
        GameObject enemy = Instantiate(enemyPrefab,gameObject.transform);
        enemy.transform.position = transform.position;

        // Deactivate the enemy initially.
        enemy.SetActive(false);

        return enemy;
    }

    public GameObject GetFromPool(int prefabHashCode)
    {
        if (enemyPools.ContainsKey(prefabHashCode))
        {
            foreach (GameObject enemy in enemyPools[prefabHashCode])
            {
                if (!enemy.activeInHierarchy)
                {
                    enemy.SetActive(true);
                    return enemy;
                }
            }

            // If no inactive enemies are available, increase the pool size.
            int currentPoolSize = enemyPools[prefabHashCode].Count;
            int newPoolSize = Mathf.CeilToInt(currentPoolSize * poolSizeIncreaseFactor);

            foreach (EnemyPoolData pool in enemyPoolData)
            {
                if(pool.prefab.GetHashCode() == prefabHashCode)
                {
                    //update pool size
                    pool.poolSize = newPoolSize;
        
                    //add new enemies
                    for (int i = currentPoolSize; i < newPoolSize; i++)
                    {
                        GameObject enemy = InstantiateEnemy(Resources.Load<GameObject>(prefabHashCode.ToString()));
                        enemyPools[prefabHashCode].Add(enemy);
                    }
                }
            }

            return GetFromPool(prefabHashCode);
        }
        else
        {
            Debug.LogWarning("Enemy type not found: " + prefabHashCode);
            return null;
        }
    }

    public void ReturnToPool(GameObject go)
    {
        go.SetActive(false);    
    }

    public List<EnemyPoolData> GetPoolData() { return enemyPoolData; }
}


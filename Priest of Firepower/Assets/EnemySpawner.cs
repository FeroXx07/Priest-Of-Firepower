using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    EnemySpawnManager spawnerManager;
    private void OnEnable()
    {
        if(spawnerManager == null) 
            spawnerManager = GetComponentInParent<EnemySpawnManager>();
        if(spawnerManager != null) 
            spawnerManager.AddSpawnpoint(gameObject.transform);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    EnemyManager spawnerManager;
    private void OnEnable()
    {
        if(spawnerManager == null) 
            spawnerManager = GetComponentInParent<EnemyManager>();
        if(spawnerManager != null) 
            spawnerManager.AddSpawnpoint(gameObject.transform);
    }
}

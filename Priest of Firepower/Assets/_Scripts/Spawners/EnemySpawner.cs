using _Scripts.Enemies;
using UnityEngine;

namespace _Scripts.Spawners
{
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
}

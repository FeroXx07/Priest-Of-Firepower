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
        }

        public void Activate()
        {
            if(spawnerManager != null) 
                spawnerManager.AddSpawnpoint(gameObject.transform);
        }
        
        public void DeActivate()
        {
            if(spawnerManager != null) 
                spawnerManager.RemoveSpawnPoint(gameObject.transform);
        }
    }
}

using System;
using System.IO;
using _Scripts.Enemies;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.Networking.Utility;
using UnityEngine;

namespace _Scripts.Power_Ups
{
    public class NuclearBomb : NetworkBehaviour, IDamageDealer
    {
        int _damage = 10000;
        public int Damage { get => _damage; set => _damage = value; }

        public void ProcessHit(IDamageable damageable, Vector3 dir, GameObject hitOwnerGameObject, GameObject hitterGameObject,
            GameObject hittedGameObject)
        {
            RaiseDamageDealthEvent(gameObject);
        }

        public event Action<GameObject> OnDamageDealerDestroyed;
        public event Action<GameObject> OnDamageDealth;

        private void RaiseDamageDealthEvent(GameObject go)
        {
            OnDamageDealth?.Invoke(go);
            OnDamageDealerDestroyed?.Invoke(go);

            Destroy(this, 2.0f);
        }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        protected override void InitNetworkVariablesList()
        {
            
        }
        
        public void KillAllEnemies()
        {
            // Execute logic of enemy manager only in server
            if (!isHost) return;
            foreach (Enemy enemy in EnemyManager.Instance.enemiesAlive.ToArray())
            {
                if (enemy.TryGetComponent<NetworkObject>(out NetworkObject enemyNetworkObject))
                {
                    HitManager.Instance.RegisterHit(NetworkObject.GetNetworkId(),
                        NetworkObject.GetNetworkId(), 
                        enemyNetworkObject.GetNetworkId(),
                        true,
                        enemy.GetComponent<Collider2D>().isTrigger,
                        (Vector2)enemy.transform.position, 
                        Vector3.zero);
                }
            }
        }

        public override void OnClientNetworkSpawn(NetworkObject spawner, BinaryReader reader, long timeStamp, int lenght)
        {
            Debug.Log("NuclearBomb: Spawned on client");
        }
    }
}

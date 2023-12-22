using System;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.Weapon;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace _Scripts
{
    public class HealthSystem : NetworkBehaviour,IDamageable
    {
        [SerializeField] private int health;
        [SerializeField] private int maxHealth;
        [SerializeField] private LayerMask layer;
        public LayerMask Layers { get => layer; set => layer = value; }
        public int Health { get => health; set => health = value; }
        public int MaxHealth { get => maxHealth; set => maxHealth = value; }

        public event Action<GameObject, GameObject> OnDamageableDestroyed;
        public event Action<GameObject, GameObject> OnDamageTaken;

        protected override void InitNetworkVariablesList()
        {
        }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            
            health = maxHealth;
        }

        public void RaiseEventOnDamageableDestroyed(GameObject destroyer)
        {
            if (NetworkManager.Instance.IsClient()) return;
            
            MemoryStream objStream = new MemoryStream();
            NetworkObject nObj = GetComponent<NetworkObject>();
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, objStream.ToArray().Length);
            NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(nObj,replicationHeader,objStream);    
            OnDamageableDestroyed?.Invoke(gameObject, destroyer);
        }

        public override void CallBackDeSpawnObjectOther(NetworkObject objectDestroyed, BinaryReader reader,
            Int64 timeStamp, int lenght)
        {
            Debug.Log("Client despawning " + gameObject.name);
            Destroy(gameObject);
        }

        public void TakeDamage(IDamageDealer damageDealer, Vector3 dir, GameObject owner)
        {
            health -= damageDealer.Damage;
            OnDamageTaken?.Invoke(gameObject, owner);

            if (TryGetComponent<IPointsProvider>(out IPointsProvider pointsProvider ))
            {
                if (owner.TryGetComponent<PointSystem>(out PointSystem pointSystem))
                {
                    pointSystem.PointsOnHit(pointsProvider);
                }
            }

            if (health <= 0)
            {
                if (TryGetComponent<IPointsProvider>(out IPointsProvider pointsProviders))
                {
                    if (owner.TryGetComponent<PointSystem>(out PointSystem pointSystem))
                    {
                        pointSystem.PointsOnDeath(pointsProviders);
                    }
                }
            
                health = 0;
                RaiseEventOnDamageableDestroyed(owner);
            }
        }
        public override bool ReadReplicationPacket(BinaryReader reader, long currentPosition = 0)
        {   
            Debug.Log("HealthSystem behaviour update");
            return true;
        }
    }
}

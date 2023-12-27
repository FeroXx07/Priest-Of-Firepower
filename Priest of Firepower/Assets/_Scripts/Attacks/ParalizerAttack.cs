using System;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using UnityEngine;

namespace _Scripts.Attacks
{
    public class ParalizerAttack : Attack
    {
        private float _timer;
        public int tickRate = 1;
        public GameObject targetedPlayer;
        public float rotationSpeed = 1.0f;
        public float rotationAmount = 1.0f;
        
        public event Action<GameObject> OnDamageDealerDestroyed;
        public event Action<GameObject> OnDamageDealth;
        public int Damage { get => damage; set => damage = value; }
        
        public void ProcessHit(IDamageable damageable, Vector3 dir, GameObject hitOwnerGameObject, GameObject hitterGameObject,
            GameObject hittedGameObject)
        {
            OnDamageDealth?.Invoke(hittedGameObject);
            //Debug.Log($"OnTriggerAttack: Processed Hit. Owner: {hitOwnerGameObject.name}, Hitter: {hitterGameObject}, Hitted: {hittedGameObject}");
        }

        public void SetPlayer(GameObject player)
        {
            targetedPlayer = player;
        }
        private void Start()
        {
            _timer = tickRate;
        }

        public override void Update()
        {
            base.Update();
            var transform1 = transform;
            var rotation = transform1.rotation;
            Quaternion target = Quaternion.Euler(rotation.x, rotation.y, rotation.z + rotationAmount);
            transform.rotation = Quaternion.Slerp(transform.rotation, target,  Time.deltaTime * rotationSpeed);
            
            _timer -= Time.deltaTime;
            
            if (_timer < 0.0f && !isDeSpawned)
            {
                _timer = tickRate;
                DoDamage();
            }
        }

        private void DoDamage()
        {
            if (targetedPlayer == null || !NetworkManager.Instance.IsHost()) return;
            
            if (owner.TryGetComponent<NetworkObject>(out NetworkObject obj) &&
                targetedPlayer.TryGetComponent<NetworkObject>(out NetworkObject coll)&&
                TryGetComponent<Rigidbody2D>(out Rigidbody2D rb2d))
            {
                if (obj == null) { Debug.Log(owner.name + " has no network object"); return; }
                if (coll == null) { Debug.Log(targetedPlayer.name + " has no network object"); return; }
                if (rb2d == null) { Debug.Log(name + " has no rb2d"); return; }
                        
                HitManager.Instance.RegisterHit(obj.GetNetworkId(),
                    NetworkObject.GetNetworkId(), 
                    coll.GetNetworkId(),
                    GetComponent<Collider2D>().isTrigger,
                    targetedPlayer.GetComponent<Collider2D>().isTrigger,
                    (Vector2)transform.position, 
                    GetComponent<Rigidbody2D>().velocity.normalized);


            }
        }
        
        protected override void DisposeGameObject()
        {
            OnDamageDealerDestroyed?.Invoke(gameObject);
            base.DisposeGameObject();
        }

        public void DoDisposeGameObject()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, stream.ToArray().Length);
            NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(NetworkObject,replicationHeader, stream);
            DisposeGameObject();
        }
    }
}

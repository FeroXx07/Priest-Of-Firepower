using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using UnityEngine;

namespace _Scripts.Attacks
{
    public class OnTriggerAttack : Attack
    {
        [SerializeField] protected bool destroyOnContactWithLayer = true;
        [SerializeField] protected float destructionTime = 1.0f;
        private float _timer;
        public override void OnEnable()
        {
            base.OnEnable();
            
            _timer = destructionTime;
        }

        public override void Update()
        {
            base.Update();

            if (!NetworkManager.Instance.IsHost()) return;
            
            _timer -= Time.deltaTime;
            
            if (_timer < 0.0f)
            {
                Debug.Log("OnTriggerAttack: Timer destroy");
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);
                ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, stream.ToArray().Length);
                NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(NetworkObject,replicationHeader, stream);
                DisposeGameObject();
            }
        }
        protected virtual void CollisionHandeler(GameObject collision)
        {
            if (collision.TryGetComponent<IDamageable>(out IDamageable dmg) && NetworkManager.Instance.IsHost())
            {
                if (IsSelected(collision.layer))
                {

                    if (Owner.TryGetComponent<NetworkObject>(out NetworkObject obj) &&
                        collision.TryGetComponent<NetworkObject>(out NetworkObject coll)&&
                        TryGetComponent<Rigidbody2D>(out Rigidbody2D rb2d))
                    {
                        if (obj == null) { Debug.Log(Owner.name + " has no network object"); return; }
                        if (coll == null) { Debug.Log(collision.name + " has no network object"); return; }
                        if (rb2d == null) { Debug.Log(name + " has no rb2d"); return; }
                        
                        HitManager.Instance.RegisterHit(obj.GetNetworkId(),
                                                        NetworkObject.GetNetworkId(), 
                                                        coll.GetNetworkId(),
                                                        GetComponent<Collider2D>().isTrigger,
                                                        collision.GetComponent<Collider2D>().isTrigger,
                                                        (Vector2)transform.position, 
                                                        GetComponent<Rigidbody2D>().velocity.normalized);


                    }
                }
            }

            if (IsSelected(collision.layer) && destroyOnContactWithLayer && NetworkManager.Instance.IsHost())
            {
                Debug.Log("OnTriggerAttack: Collision destroy");
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);
                ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, stream.ToArray().Length);
                NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(NetworkObject,replicationHeader, stream);
                DisposeGameObject();
            }
        }
        
        public override void OnClientNetworkDespawn(NetworkObject destroyer, BinaryReader reader, long timeStamp, int lenght)
        {
            Debug.Log("OnTriggerAttack: Despawn by server");
            DisposeGameObject();
        }

        #region Collisions
        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            CollisionHandeler(collision.gameObject);
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            CollisionHandeler(collision.gameObject);
        }
        #endregion
    }
}

using System;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using UnityEngine;

namespace _Scripts.Attacks
{
    public class OnTriggerAttack : Attack, IDamageDealer
    {
        [Header("OnTriggerAttack Properties")]
        [SerializeField] protected bool destroyOnContactWithLayer = true;
        [SerializeField] protected float destructionTime = 1.0f;
        private float _timer;
        public event Action<GameObject> OnDamageDealerDestroyed;
        public event Action<GameObject> OnDamageDealth;
        public int Damage { get => damage; set => damage = value; }
        public void ProcessHit(IDamageable damageable, Vector3 dir, GameObject hitOwnerGameObject, GameObject hitterGameObject,
            GameObject hittedGameObject)
        {
            OnDamageDealth?.Invoke(hittedGameObject);
            //Debug.Log($"OnTriggerAttack: Processed Hit. Owner: {hitOwnerGameObject.name}, Hitter: {hitterGameObject}, Hitted: {hittedGameObject}");
            //Play audio
        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            
            _timer = destructionTime;
        }

        public override void Update()
        {
            base.Update();
            
            _timer -= Time.deltaTime;
            
            if (_timer < 0.0f && !NetworkObject.isDeSpawned)
            {
                if (NetworkManager.Instance.IsHost())
                {
                    //Debug.Log("OnTriggerAttack: Timer destroy");
                    DoDisposeGameObject();
                }
            }
        }
        protected virtual void CollisionHandeler(GameObject collision)
        {
            if (collision.TryGetComponent<IDamageable>(out IDamageable dmg) && NetworkManager.Instance.IsHost())
            {
                if (IsSelected(collision.layer))
                {

                    if (owner.TryGetComponent<NetworkObject>(out NetworkObject obj) &&
                        collision.TryGetComponent<NetworkObject>(out NetworkObject coll)&&
                        TryGetComponent<Rigidbody2D>(out Rigidbody2D rb2d))
                    {
                        if (obj == null) { Debug.Log(owner.name + " has no network object"); return; }
                        if (coll == null) { Debug.Log(collision.name + " has no network object"); return; }
                        if (rb2d == null) { Debug.Log(name + " has no rb2d"); return; }
                        
                        HitManager.Instance.RegisterHit(obj.GetNetworkId(),
                                                        NetworkObject.GetNetworkId(), 
                                                        coll.GetNetworkId(),
                                                        GetComponent<Collider2D>().isTrigger,
                                                        collision.GetComponent<Collider2D>().isTrigger,
                                                        (Vector2)transform.position, 
                                                        rb2d.velocity.normalized);


                    }
                }
            }

            if (IsSelected(collision.layer) && destroyOnContactWithLayer && NetworkManager.Instance.IsHost())
            {
                DoDisposeGameObject();
            }
        }
        
        public override void OnClientNetworkDespawn(NetworkObject destroyer, BinaryReader reader, long timeStamp, int length)
        {
            //Debug.Log("OnTriggerAttack: Despawn by server");
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

        protected override void DisposeGameObject()
        {
            OnDamageDealerDestroyed?.Invoke(gameObject);
            base.DisposeGameObject();
        }

        public void DoDisposeGameObject()
        {
            if (NetworkObject.isDeSpawned)
                return;
            
            NetworkObject.isDeSpawned = true;
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, stream.ToArray().Length);
            NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(NetworkObject,replicationHeader, stream);
            DisposeGameObject();
        }
    }
}

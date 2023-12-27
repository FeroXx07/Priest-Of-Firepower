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
        
        public event Action<GameObject> OnDamageDealerDestroyed;
        public event Action<GameObject> OnDamageDealth;
        public int Damage { get => damage; set => damage = value; }
        
        public void ProcessHit(IDamageable damageable, Vector3 dir, GameObject hitOwnerGameObject, GameObject hitterGameObject,
            GameObject hittedGameObject)
        {
            OnDamageDealth?.Invoke(hittedGameObject);
            //Debug.Log($"ParalizerAttack: Processed Hit. Owner: {hitOwnerGameObject.name}, Hitter: {hitterGameObject}, Hitted: {hittedGameObject}");
        }

        public void SetPlayer(GameObject player)
        {
            targetedPlayer = player;
            targetedPlayer.gameObject.GetComponent<Player.Player>().isParalized = true;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (targetedPlayer) targetedPlayer.gameObject.GetComponent<Player.Player>().isParalized = false;
        }

        private void Start()
        {
            _timer = tickRate;
        }

        public override void Update()
        {
            base.Update();
            _timer -= Time.deltaTime;
            targetedPlayer.gameObject.GetComponent<Player.Player>().isParalized = true;
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
        
        public override void OnClientNetworkDespawn(NetworkObject destroyer, BinaryReader reader, long timeStamp, int lenght)
        {
            //Debug.Log("OnTriggerAttack: Despawn by server");
            if (targetedPlayer) targetedPlayer.gameObject.GetComponent<Player.Player>().isParalized = false;
            DisposeGameObject();
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

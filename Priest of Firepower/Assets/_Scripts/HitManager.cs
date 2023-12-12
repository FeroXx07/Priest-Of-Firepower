using System;
using System.Collections.Concurrent;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using UnityEngine;

namespace _Scripts
{
    class Hit
    {
        public Hit(UInt64 owner, UInt64 hitter, UInt64 hitted, string typeHitter, Vector3 position,
            Vector3 direction)
        {
            this.owner = owner;
            this.hitter = hitter;
            this.hitted = hitted;
            this.typeHitter = typeHitter;
            hitPosition = position;
            hitDirection = direction;
        }

        public UInt64 owner;
        public UInt64 hitter;
        public UInt64 hitted;

        public string typeHitter;
        public string typeHitted;

        public Vector3 hitPosition;
        public Vector3 hitDirection;
        
        public static void SerializeHit(BinaryWriter writer, Hit hit)
        {
            writer.Write(hit.owner);
            writer.Write(hit.hitter);
            writer.Write(hit.hitted);
            writer.Write(hit.typeHitter);
            writer.Write(hit.typeHitted);
            writer.Write(hit.hitDirection.x);
            writer.Write(hit.hitDirection.y);
            writer.Write(hit.hitDirection.z);
        }

        public static Hit DeSerializeHit(BinaryReader reader)
        {
            return new Hit(reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),
                reader.ReadString(), Vector3.zero,new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));
        }
    }
    public class HitManager : NetworkBehaviour
    {
        [SerializeField] private bool isHost => NetworkManager.Instance.IsHost();
        [SerializeField] private bool isClient => NetworkManager.Instance.IsClient();
        
        private ConcurrentQueue<Hit> _hits = new ConcurrentQueue<Hit>();
        private object _hitObj;
        private Hit _lastHit = null;

        #region Singleton
        private static HitManager _instance;
        public static HitManager Instance
        {
            get
            {
                // if instance is null
                if (_instance == null)
                {
                    // find the generic instance
                    _instance = FindObjectOfType<HitManager>();

                    // if it's null again create a new object
                    // and attach the generic instance
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(HitManager).Name;
                        _instance = obj.AddComponent<HitManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion
        
        public override void Awake()
        {
            // create the instance
            if (_instance == null)
            {
                _instance = this as HitManager;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }
        protected override void InitNetworkVariablesList()
        {
        }

        public override void Update()
        {
            base.Update();

            // Process all hits
            lock (_hitObj)
            {
                while (_hits.Count > 0)
                {
                    if (_hits.TryDequeue(out Hit hit))
                    {
                        ProcessHit(hit);
                    }
                }
            }
        }

        public void RegisterHit(UInt64 owner, UInt64 hitter, UInt64 hitted, string typeHitter, Vector3 position, Vector3 direction)
        {
            if (!isHost) Debug.LogError("Clients cannot register hit, only server authority can");
            
            // Cache new Hit
            _lastHit = new Hit(owner, hitter, hitted, typeHitter, position, direction);
            
            // Send hit to client's Hit Managers
            SendInputToClients();
            
            // Process the hit in the server
            ProcessHit(_lastHit);
        }
        void ProcessHit(Hit hit)
        {
             GameObject hitterObj = NetworkManager.Instance.replicationManager.networkObjectMap[hit.hitter].gameObject;
             GameObject hittedObj = NetworkManager.Instance.replicationManager.networkObjectMap[hit.hitted].gameObject;
             GameObject owner = NetworkManager.Instance.replicationManager.networkObjectMap[hit.owner].gameObject;

             if (hitterObj == null || hittedObj == null || owner == null)
             {
                 Debug.LogError("Cannot process hit because one field is null");
                 return;
             }
             
             if (hitterObj.TryGetComponent<IDamageDealer>(out IDamageDealer damageDealer))
             {
                 if (hittedObj.TryGetComponent<IDamageable>(out IDamageable damageable))
                 {
                     damageable.TakeDamage(damageDealer, hit.hitDirection, owner);
                 }
             }
        }

        public override void SendInputToClients()
        {
            // Create a hit input, because inputs are faster and have more priority than states.
            if (_lastHit == null) return;
            
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            Hit.SerializeHit(writer, _lastHit);
            
            NetworkManager.Instance.AddInputStreamQueue(stream);
        }

        public override void ReceiveInputFromServer(BinaryReader reader)
        {
            Hit hit = Hit.DeSerializeHit(reader);
            ProcessHit(hit);
        }
        
        
        
    }
}

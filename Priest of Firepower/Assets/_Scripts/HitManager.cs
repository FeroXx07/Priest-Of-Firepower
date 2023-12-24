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
        public Hit(UInt64 owner, UInt64 hitter, UInt64 hitted, bool hitterIsTrigger, bool hittedIsTrigger, Vector2 position,
            Vector2 direction)
        {
            this.owner = owner; 
            this.hitter = hitter; 
            this.hitted = hitted; 
            this.hitterIsTrigger = hitterIsTrigger; 
            hitPosition = position; 
            hitDirection = direction; 
        }

        public UInt64 owner;// owner of the attack
        public UInt64 hitter;// attack itself
        public UInt64 hitted;// damageable recieving hit

        public bool hitterIsTrigger;// type of collision aka trigger or collsion
        public bool hittedIsTrigger;// type of collision 

        public Vector2 hitPosition;// the position where the hit happend
        public Vector2 hitDirection;// the direction of the incoming hit

        public static void SerializeHit(BinaryWriter writer, Hit hit)
        {
            writer.Write(hit.owner);
            writer.Write(hit.hitter);
            writer.Write(hit.hitted);
            writer.Write(hit.hitterIsTrigger);
            writer.Write(hit.hittedIsTrigger);
            writer.Write(hit.hitPosition.x);
            writer.Write(hit.hitPosition.y);
            writer.Write(hit.hitDirection.x);
            writer.Write(hit.hitDirection.y);
        }

        public static Hit DeSerializeHit(BinaryReader reader)
        {
            return new Hit(reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadBoolean(),reader.ReadBoolean(),new Vector2(reader.ReadSingle(),reader.ReadSingle()),new Vector2(reader.ReadSingle(),reader.ReadSingle()));
        }
    }
    public class HitManager : NetworkBehaviour
    {
        private ConcurrentQueue<Hit> _hits = new ConcurrentQueue<Hit>();
        private object _hitObj = new object();
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

        public void RegisterHit(UInt64 owner, UInt64 hitter, UInt64 hitted, bool hitterIsTrigger, bool hittedIsTrigger, Vector2 position, Vector2 direction)
        {
            if (!isHost) Debug.LogError("Clients cannot register hit, only server authority can");
            //
            Debug.Log("Registered Hit: " + owner + " hitter id: "+ hitter + " hitted id: " + hitted);

            // Cache new Hit
            _lastHit = new Hit(owner, hitter, hitted, hitterIsTrigger, hittedIsTrigger, position, direction);
            
            // Send hit to client's Hit Managers
            SendInputToClients();
            
            // Process the hit in the server
            ProcessHit(_lastHit);
        }
        void ProcessHit(Hit hit)
        {
            if (NetworkManager.Instance.replicationManager.networkObjectMap.TryGetValue(hit.hitter,
                    out NetworkObject hitterObj) && NetworkManager.Instance.replicationManager.networkObjectMap.TryGetValue(hit.hitted,
                    out NetworkObject hittedObj) && NetworkManager.Instance.replicationManager.networkObjectMap.TryGetValue(hit.owner,
                    out NetworkObject owner))
            {
                if (hitterObj == null || hittedObj == null || owner == null)
                {
                    Debug.LogError("Cannot process hit because one field is null");
                    return;
                }
             
                if (hitterObj.TryGetComponent<IDamageDealer>(out IDamageDealer damageDealer))
                {
                    if (hittedObj.TryGetComponent<IDamageable>(out IDamageable damageable))
                    {
                        damageDealer.ProcessHit(damageable, hit.hitDirection, owner.gameObject, hitterObj.gameObject, hittedObj.gameObject);
                        damageable.ProcessHit(damageDealer, hit.hitDirection, owner.gameObject, hitterObj.gameObject, hittedObj.gameObject);
                    }
                }
            }
        }
        public override void SendInputToClients()
        {
            // Create a hit input, because inputs are faster and have more priority than states.
            if (_lastHit == null) return;
            
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
 
            Hit.SerializeHit(writer, _lastHit);

            SendInput(stream, false);
        }

        public override void ReceiveInputFromServer(InputHeader header, BinaryReader reader)
        {
            Hit hit = Hit.DeSerializeHit(reader);
            ProcessHit(hit);
        }
    }
}

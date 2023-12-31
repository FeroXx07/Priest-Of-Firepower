using System;
using System.IO;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Power_Ups
{
    public class PowerUpBase : NetworkBehaviour
    {
        public enum PowerUpType
        {
            MAX_AMMO,
            NUKE,
            DOUBLE_POINTS,
            ONE_SHOT
        }
        public PowerUpType type;
        public static Action<PowerUpType> PowerUpPickedGlobal;
        private Collider2D Coll2d;
        public bool pickedUp = false;
        public float powerUpTime = 10.0f;
        public float powerUpCount = 0.0f;
        public float timeToDestroy = 0.2f;
        public float destroyCount = 0.0f;

        [SerializeField] LayerMask layers;

        public override void Awake()
        {
            Coll2d = GetComponent<Collider2D>();
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }
        protected override void InitNetworkVariablesList()
        {
            
        }

        private void ApplyPowerUp()
        {
            pickedUp = true;
            
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false); // Hide sprites
            }
            
            if (Coll2d)
                Coll2d.enabled = false;
            PowerUpPickedGlobal?.Invoke(type);

            if (NetworkManager.Instance.IsHost())
            {
                ApplyPowerUpServer();
            }
        }

        protected virtual void ApplyPowerUpServer()
        {
            
        }
        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            writer.Write(pickedUp);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            bool receivedPickUp = reader.ReadBoolean();
            if (receivedPickUp != pickedUp && receivedPickUp)
                ApplyPowerUp();
            return true;
        }

        public override void Update()
        {
            base.Update();
            
            if (!isHost) return;

            if (!pickedUp) return;
            destroyCount += Time.deltaTime;
            if (destroyCount >= timeToDestroy)
            {
                destroyCount = 0.0f;
                DoDisposeGameObject();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!IsSelected(collision.gameObject.layer))
                return;

            Debug.Log($"OnTriggerEnter2D {gameObject.name} has collided with {collision.gameObject.name}");
            ApplyPowerUp();
        }

        bool IsSelected(int layer) => ((layers.value >> layer) & 1) == 1;
        
        public override void OnClientNetworkDespawn(NetworkObject destroyer, BinaryReader reader, long timeStamp, int length)
        {
            DisposeGameObject();
        }

        private void DisposeGameObject()
        {
            NetworkObject.isDeSpawned = true;
            Destroy(gameObject);
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

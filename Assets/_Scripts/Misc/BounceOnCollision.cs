using System.IO;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts.Misc
{
    public class BounceOnCollision : NetworkBehaviour
    {
        #region Fields
        public int maxBounces = 3;
        public int reboundCounter = 0;
        private Rigidbody2D rb;
        #endregion

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            rb = GetComponent<Rigidbody2D>();
        }
        protected override void InitNetworkVariablesList()
        {
            
        }

        public override void OnEnable()
        {
            base.OnEnable();
            reboundCounter = 0;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            reboundCounter++;
            if (reboundCounter > maxBounces)
            {
                if (isHost)
                {
                    DoDisposeGameObject();
                }
            }
            transform.right = rb.velocity.normalized;
        }
        public void DoDisposeGameObject()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, stream.ToArray().Length);
            NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(NetworkObject,replicationHeader, stream);
            DisposeGameObject();
        }
        private void DisposeGameObject()
        {
            NetworkObject.isDeSpawned = true;
            if (TryGetComponent(out PoolObject pool))
                gameObject.SetActive(false);
            else
                Destroy(gameObject);
        }
    }
}

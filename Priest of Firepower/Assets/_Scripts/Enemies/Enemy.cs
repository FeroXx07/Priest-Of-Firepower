using System;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using _Scripts.Object_Pool;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace _Scripts.Enemies
{
    public enum EnemyState
    {
        SPAWN,
        CHASE,
        ATTACK,
        DIE,
    }

    public class Enemy : NetworkBehaviour, IPointsProvider
    {
        [Header("Enemy properties")]
        [SerializeField] private int pointsOnHit = 10;
        [SerializeField] private int pointsOnDeath = 100;
        [SerializeField] private float speed = 2;
        
        [SerializeField] protected Transform target;
        protected NavMeshAgent agent;
        protected HealthSystem healthSystem;
        protected new Collider2D collider2D;

        [SerializeField] protected GameObject attackPrefab;

        protected float timeRemaining = 1.2f;

        protected float cooldownDuration = 1.5f;
        protected float attackOffset = 1.0f;
        protected float cooldownTimer = 1f;

        protected GameObject[] playerList;
        protected GameObject internalAttackObject;

        protected EnemyState enemyState;

        public UnityEvent<Enemy> onDeath = new UnityEvent<Enemy>();
        public int PointsOnHit { get => pointsOnHit; }
        public int PointsOnDeath { get => pointsOnDeath; }

        public int logicFrameIntervalUpdate = 10;
        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            
            healthSystem = GetComponent<HealthSystem>();
            agent = GetComponent<NavMeshAgent>();
            collider2D = gameObject.GetComponent<Collider2D>();
            
            agent.speed = speed;
            GetComponent<NetworkObject>().speed = speed;
            
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
        protected override void InitNetworkVariablesList()
        {
        }
        void Start()
        {
            // Only server executes the logic of the enemy
            if (!isHost) return;
            
            ServerSetTarget();
        }

        public override void Update()
        {
            base.Update();
            if (Time.frameCount % logicFrameIntervalUpdate == 0)
            {
                ServerSetTarget();
            }
            
            if (isHost)
            {
                UpdateServer();
            }
            else if (isClient)
            {
                UpdateClient();
            }
        }

        // Override in each enemy behaviour
        protected virtual void UpdateServer()
        {
        }

        protected virtual void UpdateClient()
        {
        }

        private void ServerSetTarget()
        {
            playerList = GameObject.FindGameObjectsWithTag("Player");
            float smallerDistance = Mathf.Infinity;

            foreach (var player in playerList)
            {
                float actualDistance = Vector2.Distance(player.transform.position, this.transform.position);

                if (actualDistance < smallerDistance)
                {
                    smallerDistance = actualDistance;
                    target = player.transform;
                }
            }
        }

        private void ClientSetTarget(UInt64 playerNetworkObjectId)
        {
            GameObject targetPlayer = NetworkManager.Instance.replicationManager.networkObjectMap[playerNetworkObjectId].gameObject;
            target = targetPlayer.transform;
        }

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            if (target.TryGetComponent<Player.Player>(out Player.Player player))
            {
                NetworkObject netObj = target.GetComponent<NetworkObject>();
                writer.Write(player.GetPlayerId());
                writer.Write(player.GetName());
                writer.Write(netObj.GetNetworkId());
                writer.Write((int)enemyState);
            }
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            
            //if (showDebugInfo) Debug.Log($"{_playerId} Player Shooter: Sending data");
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            UInt64 playerId = reader.ReadUInt64();
            string playerName = reader.ReadString();
            UInt64 networkId = reader.ReadUInt64();
            enemyState = (EnemyState)reader.ReadInt32();
            ClientSetTarget(networkId);
            UpdateClient();
            return true;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            
            healthSystem.OnDamageableDestroyed += HandleDeath;
            enemyState = EnemyState.SPAWN;
            collider2D.enabled = true;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            
            healthSystem.OnDamageableDestroyed -= HandleDeath;
        }

        private void HandleDeath(GameObject destroyed, GameObject destroyer)
        {
            enemyState = EnemyState.DIE;
            onDeath?.Invoke(this);
        }

        public int ProvidePointsOnHit()
        {
            return PointsOnHit;
        }

        public int ProvidePointsOnDeath()
        {
            return PointsOnDeath;
        }

        protected bool CheckLineOfSight(Transform playerTransform)
        {
            Vector2 directionToPlayer = (playerTransform.position - transform.position);
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            int playerMask = LayerMask.GetMask("Player");
            int mapMask = LayerMask.GetMask("Enviroment");

            int combinedMask = playerMask | mapMask;

            // Cast a ray from the enemy towards the player
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, combinedMask);

            // Draw the ray in the editor for debugging purposes
            if (hit)
            {
                // Draw a red line to show that line of sight is blocked
                Debug.DrawRay(transform.position, directionToPlayer, Color.green);
            }
            else
            {
                // Draw a green line to show that line of sight is clear
                Debug.DrawRay(transform.position, directionToPlayer, Color.red);
            }

            // If we hit something, check if it was the player
            return hit.collider != null && hit.collider.transform == playerTransform;
        }
        
        protected void DisposeGameObject()
        {
            if (TryGetComponent(out PoolObject pool))
            {
                gameObject.SetActive(false);
            }
            else
                Destroy(gameObject);
        }
    }
}
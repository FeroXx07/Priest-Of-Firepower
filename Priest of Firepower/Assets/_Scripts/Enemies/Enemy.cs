using System;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace _Scripts.Enemies
{
    public enum EnemyState
    {
        SPAWN,
        CHASE,
        ATTACK,
        DIE,
    }

    public class Enemy : NetworkBehaviour, IPointsProvider, IDamageable
    {
        [Header("Enemy properties")]
        [SerializeField] private int pointsOnHit = 10;
        [SerializeField] private int pointsOnDeath = 100;
        [SerializeField] private float speed = 2;
        
        [SerializeField] protected Transform Target;
        protected NavMeshAgent Agent;
        protected HealthSystem HealthSystem;
        protected Collider2D Collider;

        [SerializeField] protected GameObject attackPrefab;

        protected float TimeRemaining = 1.2f;

        protected float CooldownDuration = 1.5f;
        protected float AttackOffset = 1.0f;
        protected float CooldownTimer = 1f;

        protected GameObject[] PlayerList;
        protected GameObject InternalAttackObject;

        protected EnemyState EnemyState;

        public UnityEvent<Enemy> onDeath = new UnityEvent<Enemy>();
        public int PointsOnHit { get => pointsOnHit; }
        public int PointsOnDeath { get => pointsOnDeath; }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            
            HealthSystem = GetComponent<HealthSystem>();
            Agent = GetComponent<NavMeshAgent>();
            Collider = gameObject.GetComponent<Collider2D>();
            
            Agent.speed = speed;
            GetComponent<NetworkObject>().speed = speed;
            
            Agent.updateRotation = false;
            Agent.updateUpAxis = false;
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
            PlayerList = GameObject.FindGameObjectsWithTag("Player");
            float smallerDistance = Mathf.Infinity;

            foreach (var player in PlayerList)
            {
                float actualDistance = Vector2.Distance(player.transform.position, this.transform.position);

                if (actualDistance < smallerDistance)
                {
                    smallerDistance = actualDistance;
                    Target = player.transform;
                }
            }
        }

        private void ClientSetTarget(UInt64 playerNetworkObjectId)
        {
            GameObject targetPlayer = NetworkManager.Instance.replicationManager.networkObjectMap[playerNetworkObjectId].gameObject;
            Target = targetPlayer.transform;
        }

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            if (Target.TryGetComponent<Player.Player>(out Player.Player player))
            {
                NetworkObject netObj = Target.GetComponent<NetworkObject>();
                writer.Write(player.GetPlayerId());
                writer.Write(player.GetName());
                writer.Write(netObj.GetNetworkId());
                writer.Write((int)EnemyState);
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
            EnemyState = (EnemyState)reader.ReadInt32();
            ClientSetTarget(networkId);
            UpdateClient();
            return true;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            
            HealthSystem.OnDamageableDestroyed += HandleDeath;
            EnemyState = EnemyState.SPAWN;
            Collider.enabled = true;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            
            HealthSystem.OnDamageableDestroyed -= HandleDeath;
        }

        private void HandleDeath(GameObject destroyed, GameObject destroyer)
        {
            EnemyState = EnemyState.DIE;
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

        public event Action<GameObject, GameObject> OnDamageableDestroyed;
        public event Action<GameObject, GameObject> OnDamageTaken;
        public LayerMask Layers { get; set; }
        public int Health { get; set; }
        public void TakeDamage(IDamageDealer damageDealer, Vector3 dir, GameObject owner)
        {
            throw new NotImplementedException();
        }

        public void RaiseEventOnDamageableDestroyed(GameObject destroyer)
        {
            throw new NotImplementedException();
        }
    }
}
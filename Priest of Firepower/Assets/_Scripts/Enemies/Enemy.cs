using _Scripts.Interfaces;
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

    public class Enemy : MonoBehaviour, IPointsProvider
    {
        [SerializeField] private int pointsOnHit = 10;
        [SerializeField] private int pointsOnDeath = 100;

        protected Transform Target;
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

        private void Awake()
        {
            HealthSystem = GetComponent<HealthSystem>();
            Agent = GetComponent<NavMeshAgent>();
            Collider = gameObject.GetComponent<Collider2D>();

            Agent.updateRotation = false;
            Agent.updateUpAxis = false;
        }
        void Start()
        {
            SetTarget();
        }

        protected void SetTarget()
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

        private void OnEnable()
        {
            HealthSystem.OnDamageableDestroyed += HandleDeath;
            EnemyState = EnemyState.SPAWN;
            Collider.enabled = true;
        }

        private void OnDisable()
        {
            HealthSystem.OnDamageableDestroyed -= HandleDeath;
        }

        protected virtual void HandleDeath(GameObject destroyed, GameObject destroyer)
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




            //// Draw the ray in the editor for debugging purposes
            //if (hit)
            //{
            //    // Draw a red line to show that line of sight is blocked
            //    Debug.DrawRay(transform.position, directionToPlayer, Color.green);

            //}
            //else
            //{
            //    // Draw a green line to show that line of sight is clear
            //    Debug.DrawRay(transform.position, directionToPlayer, Color.red);

            //}

            // If we hit something, check if it was the player
            return hit.collider != null && hit.collider.transform == playerTransform;
        }
    }
}
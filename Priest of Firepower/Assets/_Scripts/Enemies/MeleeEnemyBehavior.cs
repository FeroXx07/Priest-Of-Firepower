using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class MeleeEnemyBehavior : Enemy
    {
        public float attackDuration = 0.5f;
        private bool _isAttacking = false;
        private float _attackTimer = 0.1f;

        // Update is called once per frame
        void Update()
        {
            switch (EnemyState)
            {
                case EnemyState.SPAWN:
                    Agent.isStopped = true;
                    // Spawn sound, particle and animation
                    EnemyState = EnemyState.CHASE;
                    break;

                case EnemyState.CHASE:
                    //Debug.Log("Enemy chase");
                    Agent.isStopped = false;
                    // animation, particles and sound
                    Agent.SetDestination(Target.position);

                    if(Vector3.Distance(Target.position, this.transform.position) <= 1)
                    {
                        EnemyState = EnemyState.ATTACK;
                    }

                    break;

                case EnemyState.ATTACK:
                    //Debug.Log("Enemy attacks");
                    Agent.isStopped = true;

                    if (!_isAttacking && CooldownTimer <= 0f)
                    {
                        StartMeleeAttack();
                    }

                    if (_isAttacking)
                    {
                        _attackTimer -= Time.deltaTime;
                        if (_attackTimer <= 0f)
                        {
                            EndMeleeAttack();
                        }
                    }
                    else if (CooldownTimer > 0f)
                    {
                        CooldownTimer -= Time.deltaTime;
                    }

                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (Vector3.Distance(Target.position, this.transform.position) > 1)
                    {
                        EnemyState = EnemyState.CHASE;                    
                    }

                    break;

                case EnemyState.DIE:
                
                    Agent.isStopped = true;
                    // Play death animation, sound and particles, destroy enemy object
                    Collider.enabled = false;
                
                    TimeRemaining -= Time.deltaTime;
                    if (TimeRemaining <= 0)
                    {
                        DisposeGameObject();
                    }
                    break;

                default:
                    Agent.isStopped = true;
                    break;
            }
        }

        private void StartMeleeAttack()
        {
            _isAttacking = true;
            Vector3 closerPlayerPosition = new Vector3(0,0,0);
            float distance = Mathf.Infinity;

            for(int i = 0; i < PlayerList.Length; i++)
            {
                if(Vector3.Distance(PlayerList[i].transform.position, gameObject.transform.position) < distance)
                {
                    closerPlayerPosition = PlayerList[i].transform.position;
                    distance = Vector3.Distance(PlayerList[i].transform.position, gameObject.transform.position);
                }
            }

            Vector3 directionToPlayer = (closerPlayerPosition - gameObject.transform.position).normalized;

            InternalAttackObject = Instantiate(attackPrefab);
            InternalAttackObject.transform.position = gameObject.transform.position + directionToPlayer * AttackOffset;

            _attackTimer = attackDuration;
        }

        private void EndMeleeAttack()
        {
            _isAttacking = false;

            //

            if(InternalAttackObject != null) // TODO Add to pool
            {
                Destroy(InternalAttackObject);
            }

            CooldownTimer = CooldownDuration;
        }

        private void DisposeGameObject()
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

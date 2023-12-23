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
        public override void Update()
        {
            base.Update();
            
            // Execute logic of enemy manager only in server
            if (!isHost) return;
            
            switch (enemyState)
            {
                case EnemyState.SPAWN:
                    agent.isStopped = true;
                    // Spawn sound, particle and animation
                    enemyState = EnemyState.CHASE;
                    break;

                case EnemyState.CHASE:
                    //Debug.Log("Enemy chase");
                    agent.isStopped = false;
                    // animation, particles and sound
                    agent.SetDestination(target.position);

                    if(Vector3.Distance(target.position, this.transform.position) <= 1)
                    {
                        enemyState = EnemyState.ATTACK;
                    }

                    break;

                case EnemyState.ATTACK:
                    //Debug.Log("Enemy attacks");
                    agent.isStopped = true;

                    if (!_isAttacking && cooldownTimer <= 0f)
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
                    else if (cooldownTimer > 0f)
                    {
                        cooldownTimer -= Time.deltaTime;
                    }

                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (Vector3.Distance(target.position, this.transform.position) > 1)
                    {
                        enemyState = EnemyState.CHASE;                    
                    }

                    break;

                case EnemyState.DIE:
                
                    agent.isStopped = true;
                    // Play death animation, sound and particles, destroy enemy object
                    GetComponent<Collider>().enabled = false;
                
                    timeRemaining -= Time.deltaTime;
                    if (timeRemaining <= 0)
                    {
                        DisposeGameObject();
                    }
                    break;

                default:
                    agent.isStopped = true;
                    break;
            }
        }

        private void StartMeleeAttack()
        {
            _isAttacking = true;
            Vector3 closerPlayerPosition = new Vector3(0,0,0);
            float distance = Mathf.Infinity;

            for(int i = 0; i < playerList.Length; i++)
            {
                if(Vector3.Distance(playerList[i].transform.position, gameObject.transform.position) < distance)
                {
                    closerPlayerPosition = playerList[i].transform.position;
                    distance = Vector3.Distance(playerList[i].transform.position, gameObject.transform.position);
                }
            }

            Vector3 directionToPlayer = (closerPlayerPosition - gameObject.transform.position).normalized;

            internalAttackObject = Instantiate(attackPrefab);
            internalAttackObject.transform.position = gameObject.transform.position + directionToPlayer * attackOffset;

            _attackTimer = attackDuration;
        }

        private void EndMeleeAttack()
        {
            _isAttacking = false;

            //

            if(internalAttackObject != null) // TODO Add to pool
            {
                Destroy(internalAttackObject);
            }

            cooldownTimer = cooldownDuration;
        }
    }
}

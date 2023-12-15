using _Scripts.Object_Pool;
using _Scripts.Player;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class ParalizerEnemyBehavior : Enemy
    {
        public float bulletSpeedMultiplier = 6.0f;

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
            
            // Execute logic of enemy manager only in server
            if (!isHost) return;
            
            switch (EnemyState)
            {
                case EnemyState.SPAWN:
                    Agent.isStopped = true;
                    // Spawn sound, particle and animation
                    EnemyState = EnemyState.CHASE;
                    break;

                case EnemyState.CHASE:

                    Agent.isStopped = false;

                    Agent.SetDestination(Target.position);

                    float distance = Vector3.Distance(Target.position, this.transform.position);

                    if (distance < 3)
                    {
                        Agent.SetDestination(-Target.position); // To be revewed
                    }
                    else
                    {
                        Agent.SetDestination(Target.position);
                    }

                    //Debug.Log("Before if: "+ CheckLineOfSight(target));

                    if (distance <= 9 && distance >= 3) // && (CheckLineOfSight(target) == true)
                    {
                        EnemyState = EnemyState.ATTACK;
                        // Debug.Log("Attack mode");
                    }


                    break;

                case EnemyState.ATTACK:

                    Agent.isStopped = true;

                    if (CooldownTimer <= 0f)
                    {
                        StartParalyzerAttack();
                    }

                    if (CooldownTimer > 0f)
                    {
                        CooldownTimer -= Time.deltaTime;
                    }

                    if (InternalAttackObject) 
                    {
                        InternalAttackObject.transform.position = Target.position;
                    }

                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (Vector3.Distance(Target.position, this.transform.position) > 9)
                    {
                        EnemyState = EnemyState.CHASE;

                        StopParalyzerAttack();
                    }

                    break;

                case EnemyState.DIE:

                    Agent.isStopped = true;
                    // Play death animation, sound and particles, destroy enemy object
                    Collider.enabled = false;

                    StopParalyzerAttack();

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

        private void StartParalyzerAttack()
        {
            Target.gameObject.GetComponent<Player.Player>().enabled = false;

            if(InternalAttackObject){}
            else
            {
                InternalAttackObject = Instantiate(attackPrefab);
                InternalAttackObject.transform.position = Target.position;
            }
        

            CooldownTimer = CooldownDuration;
        }

        private void StopParalyzerAttack()
        {
            Target.gameObject.GetComponent<Player.Player>().enabled = true;

            if (InternalAttackObject)
            {
                Destroy(InternalAttackObject);
            }

        }

        new bool CheckLineOfSight(Transform playerTransform)
        {
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            // Cast a ray from the enemy towards the player
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer);

            // Draw the ray in the editor for debugging purposes
            if (hit)
            {
                // Draw a red line to show that line of sight is blocked
                Debug.DrawLine(transform.position, hit.point, Color.red);
            }
            else
            {
                // Draw a green line to show that line of sight is clear
                Debug.DrawLine(transform.position, (Vector2)transform.position + directionToPlayer * distanceToPlayer, Color.green);
            }

            // If we hit something, check if it was the player
            return hit.collider != null && hit.collider.transform == playerTransform;
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

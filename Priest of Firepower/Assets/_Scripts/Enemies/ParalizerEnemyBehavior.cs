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
            
            switch (enemyState)
            {
                case EnemyState.SPAWN:
                    agent.isStopped = true;
                    // Spawn sound, particle and animation
                    enemyState = EnemyState.CHASE;
                    break;

                case EnemyState.CHASE:

                    agent.isStopped = false;

                    agent.SetDestination(target.position);

                    float distance = Vector3.Distance(target.position, this.transform.position);

                    if (distance < 3)
                    {
                        agent.SetDestination(-target.position); // To be revewed
                    }
                    else
                    {
                        agent.SetDestination(target.position);
                    }

                    //Debug.Log("Before if: "+ CheckLineOfSight(target));

                    if (distance <= 9 && distance >= 3) // && (CheckLineOfSight(target) == true)
                    {
                        enemyState = EnemyState.ATTACK;
                        // Debug.Log("Attack mode");
                    }


                    break;

                case EnemyState.ATTACK:

                    agent.isStopped = true;

                    if (cooldownTimer <= 0f)
                    {
                        StartParalyzerAttack();
                    }

                    if (cooldownTimer > 0f)
                    {
                        cooldownTimer -= Time.deltaTime;
                    }

                    if (internalAttackObject) 
                    {
                        internalAttackObject.transform.position = target.position;
                    }

                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (Vector3.Distance(target.position, this.transform.position) > 9)
                    {
                        enemyState = EnemyState.CHASE;

                        StopParalyzerAttack();
                    }

                    break;

                case EnemyState.DIE:

                    agent.isStopped = true;
                    // Play death animation, sound and particles, destroy enemy object
                    GetComponent<Collider2D>().enabled = false;

                    StopParalyzerAttack();

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

        private void StartParalyzerAttack()
        {
            target.gameObject.GetComponent<Player.Player>().enabled = false;

            if(internalAttackObject){}
            else
            {
                internalAttackObject = Instantiate(attackPrefab);
                internalAttackObject.transform.position = target.position;
            }
        

            cooldownTimer = cooldownDuration;
        }

        private void StopParalyzerAttack()
        {
            target.gameObject.GetComponent<Player.Player>().enabled = true;

            if (internalAttackObject)
            {
                Destroy(internalAttackObject);
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
    }
}

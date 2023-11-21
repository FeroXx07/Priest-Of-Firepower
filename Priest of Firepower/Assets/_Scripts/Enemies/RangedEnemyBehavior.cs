using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class RangedEnemyBehavior : Enemy
    {
        public float bulletSpeedMultiplier = 2.0f;

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

                    if (distance <= 8 && distance >= 3 && (CheckLineOfSight(Target) == true))
                    {
                        EnemyState = EnemyState.ATTACK;
                        // Debug.Log("Attack mode");
                    }

                    break;

                case EnemyState.ATTACK:

                    Agent.isStopped = true;

                    if ( CooldownTimer <= 0f)
                    {
                        StartRangedAttack();
                    }

                    if (CooldownTimer > 0f)
                    {
                        CooldownTimer -= Time.deltaTime;
                    }

                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (Vector3.Distance(Target.position, this.transform.position) > 8)  
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

        private void StartRangedAttack()
        {
            Vector3 closerPlayerPosition = new Vector3(0, 0, 0);
            float distance = Mathf.Infinity;

            for (int i = 0; i < PlayerList.Length; i++)
            {
                if (Vector3.Distance(PlayerList[i].transform.position, gameObject.transform.position) < distance)
                {
                    closerPlayerPosition = PlayerList[i].transform.position;
                    distance = Vector3.Distance(PlayerList[i].transform.position, gameObject.transform.position);
                }
            }

            Vector3 directionToPlayer = (closerPlayerPosition - gameObject.transform.position).normalized;

            InternalAttackObject = Instantiate(attackPrefab);
            InternalAttackObject.transform.position = gameObject.transform.position + directionToPlayer * AttackOffset;

            Rigidbody2D rbComp = InternalAttackObject.GetComponent<Rigidbody2D>();

            if (rbComp)
            {
                rbComp.AddForce(directionToPlayer * bulletSpeedMultiplier);
            }

            //Debug.Log("Ranged Attack done");

            CooldownTimer = CooldownDuration;
        }

        bool CheckLineOfSight(Transform playerTransform)
        {
            Vector2 directionToPlayer = (playerTransform.position - transform.position);
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);


            int playerMask = LayerMask.GetMask("Player");

            // Cast a ray from the enemy towards the player
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, playerMask);


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

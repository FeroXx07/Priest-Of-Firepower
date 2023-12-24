using UnityEngine;

namespace _Scripts.Enemies
{
    public class SniperEnemyBehavior : Enemy
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

                    if (distance <= 12 && distance >= 3 && (CheckLineOfSight(target) == true)) 
                    {
                        enemyState = EnemyState.ATTACK;
                        // Debug.Log("Attack mode");
                    }

                    break;

                case EnemyState.ATTACK:

                    agent.isStopped = true;

                    if (cooldownTimer <= 0f)
                    {
                        StartSniperAttack();
                    }

                    if (cooldownTimer > 0f)
                    {
                        cooldownTimer -= Time.deltaTime;
                    }

                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (Vector3.Distance(target.position, this.transform.position) > 12 || (CheckLineOfSight(target) == false))
                    {
                        enemyState = EnemyState.CHASE;
                    }

                    break;

                case EnemyState.DIE:

                    agent.isStopped = true;
                    // Play death animation, sound and particles, destroy enemy object
                    GetComponent<Collider2D>().enabled = false;

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

        private void StartSniperAttack()
        {
            Vector3 closerPlayerPosition = new Vector3(0, 0, 0);
            float distance = Mathf.Infinity;

            for (int i = 0; i < playerList.Length; i++)
            {
                if (Vector3.Distance(playerList[i].transform.position, gameObject.transform.position) < distance)
                {
                    closerPlayerPosition = playerList[i].transform.position;
                    distance = Vector3.Distance(playerList[i].transform.position, gameObject.transform.position);
                }
            }

            Vector3 directionToPlayer = (closerPlayerPosition - gameObject.transform.position).normalized;

            internalAttackObject = Instantiate(attackPrefab);
            internalAttackObject.transform.position = gameObject.transform.position + directionToPlayer * attackOffset;

            Rigidbody2D rbComp = internalAttackObject.GetComponent<Rigidbody2D>();

            if (rbComp)
            {
                rbComp.AddForce(directionToPlayer * bulletSpeedMultiplier);
            }

            //Debug.Log("Ranged Attack done");

            cooldownTimer = cooldownDuration;
        }
    }
}

using _Scripts.Object_Pool;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class WizardBossEnemyBehavior : Enemy
    {
        public GameObject secondAttackPrefab;
        public GameObject thirdAttackPrefab;

        private float _bulletSpeedMultiplierOne = 6.0f;
        private float _bulletSpeedMultiplierTwo = 5.0f;
        private float _bulletSpeedMultiplierThree = 8.0f;

        private int _shotCount = 0;

        //private void Start()
        //{
        //    cooldownDuration = 2.5f;
        //
        //}

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

                    //Debug.Log("Chase");

                    if (distance <= 9 && distance >= 3 && (CheckLineOfSight(target) == true)) 
                    {
                        enemyState = EnemyState.ATTACK;
                        // Debug.Log("Attack mode");
                    }



                    break;

                case EnemyState.ATTACK:

                    agent.isStopped = true;

                    //Debug.Log("Attack");

                    if (cooldownTimer <= 0f)
                    {
                        int randNum = -1;

                        randNum = RandomizeInt(1,3);

                        switch (randNum)
                        {
                            case 1:
                            {
                                InvokeRepeating("StartSimpleBulletPatternAttack", 0f, 0.5f);
                            }
                                break;
                            case 2:
                            {
                                InvokeRepeating("StartBouncingBulletPatternAttack", 0f, 0.5f);
                            }
                                break;
                            case 3:
                            {
                                InvokeRepeating("StartDuplicatingBulletAttack", 0f, 0.5f);
                            }
                                break;
                            default:
                            {
                                InvokeRepeating("StartDuplicatingBulletAttack", 0f, 0.5f);
                            }
                                break;
                        }

                    
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
                    if (Vector3.Distance(target.position, this.transform.position) > 9 || (CheckLineOfSight(target) == false))
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

        private void StartSimpleBulletPatternAttack()
        {
            Vector3 directionToPlayer = (target.position - gameObject.transform.position).normalized;

            for (int i = 0; i < 12; i++)
            {
                // Calculate the rotation for each bullet
                float angle = (i / 12.0f) * 360.0f;
                Vector3 direction = Quaternion.Euler(0, 0, angle) * directionToPlayer;

                // Instantiate the bullet and set its position
                GameObject bullet = Instantiate(attackPrefab);
                bullet.transform.position = gameObject.transform.position + direction * attackOffset;

                // Apply force to the bullet
                Rigidbody2D rbComp = bullet.GetComponent<Rigidbody2D>();
                if (rbComp)
                {
                    rbComp.AddForce(direction * _bulletSpeedMultiplierOne);
                }
            }

            _shotCount++;
            if (_shotCount >= 5)
            {
                CancelInvoke("StartSimpleBulletPatternAttack");
                _shotCount = 0;
            }

            cooldownTimer = cooldownDuration;
        }


        private void StartBouncingBulletPatternAttack()
        {
            Vector3 directionToPlayer = (target.position - gameObject.transform.position).normalized;

            for (int i = 0; i < 8; i++)
            {
                // Calculate the rotation for each bullet
                float angle = (i / 8.0f) * 360.0f;
                Vector3 direction = Quaternion.Euler(0, 0, angle) * directionToPlayer;

                // Instantiate the bullet and set its position
                GameObject bullet = Instantiate(secondAttackPrefab);
                bullet.transform.position = gameObject.transform.position + direction * attackOffset;

                // Apply force to the bullet
                Rigidbody2D rbComp = bullet.GetComponent<Rigidbody2D>();
                if (rbComp)
                {
                    rbComp.AddForce(direction * _bulletSpeedMultiplierTwo);
                }
            }

            _shotCount++;
            if (_shotCount >= 3)
            {
                CancelInvoke("StartBouncingBulletPatternAttack");
                _shotCount = 0;
            }

            cooldownTimer = cooldownDuration;
        }

        private void StartDuplicatingBulletAttack()
        {
            Vector3 directionToPlayer = (target.position - gameObject.transform.position).normalized;

            for (int i = 0; i < 3; i++)
            {
                // Calculate the rotation for each bullet
                float angle = (i / 3.0f) * 360.0f;
                Vector3 direction = Quaternion.Euler(0, 0, angle) * directionToPlayer;

                // Instantiate the bullet and set its position
                GameObject bullet = Instantiate(thirdAttackPrefab);
                bullet.transform.position = gameObject.transform.position + direction * attackOffset;

                // Apply force to the bullet
                Rigidbody2D rbComp = bullet.GetComponent<Rigidbody2D>();
                if (rbComp)
                {
                    rbComp.AddForce(direction * _bulletSpeedMultiplierThree);
                }
            }

            _shotCount++;
            if (_shotCount >= 1)
            {
                CancelInvoke("StartDuplicatingBulletAttack");
                _shotCount = 0;
            }

            cooldownTimer = cooldownDuration;
        }

        int RandomizeInt(int min, int max)
        {
            return Random.Range(min, max + 1);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class WizardBossEnemyBehavior : Enemy
{
    public GameObject secondAttackPrefab;
    public GameObject thirdAttackPrefab;

    private float bulletSpeedMultiplierOne = 6.0f;
    private float bulletSpeedMultiplierTwo = 5.0f;
    private float bulletSpeedMultiplierThree = 4.0f;

    private int shotCount = 0;

    // Update is called once per frame
    void Update()
    {
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
                                InvokeRepeating("StartThreeMagicSlashAttack", 0f, 0.5f);
                            }
                            break;
                        default:
                            {
                                InvokeRepeating("StartThreeMagicSlashAttack", 0f, 0.5f);
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
                if (Vector3.Distance(target.position, this.transform.position) > 9)
                {
                    enemyState = EnemyState.CHASE;

                }

                break;

            case EnemyState.DIE:

                agent.isStopped = true;
                // Play death animation, sound and particles, destroy enemy object
                collider.enabled = false;


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
                rbComp.AddForce(direction * bulletSpeedMultiplierOne);
            }
        }

        shotCount++;
        if (shotCount >= 3)
        {
            CancelInvoke("StartSimpleBulletPatternAttack");
            shotCount = 0;
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
                rbComp.AddForce(direction * bulletSpeedMultiplierTwo);
            }
        }

        shotCount++;
        if (shotCount >= 3)
        {
            CancelInvoke("StartBouncingBulletPatternAttack");
            shotCount = 0;
        }

        cooldownTimer = cooldownDuration;
    }

    private void StartThreeMagicSlashAttack()
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

        internalAttackObject = Instantiate(thirdAttackPrefab);
        internalAttackObject.transform.position = gameObject.transform.position + directionToPlayer * attackOffset;

        Rigidbody2D rbComp = internalAttackObject.GetComponent<Rigidbody2D>();

        if (rbComp)
        {
            rbComp.AddForce(directionToPlayer * bulletSpeedMultiplierThree);
        }

        shotCount++;
        if (shotCount >= 3)
        {
            CancelInvoke("StartThreeMagicSlashAttack");
            shotCount = 0;
        }

        cooldownTimer = cooldownDuration;
    }

    int RandomizeInt(int min, int max)
    {
        return Random.Range(min, max + 1);
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

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class RangedEnemyBehavior : Enemy
{
    public float bulletSpeedMultiplier = 2.0f;

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

                if (distance <= 8 && distance >= 3) // && (CheckLineOfSight(target) == true)
                {
                    enemyState = EnemyState.ATTACK;
                   // Debug.Log("Attack mode");
                }

                break;

            case EnemyState.ATTACK:

                agent.isStopped = true;

                if ( cooldownTimer <= 0f)
                {
                    StartRangedAttack();
                }

                if (cooldownTimer > 0f)
                {
                    cooldownTimer -= Time.deltaTime;
                }

                // For example: Perform attack, reduce player health, animation sound and particles
                if (Vector3.Distance(target.position, this.transform.position) > 8)  
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

    private void StartRangedAttack()
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

    bool CheckLineOfSight(Transform playerTransform)
    {
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Cast a ray from the enemy towards the player
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, 9);
        

        // Draw the ray in the editor for debugging purposes
        if (hit)
        {
            // Draw a red line to show that line of sight is blocked
            Debug.DrawLine(transform.position, hit.point, Color.red,5f);
        }
        else
        {
            // Draw a green line to show that line of sight is clear
            Debug.DrawLine(transform.position, (Vector2)transform.position + directionToPlayer * distanceToPlayer, Color.green, 5f);
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

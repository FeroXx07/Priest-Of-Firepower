using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class RangedEnemyBehavior : MonoBehaviour
{
    Transform target;

    NavMeshAgent agent;

    HealthSystem enemyData;

    new Collider2D collider;

    [SerializeField]
    GameObject rangedAttackPrefab;

    public float bulletSpeedMultiplier = 2.0f;

    float timeRemaining = 1.2f;

    public float cooldownDuration = 1.5f;
    public float attackOffset = 1.0f;
    private float cooldownTimer = 1f;

    private GameObject[] playerList;
    private GameObject internalRangedAttackObject;

    enum rangedEnemyState
    {
        SPAWN,
        CHASE,
        ATTACK,
        DIE,
    }

    rangedEnemyState enemyState = rangedEnemyState.SPAWN;


    // Start is called before the first frame update
    void Start()
    {
        enemyData = GetComponent<HealthSystem>();
        target = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        collider = gameObject.GetComponent<Collider2D>();

        playerList = GameObject.FindGameObjectsWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyData.Health <= 0)
        {
            enemyState = rangedEnemyState.DIE;
        }


        switch (enemyState)
        {
            case rangedEnemyState.SPAWN:
                agent.isStopped = true;
                // Spawn sound, particle and animation
                enemyState = rangedEnemyState.CHASE;
                break;

            case rangedEnemyState.CHASE:
                
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
                    enemyState = rangedEnemyState.ATTACK;
                   // Debug.Log("Attack mode");
                }

                break;

            case rangedEnemyState.ATTACK:

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
                    enemyState = rangedEnemyState.CHASE;
                }

                break;

            case rangedEnemyState.DIE:

                agent.isStopped = true;
                // Play death animation, sound and particles, destroy enemy object
                collider.enabled = false;

                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0)
                {
                    Destroy(gameObject);
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

        internalRangedAttackObject = Instantiate(rangedAttackPrefab);
        internalRangedAttackObject.transform.position = gameObject.transform.position + directionToPlayer * attackOffset;

        Rigidbody2D rbComp = internalRangedAttackObject.GetComponent<Rigidbody2D>();

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


}

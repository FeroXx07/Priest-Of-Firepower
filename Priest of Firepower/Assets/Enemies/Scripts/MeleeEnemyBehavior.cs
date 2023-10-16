using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MeleeEnemyBehavior : MonoBehaviour
{
    Transform target;

    NavMeshAgent agent;

    HealthSystem enemyData;

    Collider2D collider;

    [SerializeField]
    GameObject meleAttackPrefab;

    float timeRemaining = 3f;

    public float attackDuration = 0.5f;
    public float cooldownDuration = 1.5f;
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private float cooldownTimer = 0f;

    enum meleeEnemyState
    {
       SPAWN,
       CHASE,
       ATTACK,
       DIE,
    }

    meleeEnemyState enemyState = meleeEnemyState.SPAWN;


    // Start is called before the first frame update
    void Start()
    {
        enemyData = GetComponent<HealthSystem>();
        target = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        collider = gameObject.GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if(enemyData.Health <= 0)
        {
            enemyState = meleeEnemyState.DIE;
        }


        switch (enemyState)
        {
            case meleeEnemyState.SPAWN:
                agent.isStopped = true;
                // Spawn sound, particle and animation
                enemyState = meleeEnemyState.CHASE;
                break;

            case meleeEnemyState.CHASE:
                //Debug.Log("Enemy chase");
                agent.isStopped = false;
                // animation, particles and sound
                agent.SetDestination(target.position);

                if(Vector3.Distance(target.position, this.transform.position) <= 2)
                {
                    enemyState = meleeEnemyState.ATTACK;
                }

                break;

            case meleeEnemyState.ATTACK:
                //Debug.Log("Enemy attacks");
                agent.isStopped = true;

                if (!isAttacking && cooldownTimer <= 0f)
                {
                    StartMeleeAttack();
                }

                if (isAttacking)
                {
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        EndMeleeAttack();
                    }
                }
                else if (cooldownTimer > 0f)
                {
                    cooldownTimer -= Time.deltaTime;
                }

                // For example: Perform attack, reduce player health, animation sound and particles
                if (Vector3.Distance(target.position, this.transform.position) > 2)
                {
                    enemyState = meleeEnemyState.CHASE;                    
                }

                break;

            case meleeEnemyState.DIE:
                
                agent.isStopped = true;
                // Play death animation, sound and particles, destroy enemy object
                collider.enabled = false;
                
                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0)
                {
                    DestroyObject(gameObject);
                }
                break;

            default:
                agent.isStopped = true;
                break;
        }
    }

    private void StartMeleeAttack()
    {
        isAttacking = true;

        // If you have an animation, play it here
        // animator.SetTrigger("Attack");
        GameObject GO = Instantiate(meleAttackPrefab);
        GO.transform.position = gameObject.transform.position;

        attackTimer = attackDuration;
    }

    private void EndMeleeAttack()
    {
        isAttacking = false;
        cooldownTimer = cooldownDuration;
    }
}

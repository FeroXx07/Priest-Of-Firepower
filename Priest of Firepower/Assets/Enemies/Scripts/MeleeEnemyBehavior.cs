using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MeleeEnemyBehavior : MonoBehaviour
{
    Transform target;

    NavMeshAgent agent;

    EnemyData enemyData;

    float timeRemaining = 3f;

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
        enemyData = GetComponent<EnemyData>();
        target = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(enemyData.currentLife <= 0)
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
                // For example: Perform attack, reduce player health, animation sound and particles
                if(Vector3.Distance(target.position, this.transform.position) > 2)
                {
                    enemyState = meleeEnemyState.CHASE;                    
                }

                break;

            case meleeEnemyState.DIE:
                
                agent.isStopped = true;
                // Play death animation, sound and particles, destroy enemy object
                
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
}

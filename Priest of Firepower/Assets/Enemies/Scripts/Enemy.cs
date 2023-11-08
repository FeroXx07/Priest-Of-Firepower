using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public enum EnemyState
{
    SPAWN,
    CHASE,
    ATTACK,
    DIE,
}

public class Enemy : MonoBehaviour
{
    protected Transform target;
    protected NavMeshAgent agent;
    protected HealthSystem enemyData;
    protected new Collider2D collider;

    [SerializeField] protected GameObject attackPrefab;

    protected float timeRemaining = 1.2f;

    protected float cooldownDuration = 1.5f;
    protected float attackOffset = 1.0f;
    protected float cooldownTimer = 1f;

    protected GameObject[] playerList;
    protected GameObject internalAttackObject;

    protected EnemyState enemyState;

    public UnityEvent<Enemy> onDeath = new UnityEvent<Enemy>();

    private void Awake()
    {
        enemyData = GetComponent<HealthSystem>();
        agent = GetComponent<NavMeshAgent>();
        collider = gameObject.GetComponent<Collider2D>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }
    void Start()
    {
        target = GameObject.Find("Player").transform;
        playerList = GameObject.FindGameObjectsWithTag("Player");
    }

    private void OnEnable()
    {
        enemyData.onDamageableDestroyed += HandleDeath;
    }

    private void OnDisable()
    {
        enemyData.onDamageableDestroyed -= HandleDeath;
    }

    protected virtual void HandleDeath(GameObject destroyed, GameObject destroyer)
    {
        enemyState = EnemyState.DIE;
        onDeath?.Invoke(this);
    }
}

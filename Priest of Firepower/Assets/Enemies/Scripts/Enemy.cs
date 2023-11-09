using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

public class Enemy : MonoBehaviour, IPointsProvider
{
    [SerializeField] private int pointsOnHit = 10;
    [SerializeField] private int pointsOnDeath = 100;
    [SerializeField] private int pointsMultiplyer = 1;

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

    public int PointsOnHit { get => pointsOnHit; }
    public int PointsOnDeath { get => pointsOnDeath; }
    public int Multiplyer { get => pointsMultiplyer; set => pointsMultiplyer = value; }

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
        playerList = GameObject.FindGameObjectsWithTag("Player");
        float smallerDistance = Mathf.Infinity;

        foreach (var player in playerList)
        {
            float actualDistance = Vector2.Distance(player.transform.position, this.transform.position);

            if (actualDistance < smallerDistance)
            { 
                smallerDistance = actualDistance;
                target = player.transform;
            }
        }        
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

    public int ProvidePointsOnHit()
    {
        return PointsOnHit;
    }

    public int ProvidePointsOnDeath()
    {
        return PointsOnDeath;
    }
}
using System.IO;
using _Scripts.Attacks;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class ParalizerEnemyBehavior : Enemy
    {
        public float minRange = 3.0f;
        public float maxRange = 9.0f;

        public override void Start()
        {
            base.Start();
            attackState = EnemyAttackState.END;
        }

        protected override void UpdateServer()
        {
            float distance = Vector3.Distance(target.position, this.transform.position);
            switch (enemyState)
            {
                case EnemyState.SPAWN:
                {
                    agent.isStopped = true;
                    enemyState = EnemyState.CHASE;
                }
                    break;
                case EnemyState.CHASE:
                {
                    agent.isStopped = false;
                    overrideTarget = false;
                    
                    agent.SetDestination(target.position);
                    
                    if (distance < minRange)
                    {
                        agent.SetDestination(-target.position); 
                    }
                    else
                    {
                        agent.SetDestination(target.position);
                    }
                    bool inSight = CheckLineOfSight(target);
                    
                    if (distance <= maxRange && distance >= minRange && inSight)
                    {
                        enemyState = EnemyState.ATTACK;
                    }
                }
                    break;
                case EnemyState.ATTACK:
                {
                    agent.isStopped = true;
                    overrideTarget = true;
                    
                    if (attackState == EnemyAttackState.END && cooldownTimer <= 0f)
                    {
                        attackState = EnemyAttackState.EXECUTE;
                    }

                    if (attackState == EnemyAttackState.END)
                    {
                        cooldownTimer -= Time.deltaTime;
                    }

                    if (internalAttackObject) 
                    {
                        internalAttackObject.transform.position = target.position;
                    }
                    
                    if (attackState == EnemyAttackState.EXECUTE)
                        StartServerAttack();
                    
                    if (distance > 9)
                    {
                        enemyState = EnemyState.CHASE;
                        ServerEndAttack();
                    }
                }
                    break;
                case EnemyState.DIE:
                {
                    agent.isStopped = true;
                    // Play death animation, sound and particles, destroy enemy object
                    collider2D.enabled = false;
                    
                    if (timeRemaining == 1.2f) 
                        ServerEndAttack();
                    
                    timeRemaining -= Time.deltaTime;
                    if (timeRemaining <= 0 && !isDeSpawned)
                    {
                        isDeSpawned = true;
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(stream);
                        ReplicationHeader enemyDeSpawnHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, stream.ToArray().Length);
                        NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(NetworkObject, enemyDeSpawnHeader, stream);
                        DisposeGameObject();
                    }
                }
                    break;
                default:
                    agent.isStopped = true;
                    break;
            }
        }
        protected override void UpdateClient()
        {
            switch (enemyState)
            {
                case EnemyState.SPAWN:
                {
                    agent.isStopped = true;
                    // Spawn sound, particle and animation
                    enemyState = EnemyState.CHASE;
                }
                    break;
                case EnemyState.CHASE:
                {
                    agent.isStopped = false;
                    
                    if (target == null) return;
                    float distance = Vector3.Distance(target.position, this.transform.position);
                    
                    agent.SetDestination(target.position);
                    
                    if (distance < minRange)
                    {
                        agent.SetDestination(-target.position); 
                    }
                    else
                    {
                        agent.SetDestination(target.position);
                    }
                    
                    if (distance <= maxRange && distance >= minRange && (CheckLineOfSight(target) == true))
                    {
                        enemyState = EnemyState.ATTACK;
                    }
                }
                    break;
                case EnemyState.ATTACK:
                {
                    agent.isStopped = true;
                    
                    if (target == null) return;
                    float distance = Vector3.Distance(target.position, this.transform.position);

                    if (attackState == EnemyAttackState.END)
                    {
                        cooldownTimer -= Time.deltaTime;
                    }

                    if (internalAttackObject) 
                    {
                        internalAttackObject.transform.position = target.position;
                    }
                    
                    if (distance > 9)
                    {
                        enemyState = EnemyState.CHASE;
                        ClientEndAttack();
                    }
                }
                    break;
                case EnemyState.DIE:
                {
                    agent.isStopped = true;
                    // Play death animation, sound and particles, destroy enemy object
                    collider2D.enabled = false;
                }
                    break;
                default:
                    agent.isStopped = true;
                    break;
            }
        }
        private void StartServerAttack()
        {
            if (target == null) return;
            
            if(internalAttackObject != null) 
            {
                ParalizerAttack p = internalAttackObject.GetComponent<ParalizerAttack>();
                p.DoDisposeGameObject();
            }
            
            attackState = EnemyAttackState.COOLDOWN;
            
            target.gameObject.GetComponent<Player.Player>().isParalized = true;
            
            MemoryStream meleeAttackMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(meleeAttackMemoryStream);
            writer.Write("ParalizerAttack");
            ReplicationHeader changeWeaponHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, meleeAttackMemoryStream.ToArray().Length);
            internalAttackObject = NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(attackPrefab,
                changeWeaponHeader, meleeAttackMemoryStream);
            
            internalAttackObject.transform.position = target.position;
            
            ParalizerAttack paralizerAttack = internalAttackObject.GetComponent<ParalizerAttack>();
            paralizerAttack.Damage = damage;
            paralizerAttack.SetOwner(gameObject);
            paralizerAttack.SetPlayer(target.gameObject);
            
            cooldownTimer = cooldownDuration;
        }
        
        public override void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader, long timeStamp, int lenght)
        {
            string type = reader.ReadString();
        
            if (type == "ParalizerAttack")
            {
                StartClientAttack(objectSpawned.gameObject);
            }
        }

        private void StartClientAttack(GameObject objectSpawned)
        {
            internalAttackObject = objectSpawned;
            internalAttackObject.transform.position = target.position;
            
            ParalizerAttack paralizerAttack = internalAttackObject.GetComponent<ParalizerAttack>();
            paralizerAttack.Damage = damage;
            paralizerAttack.SetOwner(gameObject);
            paralizerAttack.SetPlayer(target.gameObject);
        }

        private void ServerEndAttack()
        {
            if (target == null) return;
            attackState = EnemyAttackState.END;
            
            target.gameObject.GetComponent<Player.Player>().isParalized = false;
            
            if(internalAttackObject != null) 
            {
                ParalizerAttack paralizerAttack = internalAttackObject.GetComponent<ParalizerAttack>();
                paralizerAttack.DoDisposeGameObject();
            }
            cooldownTimer = cooldownDuration;
        }
        private void ClientEndAttack()
        {
            attackState = EnemyAttackState.END;
            cooldownTimer = cooldownDuration;
        }
        public override void OnClientNetworkSpawn(NetworkObject spawner, BinaryReader reader, long timeStamp, int lenght)
        {
            Debug.Log("Enemy spawned in client");
        }

        public override void OnClientNetworkDespawn(NetworkObject destroyer, BinaryReader reader, long timeStamp, int lenght)
        {
            Debug.Log("Enemy dead in client");
            DisposeGameObject();
        }
        bool CheckLineOfSightParalizer(Transform playerTransform)
        {
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            // Cast a ray from the enemy towards the player
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer);

            // Draw the ray in the editor for debugging purposes
            if (hit)
            {
                // Draw a red line to show that line of sight is blocked
                Debug.DrawLine(transform.position, hit.point, Color.red);
            }
            else
            {
                // Draw a green line to show that line of sight is clear
                Debug.DrawLine(transform.position, (Vector2)transform.position + directionToPlayer * distanceToPlayer, Color.green);
            }

            // If we hit something, check if it was the player
            return hit.collider != null && hit.collider.transform == playerTransform;
        }
    }
}

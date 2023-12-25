using System.IO;
using _Scripts.Attacks;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class RangedEnemyBehavior : Enemy
    {
        public float bulletSpeedMultiplier = 2.0f;

        // Update is called once per frame
        protected override void UpdateServer()
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

                    if (distance <= 8 && distance >= 3 && (CheckLineOfSight(target) == true))
                    {
                        enemyState = EnemyState.ATTACK;
                        // Debug.Log("Attack mode");
                    }
                }
                    break;
                case EnemyState.ATTACK:
                {
                    agent.isStopped = true;

                    if (cooldownTimer <= 0f)
                    {
                        attackState = EnemyAttackState.EXECUTE;
                    }

                    if (cooldownTimer > 0f)
                    {
                        attackState = EnemyAttackState.COOLDOWN;
                        cooldownTimer -= Time.deltaTime;
                    }
                    
                    if (attackState == EnemyAttackState.EXECUTE)
                        ServerAttack();
                    
                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (Vector3.Distance(target.position, this.transform.position) > 8 || (CheckLineOfSight(target) == false))  
                    {
                        enemyState = EnemyState.CHASE;
                    }

                }
                    break;
                case EnemyState.DIE:
                {
                    agent.isStopped = true;
                    // Play death animation, sound and particles, destroy enemy object
                    collider2D.enabled = false;

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
                }
                    break;
                case EnemyState.ATTACK:
                {
                    agent.isStopped = true;
                    
                    if (target == null) return;
                    
                    // if (attackState == EnemyAttackState.EXECUTE)
                    //     ClientAttack();
                    
                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (Vector3.Distance(target.position, this.transform.position) > 8 || (CheckLineOfSight(target) == false))  
                    {
                        enemyState = EnemyState.CHASE;
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

        public override void OnClientNetworkSpawn(NetworkObject spawner, BinaryReader reader, long timeStamp, int lenght)
        {
            Debug.Log("Enemy spawned in client");
        }

        public override void OnClientNetworkDespawn(NetworkObject destroyer, BinaryReader reader, long timeStamp, int lenght)
        {
            Debug.Log("Enemy dead in client");
            DisposeGameObject();
        }

        private void ServerAttack()
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
            Vector3 spawnPos = gameObject.transform.position + directionToPlayer * attackOffset;

            MemoryStream rangedAttackMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(rangedAttackMemoryStream);
            
            writer.Write("RangedAttack");
            writer.Write(spawnPos.x);
            writer.Write(spawnPos.y);
            writer.Write(spawnPos.z);
            writer.Write(directionToPlayer.x);
            writer.Write(directionToPlayer.y);
            writer.Write(directionToPlayer.z);
            
            ReplicationHeader changeWeaponHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, rangedAttackMemoryStream.ToArray().Length);
            internalAttackObject = NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(attackPrefab,
                changeWeaponHeader, rangedAttackMemoryStream);
            
            OnTriggerAttack onTriggerAttack = internalAttackObject.GetComponent<OnTriggerAttack>();
            onTriggerAttack.Damage = damage;
            onTriggerAttack.SetOwner(gameObject);
            
            internalAttackObject.transform.position = spawnPos;
            
            if (internalAttackObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D rbComp))
            {
                rbComp.AddForce(directionToPlayer * bulletSpeedMultiplier);
            }

            //Debug.Log("RangedEnemyBehaviour: Server Attack done");

            cooldownTimer = cooldownDuration;
        }

        public override void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader, long timeStamp, int lenght)
        {
            string type = reader.ReadString();
        
            if (type == "RangedAttack")
            {
                Vector3 spawnPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Vector3 direction = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                ClientAttack(objectSpawned.gameObject, spawnPos, direction);
            }
        }

        private void ClientAttack(GameObject objectSpawned, Vector3 spawnPos, Vector3 directionToPlayer)
        {
            internalAttackObject = objectSpawned;
            internalAttackObject.transform.position = spawnPos;
            
            OnTriggerAttack onTriggerAttack = internalAttackObject.GetComponent<OnTriggerAttack>();
            onTriggerAttack.Damage = damage;
            onTriggerAttack.SetOwner(gameObject);
            onTriggerAttack.SetOwner(gameObject);
            
            if (internalAttackObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D rbComp))
            {
                rbComp.AddForce(directionToPlayer * bulletSpeedMultiplier);
            }
            
            //Debug.Log("RangedEnemyBehaviour: Client Attack done");
        }
    }
}

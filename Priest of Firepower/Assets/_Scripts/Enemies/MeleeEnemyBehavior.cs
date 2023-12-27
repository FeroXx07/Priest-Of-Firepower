using System;
using System.IO;
using _Scripts.Attacks;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class MeleeEnemyBehavior : Enemy
    {
        public float attackDuration = 0.5f;
        private bool _isAttacking = false;
        private float _attackTimer = 0.1f;

        protected override void UpdateServer()
        {
            float distance = Vector3.Distance(target.position, this.transform.position);

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

                    if(distance <= 1)
                    {
                        enemyState = EnemyState.ATTACK;
                    }
                }
                    break;
                case EnemyState.ATTACK:
                {
                    agent.isStopped = true;

                    if (!_isAttacking && cooldownTimer <= 0f)
                    {
                        attackState = EnemyAttackState.EXECUTE;
                        StartServerAttack();
                    }

                    if (_isAttacking)
                    {
                        _attackTimer -= Time.deltaTime;
                        if (_attackTimer <= 0f)
                        {
                            ServerEndAttack();
                        }
                        else
                        {
                            attackState = EnemyAttackState.COOLDOWN;
                        }
                    }
                    else if (cooldownTimer > 0f)
                    {
                        cooldownTimer -= Time.deltaTime;
                    }

                    // For example: Perform attack, reduce player health, animation sound and particles
                    if (distance > 1)
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

                    if(Vector3.Distance(target.position, this.transform.position) <= 1)
                    {
                        enemyState = EnemyState.ATTACK;
                    }
                }
                    break;
                case EnemyState.ATTACK:
                {
                    agent.isStopped = true;
                    
                    if (_isAttacking)
                    {
                        _attackTimer -= Time.deltaTime;
                        if (_attackTimer <= 0f)
                        {
                            ClientEndAttack();
                        }
                    }
                    else if (cooldownTimer > 0f)
                    {
                        cooldownTimer -= Time.deltaTime;
                    }
                    
                    if (target == null) return;

                    if (Vector3.Distance(target.position, this.transform.position) > 1)
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

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);

            if (target == null)
            {
                writer.Write(false);
            }
            
            if (target.TryGetComponent<Player.Player>(out Player.Player player))
            {
                NetworkObject netObj = target.GetComponent<NetworkObject>();
                writer.Write(true);
                writer.Write(netObj.GetNetworkId());
                writer.Write((int)enemyState);
                writer.Write((int)attackState);
                
                writer.Write(_isAttacking);
            }
            
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            bool hasTarget = reader.ReadBoolean();

            if (hasTarget)
            {
                UInt64 networkId = reader.ReadUInt64();
                enemyState = (EnemyState)reader.ReadInt32();
                attackState = (EnemyAttackState)reader.ReadInt32();
                _isAttacking = reader.ReadBoolean();
                ClientSetTarget(networkId);
                UpdateClient();
            }
            return true;
        }

        private void StartServerAttack()
        {
            _isAttacking = true;
            Vector3 closerPlayerPosition = new Vector3(0,0,0);
            float distance = Mathf.Infinity;

            for(int i = 0; i < playerList.Length; i++)
            {
                if(Vector3.Distance(playerList[i].transform.position, gameObject.transform.position) < distance)
                {
                    closerPlayerPosition = playerList[i].transform.position;
                    distance = Vector3.Distance(playerList[i].transform.position, gameObject.transform.position);
                }
            }

            var position = gameObject.transform.position;
            Vector3 directionToPlayer = (closerPlayerPosition - position).normalized;
            Vector3 spawnPos = position + directionToPlayer * attackOffset;
            
            MemoryStream meleeAttackMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(meleeAttackMemoryStream);
            
            writer.Write("MeleeAttack");
            writer.Write(spawnPos.x);
            writer.Write(spawnPos.y);
            writer.Write(spawnPos.z);
            writer.Write(directionToPlayer.x);
            writer.Write(directionToPlayer.y);
            writer.Write(directionToPlayer.z);
            
            ReplicationHeader changeWeaponHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, meleeAttackMemoryStream.ToArray().Length);
            internalAttackObject = NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(attackPrefab,
                changeWeaponHeader, meleeAttackMemoryStream);
            
            OnTriggerAttack onTriggerAttack = internalAttackObject.GetComponent<OnTriggerAttack>();
            onTriggerAttack.Damage = damage;
            onTriggerAttack.SetOwner(gameObject);
            
            internalAttackObject.transform.position = spawnPos;
            
            _attackTimer = attackDuration;
        }
        private void ServerEndAttack()
        {
            _isAttacking = false;
            if(internalAttackObject != null) 
            {
                OnTriggerAttack onTriggerAttack = internalAttackObject.GetComponent<OnTriggerAttack>();
                onTriggerAttack.DoDisposeGameObject();
            }
            cooldownTimer = cooldownDuration;
        }
        
        private void ClientEndAttack()
        {
            _isAttacking = false;
            cooldownTimer = cooldownDuration;
        }
        
        public override void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader, long timeStamp, int lenght)
        {
            string type = reader.ReadString();
        
            if (type == "MeleeAttack")
            {
                Vector3 spawnPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Vector3 direction = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                StartClientAttack(objectSpawned.gameObject, spawnPos, direction);
            }
        }

        private void StartClientAttack(GameObject objectSpawned, Vector3 spawnPos, Vector3 directionToPlayer)
        {
            internalAttackObject = objectSpawned;
            internalAttackObject.transform.position = spawnPos;
            
            OnTriggerAttack onTriggerAttack = internalAttackObject.GetComponent<OnTriggerAttack>();
            onTriggerAttack.Damage = damage;
            onTriggerAttack.SetOwner(gameObject);
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
    }
}

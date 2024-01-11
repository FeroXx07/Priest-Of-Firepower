using System.Collections.Generic;
using _Scripts.Power_Ups;
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using _Scripts.UI.Enemies;

namespace _Scripts.Enemies
{
    public enum EnemyManagerEvent
    {
        SPAWN_ENEMY,
    }
    public class EnemyManager : NetworkBehaviour
    {
        List<Transform> _spawnPoints = new List<Transform>();
        public List<GameObject> enemiesPrefabs = new List<GameObject>();
        private List<GameObject> _enemiesToSpawn = new List<GameObject>();
                
        [SerializeField] AnimationCurve enemyCountProgression = new AnimationCurve();
        public List<Enemy> enemiesAlive = new List<Enemy>();

        
        //current number of enemies to spanw on the current wave
        int _numberOfEnemiesToSpwan = 0;
        float _spawnRate = 0.5f;
        float _spawnTimer;
        
        public Action<int> OnEnemyCountUpdate;
        public Action<Enemy> OnEnemySpawn;
        public Action<Enemy> OnEnemyRemove;

        public override void Awake()
        {
            // create the instance
            if (_instance == null)
            {
                _instance = this as EnemyManager;
                //DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        private void Start()
        {
            //StartCoroutine(NullCheckRoutine());
        }

        protected override void InitNetworkVariablesList()
        {
            
        }

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            writer.Write(_numberOfEnemiesToSpwan);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            _numberOfEnemiesToSpwan = reader.ReadInt32();
            return true;
        }

        public void SpawnEnemies(int round)
        {
            _enemiesToSpawn.Clear();
            _enemiesToSpawn = GetEnemiesToSpawn(round);
            Debug.Log(_enemiesToSpawn);
            _numberOfEnemiesToSpwan = _enemiesToSpawn.Count;
            Debug.Log("Enemies remaining: " + _numberOfEnemiesToSpwan);
        }

        public override void Update()
        {
            base.Update();
            
            // Execute logic of enemy manager only in server
            if (!isHost) return;
            
            if (_numberOfEnemiesToSpwan > 0)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0)
                {
                    Transform p = GetRandomSpawnPoint();
                    if (p != null)
                    {
                        ServerSpawnEnemy(p.position,_numberOfEnemiesToSpwan);
                        _numberOfEnemiesToSpwan--;
                        _spawnTimer = _spawnRate;
                        OnEnemyCountUpdate?.Invoke(enemiesAlive.Count);
                    }

                }
            }
        }
        // IEnumerator NullCheckRoutine()
        // {
        //     WaitForSeconds seconds = new WaitForSeconds(1);
        //     while (true)
        //     {
        //         foreach (Enemy enemy in enemiesAlive)
        //         {
        //             if (enemy == null)
        //                 enemiesAlive.Remove(enemy);
        //         }
        //         yield return seconds;
        //     }
        //     yield return null;
        // }
        private List<GameObject> GetEnemiesToSpawn(int round)
        {
            List<GameObject> enemiesToSpawnList = new List<GameObject>();
            float totalProbability = 0f;

            // Calculate the total probability of all enemy types
            foreach (GameObject enemyPrefab in enemiesPrefabs)
            {
                Enemy enemyScript = enemyPrefab.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    totalProbability += enemyScript.spawnProbability;
                }
            }

            // For each enemy type, calculate the number of enemies to spawn based on its probability
            foreach (GameObject enemyPrefab in enemiesPrefabs)
            {
                Enemy enemyScript = enemyPrefab.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    float probability = enemyScript.spawnProbability / totalProbability;
                    int enemiesForType = Mathf.FloorToInt(probability * enemyCountProgression.Evaluate(round));

                    // Add the enemy prefab to the list based on the calculated count
                    for (int i = 0; i < enemiesForType; i++)
                    {
                        enemiesToSpawnList.Add(enemyPrefab);
                    }
                }
            }

            return enemiesToSpawnList;
        }
        void ServerSpawnEnemy(Vector3 spawnPosition,int enemyCount)
        {
            Debug.Log("Enemy Manager: Server spawning enemy!");
            int enemyType = UnityEngine.Random.Range(0, enemiesPrefabs.Count);
            GameObject enemyPrefab = _enemiesToSpawn[enemyCount-1];
            
            MemoryStream changeWeaponMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(changeWeaponMemoryStream);
            
            writer.Write((int)EnemyManagerEvent.SPAWN_ENEMY);
            writer.Write(spawnPosition.x);
            writer.Write(spawnPosition.y);
            writer.Write(spawnPosition.z);
            
            ReplicationHeader changeWeaponHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, changeWeaponMemoryStream.ToArray().Length);
            GameObject enemyGo = NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(enemyPrefab,
                changeWeaponHeader, changeWeaponMemoryStream);

            enemyGo.transform.position = spawnPosition;
            
            if (enemyGo.TryGetComponent(out Enemy enemy)) AddEnemyToList(enemy);
            OnEnemySpawn?.Invoke(enemy);
        }

        void ClientSpawnEnemy(GameObject spawnedEnemy, Vector3 spawnPosition)
        {
            Debug.Log("Enemy Manager: Client spawning enemy!");
            _numberOfEnemiesToSpwan--;
            spawnedEnemy.transform.position = spawnPosition;
            
            if (spawnedEnemy.TryGetComponent(out Enemy enemy)) AddEnemyToList(enemy);
            OnEnemySpawn?.Invoke(enemy);
        }

        public override void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader, long timeStamp, int lenght)
        {
            // Client handles the objects spawned by the server
            EnemyManagerEvent enemyManagerEvent = (EnemyManagerEvent)reader.ReadInt32();

            if (enemyManagerEvent == EnemyManagerEvent.SPAWN_ENEMY)
            {
                Vector3 spawnPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                ClientSpawnEnemy(objectSpawned.gameObject, spawnPosition);
            }
        }

        void AddEnemyToList(Enemy enemy)
        {
            enemiesAlive.Add(enemy);
            enemy.onDeath.AddListener(RemoveEnemyFromList);
        }

        void RemoveEnemyFromList(Enemy enemy)
        {
            Debug.Log($"Enemy Manager: Enemy remove callback!, enemies alive {enemiesAlive.Count}");
            
            if (enemiesAlive.Contains(enemy))
                enemiesAlive.Remove(enemy);
            
            enemy.onDeath.RemoveListener(RemoveEnemyFromList);
            OnEnemyCountUpdate?.Invoke(enemiesAlive.Count);
            OnEnemyRemove?.Invoke(enemy);
        }

        
        private int GetNumberOfEnemiesToSpawn(int round)
        {
            return Mathf.FloorToInt(enemyCountProgression.Evaluate(round));
        }

        private Transform GetRandomSpawnPoint()
        {
            if(_spawnPoints.Count > 0)
            return _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Count)];
            else
            {
               return null;
            }
        }

        public void AddSpawnpoint(Transform spawnPoint)
        {
            _spawnPoints.Add(spawnPoint);
        }
        
        public void RemoveSpawnPoint(Transform spawnPoint)
        {
            if (_spawnPoints.Contains(spawnPoint))
                _spawnPoints.Remove(spawnPoint);
        }

        public int GetEnemiesAlive()
        {
            return enemiesAlive.Count;
        }

        public int GetEnemiesCountLeft()
        {
            return _numberOfEnemiesToSpwan;
        }
        
        #region Singleton
        private static EnemyManager _instance;
        public static EnemyManager Instance
        {
            get
            {
                // if instance is null
                if (_instance == null)
                {
                    // find the generic instance
                    _instance = FindObjectOfType<EnemyManager>();

                    // if it's null again create a new object
                    // and attach the generic instance
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(EnemyManager).Name;
                        _instance = obj.AddComponent<EnemyManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion
    }
}
using System.Collections.Generic;
using _Scripts.Object_Pool;
using _Scripts.Power_Ups;
using UnityEngine;
using System;
using System.IO;
using _Scripts.Networking;
using _Scripts.Weapon;

namespace _Scripts.Enemies
{
    public enum EnemyManagerEvent
    {
        SPAWN_ENEMY,
        DESPAWN_ENEMY
    }
    public class EnemyManager : NetworkBehaviour
    {
        List<Transform> _spawnPoints = new List<Transform>();
        public List<GameObject> enemiesPrefabs = new List<GameObject>();
        public int numToInit = 5;

        [SerializeField] AnimationCurve enemyCountProgression = new AnimationCurve();
        [SerializeField] List<Enemy> enemiesAlive = new List<Enemy>();

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
                DontDestroyOnLoad(this.gameObject);
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
            SpawnEnemies(numToInit);
        }

        protected override void InitNetworkVariablesList()
        {
            
        }
        public void SpawnEnemies(int round)
        {
            _numberOfEnemiesToSpwan = GetNumberOfEnemiesToSpawn(1);
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
                    Transform p = GetRadomSpawnPoint();
                    ServerSpawnEnemy(p.position);
                    _numberOfEnemiesToSpwan--;
                    _spawnTimer = _spawnRate;
                    //Debug.Log("Enemies remaining: " + _numberOfEnemiesToSpwan);
                    OnEnemyCountUpdate?.Invoke(enemiesAlive.Count);
                }
            }
        }

        void ServerSpawnEnemy(Vector3 spawnPosition)
        {
            Debug.Log("Enemy Manager: spawning enemy!");
            // TODO add probability
            int enemyType = UnityEngine.Random.Range(0, enemiesPrefabs.Count - 1);
            GameObject enemyPrefab = enemiesPrefabs[enemyType];
            
            //GameObject polledObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            MemoryStream changeWeaponMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(changeWeaponMemoryStream);
            
            writer.Write((int)EnemyManagerEvent.SPAWN_ENEMY);
            writer.Write(spawnPosition.x);
            writer.Write(spawnPosition.y);
            writer.Write(spawnPosition.z);
            
            ReplicationHeader changeWeaponHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, changeWeaponMemoryStream.ToArray().Length);
            GameObject enemyGO = NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(enemyPrefab,
                changeWeaponHeader, changeWeaponMemoryStream);

            enemyGO.transform.position = spawnPosition;
            
            if (enemyGO.TryGetComponent(out Enemy enemy)) AddEnemyToList(enemy);
            OnEnemySpawn?.Invoke(enemy);
        }

        void ClientSpawnEnemy(GameObject spawnedEnemy, Vector3 spawnPosition)
        {
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
            else if (enemyManagerEvent == EnemyManagerEvent.DESPAWN_ENEMY)
            {
                
            }
        }

        public override void CallBackDeSpawnObjectOther(NetworkObject objectDestroyed, BinaryReader reader, long timeStamp, int lenght)
        {
            base.CallBackDeSpawnObjectOther(objectDestroyed, reader, timeStamp, lenght);
        }

        void AddEnemyToList(Enemy enemy)
        {
            enemiesAlive.Add(enemy);
            enemy.onDeath.AddListener(RemoveEnemyFromList);
        }

        void RemoveEnemyFromList(Enemy enemy)
        {
            enemiesAlive.Remove(enemy);
            enemy.onDeath.RemoveListener(RemoveEnemyFromList);
            Debug.Log("enemies alive: " + enemiesAlive.Count);
            OnEnemyCountUpdate?.Invoke(enemiesAlive.Count);
            OnEnemyRemove?.Invoke(enemy);
        }

        public void KillAllEnemies()
        {
            // Execute logic of enemy manager only in server
            if (!isHost) return;
            
            GameObject bombObject = new GameObject("NUKE");
            NuclearBomb nuke = bombObject.AddComponent<NuclearBomb>();
            nuke.Damage = 100000;
            foreach (Enemy enemy in enemiesAlive.ToArray())
            {
                if (enemy.TryGetComponent(out HealthSystem healthSystem))
                {
                    healthSystem.TakeDamage(nuke, Vector3.zero, gameObject);
                }
            }

            nuke.RaiseDamageDealthEvent(gameObject);
        }

        int GetNumberOfEnemiesToSpawn(int round)
        {
            return Mathf.FloorToInt(enemyCountProgression.Evaluate(round));
        }

        Transform GetRadomSpawnPoint()
        {
            return _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Count)];
        }

        public void AddSpawnpoint(Transform spawnPoint)
        {
            _spawnPoints.Add(spawnPoint);
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
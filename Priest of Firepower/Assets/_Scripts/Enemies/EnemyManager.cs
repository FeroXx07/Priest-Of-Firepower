using System.Collections.Generic;
using _Scripts.Object_Pool;
using _Scripts.Power_Ups;
using UnityEngine;
using System;

namespace _Scripts.Enemies
{
    public class EnemyManager : GenericSingleton<EnemyManager>
    {
        List<Transform> _spawnPoints = new List<Transform>();
        public List<GameObject> enemiesPrefabs = new List<GameObject>();
        public int numToInit = 5;

        [SerializeField] AnimationCurve enemyCountProgression = new AnimationCurve();
        // [SerializeField] float spawnFrequency = 0.5f;
        [SerializeField] List<Enemy> enemiesAlive = new List<Enemy>();

        //current number of enemies to spanw on the current wave
        int _numberOfEnemiesToSpwan = 0;

        float _spawnRate  = 0.5f;
        float _spawnTimer;

        public Action<int> OnEnemyCountUpdate;
        public Action<Enemy> OnEnemySpawn;
        public Action<Enemy> OnEnemyRemove;

        public void SpawnEnemies(int round)
        {
            _numberOfEnemiesToSpwan = GetNumberOfEnemiesToSpawn(round);
            Debug.Log("Enemies remaining: " + _numberOfEnemiesToSpwan);
        }

        private void Update()
        {
            if (_numberOfEnemiesToSpwan > 0)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0)
                {
                    Transform p = GetRadomSpawnPoint();
                    SpawnEnemy(p.position);
                    _numberOfEnemiesToSpwan--;
                    _spawnTimer = _spawnRate;
                    //Debug.Log("Enemies remaining: " + _numberOfEnemiesToSpwan);
                    OnEnemyCountUpdate?.Invoke(enemiesAlive.Count);

                }
            }
        }

        GameObject SpawnEnemy(Vector3 spawnPosition)
        {
            // TODO add probability
            int enemyType = UnityEngine.Random.Range(0, enemiesPrefabs.Count - 1);
            GameObject enemyPrefab = enemiesPrefabs[enemyType];
            GameObject polledObj = PoolManager.Instance.Pull(enemyPrefab, spawnPosition);

            if (polledObj.TryGetComponent(out Enemy enemy))
                AddEnemyToList(enemy);


            OnEnemySpawn?.Invoke(enemy);

            return polledObj;
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
            Debug.Log("enemies alive: " + enemiesAlive.Count );
            OnEnemyCountUpdate?.Invoke(enemiesAlive.Count);
            OnEnemyRemove?.Invoke(enemy);
        }

        public void KillAllEnemies()
        {
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
            //increase number of enemies 
            //TODO improve function
            // return Mathf.RoundToInt(Mathf.Sqrt(Mathf.Exp(round)));
            return  Mathf.FloorToInt( enemyCountProgression.Evaluate(round));
        }

        Transform GetRadomSpawnPoint()
        {
            return _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Count)];
        }
        public void AddSpawnpoint(Transform spawnPoint)
        {
            _spawnPoints.Add(spawnPoint);
        }
        public int GetEnemiesAlive() { return enemiesAlive.Count; }
        public int GetEnemiesCountLeft() { return _numberOfEnemiesToSpwan; }
    }
}

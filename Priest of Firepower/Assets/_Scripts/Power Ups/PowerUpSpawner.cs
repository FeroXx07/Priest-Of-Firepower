using System.Collections.Generic;
using System.IO;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using UnityEngine;

namespace _Scripts.Power_Ups
{
    public class PowerUpSpawner : NetworkBehaviour
    {
        [SerializeField] private float spawnTime = 6;
        [SerializeField] private float counter = 0;
        [SerializeField] private bool hasPowerUp = false;
        [SerializeField] private PowerUpBase _currentPowerUp;
        public List<GameObject> powerUpPrefabs;
        protected override void InitNetworkVariablesList()
        {
           
        }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
        }

        public override void Update()
        {
            if (!isHost) return;
            if (!hasPowerUp)
            {
                counter += Time.deltaTime;
                if (counter >= spawnTime)
                {
                    counter = 0.0f;
                    SpawnPowerUp();
                }
            }
            else
            {
                if (_currentPowerUp != null && _currentPowerUp.pickedUp)
                {
                    hasPowerUp = false;
                }
            }
        }

        void SpawnPowerUp()
        {
            hasPowerUp = true;
            int powerUpType = UnityEngine.Random.Range(0, powerUpPrefabs.Count);
            GameObject powerUpPrefab = powerUpPrefabs[powerUpType];
            Debug.Log($"PowerUpSpawner: Spawning {powerUpType}");

            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memoryStream);
            writer.Write(powerUpPrefab.name);
            ReplicationHeader spawnerObjHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, memoryStream.ToArray().Length);
            _currentPowerUp = NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(powerUpPrefab,
                spawnerObjHeader, memoryStream).GetComponent<PowerUpBase>();

            _currentPowerUp.transform.position = transform.position;
        }

        public override void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader, long timeStamp, int lenght)
        {
            Debug.Log("PowerUpSpawner: Spawning in client");
            objectSpawned.transform.position = transform.position;
        }
    }
}

using System.IO;
using _Scripts.Enemies;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using UnityEngine;

namespace _Scripts.Power_Ups
{
    public class PowerUpNuke : PowerUpBase
    {
        public GameObject nukePrefab;
        public GameObject nukeReference;
        protected override void ApplyPowerUpServer()
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memoryStream);
            writer.Write("Nuke");
            ReplicationHeader spawnerObjHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, memoryStream.ToArray().Length);
            nukeReference = NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(nukePrefab,
                spawnerObjHeader, memoryStream);
        }
        
        public override void Update()
        {
            base.Update();
            
            if (pickedUp)
            {
                powerUpCount += Time.deltaTime;
                if (powerUpCount >= powerUpTime)
                {
                    powerUpCount = -10.0f;
                    NuclearBomb nuke = nukeReference.GetComponent<NuclearBomb>();
                    if (nuke)
                    {
                        nuke.Damage = 100000;
                        nuke.KillAllEnemies();
                    }
                }
            }
        }

        public override void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader, long timeStamp, int lenght)
        {
            string typeName = reader.ReadString();
            if (typeName.Equals("Nuke"))
            {
            }
        }
    }
}

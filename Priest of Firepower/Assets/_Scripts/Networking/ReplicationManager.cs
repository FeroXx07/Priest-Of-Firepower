using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace _Scripts.Networking
{
    public enum ReplicationAction
    {
        CREATE,
        UPDATE,
        DESTROY,
        TRANSFORM,
        EVENT,
        IMPORTANT_EVENT
    }
    public class ReplicationHeader
    {
        public ReplicationHeader(UInt64 id, string objectFullName, ReplicationAction replicationAction, int memoryStreamSize)
        {
            this.id = id;
            this.objectFullName = objectFullName;
            this.replicationAction = replicationAction;
            this.memoryStreamSize = memoryStreamSize;
        }
        public UInt64 id { get; private set; }
        public string objectFullName { get; private set; }
        public ReplicationAction replicationAction { get; private set; }
        
        public int memoryStreamSize { get; private set; }

        public MemoryStream GetSerializedHeader()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(id);
            writer.Write(objectFullName);
            writer.Write((int)replicationAction);
            writer.Write(memoryStreamSize);
            return stream;
        }
        public static ReplicationHeader DeSerializeHeader(BinaryReader reader)
        {
            return new ReplicationHeader(reader.ReadUInt64(), reader.ReadString(), (ReplicationAction)reader.ReadInt32(), reader.ReadInt32());
        }

        public static List<ReplicationHeader> DeSerializeHeadersList(BinaryReader reader, int count)
        {
            List<ReplicationHeader> list = new List<ReplicationHeader>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(DeSerializeHeader(reader));
            }
            return list;
        }
    }
    public class ReplicationItem
    {
        public ReplicationItem(ReplicationHeader header, MemoryStream memoryStream)
        {
            this.header = header;
            this.memoryStream = memoryStream;
        }
        public ReplicationHeader header { get; private set; }
        public MemoryStream memoryStream { get; private set; }
        public void ReplaceMemoryStream(MemoryStream newStream) => memoryStream = newStream;
    }
    public class ReplicationManager
    {
        public GameObject gameObject;
        public UInt64 id { get; private set; }
        public Dictionary<UInt64, NetworkObject> networkObjectMap = new Dictionary<ulong, NetworkObject>();

        public void InitManager(List<NetworkObject> listNetObj)
        {
            networkObjectMap.Clear();
            foreach (var networkObject in listNetObj)
            {
                RegisterObjectLocally(networkObject);
            }
        }

        public UInt64 RegisterObjectLocally(NetworkObject obj)
        {
            obj.SetNetworkId(id);
            networkObjectMap.Add(id, obj);
            id++;
            return obj.GetNetworkId();
        }

        public UInt64 RegisterObjectFromServer(UInt64 id_, NetworkObject obj)
        {
            obj.SetNetworkId(id_);
            networkObjectMap.Add(id_, obj);
            return id_;
        }

        public void HandleReplication(BinaryReader reader, ulong id, long timeStamp, ulong sequenceNumState, ReplicationAction action, Type type)
        {
            //Debug.Log( $"Network Manager: HandlingNetworkAction: ID: {id}, Action: {action}, Type: {type.FullName}, Stream Position: {reader.BaseStream.Position}");
            switch (action)
            {
                case ReplicationAction.CREATE:
                { 
                    Client_ObjectCreationRegistryRead(id, reader, timeStamp);
                }
                    break;
                case ReplicationAction.UPDATE:
                {
                    if (networkObjectMap.ContainsKey(id))
                    {
                        networkObjectMap[id].HandleNetworkBehaviour(reader, id, timeStamp, sequenceNumState, type);
                    }
                    else
                    {
                        Debug.LogError($"Replication Manager: Network object map does NOT contain ID {id}");
                    }
                }
                    break;
                case ReplicationAction.DESTROY:
                    break;
                case ReplicationAction.TRANSFORM:
                {
                    if (networkObjectMap.ContainsKey(id))
                    {
                        if (networkObjectMap[id].synchronizeTransform)
                            networkObjectMap[id].ReadReplicationTransform(reader, id, timeStamp, sequenceNumState);
                    }
                    else
                    {
                        Debug.LogError($"Replication Manager: Network object map does NOT contain ID {id}");
                    }
                }
                    break;
                case ReplicationAction.EVENT:
                {
                    UInt64 messageSenderId = reader.ReadUInt64();
                    string message = reader.ReadString();
                    NetworkManager.Instance.OnGameEventMessageReceived?.Invoke(messageSenderId, message, timeStamp);
                }
                    break;
                case ReplicationAction.IMPORTANT_EVENT:
                {
                    UInt64 messageSenderId = reader.ReadUInt64();
                    string message = reader.ReadString();
                    NetworkManager.Instance.OnGameEventMessageReceived?.Invoke(messageSenderId, message, timeStamp);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
        
        public GameObject Server_InstantiateNetworkObject(GameObject prefab, ReplicationHeader ownerSpawner, MemoryStream ownerSpawnerData)
        {
            NetworkObject newGo = GameObject.Instantiate<GameObject>(prefab).GetComponent<NetworkObject>();
            UInt64 newId = RegisterObjectLocally(newGo);
            
            // Send replication packet to clients to create this prefab
            MemoryStream outputMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            writer.Write(prefab.name);
            
            MemoryStream headerOwner = ownerSpawner.GetSerializedHeader();
            headerOwner.Position = 0;
            headerOwner.CopyTo(outputMemoryStream);
            ownerSpawnerData.Position = 0;
            ownerSpawnerData.CopyTo(outputMemoryStream);
            
            ReplicationHeader replicationHeader = new ReplicationHeader(newId, this.GetType().FullName, ReplicationAction.CREATE, outputMemoryStream.ToArray().Length);
            Debug.LogWarning($"Network Manager: Sending create, header size {replicationHeader.GetSerializedHeader().Length}, data size {replicationHeader.memoryStreamSize}");
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, outputMemoryStream);
            return newGo.gameObject;
        }
        

        public void Client_ObjectCreationRegistryRead(UInt64 serverAssignedNetObjId, BinaryReader reader, Int64 timeStamp)
        {
            // Read essential data
            string prefabName = reader.ReadString();
            ReplicationHeader spawnerOwnerHeader = ReplicationHeader.DeSerializeHeader(reader);
            
            var prefab = NetworkManager.Instance.instantiatablesPrefabs.First(p => p.name == prefabName);
            NetworkObject newGo = GameObject.Instantiate(prefab).GetComponent<NetworkObject>();
            RegisterObjectFromServer(serverAssignedNetObjId, newGo);
            Debug.LogWarning($"Replication Manager: Creating object {prefabName} ID {id}, size: {spawnerOwnerHeader.memoryStreamSize}");

            // An temporary exception
            if (prefabName.Equals("PlayerPrefab"))
            {
                string clientName = reader.ReadString();
                UInt64 clientId = reader.ReadUInt64();
                Player.Player player = newGo.GetComponent<Player.Player>();
                player.SetName(clientName);
                player.SetPlayerId(clientId);
                newGo.gameObject.name = clientName;
                NetworkManager.Instance.GetClient()._clientData.playerInstantiated = true;
                return;
            }
            
            // Call init functions in both the object spawned and the spawner of that object.
            // For example if a bullet is spawned, on the bullet OnNetworkSpawn(weapon...) is called and on the weapon CallBackSpawnObjectOther(bullet...);
            Type type = Type.GetType(spawnerOwnerHeader.objectFullName);
            NetworkObject spawnerGameObject = networkObjectMap[spawnerOwnerHeader.id];
            NetworkBehaviour spawnerBehaviour = spawnerGameObject.GetComponent(type) as NetworkBehaviour;
            
            long startPosData = reader.BaseStream.Position;
            if (spawnerBehaviour != null)
            {
                spawnerBehaviour.CallBackSpawnObjectOther(newGo, reader, timeStamp, spawnerOwnerHeader.memoryStreamSize);
            }
            
            NetworkBehaviour[] listToInit = newGo.GetComponents<NetworkBehaviour>();
            foreach (NetworkBehaviour networkBehaviour in listToInit)
            {
                reader.BaseStream.Position = startPosData;
                networkBehaviour.OnNetworkSpawn(spawnerGameObject, reader, timeStamp, spawnerOwnerHeader.memoryStreamSize);
            }

            reader.BaseStream.Position = startPosData;
            reader.BaseStream.Seek(spawnerOwnerHeader.memoryStreamSize, SeekOrigin.Current);
        }
    }
}
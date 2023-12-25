﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace _Scripts.Networking.Replication
{
    public class ReplicationManager
    {
        public GameObject gameObject;
        public UInt64 idReplication { get; private set; }
        public Dictionary<UInt64, NetworkObject> networkObjectMap = new Dictionary<ulong, NetworkObject>();
        public List<UInt64> unRegisteredNetIds = new List<UInt64>();

        public void InitManager(List<NetworkObject> listNetObj)
        {
            networkObjectMap.Clear();
            foreach (var networkObject in listNetObj)
            {
                RegisterObjectServer(networkObject);
            }
        }

        public UInt64 RegisterObjectServer(NetworkObject obj)
        {
            obj.SetNetworkId(idReplication);
            networkObjectMap.Add(idReplication, obj);
            idReplication++;
            return obj.GetNetworkId();
        }

        public UInt64 RegisterObjectClient(UInt64 id_, NetworkObject obj)
        {
            obj.SetNetworkId(id_);
            networkObjectMap.Add(id_, obj);
            return id_;
        }

        public void UnRegisterObjectServer(NetworkObject obj)
        {
            unRegisteredNetIds.Add(obj.GetNetworkId());
            networkObjectMap.Remove(obj.GetNetworkId());
        }
        public void UnRegisterObjectClient(UInt64 id_)
        {
            unRegisteredNetIds.Add(id_);
            networkObjectMap.Remove(id_);
        }

        public void HandleReplication(BinaryReader reader, ReplicationHeader header, long timeStamp, ulong sequenceNumState)
        {
            //Debug.Log( $"Network Manager: HandlingNetworkAction: ID: {id}, Action: {action}, Type: {type.FullName}, Stream Position: {reader.BaseStream.Position}");
            switch (header.replicationAction)
            {
                case ReplicationAction.CREATE:
                { 
                    Client_ObjectCreationRegistryRead(header.id, reader, timeStamp);
                }
                    break;
                case ReplicationAction.UPDATE:
                {
                    if (networkObjectMap.ContainsKey(header.id))
                    {
                        networkObjectMap[header.id].HandleNetworkBehaviour(reader, header, timeStamp, sequenceNumState);
                    }
                    else if (unRegisteredNetIds.Contains(header.id))
                    {
                        Debug.LogWarning($"Replication Manager: Network object map trying to access unregistered ID {header.id}");
                        reader.BaseStream.Seek(header.memoryStreamSize, SeekOrigin.Current);
                    }
                    else
                    {
                        Debug.LogError($"Replication Manager: Network object map does NOT contain ID {header.id}");
                        reader.BaseStream.Seek(header.memoryStreamSize, SeekOrigin.Current);
                    }
                }
                    break;
                case ReplicationAction.DESTROY:
                {
                    bool successfulDespawn = Client_DeSpawnNetworkObject(header.id,reader,timeStamp);
                    
                    if (!successfulDespawn)
                        reader.BaseStream.Seek(header.memoryStreamSize, SeekOrigin.Current);
                }
                    break;
                case ReplicationAction.TRANSFORM:
                {
                    if (networkObjectMap.ContainsKey(header.id))
                    {
                        if (networkObjectMap[header.id].synchronizeTransform)
                            networkObjectMap[header.id].ReadReplicationTransform(reader, header.id, timeStamp, sequenceNumState);
                    }
                    else if (unRegisteredNetIds.Contains(header.id))
                    {
                        Debug.LogWarning($"Replication Manager: Network object map trying to access unregistered ID {header.id}");
                        reader.BaseStream.Seek(header.memoryStreamSize, SeekOrigin.Current);
                    }
                    else
                    {
                        Debug.LogError($"Replication Manager: Network object map does NOT contain ID {header.id}");
                        reader.BaseStream.Seek(header.memoryStreamSize, SeekOrigin.Current);
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
                    throw new ArgumentOutOfRangeException(nameof(header.replicationAction), header.replicationAction, null);
            }
        }
        
        public GameObject Server_InstantiateNetworkObject(GameObject prefab, ReplicationHeader spawnerObjHeader, MemoryStream ownerSpawnerData)
        {
            NetworkObject newGo = GameObject.Instantiate<GameObject>(prefab).GetComponent<NetworkObject>();
            UInt64 newId = RegisterObjectServer(newGo);
            
            // Send replication packet to clients to create this prefab
            MemoryStream outputMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            writer.Write(prefab.name);
            
            MemoryStream headerOwner = spawnerObjHeader.GetSerializedHeader();
            headerOwner.Position = 0;
            headerOwner.CopyTo(outputMemoryStream);
            ownerSpawnerData.Position = 0;
            ownerSpawnerData.CopyTo(outputMemoryStream);
            
            ReplicationHeader replicationHeader = new ReplicationHeader(newId, this.GetType().FullName, ReplicationAction.CREATE, outputMemoryStream.ToArray().Length);
            // Debug.LogWarning($"Replication Manager: Sending spawn. Name: {prefab.name} ID: {newId}, Obj: {this.GetType().FullName}, " +
            //                  $"Header Size: {replicationHeader.GetSerializedHeader().Length}, Data size {outputMemoryStream.ToArray().Length}");            
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, outputMemoryStream);
            return newGo.gameObject;
        }
        

        public void Client_ObjectCreationRegistryRead(UInt64 serverAssignedNetObjId, BinaryReader reader, Int64 timeStamp)
        {
            // Read essential data
            string prefabName = reader.ReadString();
            ReplicationHeader spawnerOwnerHeader = ReplicationHeader.DeSerializeHeader(reader);
            
            var prefab = NetworkManager.Instance.instantiatablesPrefabs.First(prefab => prefab.name == prefabName);
            NetworkObject newGo = GameObject.Instantiate(prefab).GetComponent<NetworkObject>();
            RegisterObjectClient(serverAssignedNetObjId, newGo);
            // Debug.LogWarning($"Replication Manager: Receiving spawn. Name: {prefab.name} ID: {serverAssignedNetObjId}, Obj: {this.GetType().FullName}, " +
            //                  $"Header Size: {spawnerOwnerHeader.GetSerializedHeader().Length}, Data size {spawnerOwnerHeader.memoryStreamSize}");  
            
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
                networkBehaviour.OnClientNetworkSpawn(spawnerGameObject, reader, timeStamp, spawnerOwnerHeader.memoryStreamSize);
            }

            reader.BaseStream.Position = startPosData;
            reader.BaseStream.Seek(spawnerOwnerHeader.memoryStreamSize, SeekOrigin.Current);
        }

        public void Server_DeSpawnNetworkObject(NetworkObject nObjToDespawn, ReplicationHeader objDestroyer, MemoryStream objDestroyerData)
        {
            // Check if it has already been despawned
            if (unRegisteredNetIds.Contains(nObjToDespawn.GetNetworkId()))
                return;
            
            // Send replication packet to clients to remove this object
            MemoryStream outputMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            MemoryStream headerOwner = objDestroyer.GetSerializedHeader();
            headerOwner.Position = 0;
            headerOwner.CopyTo(outputMemoryStream);
            objDestroyerData.Position = 0;
            objDestroyerData.CopyTo(outputMemoryStream);
            
            ReplicationHeader replicationHeader = new ReplicationHeader(nObjToDespawn.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, outputMemoryStream.ToArray().Length);
            // Debug.LogWarning($"Replication Manager: Sending despawn. ID: {nObjToDespawn.GetNetworkId()}, Obj: {this.GetType().FullName}, " +
            //                  $"Header Size: {replicationHeader.GetSerializedHeader().Length}, Data size {outputMemoryStream.ToArray().Length}");
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, outputMemoryStream);
            
            UnRegisterObjectServer(nObjToDespawn);
            //GameObject.Destroy(nObjToDespawn.gameObject);
        }

        public bool Client_DeSpawnNetworkObject(UInt64 networkObjectId, BinaryReader reader, Int64 timeStamp)
        {
            ReplicationHeader deSpawnerOwnerHeader = ReplicationHeader.DeSerializeHeader(reader);
            // Debug.LogWarning($"Replication Manager: Receiving despawn. ID: {networkObjectId}, Obj: {this.GetType().FullName}, " +
            //                  $"Header Size: {deSpawnerOwnerHeader.GetSerializedHeader().Length}, Data size {deSpawnerOwnerHeader.memoryStreamSize}");
            Type type = Type.GetType(deSpawnerOwnerHeader.objectFullName);

            if (networkObjectMap.TryGetValue(networkObjectId, out NetworkObject objectToDestroy) && networkObjectMap.TryGetValue(deSpawnerOwnerHeader.id, out NetworkObject objectDestroyee))
            {
                NetworkBehaviour deSpawnerBehaviour = objectDestroyee.GetComponent(type) as NetworkBehaviour;
            
                long startPosData = reader.BaseStream.Position;
                if (deSpawnerBehaviour != null)
                {
                    deSpawnerBehaviour.CallBackDeSpawnObjectOther(objectToDestroy, reader, timeStamp, deSpawnerOwnerHeader.memoryStreamSize);
                }
            
                NetworkBehaviour[] listToInit = objectToDestroy.GetComponents<NetworkBehaviour>();
                foreach (NetworkBehaviour networkBehaviour in listToInit)
                {
                    reader.BaseStream.Position = startPosData;
                    networkBehaviour.OnClientNetworkDespawn(objectDestroyee, reader, timeStamp, deSpawnerOwnerHeader.memoryStreamSize);
                }

                reader.BaseStream.Position = startPosData;
                reader.BaseStream.Seek(deSpawnerOwnerHeader.memoryStreamSize, SeekOrigin.Current);
            
                UnRegisterObjectClient(networkObjectId);
            }
            else
            {
                return false;
            }
            
            return true;
        }
    }
}
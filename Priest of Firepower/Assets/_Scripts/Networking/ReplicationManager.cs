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
        TRANSFORM
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
                    networkObjectMap[id].HandleNetworkBehaviour(reader, id, timeStamp, sequenceNumState, type);
                }
                    break;
                case ReplicationAction.DESTROY:
                    break;
                case ReplicationAction.TRANSFORM:
                {
                    if (networkObjectMap[id].synchronizeTransform)
                        networkObjectMap[id].ReadReplicationTransform(reader, id, timeStamp, sequenceNumState);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
        
          public GameObject Server_InstantiateNetworkObject(GameObject prefab, ClientData clientData)
        {
            NetworkObject newGo = GameObject.Instantiate<GameObject>(prefab).GetComponent<NetworkObject>();
            UInt64 newId = RegisterObjectLocally(newGo);
            Server_ObjectCreationRegistrySend(newId, prefab, clientData);
            return newGo.gameObject;
        }

        public void Server_ObjectCreationRegistrySend(UInt64 newNetObjId, GameObject prefab, ClientData clientData)
        {
            // Send replication packet to clients to create this prefab
            MemoryStream outputMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(newNetObjId);
            writer.Write((int)ReplicationAction.CREATE);
            writer.Write(prefab.name);
            writer.Write(clientData.userName);
            writer.Write(clientData.id);
            NetworkManager.Instance.AddStateStreamQueue(outputMemoryStream);
        }

        public void Client_ObjectCreationRegistryRead(UInt64 serverAssignedNetObjId, BinaryReader reader, Int64 timeStamp)
        {
            string prefabName = reader.ReadString();
            string clientName = reader.ReadString();
            UInt64 clientId = reader.ReadUInt64();
            var prefab = NetworkManager.Instance.instantiatablesPrefabs.First(p => p.name == prefabName);
            NetworkObject newGo = GameObject.Instantiate(prefab).GetComponent<NetworkObject>();
            RegisterObjectFromServer(serverAssignedNetObjId, newGo);
            if (prefabName.Equals("PlayerPrefab"))
            {
                Player.Player player = newGo.GetComponent<Player.Player>();
                player.SetName(clientName);
                player.SetPlayerId(clientId);
                newGo.gameObject.name = clientName;
                NetworkManager.Instance.GetClient()._clientData.playerInstantiated = true;
            }
        }
    }
}
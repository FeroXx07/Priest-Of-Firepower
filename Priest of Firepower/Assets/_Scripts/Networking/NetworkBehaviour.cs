    using System;
    using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
    using UnityEngine.Serialization;

    namespace _Scripts.Networking
{
    [RequireComponent(typeof(NetworkObject))]
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        [SerializeField] protected bool isHost => NetworkManager.Instance.IsHost();
        [SerializeField] protected bool isClient => NetworkManager.Instance.IsClient();
        
        [SerializeField] protected bool showDebugInfo = true;
        #region TickInfo
        [Header("NetworkBehaviour TickInfo")]
        public float tickRateBehaviour = 1.0f; // Network writes inside a second.
        private float _tickCounter = 0.0f;
        public bool doTickUpdates = true;
        public bool clientSendReplicationData = false;
        #endregion
        
        #region TrackingInfo
        protected ChangeTracker BITTracker;
        protected NetworkObject NetworkObject;
        protected List<INetworkVariable> NetworkVariableList = new List<INetworkVariable>();
        protected abstract void InitNetworkVariablesList();
        protected List<INetworkVariable> GetNetworkVariables()
        {
            return NetworkVariableList;
        }
        #endregion
        public virtual void OnEnable()
        {
            NetworkManager.Instance.OnGameEventMessageReceived += ListenToMessages;
        }
        public virtual void OnDisable()
        {
            NetworkManager.Instance.OnGameEventMessageReceived -= ListenToMessages;
        }

        ///<summary>
        /// returns the id of the networkobjet this behaviour belong to
        /// </summary>
        
        public UInt64 GetObjId()
        {
            return GetComponent<NetworkObject>().GetNetworkId();
        }
        
        #region Serialization

        /// <summary>
        /// Return true if stream has been filled with data, false if not.
        /// </summary>
        /// <param name="outputMemoryStream">Stream to fill with serialization data</param>
        /// <param name="action">The network action header to include</param>
        /// <returns>True: stream has been filled. False: stream has not been filled</returns>
        protected virtual ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            // [Object State] -- We are here! -- [Object Class][Object ID][NetworkAction][Bitfield Lenght][Bitfield Data][DATA I][Data J]...[Object Class][Object ID][NetworkAction][Bitfield Lenght]...
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            // Serialize
            
            BitArray bitfield = BITTracker.GetBitfield();
            for (int i = 0; i < bitfield.Length; i++)
            {
                if (NetworkVariableList[i].IsDirty)
                {
                    BITTracker.TrackChange(i);
                }
            }

            int fieldCount = bitfield.Length;
            
            writer.Write(fieldCount);
            
            byte[] bitfieldBytes = new byte[(fieldCount + 7) / 8];
            bitfield.CopyTo(bitfieldBytes, 0);
            writer.Write(bitfieldBytes);

            int count = 0;
            count = SerializeFieldsData(bitfield, count, writer);

            // If no fields have changed don't send the stream.
            if (count == 0)
            {
                return null;
            }
            
            if (showDebugInfo)
                Debug.Log($"ID: {NetworkObject.GetNetworkId()}, Trying to send {count} variables from network behavior: {name}");
            
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);

            // Detrack all variables and return stream to the 
            BITTracker.SetAll(false);
            return replicationHeader;
        }

        private int SerializeFieldsData(BitArray bitfield, int count, BinaryWriter writer)
        {
            for (int i = 0; i < bitfield.Length; i++)
            {
                if (bitfield[i] != NetworkVariableList[i].IsDirty)
                {
                    Debug.LogWarning("Mismatch in bitfield and isDirty!!");
                }

                if (bitfield[i] || NetworkVariableList[i].IsDirty)
                {
                    count++;
                    NetworkVariableList[i].WriteInBinaryWriter(writer);
                }
            }
            return count;
        }

        /// <summary>
        /// Takes a binary reader an de serializes.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="position"></param>
        /// <returns>True: stream has been read correctly. False: stream has not been read corrcetly.</returns>
        public virtual bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            reader.BaseStream.Position = position;
            // [Object State][Object Class] -- We are here! -- [Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...[Object Class][Object ID][Bitfield Lenght]...
            if (showDebugInfo)
                Debug.Log($"ID: {NetworkObject.GetNetworkId()}, Receiving data network behavior: {name}");

            int fieldCount = BITTracker.GetBitfield().Length;
            int receivedFieldCount = reader.ReadInt32();
            if (receivedFieldCount != fieldCount)
            {
                Debug.LogError("Mismatch in the count of fields");
                return false;
            }
            
            byte[] receivedBitfieldBytes = reader.ReadBytes((fieldCount + 7) / 8);
            BitArray receivedBitfield = new BitArray(receivedBitfieldBytes);

            DeSerializeFieldsData(reader, receivedFieldCount, receivedBitfield);
            return true;
        }
        
        private void DeSerializeFieldsData(BinaryReader reader, int receivedFieldCount, BitArray receivedBitfield)
        {
            for (int i = 0; i < receivedFieldCount; i++)
            {
                if (receivedBitfield[i])
                {
                    NetworkVariableList.ElementAt(i).ReadFromBinaryReader(reader);
                }
            }
        }
        #endregion
        public virtual void OnNetworkSpawn(NetworkObject spawner, BinaryReader reader, Int64 timeStamp, int lenght) { }
        public virtual void OnNetworkDespawn(NetworkObject destroyer, BinaryReader reader,  Int64 timeStamp, int lenght) { }
        public virtual void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader,  Int64 timeStamp, int lenght){}
        public virtual void CallBackDeSpawnObjectOther(NetworkObject objectDestroyed, BinaryReader reader,  Int64 timeStamp, int lenght){}
        public virtual void Awake()
        {
            if (TryGetComponent<NetworkObject>(out NetworkObject) == false)
            {
                Debug.LogWarning("A NetworkBehaviour needs a NetworkObject");
            }
        }
        protected void SendReplicationData(ReplicationAction action)
        {
            // Cannot send data if no network manager
            if (NetworkManager.Instance == false && NetworkObject == false)
            {
                Debug.LogWarning("No NetworkManager or NetworkObject");
            }

            if (NetworkManager.Instance.IsClient())
            {
                if (clientSendReplicationData == false)
                    return;
            }
            
            MemoryStream stream = new MemoryStream();
            ReplicationHeader replicationHeader = null;
            switch (action)
            {
                case ReplicationAction.CREATE:
                {
                    BITTracker.SetAll(true);
                    replicationHeader = WriteReplicationPacket(stream, ReplicationAction.CREATE);
                }
                    break;
                case ReplicationAction.UPDATE:
                {
                    replicationHeader = WriteReplicationPacket(stream, ReplicationAction.UPDATE);
                }
                    break;
                case ReplicationAction.DESTROY:
                {
                    replicationHeader = WriteReplicationPacket(stream, ReplicationAction.DESTROY);
                }
                    break;
                default:
                {
                    replicationHeader = WriteReplicationPacket(stream, ReplicationAction.UPDATE);
                }
                    break;
            }
            
            if (replicationHeader == null) return;
            if (showDebugInfo) Debug.Log($"{gameObject.name}.{GetType().Name} -> Sending data: with size {stream.ToArray().Length} and {action}");
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, stream);
        }
        public void SendReplicationData(ReplicationAction action,MemoryStream stream)
        {
            // Cannot send data if no network manager
            if (NetworkManager.Instance == false && NetworkObject == false)
            {
                Debug.LogWarning("No NetworkManager or NetworkObject");
            }
            ReplicationHeader header = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName,
                action, stream.ToArray().Length);

            if (showDebugInfo) Debug.Log($"{gameObject.name}.{GetType().Name} -> Sending data: with size {stream.ToArray().Length} and {action}");
            NetworkManager.Instance.AddStateStreamQueue(header, stream);
        }
        public virtual void Update()
        {
            if (!doTickUpdates)
                return;
            
            // Send Write to state buffer
            float finalRate = 1.0f / tickRateBehaviour;
            if (_tickCounter >= finalRate )
            {
                SendReplicationData(ReplicationAction.UPDATE);
                _tickCounter = 0.0f;
            }
            _tickCounter += Time.deltaTime;
        }
        protected void SendInput(MemoryStream dataStream, bool Reliable)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            writer.Write(NetworkManager.Instance.getId);

            writer.Write(dataStream.ToArray());

            if(Reliable)
            {
                NetworkManager.Instance.AddReliableInputStreamQueue(stream);
            }
            else
            {
                NetworkManager.Instance.AddInputStreamQueue(stream);
            }
        }
        public virtual void SendInputToServer(){}
        public virtual void ReceiveInputFromClient(InputPacketHeader header,BinaryReader reader){}
        public virtual void SendInputToClients(){}
        public virtual void ReceiveInputFromServer(InputPacketHeader header,BinaryReader reader){}
        public virtual void SendStringMessage(string message, bool isImportant = true)
        {
            if (NetworkManager.Instance.IsHost())
                return;
            
            Debug.Log($"Sending message: {message}");
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            ReplicationAction action = isImportant ? ReplicationAction.IMPORTANT_EVENT : ReplicationAction.EVENT;
            writer.Write(NetworkManager.Instance.getId);
            writer.Write(message);
            
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, stream.ToArray().Length);
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, stream);
        }

        public virtual void ListenToMessages(UInt64 senderId, string message, long timeStamp){}
    }
}
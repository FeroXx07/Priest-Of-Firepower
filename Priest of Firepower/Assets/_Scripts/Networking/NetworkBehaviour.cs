    using System;
    using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace _Scripts.Networking
{
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        [SerializeField] private bool showDebugInfo = true;
        #region TickInfo
        public float tickRate = 10.0f; // Network writes inside a second.
        private float _tickCounter = 0.0f;
        public bool doTickUpdates = true;
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
        
        #region Serialization
        /// <summary>
        /// Return true if stream has been filled with data, false if not.
        /// </summary>
        /// <param name="outputMemoryStream">Stream to fill with serialization data</param>
        /// <param name="action">The network action header to include</param>
        /// <returns>True: stream has been filled. False: stream has not been filled</returns>
        protected virtual bool Write(MemoryStream outputMemoryStream, NetworkAction action)
        {
            // [Object State][Object Class] -- We are here! -- [Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...[Object Class][Object ID][Bitfield Lenght]...
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            
            // Serialize
            Type objectType = this.GetType();
            writer.Write(objectType.FullName);
            writer.Write(NetworkObject.GetNetworkId());
            writer.Write((int)action);
            
            BitArray bitfield = BITTracker.GetBitfield();
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
                return false;
            }
            
            if (showDebugInfo)
                Debug.Log($"ID: {NetworkObject.GetNetworkId()}, Trying to send {count} variables from network behavior: {name}");
            
            // Detrack all variables and return stream to the 
            BITTracker.SetAll(false);
            return true;
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
        /// <returns>True: stream has been read correctly. False: stream has not been read corrcetly.</returns>
        public virtual bool Read(BinaryReader reader) 
        {
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
        public virtual void OnNetworkSpawn() { }
        public virtual void OnNetworkDespawn() { }

        public virtual void Awake()
        {
            if (TryGetComponent<NetworkObject>(out NetworkObject) == false)
            {
                Debug.LogWarning("A NetworkBehaviour needs a NetworkObject");
            }
        }
        public void SendData(NetworkAction action)
        {
            // Cannot send data if no network manager
            if (NetworkManager.Instance == false && NetworkObject == false)
            {
                Debug.LogWarning("No NetworkManager or NetworkObject");
            }
            
            MemoryStream stream = new MemoryStream();
            switch (action)
            {
                case NetworkAction.CREATE:
                {
                    BITTracker.SetAll(true);
                    Write(stream, NetworkAction.CREATE);
                }
                    break;
                case NetworkAction.UPDATE:
                {
                    if(Write(stream, NetworkAction.UPDATE) == false)
                        return;
                }
                    break;
                case NetworkAction.DESTROY:
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    Type objectType = this.GetType();
                    writer.Write(objectType.FullName);
                    writer.Write(NetworkObject.GetNetworkId());
                    writer.Write((int)NetworkAction.DESTROY);
                }
                    break;
                case NetworkAction.EVENT:
                    break;
                default:
                    if(Write(stream, NetworkAction.UPDATE) == false)
                        return;
                    break;
            }

            if (showDebugInfo) Debug.Log($"{gameObject.name}.{GetType().Name} -> Sending data: with size {stream.ToArray().Length} and {action}");
            NetworkManager.Instance.AddStateStreamQueue(stream);
        }
        public virtual void Update()
        {
            if (!doTickUpdates)
                return;
            
            // Send Write to state buffer
            float finalRate = 1.0f / tickRate;
            if (_tickCounter >= finalRate )
            {
                SendData(NetworkAction.UPDATE);
                _tickCounter = 0.0f;
            }
            _tickCounter += Time.deltaTime;
        }
    }
}
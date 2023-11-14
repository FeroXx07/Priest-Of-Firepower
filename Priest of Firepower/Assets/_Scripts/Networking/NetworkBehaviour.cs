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
        public float tickRate = 10.0f; // Network writes inside a second.
        private float _tickCounter = 0.0f;
        public bool doTickUpdates = true;
        
        protected ChangeTracker BITTracker;
        protected NetworkObject NetworkObject;
        protected List<INetworkVariable> NetworkVariableList = new List<INetworkVariable>();

        #region Serialization
        protected abstract void InitNetworkVariablesList();
        protected List<INetworkVariable> GetNetworkVariables()
        {
            return NetworkVariableList;
        }

    
        protected virtual MemoryStream Write(MemoryStream outputMemoryStream, NetworkAction action)
        {
            // [Object State][Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]... <- End of an object packet
            //[Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...[Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            Type objectType = this.GetType();
            writer.Write(objectType.AssemblyQualifiedName);
            writer.Write(NetworkObject.GetNetworkId());
            writer.Write((int)action);
            
            // Serialize the changed fields using the bitfield
            BitArray bitfield = BITTracker.GetBitfield();
            int fieldCount = bitfield.Length;
            // Write the count of fields
            writer.Write(fieldCount);
            // Write the bitfield
            byte[] bitfieldBytes = new byte[(fieldCount + 7) / 8];
            bitfield.CopyTo(bitfieldBytes, 0);
            writer.Write(bitfieldBytes);

            foreach (var variable in NetworkVariableList)
            {
                if(variable.IsDirty)
                {
                    variable.WriteInBinaryWriter(writer);
                }
            }
            
            BITTracker.SetAll(false);
            return outputMemoryStream;
        }

        public virtual void Read(BinaryReader reader) 
        {
            reader.BaseStream.Position = 0;
            // [Object State][Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]... <- End of an object packet
            //[Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...[Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...
            int fieldCount = BITTracker.GetBitfield().Length;
            int receivedFieldCount = reader.ReadInt32();
            if (receivedFieldCount != fieldCount)
            {
                Debug.LogError("Mismatch in the count of fields");
            }
            // Read the bitfield from the input stream
            byte[] receivedBitfieldBytes = reader.ReadBytes((fieldCount + 7) / 8);
            BitArray receivedBitfield = new BitArray(receivedBitfieldBytes);

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
                    Write(stream, NetworkAction.UPDATE);
                } 
                    break;
                case NetworkAction.DESTROY:
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    Type objectType = this.GetType();
                    writer.Write(objectType.AssemblyQualifiedName);
                    writer.Write(NetworkObject.GetNetworkId());
                    writer.Write((int)NetworkAction.DESTROY);
                }
                    break;
                case NetworkAction.EVENT:
                    break;
                default:
                    Write(stream, NetworkAction.UPDATE);
                    break;
            }
        
            // MemoryStream Write(MemoryStream outputStream);

            // Send MemoryStream to netowrk manager buffer

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

    public class ChangeTracker
    {
        private readonly BitArray _bitfield;

        public ChangeTracker(int numberOfFields)
        {
            // Initialize the bitfield with the specified number of fields
            _bitfield = new BitArray(numberOfFields);
        }

        public void TrackChange(int fieldIndex)
        {
            // Set the corresponding bit to 1 to indicate the field has changed
            _bitfield.Set(fieldIndex, true);
        }
        public void DeTrackChange(int fieldIndex)
        {
            // Set the corresponding bit to 1 to indicate the field has changed
            _bitfield.Set(fieldIndex, false);
        }
        public void SetAll(bool value)
        {
            _bitfield.SetAll(value);
        }

        public bool HasChanged(int fieldIndex)
        {
            // Check if the corresponding bit is set
            return _bitfield.Get(fieldIndex);
        }

        public BitArray GetBitfield()
        {
            return _bitfield;
        }
    }
}
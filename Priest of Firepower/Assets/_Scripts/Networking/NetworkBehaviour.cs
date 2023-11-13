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
        protected ChangeTracker bitTracker;
        protected NetworkObject networkObject;
        protected List<INetworkVariable> networkVariableList = new List<INetworkVariable>();

        #region Serialization
        protected abstract void InitNetworkVariablesList();
        protected List<INetworkVariable> GetNetworkVariables()
        {
            return networkVariableList;
        }

    
        protected virtual MemoryStream Write(MemoryStream outputMemoryStream)
        {
            // [Object State][Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]... <- End of an object packet
            //[Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...[Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);

            //Type objectType = this.GetType();
            //writer.Write(objectType.AssemblyQualifiedName);
            //writer.Write(networkObject.GetNetworkId());

            // Serialize the changed fields using the bitfield
            BitArray bitfield = bitTracker.GetBitfield();
            int fieldCount = bitfield.Length;
            // Write the count of fields
            writer.Write(fieldCount);
            // Write the bitfield
            byte[] bitfieldBytes = new byte[(fieldCount + 7) / 8];
            bitfield.CopyTo(bitfieldBytes, 0);
            writer.Write(bitfieldBytes);

            foreach (var variable in networkVariableList)
            {
                if(variable.IsDirty)
                {
                    variable.WriteInBinaryWriter(writer);
                }
            }

            return outputMemoryStream;
        }

        public virtual void Read(BinaryReader reader) 
        {
            reader.BaseStream.Position = 0;
            // [Object State][Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]... <- End of an object packet
            //[Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...[Object Class][Object ID][Bitfield Lenght][Bitfield Data][DATA I][Data J]...
            int fieldCount = bitTracker.GetBitfield().Length;
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
                    networkVariableList.ElementAt(i).ReadFromBinaryReader(reader);
                }
            }
        
        }
        #endregion
    

        public virtual void OnNetworkSpawn() { }
        public virtual void OnNetworkDespawn() { }

        public virtual void Awake()
        {
            if (TryGetComponent<NetworkObject>(out networkObject) == false)
            {
                Debug.LogWarning("A NetworkBehaviour needs a NetworkObject");
            }
        }
        public void SendData()
        {
            if (NetworkManager.Instance == false && networkObject == false)
            {
                Debug.LogWarning("No NetworkManager or NetworkObject");
            }

            MemoryStream stream = new MemoryStream();
            Write(stream);
        
            // MemoryStream Write(MemoryStream outputStream);

            // Send MemoryStream to netowrk manager buffer

            NetworkManager.Instance.AddStateStreamQueue(stream);
        }
        private void Update()
        {
            // Send Write to state buffer
            // SendData();
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
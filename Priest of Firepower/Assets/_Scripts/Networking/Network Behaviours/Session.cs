using System;
using System.Collections;
using System.IO;

namespace _Scripts.Networking.Network_Behaviours
{
    public class Session : NetworkBehaviour
    {
        int _clientId;
        string _username;
        bool _isHost;

        public override void Awake()
        {
            base.Awake();
            BITTracker = new ChangeTracker(3);
        }
        public override void Read(BinaryReader reader)
        {

        
            int fieldCount = BITTracker.GetBitfield().Length;
            int receivedFieldCount = reader.ReadInt32();
            if (receivedFieldCount != fieldCount)
            {
                UnityEngine.Debug.LogError("Mismatch in the count of fields");
                return;
            }

            // Read the bitfield from the input stream
            byte[] receivedBitfieldBytes = reader.ReadBytes((fieldCount + 7) / 8);
            BitArray receivedBitfield = new BitArray(receivedBitfieldBytes);

            if (receivedBitfield.Get(0))
                _clientId = reader.ReadInt32();
            if (receivedBitfield.Get(1))
                _username = reader.ReadString();
            if (receivedBitfield.Get(2))
                _isHost = reader.ReadBoolean();
        }

        protected override void InitNetworkVariablesList()
        {
        
        }

        protected override MemoryStream Write(MemoryStream outputMemoryStream)
        {
            MemoryStream tempStream = new MemoryStream();
            BinaryWriter tempWriter = new BinaryWriter(tempStream);

            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            Type objectType = this.GetType();
            writer.Write(objectType.AssemblyQualifiedName);
            writer.Write(NetworkObject.GetNetworkId());

            // Serialize the changed fields using the bitfield
            BitArray bitfield = BITTracker.GetBitfield();

            if (BITTracker.GetBitfield().Get(0))
                tempWriter.Write(_clientId);
            if (BITTracker.GetBitfield().Get(1))
                tempWriter.Write(_username);
            if (BITTracker.GetBitfield().Get(2))
                tempWriter.Write(_isHost);

            byte[] data = tempStream.ToArray();
            int fieldsTotalSize = data.Length;
            writer.Write(fieldsTotalSize);

            int fieldCount = bitfield.Length;
            writer.Write(fieldCount);

            // Write the bitfield
            byte[] bitfieldBytes = new byte[(fieldCount + 7) / 8];
            bitfield.CopyTo(bitfieldBytes, 0);
            writer.Write(bitfieldBytes);

            tempStream.CopyTo(outputMemoryStream);

            return outputMemoryStream;
        }
    }
}

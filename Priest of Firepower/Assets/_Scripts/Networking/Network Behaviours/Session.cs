using System;
using System.Collections;
using System.IO;

namespace _Scripts.Networking.Network_Behaviours
{
    public class Session : NetworkBehaviour
    {
        int clientId;
        string username;
        bool isHost;

        public override void Awake()
        {
            base.Awake();
            bitTracker = new ChangeTracker(3);
        }
        public override void Read(BinaryReader reader)
        {

        
            int fieldCount = bitTracker.GetBitfield().Length;
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
                clientId = reader.ReadInt32();
            if (receivedBitfield.Get(1))
                username = reader.ReadString();
            if (receivedBitfield.Get(2))
                isHost = reader.ReadBoolean();
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
            writer.Write(networkObject.GetNetworkId());

            // Serialize the changed fields using the bitfield
            BitArray bitfield = bitTracker.GetBitfield();

            if (bitTracker.GetBitfield().Get(0))
                tempWriter.Write(clientId);
            if (bitTracker.GetBitfield().Get(1))
                tempWriter.Write(username);
            if (bitTracker.GetBitfield().Get(2))
                tempWriter.Write(isHost);

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

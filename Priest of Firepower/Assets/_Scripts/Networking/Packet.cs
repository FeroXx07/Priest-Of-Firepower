using System;
using System.IO;

namespace _Scripts.Networking
{
    public enum PacketType
    {
        SYNC,
        PING,
        OBJECT_STATE,
        INPUT,
        AUTHENTICATION
    }
    
    public class Packet
    {
        public Packet(PacketType packetType, UInt64 sequenceNum, UInt64 senderId, Int64 timeStamp, int itemsCount, bool isReliable, byte[] contentsData)
        {
            this.isReliable = isReliable;
            this.packetType = packetType;
            this.sequenceNum = sequenceNum;
            this.senderId = senderId;
            this.timeStamp = timeStamp;
            this.itemsCount = itemsCount;
            this.contentsData = contentsData;
            Serialize();
        }

        private bool isReliable = false;
        public PacketType packetType { get; private set; }
        public UInt64 sequenceNum { get; private set; }
        public UInt64 senderId { get; private set; }
        public Int64 timeStamp { get; private set; }
        public int itemsCount { get; private set; } // How many replication or input headers?
        public byte[] contentsData { get; private set; } // The packet contents data
        public byte[] allData { get; private set; } // The whole packet data = contents data  + "header"(packet type, senderId, timestamp...) 
        
        public void Serialize()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)packetType);
            writer.Write(sequenceNum);
            writer.Write(senderId);
            writer.Write(timeStamp);
            writer.Write(itemsCount);
            writer.Write(isReliable);
            writer.Write(contentsData);
            allData = stream.ToArray();
        }
        
        public static Packet DeSerialize(BinaryReader reader)
        {
            return new Packet((PacketType)reader.ReadInt32(), reader.ReadUInt64(), reader.ReadUInt64(),
                reader.ReadInt64(), reader.ReadInt32(),
                reader.ReadBoolean(), reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position)));
        }
    }
}
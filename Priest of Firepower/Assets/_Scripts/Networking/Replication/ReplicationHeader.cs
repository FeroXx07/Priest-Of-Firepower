using System;
using System.Collections.Generic;
using System.IO;

namespace _Scripts.Networking.Replication
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
}
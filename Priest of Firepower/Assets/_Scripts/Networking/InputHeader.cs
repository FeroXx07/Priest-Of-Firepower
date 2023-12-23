using System;
using System.Collections.Generic;
using System.IO;

namespace _Scripts.Networking
{
    public class InputHeader
    {
        public Int64 timeStamp;
        public UInt64 clientId;
        
        public UInt64 id { get; private set; }
        public string objectFullName { get; private set; }
        public int memoryStreamSize { get; private set; }
        public InputHeader(UInt64 id, string objectFullName, int memoryStreamSize, UInt64 clientRequestId, Int64 timeStamp)
        {
            this.clientId = clientRequestId;
            this.timeStamp = timeStamp;
            this.id = id;
            this.objectFullName = objectFullName;
            this.memoryStreamSize = memoryStreamSize;
        }
        public MemoryStream GetSerializedHeader()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(id);
            writer.Write(objectFullName);
            writer.Write(memoryStreamSize);
            writer.Write(timeStamp);
            writer.Write(clientId);
            return stream;
        }
        public static InputHeader DeSerializeHeader(BinaryReader reader)
        {
            return new InputHeader(reader.ReadUInt64(), reader.ReadString(), reader.ReadInt32(), reader.ReadUInt64(), reader.ReadInt64());
        }

        public static List<InputHeader> DeSerializeHeadersList(BinaryReader reader, int count)
        {
            List<InputHeader> list = new List<InputHeader>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(DeSerializeHeader(reader));
            }
            return list;
        }
    }
    
    public class InputItem
    {
        public InputItem(InputHeader header, MemoryStream memoryStream)
        {
            this.header = header;
            this.memoryStream = memoryStream;
        }
        public InputHeader header { get; private set; }
        public MemoryStream memoryStream { get; private set; }
        public void ReplaceMemoryStream(MemoryStream newStream) => memoryStream = newStream;
    }
}
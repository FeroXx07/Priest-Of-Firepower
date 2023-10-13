using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

//public class MessageEvent : UnityEvent<string> { }
public interface Br_IServer
{
    public static Action<string> OnSendMessageToClient;
    public static Action<string> OnSendMessageToServer;

    public static Action<string> OnReceiveMessageFromClient;
    public static Action<string> OnReceiveMessageFromServer;
}

public interface Br_ICreateRoomUI
{
    public static Action OnCreateRoom;
    public static Action OnDestroyRoom;
}

public interface Br_IJoinRoomUI
{
    public static Action OnJoinRoom;
    public static Action OnLeaveRoom;
}

public interface Br_ServerInfoPackaging
{
    public static byte[] SerializeData(ChatMessage data)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, data);
            return memoryStream.ToArray();
        }
    }

    public static byte[] SerializeData(ServerInformation data)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, data);
            return memoryStream.ToArray();
        }
    }
    public static T DeserializeData<T>(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            IFormatter formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(memoryStream);
        }
    }

    struct ChatMessage 
    {
        string user;
        string message;
    }

    struct ServerInformation
    {
        public string serverName;
        public string message;
    }
}
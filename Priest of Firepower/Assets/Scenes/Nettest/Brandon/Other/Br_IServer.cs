using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Sockets;

//public class MessageEvent : UnityEvent<string> { }
public interface Br_IServer
{
    public static Action<string> OnSendMessageToClient;
    public static Action<string> OnSendMessageToServer;

    public static Action<Br_ServerInfoPackaging.ChatMessage> OnReceiveMessageFromClient;
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

    enum InfoPackageType
    {
        SERVER_INFO = 0,
        CLIENT_INFO = 1,
        CHAT_MESSAGE = 2
    }

    struct InfoPackage
    {
        public InfoPackageType infoType;
        public byte[] packageData;
    }


    public static byte[] PackData<T>(T data, InfoPackageType infoType)
    {
        using MemoryStream memoryStream = new MemoryStream();

        MemoryStream dataMemoryStream = new MemoryStream();

        //serialize data to send
        IFormatter dataformatter = new BinaryFormatter();
        dataformatter.Serialize(dataMemoryStream, data);

        InfoPackage infoPackage = new InfoPackage();

        //fill header info
        switch (infoType)
        {
            case InfoPackageType.SERVER_INFO:
                infoPackage.infoType = InfoPackageType.SERVER_INFO;
                break;
            case InfoPackageType.CLIENT_INFO:
                infoPackage.infoType = InfoPackageType.CLIENT_INFO;
                break;
            case InfoPackageType.CHAT_MESSAGE:
                infoPackage.infoType = InfoPackageType.CHAT_MESSAGE;
                break;
            default:
                break;
        }

        infoPackage.packageData = memoryStream.ToArray();

        //serialize header + data
        IFormatter packformatter = new BinaryFormatter();
        packformatter.Serialize(memoryStream, data);


        return memoryStream.ToArray();
    }

    public static T DeserializeData<T>(byte[] data)
    {
        using MemoryStream memoryStream = new MemoryStream(data);
        IFormatter formatter = new BinaryFormatter();
        return (T)formatter.Deserialize(memoryStream);
    }

    public static T UnpackData<T>(byte[] data)
    {
        InfoPackage infoPackage = DeserializeData<InfoPackage>(data);

        //compare info type and return if possible
        switch (infoPackage.infoType)
        {
            case InfoPackageType.SERVER_INFO:
                {
                    if (typeof(T) == typeof(ServerInfo))
                    {
                        return UnpackData<T>(infoPackage.packageData);
                    }
                    else
                    {
                        throw new InfoPackageException("Cannot convert [" + typeof(T).ToString() + "] into [" + typeof(ServerInfo).ToString() + "].");
                    }
                }
            case InfoPackageType.CLIENT_INFO:
                {
                    if (typeof(T) == typeof(ClientInfo))
                    {
                        return UnpackData<T>(infoPackage.packageData);
                    }
                    else
                    {
                        throw new InfoPackageException("Cannot convert [" + typeof(T).ToString() + "] into [" + typeof(ClientInfo).ToString() + "].");
                    }
                }
            case InfoPackageType.CHAT_MESSAGE:
                {
                    if (typeof(T) == typeof(ChatMessage))
                    {
                        return UnpackData<T>(infoPackage.packageData);
                    }
                    else
                    {
                        throw new InfoPackageException("Cannot convert [" + typeof(T).ToString() + "] into [" + typeof(ChatMessage).ToString() + "].");
                    }
                }
            default:
                throw new InfoPackageException("Cannot recognize package type");
        }
        throw new InfoPackageException("Cannot recognize package type");
    }
    struct ChatMessage
    {
        public ClientInfo user;
        public string message;
    }

    struct ServerInfo
    {
        public string serverName;
        public string message;
    }

    struct ClientInfo
    {
        public string username;
        public Socket socket;
    }
}

public class InfoPackageException : Exception
{
    public InfoPackageException() { }
    public InfoPackageException(string message) : base(message) { }

}
using System;


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

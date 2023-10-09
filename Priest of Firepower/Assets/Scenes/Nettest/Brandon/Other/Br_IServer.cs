using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;


//public class MessageEvent : UnityEvent<string> { }
public interface Br_IServer
{
    public static Action<string> OnCreateMessage;
    public static Action<string> OnCreateResponse;
}

public interface Br_ICreateRoomUI
{
    public static Action OnCreateRoom;
    public static Action OnDestroyRoom;
}

public interface Br_IJoinRoomUI
{
    public static Action<string> OnJoinRoom;
    public static Action<string> OnLeaveRoom;
}

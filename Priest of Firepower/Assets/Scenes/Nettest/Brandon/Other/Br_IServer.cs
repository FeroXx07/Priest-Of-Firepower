using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;


//public class MessageEvent : UnityEvent<string> { }
public interface Br_IServer
{
    public static Action<string> OnCreateMessage;
}

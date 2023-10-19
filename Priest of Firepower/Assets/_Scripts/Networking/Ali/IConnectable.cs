using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public interface IConnectable
{
    void SendData(Socket socketToSend, byte[] data);
    void ListenData(Socket socketToListen);
}

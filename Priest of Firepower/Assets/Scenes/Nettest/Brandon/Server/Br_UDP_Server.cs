using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Net.Sockets;

public class Br_UDP_Server : MonoBehaviour
{
    private UdpClient udpServer;
    public int port = 5000;
    string serverIpAddress = " 192.168.104.17";
    bool createRoomRequested = false;
    Socket newSocket;
    EndPoint RemoteClient;
    [SerializeField]
    int listenCalls;
    // Start is called before the first frame update
    void Start()
    {
        
    }


    public void CreateRoomRequest()
    {
        createRoomRequested = true;
        newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        ListenForClients();
    }

    void ListenForClients()
    {
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        EndPoint senderRemote = sender;
        newSocket.Bind(senderRemote);

        byte[] msg = new Byte[256];
        Console.WriteLine("Waiting to receive datagrams from client...");
        listenCalls++;
        // This call blocks.
        newSocket.ReceiveFrom(msg, msg.Length, SocketFlags.None, ref senderRemote);
        listenCalls--;
        Console.WriteLine("Waiting has stopped.");
        newSocket.Close();
    }

    //void RecieveCallback(IAsyncResult ar)
    //{
        
    //}

    
}

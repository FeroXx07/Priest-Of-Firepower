using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Net.Sockets;

public class Alx_UDP_Server : MonoBehaviour
{
    private UdpClient udpServer;
    public int port = 5000;
    string serverIpAddress = " 192.168.104.17";
    bool createRoomRequested = false;
    Socket newSocket;
    EndPoint RemoteClient;
    // Start is called before the first frame update
    void Start()
    {
        
    }


    void CreateRoomRequest()
    {
        createRoomRequested = true;
        newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        
    }

    void ListenForClients()
    {
        var client = new IPEndPoint(IPAddress.Any, port);
        newSocket.Bind(client);
    }

    void RecieveCallback(IAsyncResult ar)
    {
        byte[] msg = new Byte[256];
        Console.WriteLine("Waiting to receive datagrams from client...");
        // This call blocks.
        //newSocket.ReceiveFrom(msg, msg.Length, SocketFlags.None ref client);
        newSocket.Close();
    }

    
}

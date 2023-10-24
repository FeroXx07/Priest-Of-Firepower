using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;

public class Alx_UDP_Client : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ConnectToServer(string serverIp) {
        try
        {
            Socket newSocket = new Socket(AddressFamily.InterNetwork,
                                            SocketType.Dgram,
                                            ProtocolType.Udp);
            Socket client = new Socket(AddressFamily.InterNetwork,
                                            SocketType.Dgram,
                                            ProtocolType.Udp);

            System.Net.IPEndPoint ipep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(serverIp), 33);

            //byte[] data = Encoding.ASCII.GetBytes(serverIp);
            //int recv = newSocket.ReceiveFrom(data, ipep);


            //newSocket.SendTo(data, recv, SocketFlags.None, Remote);

            newSocket.Listen(10);
            Debug.Log("Waiting for clients...");
            client = newSocket.Accept();
            System.Net.EndPoint clientep = (System.Net.IPEndPoint)client.RemoteEndPoint;
            Debug.Log("Connected: " + clientep.ToString());
            //bool connected = true;
        }
        catch (System.Exception e)
        {
            Debug.Log("Connection failed.. trying again..." + e.ToString());
        }
    }
}

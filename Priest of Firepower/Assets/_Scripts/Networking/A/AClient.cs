using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security;
using UnityEngine;
using System.Threading;
using System.Text;

public class AClient : MonoBehaviour
{
    Socket socket;
    [SerializeField] AddressFamily addressFamily;
    [SerializeField] SocketType socketType;
    [SerializeField] ProtocolType protocolType;

    IPEndPoint endPoint;
    [SerializeField] IPAddress IPaddress;
    [SerializeField] int port;

    Thread sendData;
    private void OnEnable()
    {

        sendData = new Thread(() => SendUDP());

        sendData.Start();

    }
    private void OnDisable()
    {
        socket.Close();
        sendData.Abort();   
    }

    void SendUDP()
    {
        if (socket == null)
        {
            socket = new Socket(addressFamily, socketType, protocolType);
            endPoint = new IPEndPoint(IPAddress.Any, port);
        }
        Byte[] sendBytes = Encoding.ASCII.GetBytes("Hello there! from clientA");
        socket.SendTo(sendBytes,sendBytes.Length,SocketFlags.None, endPoint);


    }

    void SendTCP()
    {

    }

    void Bind(Socket socketToBind, IPEndPoint endpointToBind)
    {
        try
        {
            socketToBind.Bind(endpointToBind);

        }catch(SocketException e)
        {
            Debug.LogException(e);

        }catch(ArgumentNullException e)
        {
            Debug.LogException(e);

        }catch(ObjectDisposedException e) 
        { 
            Debug.LogException(e);
        
        }catch(SecurityException e)
        {
            Debug.LogException(e);
        }
       
    }

    void CreateSocket(AddressFamily family, SocketType type, ProtocolType protocol)
    {
        socket = new Socket(family, type, protocol);
    }
    void CreateEndPoint(long ipAddress, int port)
    {
        endPoint = new IPEndPoint(ipAddress, port); 
    }
}

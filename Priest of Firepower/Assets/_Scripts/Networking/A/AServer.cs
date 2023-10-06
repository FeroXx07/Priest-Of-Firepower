using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Security;
using System;
using Unity.VisualScripting;
using System.Threading;

public class AServer : MonoBehaviour
{
    Socket socket;
    [SerializeField] AddressFamily addressFamily;
    [SerializeField] SocketType socketType;
    [SerializeField] ProtocolType protocolType;

    IPEndPoint endPoint;
    [SerializeField] IPAddress IPaddress;
    [SerializeField] int port;

    Thread listen;
    private void OnEnable()
    {
        listen = new Thread(()=> InitServer()); 
    }
    void InitServer()
    {
        socket = new Socket(addressFamily, socketType, protocolType);
        Bind(socket, endPoint);
        while (true)
        {
            try
            {
                socket.Listen(1);
                Debug.Log("waiting for clients....");
                Socket client = socket.Accept();
                IPEndPoint clientIP = (IPEndPoint)client.RemoteEndPoint;


            }catch(SocketException e)
            {
                Debug.LogException(e);
            }

        }
    }


    void Bind(Socket socketToBind, IPEndPoint endpointToBind)
    {
        try
        {
            socketToBind.Bind(endpointToBind);

        }
        catch (SocketException e)
        {
            Debug.LogException(e);

        }
        catch (ArgumentNullException e)
        {
            Debug.LogException(e);

        }
        catch (ObjectDisposedException e)
        {
            Debug.LogException(e);

        }
        catch (SecurityException e)
        {
            Debug.LogException(e);
        }

    }

}

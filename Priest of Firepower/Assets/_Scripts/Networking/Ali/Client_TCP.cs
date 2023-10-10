using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using ServerAli;
using System.Threading;
using System;
using UnityEditor.PackageManager;

public class Client_TCP: MonoBehaviour
{
    #region Fields
    private Socket _socket;
    private IPEndPoint _iPEndPointlocal;

    private IPAddress _address = IPAddress.Parse("127.0.0.1");
    [SerializeField] private int _port = SupportClass.FreeTcpPort();

    private Thread _toServerThread;

    bool connected = false;
    #endregion

    #region Initializers and Cleanup
    private void Awake()
    {
        connected = false;

        // Make sure in localhost client doesn't have the same port as server
        while (_port == 61111)
        {
            _port = SupportClass.FreeTcpPort();
        }

        // Init
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _iPEndPointlocal = new IPEndPoint(_address, _port);

        // Bind
        SupportClass.BindSocket(_socket, _iPEndPointlocal);
    }

    private void OnDisable()
    {
        InitServerDisconnection();
    }

    #endregion

    #region Core func
    public void InitServerConnection()
    {
        if (_toServerThread != null) {
            _toServerThread.Abort();
            _toServerThread = null;
        }

        _toServerThread = new Thread(TryConnectingToServer);
        _toServerThread.Start();
    }

    public void InitServerDisconnection()
    {
        SupportClass.CloseConnection(_socket);
        _toServerThread.Abort();
    }

    public void TryConnectingToServer()
    {
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 61111);

        // .Connect throws an exception if unsuccessful
        _socket.Connect(serverEndPoint);

        // This is how you can determine whether a socket is still connected.
        bool blockingState = _socket.Blocking;
        try{
            byte[] tmp = new byte[1];

            _socket.Blocking = false;
            _socket.Send(tmp, 0, 0);
            Console.WriteLine("Connected!");
        }
        catch (SocketException e){
            // 10035 == WSAEWOULDBLOCK
            if (e.NativeErrorCode.Equals(10035))  {
                Console.WriteLine("Still Connected, but the Send would block");
            }
            else {
                Console.WriteLine("Disconnected: error code {0}!", e.NativeErrorCode);
            }
        }
        finally {
            _socket.Blocking = blockingState;
        }

        Console.WriteLine("Connected: {0}", _socket.Connected);
    }
    #endregion
}

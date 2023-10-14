using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using ServerAli;
using System.Threading;
using System;
using UnityEditor.PackageManager;
using UnityEngine.UI;
using TMPro;

public class Client_TCP: MonoBehaviour
{
    #region Fields
    private Socket _socket;
    private IPEndPoint _iPEndPointlocal;

    private IPAddress _address = IPAddress.Parse("127.0.0.1");
    [SerializeField] private int _port = Utilities.FreeTcpPort();

    private Thread _toServerThread;
    #endregion

    #region Initializers and Cleanup
    private void Awake()
    {
        // Make sure in localhost client doesn't have the same port as server
        while (_port == 61111)
        {
            _port = Utilities.FreeTcpPort();
        }

        // Init
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _iPEndPointlocal = new IPEndPoint(_address, _port);

        // Bind
        Utilities.BindSocket(_socket, _iPEndPointlocal);
    }

    private void OnDisable()
    {
        InitServerDisconnection();
    }

    #endregion

    #region Core func
    public void UI_InitServerConnection(TextMeshProUGUI textMeshIP)
    {
        if (_toServerThread != null) {
            _toServerThread.Abort();
            _toServerThread = null;
        }
        
        if (Utilities.ValidateIPAdress(textMeshIP.text, out string cleanedIp))
        {
            _toServerThread = new Thread(() => TryConnectingToServer(cleanedIp));
            _toServerThread.Start();
        }
        else
        {
            Debug.LogAssertion($"CLIENT TCP: Insert an valid IP Adress, {textMeshIP.text} is not a valid IP address");
        }
    }

    public void InitServerDisconnection()
    {
        Debug.Log("CLIENT TCP: Init Server Disconnection");
        Utilities.CloseConnection(_socket);
        _toServerThread.Abort();
    }

    public void TryConnectingToServer(string ip)
    {
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), 61111);

        // .Connect throws an exception if unsuccessful
        _socket.Connect(serverEndPoint);

        // This is how you can determine whether a socket is still connected.
        bool blockingState = _socket.Blocking;
        try{
            byte[] tmp = new byte[1];

            _socket.Blocking = false;
            _socket.Send(tmp, 0, 0);
            Debug.Log($"CLIENT TCP: Connected to server: {serverEndPoint}!");
        }
        catch (SocketException e){
            // 10035 == WSAEWOULDBLOCK
            if (e.NativeErrorCode.Equals(10035))  {
                Debug.Log("CLIENT TCP: Still Connected, but the Send would block");
            }
            else {
                Debug.Log($"CLIENT TCP: Disconnected: error code {e.NativeErrorCode}!");
            }
        }
        finally {
            _socket.Blocking = blockingState;
        }

        Debug.Log($"CLIENT TCP: Connected: {_socket.Connected}");
    }
    #endregion
}

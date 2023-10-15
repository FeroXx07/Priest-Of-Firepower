using ServerAli;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class Server_TCP : MonoBehaviour
{
    #region Theory
    /* The listen backlog is a queue which is used by the operating system to store connections 
     * that have been accepted by the TCP stack but not, yet, by your program. 
     * Conceptually, when a client connects it's placed in this queue
     * until your Accept() code removes it and hands it to your program.*/

    /*Note that this is concerned with peaks in concurrent connection ATTEMPTS and in no way related 
     * to the maximum number of concurrent connections that your server can maintain. */
    #endregion

    #region Fields
    private Socket _socket;
    private IPEndPoint _iPEndPointlocal;

    private IPAddress _address = IPAddress.Parse("127.0.0.1"); 
    [SerializeField] private int _port = 61111;
    [SerializeField] private int _backlogSize = 1;

    private List<Thread> _activeThreads = new List<Thread>();
    private List<Socket> _clientSockets = new List<Socket>();

    private Thread _listeningThread;

    public string serverName;
    #endregion

    #region Events
    public Action<string> OnServerIPAssignated;
    public Action<Socket> OnNewConnection;
    #endregion

    #region Initializers and Cleanup
    private void Awake()
    {
        // Init
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _iPEndPointlocal = new IPEndPoint(_address, _port);

        // Bind
        Utilities.BindSocket(_socket, _iPEndPointlocal);

        // ListenForConnections config
        _socket.Blocking = false;
        _socket.Listen(_backlogSize);
        
        _listeningThread = null;
    }

    private void OnDisable()
    {
        Debug.Log("SERVER TCP: Cleanup client sockets");
        foreach (var socket in _clientSockets)
        {
            Debug.Log($"SERVER TCP Closing connection of socket: {socket.LocalEndPoint.ToString()}");
            Utilities.CloseConnection(socket);
        }

        _clientSockets.Clear();

        Debug.Log("SERVER TCP: Cleanup active Threads ");
        foreach (var thread in _activeThreads)
        {
            Debug.Log($"SERVER TCP: Aborting thread: {thread.Name}");
            thread.Abort();
        }

        _activeThreads.Clear();
    }
    #endregion

    #region Core func
    public void TriggerCreateGame()
    {
        OnServerIPAssignated?.Invoke(_address.ToString());

        if (_listeningThread != null)
            return;

        // Start an asynchronous/parallel/multi-threaded socket to listen for connections
        _listeningThread = new Thread(ListenForConnections);
        _listeningThread.Name = "_listeningThread";
        _listeningThread.Start();
        _activeThreads.Add(_listeningThread);
    }

    void StopListeningConnections()
    {
        _activeThreads.Remove(_listeningThread);
        _listeningThread.Abort();
        _listeningThread = null;
    }

    void ListenForConnections()
    {
        while (true)  {
            try {
                Debug.Log("SERVER TCP: Waiting for clients...");
                if (_socket.Poll(100000, SelectMode.SelectRead)) //check if there's data available for reading on the socket without blocking
                {
                    // Check if there's data available for reading (100000 microseconds = 100 milliseconds)
                    Socket client = _socket.Accept();// Contrary to socket.Accept(), async server socket.BeginAccept() starts a new thread for each client socket assigning a new port

                    if (client != null && client.Connected)
                        ProcessAccept(client);
                }
                else
                {
                    // No incoming connections, continue waiting
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.WouldBlock || se.SocketErrorCode == SocketError.IOPending)
                {
                    // Non-blocking operation would block, continue waiting
                }
                else
                {
                    Debug.Log("SERVER TCP: Connection failed.. trying again... " + se.ToString());
                }
            }
            catch (System.Exception e)  {
                Debug.Log("SERVER TCP: Connection failed.. trying again... " + e.ToString());
            }
        }
    }

    void ProcessAccept(Socket client)
    {
        IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
        Debug.Log("SERVER TCP: Connected to client: " + clientep.ToString());

        _clientSockets.Add(client);

        // Create a new thread

        OnNewConnection.Invoke(client);
    }
    #endregion
}

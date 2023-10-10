using ServerAli;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

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
    [SerializeField] private int _backlogSize = 10;

    private List<Thread> _activeThreads = new List<Thread>();
    private List<Socket> _clientSockets = new List<Socket>();

    bool connected = false;
    #endregion

    #region Initializers and Cleanup
    private void Awake()
    {
        connected = false;

        // Init
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _iPEndPointlocal = new IPEndPoint(_address, _port);

        // Bind
        SupportClass.BindSocket(_socket, _iPEndPointlocal);

        // ListenForConnections config
        _socket.Blocking = false;
        _socket.Listen(_backlogSize);

        // Start an asynchronous/parallel/multi-threaded socket to listen for connections
        Thread listeningThread = new Thread(ListenForConnections);
        listeningThread.Start();
        _activeThreads.Add(listeningThread);
    }

    private void OnDisable()
    {
        foreach (var socket in _clientSockets)
            SupportClass.CloseConnection(socket);
        
        _clientSockets.Clear();

        foreach (var thread in _activeThreads)
            thread.Abort();

        _activeThreads.Clear();
    }
    #endregion

    #region Core func
    void ListenForConnections()
    {
        while (true)  {
            try {
                Debug.Log("Waiting for clients...");
                Socket client = _socket.Accept(); // Contrary to socket.Accept(), async server socket.BeginAccept() starts a new thread for each client socket assigning a new port
                IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
                Debug.Log("Connected: " + clientep.ToString());
                connected = true;

                _clientSockets.Add(client);
            }
            catch (System.Exception e)  {
                Debug.Log("Connection failed.. trying again... " + e.ToString());
            }
        }
    }
    #endregion
}

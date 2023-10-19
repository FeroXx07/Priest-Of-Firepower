using ClientA;
using ServerAli;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ServerAli
{
    #region Theory
    /* The listen backlog is a queue which is used by the operating system to store connections 
     * that have been accepted by the TCP stack but not, yet, by your program. 
     * Conceptually, when a client connects it's placed in this queue
     * until your Accept() code removes it and hands it to your program.*/

    /*Note that this is concerned with peaks in concurrent connection ATTEMPTS and in no way related 
     * to the maximum number of concurrent connections that your server can maintain. */
    #endregion

    public class Server_TCP : Socket_Connection
    {
        [SerializeField] private int _backlogSize = 1;
        Thread _listeningThread;
        private void Awake()
        {
            _port = 61111;
        }

        public void TriggerCreateGame()
        {
            InitSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, () => Utilities.BindSocket(_localSocket, _iPEndPointlocal));

            // ListenForConnections config
            _localSocket.Blocking = false;
            _localSocket.Listen(_backlogSize);

            StopListeningConnections();

            onRemoteIPAssignated?.Invoke(_address.ToString());

            // Start an asynchronous/parallel/multi-threaded socket to listen for connections
            _listeningThread = new Thread(() => ListenForConnections(ProcessAccept));
            _listeningThread.Name = "_listeningThread";
            _listeningThread.Start();
            _activeThreads.Add(_listeningThread);
        }

        #region AwaitConnections
        void StopListeningConnections()
        {
            if (_listeningThread == null)
                return;

            _activeThreads.Remove(_listeningThread);
            _listeningThread.Abort();
            _listeningThread = null;
        }

        void ProcessAccept(Socket client)
        {
            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
            Debug.Log("Connected to client: " + clientep.ToString());
            _connectedSockets.Add(client);

            // Respond with my server name
            byte[] data = new byte[100];
            string welcome = $"Welcome to my server {_socketName}";
            data = Encoding.ASCII.GetBytes(welcome);
            client.Send(data, data.Length,
                              SocketFlags.None);

            // Create a new client thread
            Thread clientThread = new Thread(() => ListenData(client, null, HandleDisconnection));
            clientThread.Name = "clientThread_" + clientep.ToString();
            clientThread.Start();
            _activeThreads.Add(clientThread);

            OnNewConnection.Invoke(client);
        }

        protected override void HandleDisconnection(Socket socket)
        {
            string threadName = "clientThread_" + ((IPEndPoint)socket.RemoteEndPoint).ToString();
            Thread threadToStop = _activeThreads.Find(x => x.Name == threadName);
            _activeThreads.Remove(threadToStop);
            _connectedSockets.Remove(socket);
            threadToStop.Abort();
            Utilities.CloseConnection(socket);
        }
        #endregion

        #region Support func
        public List<Socket> GetAciveClients()
        {
            return _connectedSockets;
        }
        #endregion
    }
}
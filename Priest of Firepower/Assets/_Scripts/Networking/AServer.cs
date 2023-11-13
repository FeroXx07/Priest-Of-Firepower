
#define AUTHENTICATION_CODE
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace _Scripts.Networking
{

    public class ClientManager
    {
        private int _nextClientId = 0;

        public int GetNextClientId()
        {
            int clientId = _nextClientId;
            _nextClientId++;
            return clientId;
        }
    }
    public class AServer : GenericSingleton<AServer>
    {
        #region variables
        IPEndPoint _endPoint;
        //[SerializeField] int port = 12345;
        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        private CancellationTokenSource _authenticationToken;
        private Thread _authenticationThread;

        ClientManager _clientManager;

        private List<ClientProcess> _clientThreads = new List<ClientProcess>();
        private List<ClientData> _clientList = new List<ClientData>();
        private List<ClientData> _clientListToRemove = new List<ClientData>();
        //private ConcurrentBag<ClientData> clientList = new ConcurrentBag<ClientData>();

        //actions
        Action<int> _onClientAccepted;
        Action _onClientRemoved;
        Action<int> _onClientDisconnected;
        Action<byte[]> _onDataRecieved;

        //handeles connection with clients
        Socket _serverTcp;
        Socket _serverUDP;

        ServerAuthenticator _authenticator = new ServerAuthenticator();

        struct ClientProcess
        {
            public Thread Thread;
            public CancellationTokenSource Token;
        }

        private bool _isServerInitialized  = false;
        #endregion

        #region client data
        class ClientData
        {
            public int ID = -1;
            public string Username = "";
            public ClientMetadata MetaData;
            public ClientSate State;
            public Socket ConnectionTcp;
            public Socket ConnectionUDP;
            public CancellationTokenSource AuthenticationToken; //if disconnection request invoke cancellation token to shutdown all related processes
            public bool IsHost = false;
        }
        struct ClientMetadata
        {
            public int Port;
            public IPAddress IP;
            //add time stamp
        }
        public enum ClientSate
        {
            CONNECTED,
            AUTHENTICATED,
            IN_GAME
        }
        #endregion

        #region enable/disable functions
        private void OnDisable()
        {

            Debug.Log("Stopping server ...");

            StopAuthenticationThread();

            DisconnectAllClients();

            StopAllClientThreads();

            Debug.Log("Closing server connection ...");

            if (_serverTcp.Connected)
            {
                _serverTcp.Shutdown(SocketShutdown.Both);
            }
            _serverTcp.Close();
        }
        private void Update()
        {
            RemoveDisconectedClient();
        }
        #endregion

        #region helper funcitons
        void StopAuthenticationThread()
        {

            _authenticationToken.Cancel();
            if (_authenticationThread != null)
            {
                if (_authenticationThread.IsAlive)
                {
                    _authenticationThread.Join();
                }
                //make sure it is not alive
                if (_authenticationThread.IsAlive)
                {
                    _authenticationThread.Abort();
                }
            }
        }
        void DisconnectAllClients()
        {
            foreach (ClientData client in _clientList)
            {
                RemoveClient(client);
            }
        }
        void StopAllClientThreads()
        {

            Debug.Log("Server: Waiting for all threads to terminate.");
            foreach (ClientProcess p in _clientThreads)
            {
                p.Token.Cancel();
                if (p.Thread.IsAlive)
                    p.Thread.Join();
            }
            foreach (ClientProcess p in _clientThreads)
            {
                if (p.Thread.IsAlive)
                    p.Thread.Abort();
            }
        }
        void RemoveDisconectedClient()
        {
            if (_clientListToRemove.Count > 0)
            {
                lock (_clientList)
                {
                    foreach (ClientData clientToRemove in _clientListToRemove)
                    {
                        _clientList.Remove(clientToRemove);
                    }
                }
                Debug.Log("removed " + _clientListToRemove.Count + " clients");
                _clientListToRemove.Clear();
            }
        }
        #endregion

        #region getter setter funtions
        public bool GetServerInit() { return _isServerInitialized; }
        #endregion

        #region core functions
        public void InitServer()
        {
            _clientManager = new ClientManager();
            //start server
            StartConnectionListenerTcp();
        }

        public void SendToAll(byte[] data)
        {
            foreach(ClientData client in _clientList)
            {
                client.ConnectionUDP.SendTo(data, data.Length, SocketFlags.None, _endPoint);
            }
        }
        public  void SendCriticalToAll(byte[] data)
        {
            foreach (ClientData client in _clientList)
            {
                client.ConnectionTcp.SendTo(data, data.Length, SocketFlags.None, _endPoint);
            }
        }
        public void SendToClient(int clientId, byte[] data)
        {
            foreach (ClientData client in _clientList)
            {
                if (client.ID == clientId)
                {
                    client.ConnectionUDP.SendTo(data,data.Length, SocketFlags.None, _endPoint);

                    return;
                }
            }
        }
        void StartConnectionListenerTcp()
        {
            try
            {
                Debug.Log("Starting server ...");
                //create listener tcp
                _serverTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //create end point
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                //So for the ip any it listens to all directions ipv4 local LAN and 
                //also the public ip. TOconnect from the client use any of the ips
                _endPoint = new IPEndPoint(IPAddress.Any, 12345);
                //bind to ip and port to listen to
                _serverTcp.Bind(_endPoint);
                // Using ListenForConnections() method we create 
                // the Client list that will want
                // to connect to Server
                _serverTcp.Listen(4);

                _authenticationToken = new CancellationTokenSource();
                _authenticationThread = new Thread(() => Authenticate(_authenticationToken.Token));
                _authenticationThread.Start();

                _isServerInitialized = true;

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }  
        void HandleClient(ClientData clientData)
        {
            Debug.Log("Starting Client Thread " + clientData.ID + " ...");
            try
            {
                while (!clientData.AuthenticationToken.IsCancellationRequested)
                {

                    if (clientData.ConnectionTcp.Available > 0)
                    {
                        ReceiveSocketData(clientData.ConnectionTcp);
                    }
                    if (clientData.ConnectionUDP.Available > 0)
                    {
                        ReceiveSocketData(clientData.ConnectionUDP);
                    }
                    // Add some delay to avoid busy-waiting
                    Thread.Sleep(10);
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset ||
                    se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // Handle client disconnection (optional)
                    Debug.Log($"Client {clientData.ID} disconnected: {se.Message}");
                    RemoveClient(clientData);
                }
                else
                {
                    // Handle other socket exceptions
                    Debug.Log($"SocketException: {se.SocketErrorCode}, {se.Message}");
                }
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.Log($"Exception: {e.Message}");
            }
        }
        void ReceiveSocketData(Socket socket)
        {
            try
            {
                lock(NetworkManager.Instance.IncomingStreamLock)
                {
                    byte[] buffer = new byte[1500];

                    Debug.Log("Server: waiting for authentication ... ");
                    // Receive data from the client
                    socket.Receive(buffer);
                    Debug.Log("Server: recieved data ... ");
                    MemoryStream stream = new MemoryStream(buffer);

                    NetworkManager.Instance.AddIncomingDataQueue(stream);
                }
            }
            catch (SocketException se)
            {
               // Handle other socket exceptions
               Debug.Log($"SocketException: {se.SocketErrorCode}, {se.Message}");

            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.Log($"Exception: {e.Message}");
            }
        }
        int CreateClient(Socket clientSocket, string userName)
        {
            lock (_clientList)
            {
                ClientData clientData = new ClientData();

                clientData.ID = _clientManager.GetNextClientId();
                clientData.Username = userName;

                clientData.ConnectionTcp = clientSocket;
                //add a time out exeption for when the client disconnects or has lag or something
                clientData.ConnectionTcp.ReceiveTimeout = 1000;
                clientData.ConnectionTcp.SendTimeout = 1000;

                //create udp connection
                clientData.ConnectionUDP = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
                clientData.ConnectionUDP.Bind(clientSocket.RemoteEndPoint);
                clientData.ConnectionUDP.ReceiveTimeout = 100;
                clientData.ConnectionUDP.SendTimeout = 100;

                //store endpoint
                IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                clientData.MetaData.IP = clientEndPoint.Address;
                clientData.MetaData.Port = clientEndPoint.Port;

                clientData.AuthenticationToken = new CancellationTokenSource();

                _clientList.Add(clientData);

                Debug.Log("Connected client Id: " + clientData.ID);


                //Create the handeler of the chat for that client
                //create a hole thread to recive important data from server-client
                //like game state, caharacter selection, map etc

                ClientProcess process = new ClientProcess();

                process.Token = new CancellationTokenSource();

                process.Thread = new Thread(() => HandleClient(clientData));

                process.Thread.IsBackground = true;
                process.Thread.Name = clientData.ID.ToString();
                process.Thread.Start();



                _clientThreads.Add(process);

                return clientData.ID;
            }
        }
        void RemoveClient(ClientData clientData)
        {
            // Remove the client from the list of connected clients    
            lock (_clientList)
            {
                // Cancel the client's cancellation token source
                clientData.AuthenticationToken.Cancel();

                // Close the client's socket
                if (clientData.ConnectionTcp.Connected)
                {
                    clientData.ConnectionTcp.Shutdown(SocketShutdown.Both);
                }
                clientData.ConnectionTcp.Close();

                _clientListToRemove.Add(clientData);

                Debug.Log("Client " + clientData.ID + " disconnected.");
            }
        }
        #endregion

        #region Authentication
        void Authenticate(CancellationToken cancellationToken)
        {
            try
            {
                //accept new Connections ... 
                while (!cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Server: Waiting connection ... ");
                    Socket clientSocket = _serverTcp.Accept();
                    Debug.Log("Server: new socket accepted ...  ");
                    if (clientSocket.Available > 0)
                        ReceiveSocketData(clientSocket);                

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _authenticationToken.Cancel();
                Debug.Log("Shutting down authentication process ...");
            }
            finally
            {

            }
        }

        public ServerAuthenticator GetAuthenticator() { return _authenticator; }
        #endregion
    }
}

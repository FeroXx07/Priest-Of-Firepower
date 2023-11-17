#define AUTHENTICATION_CODE
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace _Scripts.Networking
{
    public class ClientManager
    {
        private UInt64 _nextClientId = 0;

        public UInt64 GetNextClientId()
        {
            UInt64 clientId = _nextClientId;
            _nextClientId++;
            return clientId;
        }
    }

    public class AServer
    {
        #region variables

        IPEndPoint _endPoint;

        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        Process _listenConnectionProcess;
        private Dictionary<IPEndPoint, Process> _authenticationProcesses = new Dictionary<IPEndPoint, Process>();
        private Dictionary<IPEndPoint, Socket> _authenticationConnections = new Dictionary<IPEndPoint, Socket>();

        private Dictionary<IPEndPoint, ServerAuthenticator> _authenticators = new Dictionary<IPEndPoint, ServerAuthenticator>();

        //private List<Process> _authenticationProcessList = new List<Process>();
        ClientManager _clientManager;
        private List<ClientData> _clientList = new List<ClientData>();
        private List<ClientData> _clientListToRemove = new List<ClientData>();

        //actions
        Action<UInt64> _onClientAccepted;
        Action _onClientRemoved;
        Action<int> _onClientDisconnected;
        Action<byte[]> _onDataRecieved;

        //handeles connection with clients
        Socket _serverTcp;
        private bool _isServerInitialized = false;

        #endregion

        public AServer(IPEndPoint endPoint)
        {
            _endPoint = endPoint;

            //_authenticator = new ServerAuthenticator();
            //_authenticator._onAuthenticationFailed += AuthenticationFailed;
            //_authenticator._onAuthenticated += AuthenticationSuccess;
        }

        #region client data

        class ClientData
        {
            public UInt64 ID;
            public string Username = "";
            public ClientMetadata MetaData;
            public ClientSate State;
            public Socket ConnectionTcp;
            public Socket ConnectionUDP;

            public Process
                listenProcess; //if disconnection request invoke cancellation token to shutdown all related processes

            public bool IsHost = false;
        }

        struct ClientMetadata
        {
            public IPEndPoint endPoint;
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
            StopConnectionListener();
            StopAuthenticationThread();
            DisconnectAllClients();
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
            try
            {
                foreach (KeyValuePair<IPEndPoint, Process> process in _authenticationProcesses)
                {
                    process.Value.Shutdown();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        void StopConnectionListener()
        {
            _listenConnectionProcess.Shutdown();
        }
        
        void DisconnectAllClients()
        {
            lock(_clientList) 
            {
                foreach (ClientData client in _clientList)
                {
                    RemoveClient(client);
                }
            }
        }

        void RemoveDisconectedClient()
        {
            lock (_clientList)
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
        }

        #endregion

        #region getter setter funtions

        public bool GetServerInit()
        {
            return _isServerInitialized;
        }

        #endregion

        #region core functions

        public void InitServer()
        {
            _clientManager = new ClientManager();
            //start server
            Debug.Log("Starting server ...");
            //create listener tcp
            _serverTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //create end point
            //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
            //In this case the operating system (TCP/IP stack) assigns a free port number for you.
            //So for the ip any it listens to all directions ipv4 local LAN and 
            //also the public ip. TOconnect from the client use any of the ips
            //if(_endPoint == null)
            _endPoint = new IPEndPoint(IPAddress.Any, NetworkManager.Instance.connectionAddress.port);

            //bind to ip and port to listen to
            _serverTcp.Bind(_endPoint);
            // Using ListenForConnections() method we create 
            // the Client list that will want
            // to connect to Server
            _serverTcp.Listen(4);
            _listenConnectionProcess = new Process();
            _listenConnectionProcess.cancellationToken = new CancellationTokenSource();
            _listenConnectionProcess.thread = new Thread(() =>
                StartConnectionListener(_listenConnectionProcess.cancellationToken.Token));
            _listenConnectionProcess.thread.Start();
            _isServerInitialized = true;
        }

        public void Shutdown()
        {
            Debug.Log("Stopping server ...");
            StopConnectionListener();
            StopAuthenticationThread();
            DisconnectAllClients();
            Debug.Log("Closing server connection ...");
            if (_serverTcp.Connected)
            {
                _serverTcp.Shutdown(SocketShutdown.Both);
            }

            _serverTcp.Close();
        }
        public void SendToAll(byte[] data)
        {
            foreach (ClientData client in _clientList)
            {
                if (client.ID != 0)
                {
                    Debug.Log("Server: sending to client " + client.Username);
                    client.ConnectionUDP.SendTo(data, data.Length, SocketFlags.None, client.ConnectionUDP.RemoteEndPoint);
                }
            }
        }

        public void SendCriticalToAll(byte[] data)
        {
            foreach (ClientData client in _clientList)
            {
                if (client.ID != 0) client.ConnectionTcp.SendTo(data, data.Length, SocketFlags.None, client.ConnectionTcp.RemoteEndPoint);
            }
        }

        public void SendCritical(UInt64 Id, byte[] data)
        {
            foreach (ClientData client in _clientList)
            {
                if (client.ID == Id)
                {
                    client.ConnectionTcp.SendTo(data, data.Length, SocketFlags.None, client.ConnectionTcp.RemoteEndPoint);
                    return;
                }
            }
        }

        public void SendToClient(UInt64 clientId, byte[] data)
        {
            foreach (ClientData client in _clientList)
            {
                if (client.ID == clientId)
                {
                    client.ConnectionUDP.SendTo(data, data.Length, SocketFlags.None, client.ConnectionUDP.RemoteEndPoint);
                    return;
                }
            }
        }
        private ManualResetEvent connectionListenerEvent = new ManualResetEvent(false);
        void StartConnectionListener(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    connectionListenerEvent.Reset(); // Reset the event before waiting for a connection

                    // Asynchronously wait for a connection or cancellation signal
                    IAsyncResult asyncResult = _serverTcp.BeginAccept(new AsyncCallback(AcceptCallback), null);

                    // Wait for either a connection or a cancellation signal
                    int waitResult = WaitHandle.WaitAny(new WaitHandle[] { asyncResult.AsyncWaitHandle, token.WaitHandle });

                    // If the wait result is for the cancellation token, exit the loop
                    if (waitResult == 1)
                    {
                        break;
                    }
                    connectionListenerEvent.Set();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the asynchronous operation
                Socket incomingConnection = null;
                try
                {
                    incomingConnection = _serverTcp.EndAccept(ar);
                }
                catch (ObjectDisposedException)
                {
                    // Handle the case where the socket is already disposed
                    return;
                }
                
                if (incomingConnection == null || incomingConnection.Handle == IntPtr.Zero || incomingConnection.Connected == false)
                {
                    // The socket is not valid; handle accordingly
                    return;
                }
                
                IPEndPoint IpEndPoint = incomingConnection.LocalEndPoint as IPEndPoint;

                // Check if the socket is connected
                if (!IsSocketConnected(incomingConnection))
                {
                    incomingConnection.Close();
                    return;
                }

                //check that the incoming socket is not being process twice
                foreach (ClientData client in _clientList)
                {
                    if (client.MetaData.endPoint == IpEndPoint)
                    {
                        incomingConnection.Close();
                    }
                }

                foreach (KeyValuePair<IPEndPoint, Socket> process in _authenticationConnections)
                {
                    if (process.Key == IpEndPoint)
                    {
                        incomingConnection.Close();
                    }
                }

                if (IsSocketConnected(incomingConnection))
                {
                    //if not local host
                    Debug.Log("Socket address: " + IpEndPoint.Address + " local address:" + IPAddress.Loopback);
                    if (IpEndPoint.Address.Equals(IPAddress.Loopback))
                    {
                        Debug.Log("Server : host connected ... ");
                        CreateClient(incomingConnection, "Host", true);
                    }
                    else
                    {
                        CreateClient(incomingConnection, "Melon", true);
                        //Process authenticate = new Process();

                        //authenticate.cancellationToken = new CancellationTokenSource();
                        //authenticate.thread = new Thread(() => Authenticate(incomingConnection, authenticate.cancellationToken.Token));
                        //authenticate.thread.Start();

                        //_authenticationConnections[IpEndPoint] = incomingConnection;
                        //_authenticationProcesses[IpEndPoint] = authenticate;
                        //_authenticators[IpEndPoint] = new ServerAuthenticator();
                    }
                }

                connectionListenerEvent.Set(); // Set the event to allow the loop to continue waiting for connections
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
                while (!clientData.listenProcess.cancellationToken.Token.IsCancellationRequested)
                {
                    if (!clientData.ConnectionTcp.Connected)
                    {
                        Debug.Log("TCP not connected ... ");
                        // Handle the case where TCP is not connected if needed
                        break; // Exit the loop if TCP is not connected
                    }

                    if (clientData.ConnectionTcp.Available > 0)
                    {
                        ReceiveSocketData(clientData.ConnectionTcp);
                    }

                    if (clientData.ConnectionUDP.Available > 0)
                    {
                        ReceiveSocketData(clientData.ConnectionUDP);
                    }

                    Thread.Sleep(10);
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset ||
                    se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // Handle client disconnection (optional)
                    Debug.LogError($"Client {clientData.ID} disconnected: {se.Message}");
                    RemoveClient(clientData);
                }
                else
                {
                    // Handle other socket exceptions
                    Debug.LogError($"SocketException: {se.SocketErrorCode}, {se.Message}");
                }
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Exception: {e.Message}");
            }
        }

        void ReceiveSocketData(Socket socket)
        {
            try
            {
                lock (NetworkManager.Instance.IncomingStreamLock)
                {
                    byte[] buffer = new byte[1500];

                    // Receive data from the client
                    int size = socket.Receive(buffer, buffer.Length, SocketFlags.None);
                    MemoryStream stream = new MemoryStream(buffer, 0, size);
                    NetworkManager.Instance.AddIncomingDataQueue(stream);
                }
            }
            catch (SocketException se)
            {
                // Handle other socket exceptions
                Debug.LogError($"SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Exception: {e.Message}");
            }
        }

        UInt64 CreateClient(Socket clientSocket, string userName, bool isHost)
        {
            lock (_clientList)
            {
                ClientData clientData = new ClientData();
                clientData.ID = _clientManager.GetNextClientId();
                clientData.Username = userName;
                clientData.IsHost = isHost;
                clientData.ConnectionTcp = clientSocket;
                //add a time out exeption for when the client disconnects or has lag or something
                clientData.ConnectionTcp.ReceiveTimeout = Timeout.Infinite;
                clientData.ConnectionTcp.SendTimeout = Timeout.Infinite;

                //create udp connection
                clientData.ConnectionUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientData.ConnectionUDP.Bind(clientSocket.LocalEndPoint);
                clientData.ConnectionUDP.ReceiveTimeout = Timeout.Infinite;
                clientData.ConnectionUDP.SendTimeout = Timeout.Infinite;

                //store endpoint
                IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.LocalEndPoint;
                clientData.MetaData.endPoint = clientEndPoint;

                //Create the process for that client
                //create a hole thread to recive important data from server-client
                //like game state, caharacter selection, map etc
                Process clientProcess = new Process();
                clientProcess.Name = "Handle Clinet " + clientData.ID.ToString();
                clientProcess.cancellationToken = new CancellationTokenSource();
                clientProcess.thread = new Thread(() => HandleClient(clientData));
                clientProcess.thread.IsBackground = true;
                clientProcess.thread.Name  = "Handle Clinet " + clientData.ID.ToString();
                clientProcess.thread.Start();
                clientData.listenProcess = clientProcess;
                _clientList.Add(clientData);
                Debug.Log("Created client Id: " + clientData.ID);
                SendClientID(clientData.ID);
                return clientData.ID;
            }
        }

        void RemoveClient(ClientData clientData)
        {
            Debug.Log("Removing client " + clientData.ID);
            try
            {
                // Remove the client from the list of connected clients    
                lock (_clientList)
                {
                    // Shutdown client thread
                    clientData.listenProcess.Shutdown();

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
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           
        }

        #endregion

        static bool IsSocketConnected(Socket socket)
        {
            try
            {
                // Poll the socket for readability and if it's not readable, it means the socket is closed.
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }

        void SendClientID(UInt64 id)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)PacketType.ID);
            writer.Write(id);
            SendCritical(id, stream.ToArray());
        }

        #region Authentication

        void Authenticate(Socket incomingSocket, CancellationToken cancellationToken)
        {
            incomingSocket.ReceiveTimeout = 5000;
            Debug.Log("Server: Authentication process started ... ");
            try
            {
                //accept new Connections ... 
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (incomingSocket.Available > 0)
                    {
                        Debug.Log("Authentication message recieved ...");
                        ReceiveSocketData(incomingSocket);
                    }

                    string p = "puto";
                    byte[] bytes = Encoding.ASCII.GetBytes(p);
                    incomingSocket.Send(bytes);
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Thread.ResetAbort();
                Debug.LogError("Shutting down authentication process ...");
            }
        }

        void AuthenticationSuccess(IPEndPoint endPoint, string username)
        {
            Socket socket = _authenticationConnections[endPoint];
            UInt64 id = CreateClient(socket, username, false);
            _onClientAccepted?.Invoke(id);

            //stop autorized client process
            _authenticationProcesses[endPoint].Shutdown();
            _authenticationProcesses.Remove(endPoint);
            _authenticationConnections.Remove(endPoint);
        }

        void AuthenticationFailed(IPEndPoint endpoint)
        {
            //remove connection
            _authenticationProcesses[endpoint].Shutdown();
            _authenticationProcesses.Remove(endpoint);
            _authenticationConnections[endpoint].Close();
            _authenticationConnections.Remove(endpoint);
        }

        public void PopulateAuthenticators(MemoryStream stream, BinaryReader reader)
        {
            lock (_authenticators)
            {
                foreach (KeyValuePair<IPEndPoint, ServerAuthenticator> process in _authenticators)
                {
                    process.Value.HandleAuthentication(stream, reader);
                }
            }
        }

        #endregion
    }
}
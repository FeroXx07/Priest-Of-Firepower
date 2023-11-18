#define AUTHENTICATION_CODE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        #region Fields

        private IPEndPoint _localEndPoint;

        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        private Process _listenConnectionProcess;
        
        private Socket _serverTcp;
        private Socket _serverUdp;
        public bool isServerInitialized { get; private set; } = false;
        
        private Dictionary<IPEndPoint, Process> _authenticationProcesses = new Dictionary<IPEndPoint, Process>();
        private Dictionary<IPEndPoint, Socket> _authenticationConnections = new Dictionary<IPEndPoint, Socket>();

        private Dictionary<IPEndPoint, ServerAuthenticator> _authenticators = new Dictionary<IPEndPoint, ServerAuthenticator>();

        //private List<Process> _authenticationProcessList = new List<Process>();
        private ClientManager _clientManager;
        
        private List<ClientData> _clientList = new List<ClientData>();
        private List<ClientData> _clientListToRemove = new List<ClientData>();

        //actions
        public Action<UInt64> onClientAccepted;
        public Action onClientRemoved;
        public Action<int> onClientDisconnected;
        public Action<byte[]> onDataRecieved;
        #endregion

        public AServer(IPEndPoint localEndPoint)
        {
            _localEndPoint = localEndPoint;
            InitServer();
            //_authenticator = new ServerAuthenticator();
            //_authenticator._onAuthenticationFailed += AuthenticationFailed;
            //_authenticator._onAuthenticated += AuthenticationSuccess;
        }

        #region Client Data

        private class ClientData
        {
            public UInt64 id;
            public string username = "";
            public ClientMetadata metaData;
            public ClientSate state;
            public Socket connectionTcp;
            // public Socket ConnectionUDP;

            public Process
                listenProcess; //if disconnection request invoke cancellation token to shutdown all related processes

            public bool isHost = false;
        }

        private struct ClientMetadata
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

        #region Helper Functions
        private void StopAuthenticationThread()
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
        private void StopConnectionListener()
        {
            _listenConnectionProcess.Shutdown();
        }

        private void DisconnectAllClients()
        {
            lock(_clientList) 
            {
                Debug.Log($"Server {_localEndPoint}: Disconnecting all clients");
                foreach (ClientData client in _clientList)
                {
                    RemoveClient(client);
                }
            }
        }
        private void RemoveDisconectedClient()
        {
            lock (_clientList)
            {
                if (_clientListToRemove.Count > 0)
                {
                    lock (_clientList)
                    {
                        foreach (ClientData clientToRemove in _clientListToRemove)
                        {
                            Debug.Log($"Server {_localEndPoint}: Removing {clientToRemove.metaData.endPoint}");
                            _clientList.Remove(clientToRemove);
                        }
                    }
                    Debug.Log($"Server {_localEndPoint}: Removed {_clientListToRemove.Count} clients");
                    _clientListToRemove.Clear();
                }
            }
        }
        #endregion

        #region Core Functions
        public void InitServer()
        {
            _clientManager = new ClientManager();
            Debug.Log($"Server {_localEndPoint}: Starting server...");
            
            _serverTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            _serverTcp.Bind(_localEndPoint);
            _serverUdp.Bind(_localEndPoint);

            _serverTcp.Listen(4);
            _listenConnectionProcess = new Process
            {
                cancellationToken = new CancellationTokenSource()
            };
            _listenConnectionProcess.thread = new Thread(() =>
                StartConnectionListener(_listenConnectionProcess.cancellationToken.Token));
            _listenConnectionProcess.thread.Start();
            isServerInitialized = true;
            Debug.Log($"Server {_localEndPoint}: Server started successfully");
        }

        public void Shutdown()
        {
            Debug.Log($"Server {_localEndPoint}: Starting to disable server...");
            
            StopConnectionListener();
            StopAuthenticationThread();
            DisconnectAllClients();
            
            Debug.Log($"Server {_localEndPoint}: Server disabled successfully");
            Debug.Log($"Server {_localEndPoint}: Starting to shutdown sockets...");
            
            if (_serverTcp.Connected)
            {
                _serverTcp.Shutdown(SocketShutdown.Both);
            }
            
            _serverUdp.Close();
            _serverTcp.Close();
            
            Debug.Log($"Server {_localEndPoint}: Sockets shutdown successfully");
        }
        public void SendToAll(byte[] data)
        {
            Debug.Log($"Server {_localEndPoint}: Sending Udp data to all...");
            foreach (ClientData client in _clientList)
            { 
                if (client.id != 0)
                {
                    // For UDP SendTo() is obligatory, or just use Connect() before if not using SendTo.
                    Debug.Log($"Server {_localEndPoint}: Sending Udp data to client {client.username} with Id: {client.id} and EP: {client.metaData.endPoint}:");
                    try
                    {
                        _serverUdp.SendTo(data, data.Length, SocketFlags.None, client.metaData.endPoint);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Server {_localEndPoint}: Error sending Udp data to all clients: {e.Message}");
                    }

                } 
            }
            // foreach (ClientData client in _clientList)
            // {
            //     if (client.ID != 0)
            //     {
            //         Debug.Log("Server: sending to client " + client.Username);
            //         client.ConnectionUDP.SendTo(data, data.Length, SocketFlags.None, client.ConnectionUDP.RemoteEndPoint);
            //     }
            // }
        }

        public void SendCriticalToAll(byte[] data)
        {
            Debug.Log($"Server {_localEndPoint}: Sending critical Tcp data to all...");
            foreach (ClientData client in _clientList)
            { 
                if (client.id != 0)
                {
                    Debug.Log($"Server {_localEndPoint}: Sending critical Tcp data to client {client.username} with Id: {client.id} and EP: {client.metaData.endPoint}:");
                    try
                    {
                        client.connectionTcp
                            .Send(data); // For TCP SendTo isn't necessary since connection is established already.
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Server {_localEndPoint}: Error sending critical Tcp data to all clients: {e.Message}");
                    }
                }
            }
            // foreach (ClientData client in _clientList)
            // {
            //     if (client.ID != 0) client.ConnectionTcp.SendTo(data, data.Length, SocketFlags.None, client.ConnectionTcp.RemoteEndPoint);
            // }
        }

        public void SendCritical(UInt64 id, byte[] data)
        {
            ClientData client = _clientList.First(cl => cl.id == id);
            Debug.Log($"Server {_localEndPoint}: Sending critical Tcp data to client {client.username} with Id: {client.id} and EP: {client.metaData.endPoint}:");
            try
            {
                client.connectionTcp.Send(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"Server {_localEndPoint}: Error sending critical Tcp data to client {client.username} with Id: {client.id} and EP: {client.metaData.endPoint}: {e}");
            }
                // foreach (ClientData client in _clientList)
                // { 
                //     if (client.id == id)
                //     {
                //         
                //         client.connectionTcp.Send(data); // For TCP SendTo isn't necessary since connection is established already.
                //     }
                // }
        }
        
            // foreach (ClientData client in _clientList)
            // {
            //     if (client.ID == Id)
            //     {
            //         client.ConnectionTcp.SendTo(data, data.Length, SocketFlags.None, client.ConnectionTcp.RemoteEndPoint);
            //         return;
            //     }
            // }

        public void SendToClient(UInt64 clientId, byte[] data)
        {
            foreach (ClientData client in _clientList)
            {
                if (client.id == clientId)
                {
                    Debug.Log($"Server {_localEndPoint}: Sending Udp data to client {client.username} with Id: {client.id} and EP: {client.metaData.endPoint}:");
                    // For UDP SendTo() is obligatory, or just use Connect() before if not using SendTo.
                    try
                    {
                        _serverUdp.SendTo(data, data.Length, SocketFlags.None, client.metaData.endPoint);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Server {_localEndPoint}: Error sending Ucp data to Client {client.username} with Id: {client.id} and EP: {client.metaData.endPoint}: {e}");
                    }
                    //client.ConnectionUDP.SendTo(data, data.Length, SocketFlags.None, client.ConnectionUDP.RemoteEndPoint);
                    return;
                }
            }
        }
        private ManualResetEvent _connectionListenerEvent = new ManualResetEvent(false);

        private void StartConnectionListener(CancellationToken token)
        {
            Debug.Log($"Server {_localEndPoint}: Starting connection listener");
            try
            {
                while (!token.IsCancellationRequested)
                {
                    _connectionListenerEvent.Reset(); // Reset the event before waiting for a connection

                    // Asynchronously wait for a connection or cancellation signal
                    IAsyncResult asyncResult = _serverTcp.BeginAccept(new AsyncCallback(AcceptCallback), null);

                    // Wait for either a connection or a cancellation signal
                    int waitResult = WaitHandle.WaitAny(new WaitHandle[] { asyncResult.AsyncWaitHandle, token.WaitHandle });

                    // If the wait result is for the cancellation token, exit the loop
                    if (waitResult == 1)
                    {
                        Debug.Log($"Server {_localEndPoint}: Ending connection listener");
                        break;
                    }
                    _connectionListenerEvent.Set();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server {_localEndPoint}: exception:");
                Debug.LogException(e);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the asynchronous operation
                Socket incomingConnection = null;
                try
                {
                    incomingConnection = _serverTcp.EndAccept(ar);
                    Debug.Log($"Server {_localEndPoint}: Incoming connection -> Local EP {incomingConnection.LocalEndPoint}, Remote EP {incomingConnection.RemoteEndPoint}");
                }
                catch (ObjectDisposedException ode)
                {
                    // Handle the case where the socket is already disposed
                    Debug.LogError($"Server {_localEndPoint}: exception {ode}");
                    return;
                }
                
                if (incomingConnection == null || incomingConnection.Handle == IntPtr.Zero || incomingConnection.Connected == false)
                {
                    // The socket is not valid; handle accordingly
                    Debug.LogError($"Server {_localEndPoint}: Incoming connection is not valid");
                    return;
                }
                
                IPEndPoint ipEndPoint = incomingConnection.RemoteEndPoint as IPEndPoint;
                if (ipEndPoint == null) throw new ArgumentNullException(nameof(ipEndPoint));
               
                // Check if the socket is connected
                if (!IsSocketConnected(incomingConnection))
                {
                    Debug.LogWarning($"Server {_localEndPoint}: Incoming connection is not connected, closing it");
                    incomingConnection.Close();
                    return;
                }

                //check that the incoming socket is not being process twice
                foreach (ClientData client in _clientList)
                {
                    if (client.metaData.endPoint.Equals(ipEndPoint))
                    {
                        Debug.LogWarning($"Server {_localEndPoint}: Incoming connection is being processed twice, closing it");
                        incomingConnection.Close();
                    }
                }

                foreach (KeyValuePair<IPEndPoint, Socket> process in _authenticationConnections)
                {
                    if (process.Key.Equals(ipEndPoint))
                    {
                        Debug.LogWarning($"Server {_localEndPoint}: Incoming connection is already in _authenticationConnections, closing it");
                        incomingConnection.Close();
                    }
                }

                if (IsSocketConnected(incomingConnection))
                {
                    if (_clientList.Count == 0)
                    {
                        if (ipEndPoint.Address.Equals(IPAddress.Loopback))
                        {
                            Debug.Log($"Server {_localEndPoint}: Incoming connection is local host, storing host client");
                            StoreClient(incomingConnection, "Host", true);
                        }
                    }
                    else
                    {
                        Debug.Log($"Server {_localEndPoint}: Incoming connection is not host, creating normal client");
                        StoreClient(incomingConnection, $"User_{_clientList.Count+1}", true);
                    }
                    // //if not local host
                    // Debug.Log("Socket address: " + ipEndPoint.Address + " local address:" + IPAddress.Loopback);
                    // if (ipEndPoint.Address.Equals(IPAddress.Loopback))
                    // {
                    //     Debug.Log("Server : host connected ... ");
                    //     CreateClient(incomingConnection, "Host", true);
                    // }
                    // else
                    // {
                    //     CreateClient(incomingConnection, "Melon", true);
                    //     //Process authenticate = new Process();
                    //
                    //     //authenticate.cancellationToken = new CancellationTokenSource();
                    //     //authenticate.thread = new Thread(() => Authenticate(incomingConnection, authenticate.cancellationToken.Token));
                    //     //authenticate.thread.Start();
                    //
                    //     //_authenticationConnections[IpEndPoint] = incomingConnection;
                    //     //_authenticationProcesses[IpEndPoint] = authenticate;
                    //     //_authenticators[IpEndPoint] = new ServerAuthenticator();
                    // }
                }

                _connectionListenerEvent.Set(); // Set the event to allow the loop to continue waiting for connections
            }
            catch (Exception e)
            {
                Debug.LogError($"Server {_localEndPoint}: exception:");
                Debug.LogException(e);
            }
        }

        private void HandleClient(ClientData clientData)
        {
            Debug.Log($"Server {_localEndPoint}: Starting client thread {clientData.username} with Id: {clientData.id} and EP: {clientData.metaData.endPoint}");
            try
            {
                while (!clientData.listenProcess.cancellationToken.Token.IsCancellationRequested)
                {
                    if (!clientData.connectionTcp.Connected)
                    {
                        Debug.Log($"Server {_localEndPoint}: Tcp is not connected client {clientData.username} with Id: {clientData.id} and EP: {clientData.metaData.endPoint}");
                        // Handle the case where TCP is not connected if needed
                        break; // Exit the loop if TCP is not connected
                    }

                    if (clientData.connectionTcp.Available > 0)
                    {
                        ReceiveTcpSocketData(clientData.connectionTcp);
                    }

                    // if (_serverUdp.Available > 0) // For connectionless protocols (UDP), the available property won't work as intended like in TCP.
                    ReceiveUDPSocketData(_serverUdp, clientData);
                    
                    Thread.Sleep(10);
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset ||
                    se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // Handle client disconnection (optional)
                    Debug.Log($"Server {_localEndPoint}: Client {clientData.username} with Id: {clientData.id} and EP: {clientData.metaData.endPoint} just disconnected: {se.Message}");
                    RemoveClient(clientData);
                }
                else
                {
                    // Handle other socket exceptions
                    Debug.LogError($"Server {_localEndPoint}: SocketException: {se.SocketErrorCode}, {se.Message}");
                }
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Server {_localEndPoint}: Exception: {e.Message}");
            }
        }

        private void ReceiveTcpSocketData(Socket socket)
        {
            try
            {
                lock (NetworkManager.Instance.IncomingStreamLock)
                {
                    Debug.LogError($"Server {_localEndPoint}: Has received Tcp Data from {socket.RemoteEndPoint}");
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
                Debug.LogError($"Server {_localEndPoint}: SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Server {_localEndPoint}: Exception: {e.Message}");
            }
        }

        private void ReceiveUDPSocketData(Socket socket, ClientData clientData)
        {
            try
            {
                lock (NetworkManager.Instance.IncomingStreamLock)
                {
                    if (socket.Poll(1000, SelectMode.SelectRead)) // Wait up to 1 seconds for data to arrive
                    {
                        Debug.LogError($"Server {_localEndPoint}: Has received Tcp Data from {socket.RemoteEndPoint}");
                        byte[] buffer = new byte[1500];
                        EndPoint senderEndPoint = clientData.metaData.endPoint;
                        int size = socket.ReceiveFrom(buffer, ref senderEndPoint);
                        MemoryStream stream = new MemoryStream(buffer, 0, size);
                        NetworkManager.Instance.AddIncomingDataQueue(stream);
                    }
                    // byte[] buffer = new byte[1500];
                    //
                    // // Receive data from the client
                    // int size = socket.Receive(buffer, buffer.Length, SocketFlags.None);
                    // MemoryStream stream = new MemoryStream(buffer, 0, size);
                    // NetworkManager.Instance.AddIncomingDataQueue(stream);
                }
            }
            catch (SocketException se)
            {
                // Handle other socket exceptions
                Debug.LogError($"Server {_localEndPoint}: SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Server {_localEndPoint}: Exception: {e.Message}");
            }
        }

        private UInt64 StoreClient(Socket clientSocket, string userName, bool isHost)
        {
            lock (_clientList)
            {
                ClientData clientData = new ClientData();
                clientData.id = _clientManager.GetNextClientId();
                clientData.username = userName;
                clientData.isHost = isHost;
                clientData.connectionTcp = clientSocket;
                //add a time out exeption for when the client disconnects or has lag or something
                clientData.connectionTcp.ReceiveTimeout = Timeout.Infinite;
                clientData.connectionTcp.SendTimeout = Timeout.Infinite;
                
                // NO NEED TO THIS UNLIKE IN TCP CONNECTIONS WHERE YOU HAVE A CONNECTION ORIENTED PROTOCOL
                // YOU USUALLY SAVE THE SOCKETS FOR THAT CONNECTION, IN UDP YOU DON'T
                // _ServerUdp IS the RESPONSIBLE FOR BOTH SENDING AND LISTENING,
                // YOU JUST HAVE TO SPECIFY WHAT CLIENTS YOU WANT TO LISTEN OR SEND AT EACH MOMENT.
                
                //create udp connection 
                // clientData.ConnectionUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                // clientData.ConnectionUDP.Bind(clientSocket.LocalEndPoint);
                // clientData.ConnectionUDP.ReceiveTimeout = Timeout.Infinite;
                // clientData.ConnectionUDP.SendTimeout = Timeout.Infinite;

                // Local in this case is server, remote is client
                IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint; 
                clientData.metaData.endPoint = clientEndPoint;

                //Create the process for that client
                //create a hole thread to recive important data from server-client
                //like game state, caharacter selection, map etc
                Process clientProcess = new Process();
                clientProcess.Name = "Handle Client " + clientData.id.ToString();
                clientProcess.cancellationToken = new CancellationTokenSource();
                clientProcess.thread = new Thread(() => HandleClient(clientData));
                clientProcess.thread.IsBackground = true;
                clientProcess.thread.Name  = "Handle Clinet " + clientData.id.ToString();
                clientProcess.thread.Start();
                clientData.listenProcess = clientProcess;
                _clientList.Add(clientData);
                
                Debug.Log($"Server {_localEndPoint}: Client {clientData.username} stored with Id: {clientData.id} and EP: {clientData.metaData.endPoint}");
                
                SendClientID(clientData.id);
                return clientData.id;
            }
        }

        private void RemoveClient(ClientData clientData)
        {
            Debug.Log($"Server {_localEndPoint}: Client {clientData.username} removed with Id: {clientData.id} and EP: {clientData.metaData.endPoint}");
            try
            {
                // Remove the client from the list of connected clients    
                lock (_clientList)
                {
                    // Shutdown client thread
                    clientData.listenProcess.Shutdown();

                    // Close the client's socket
                    if (clientData.connectionTcp.Connected)
                    {
                        clientData.connectionTcp.Shutdown(SocketShutdown.Both);
                    }

                    clientData.connectionTcp.Close();
                    _clientListToRemove.Add(clientData);
                    Debug.Log($"Server {_localEndPoint}: Client {clientData.username} with Id: {clientData.id} and EP: {clientData.metaData.endPoint} disconnected:");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           
        }

        #endregion

        private static bool IsSocketConnected(Socket socket)
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

        private void SendClientID(UInt64 id)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)PacketType.ID);
            writer.Write(id);
            SendCritical(id, stream.ToArray());
        }

        #region Authentication

/*
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
                        ReceiveTCPSocketData(incomingSocket);
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
*/

/*
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
*/

/*
        void AuthenticationFailed(IPEndPoint endpoint)
        {
            //remove connection
            _authenticationProcesses[endpoint].Shutdown();
            _authenticationProcesses.Remove(endpoint);
            _authenticationConnections[endpoint].Close();
            _authenticationConnections.Remove(endpoint);
        }
*/

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
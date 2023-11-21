#define AUTHENTICATION_CODE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using _Scripts.Networking.Network_Behaviours;
using Unity.VisualScripting;
using UnityEngine;

namespace _Scripts.Networking
{
    public class AServer
    {
        public AServer(IPEndPoint localEndPointTcp, IPEndPoint localEndPointUdp)
        {
            _localEndPointTcp = localEndPointTcp;
            _localEndPointUdp = localEndPointUdp;
            _nextClientId = 0;
            InitServer();
            NetworkManager.Instance.OnHostCreated += portUdp => _clientsList[0].endPointUdp.Port = portUdp;
        }
        
        #region Fields
        private IPEndPoint _localEndPointTcp;
        private IPEndPoint _localEndPointUdp;
        private Socket _serverTcp;
        private Socket _serverUdp;
        public bool isServerInitialized { get; private set; } = false;
        private UInt64 _nextClientId = 0;
        
        private List<ClientData> _clientsList = new List<ClientData>();
        private List<ClientData> _clientsToRemove = new List<ClientData>();
        
        private Process _listenConnectionsProcess;
        private ManualResetEvent _connectionListenerEvent = new ManualResetEvent(false);
        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        private List<ServerAuthenticator> _authenticationProcesses = new List<ServerAuthenticator>();
        // private Dictionary<IPEndPoint, Process> _authenticationProcesses = new Dictionary<IPEndPoint, Process>();
        // private Dictionary<IPEndPoint, Socket> _authenticationConnections = new Dictionary<IPEndPoint, Socket>();
        // private Dictionary<IPEndPoint, ServerAuthenticator> _authenticators = new Dictionary<IPEndPoint, ServerAuthenticator>();
        // private List<Process> _authenticationProcessList = new List<Process>();
        
        #endregion

        #region  Actions
        public Action<ClientData> onClientAccepted;
        public Action<UInt64> onClientRemoved;
        public Action<UInt64> onClientDisconnected;
        public Action<UInt64, byte[]> onDataRecieved;
        #endregion
        
        #region Disconnections & Threads Cancellation
        private void StopAuthenticationThread()
        {
            try
            {
                Debug.Log($"Server {_localEndPointTcp}: Stopping authentication threads");
                foreach (ServerAuthenticator authProcess in _authenticationProcesses)
                {
                    authProcess.clientBeingAuthenticated.listenProcess.Shutdown();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server {_localEndPointTcp}: Error stopping authentication threads {e}");
                throw;
            }
        }
        private void StopConnectionListener()
        {
            _listenConnectionsProcess.Shutdown();
        }

        private void DisconnectAllClients()
        {
            lock(_clientsList) 
            {
                Debug.Log($"Server {_localEndPointTcp}: Disconnecting all clients");
                foreach (ClientData client in _clientsList)
                {
                    RemoveClient(client);
                }
            }
        }
        private void UpdatePendingDisconnections()
        {
            lock (_clientsList)
            {
                if (_clientsToRemove.Count > 0)
                {
                    lock (_clientsList)
                    {
                        foreach (ClientData clientToRemove in _clientsToRemove)
                        {
                            //Debug.Log($"Server {_localEndPoint}: Removing {clientToRemove.metaData.endPoint}");
                            _clientsList.Remove(clientToRemove);
                        }
                    }
                    Debug.Log($"Server {_localEndPointTcp}: Removed {_clientsToRemove.Count} clients");
                    _clientsToRemove.Clear();
                }
            }
        }
        #endregion

        #region Init and Disable
        public void InitServer()
        {
            Debug.Log($"Server: Starting server... TCP LOCAL ENDPOINT:{_localEndPointTcp}, UDP LOCAL ENDPOINT:{_localEndPointUdp}:");
            
            _serverTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            _serverTcp.Bind(_localEndPointTcp);
            _serverUdp.Bind(_localEndPointUdp);

            _serverTcp.Listen(4);
            _listenConnectionsProcess = new Process
            {
                cancellationToken = new CancellationTokenSource()
            };
            _listenConnectionsProcess.thread = new Thread(() =>
                StartConnectionListener(_listenConnectionsProcess.cancellationToken.Token));
            _listenConnectionsProcess.thread.Start();
            isServerInitialized = true;
            Debug.Log($"Server {_localEndPointTcp}: Server started successfully");
        }

        public void Shutdown()
        {
            Debug.Log($"Server {_localEndPointTcp}: Starting to disable server...");
            //frist disconnect all sockets
            if (_serverTcp.Connected)
            {
                _serverTcp.Shutdown(SocketShutdown.Both);
            }
            _serverUdp.Close();
            _serverTcp.Close();

            StopConnectionListener();
            StopAuthenticationThread();
            DisconnectAllClients();

            Debug.Log($"Server {_localEndPointTcp}: Sockets shutdown successfully");
        }
        #endregion

        #region Data Transmission
         public void SendUdpToAll(byte[] data)
        {
            Debug.Log($"Server {_localEndPointTcp}: Sending Udp data to all...");
            foreach (ClientData client in _clientsList)
            { 
                if (client.id != 0)
                {
                    // For UDP SendTo() is obligatory, or just use Connect() before if not using SendTo.
                    Debug.Log($"Server {_localEndPointTcp}: Sending Udp data to client {client.userName} with Id: {client.id} and EP: {client.endPointUdp}:");
                    try
                    {
                        _serverUdp.SendTo(data, data.Length, SocketFlags.None, client.endPointUdp);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Server {_localEndPointTcp}: Error sending Udp data to all clients: {e.Message}");
                    }

                } 
            }
        }

        public void SendTcpToAll(byte[] data)
        {
            // Debug.Log($"Server {_localEndPointTcp}: Sending critical Tcp data to all...");
            foreach (ClientData client in _clientsList)
            { 
                if (client.id != 0)
                {
                    Debug.Log($"Server {_localEndPointTcp}: Sending critical Tcp data to client {client.userName} with Id: {client.id} and EP: {client.endPointTcp}:");
                    try
                    {
                        client.connectionTcp
                            .Send(data); // For TCP SendTo isn't necessary since connection is established already.
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Server {_localEndPointTcp}: Error sending critical Tcp data to all clients: {e.Message}");
                    }
                }
            }
            // foreach (ClientData client in _clientList)
            // {
            //     if (client.ID != 0) client.ConnectionTcp.SendTo(data, data.Length, SocketFlags.None, client.ConnectionTcp.RemoteEndPoint);
            // }
        }

        public void SendTcp(UInt64 id, byte[] data)
        {
            ClientData client = _clientsList.First(cl => cl.id == id);
            Debug.Log($"Server {_localEndPointTcp}: Sending critical Tcp data to client {client.userName} with Id: {client.id} and EP: {client.endPointTcp}:");
            try
            {
                client.connectionTcp.Send(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"Server {_localEndPointTcp}: Error sending critical Tcp data to client {client.userName} with Id: {client.id} and EP: {client.endPointTcp}: {e}");
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

        public void SendUdp(UInt64 clientId, byte[] data)
        {
            foreach (ClientData client in _clientsList)
            {
                if (client.id == clientId)
                {
                    Debug.Log($"Server {_localEndPointTcp}: Sending Udp data to client {client.userName} with Id: {client.id} and EP: {client.endPointUdp}:");
                    // For UDP SendTo() is obligatory, or just use Connect() before if not using SendTo.
                    try
                    {
                        _serverUdp.SendTo(data, data.Length, SocketFlags.None, client.endPointUdp);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Server {_localEndPointTcp}: Error sending Ucp data to Client {client.userName} with Id: {client.id} and EP: {client.endPointUdp}: {e}");
                    }
                    //client.ConnectionUDP.SendTo(data, data.Length, SocketFlags.None, client.ConnectionUDP.RemoteEndPoint);
                    return;
                }
            }
        }
        private void ListenDataFromClient(ClientData clientData)
        {
            Debug.Log($"Server {_localEndPointTcp}: Starting client thread {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp}");
            try
            {
                while (!clientData.listenProcess.cancellationToken.Token.IsCancellationRequested)
                {
                    if (!clientData.connectionTcp.Connected)
                    {
                        Debug.Log($"Server {_localEndPointTcp}: Tcp is not connected client {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp}");
                        // Handle the case where TCP is not connected if needed
                        break; // Exit the loop if TCP is not connected
                    }

                    if (clientData.connectionTcp.Available > 0)
                    {
                        ReceiveTcpSocketData(clientData.connectionTcp);
                    }

                    // if (_serverUdp.Available > 0) // For connectionless protocols (UDP), the available property won't work as intended like in TCP.
                    if(clientData.connectionUdp != null)
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
                    Debug.Log($"Server {_localEndPointTcp}: Client {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp} just disconnected: {se.Message}");
                    RemoveClient(clientData);
                }
                else
                {
                    // Handle other socket exceptions
                    Debug.LogError($"Server {_localEndPointTcp}: SocketException: {se.SocketErrorCode}, {se.Message}");
                }
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Server {_localEndPointTcp}: Exception: {e.Message}");
            }
        }
         private void ReceiveTcpSocketData(Socket socket)
        {
            try
            {
                lock (NetworkManager.Instance.IncomingStreamLock)
                {
                    Debug.LogError($"Server {_localEndPointTcp}: Has received Tcp Data from {socket.RemoteEndPoint}");
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
                Debug.LogError($"Server {_localEndPointTcp}: SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Server {_localEndPointTcp}: Exception: {e.Message}");
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
                        Debug.LogError($"Server {_localEndPointTcp}: Has received Tcp Data from {socket.RemoteEndPoint}");
                        byte[] buffer = new byte[1500];
                        EndPoint senderEndPoint = clientData.endPointUdp;
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
                Debug.LogError($"Server {_localEndPointTcp}: SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Server {_localEndPointTcp}: Exception: {e.Message}");
            }
        }
        #endregion

        #region Connections
        private void StartConnectionListener(CancellationToken token)
        {
            Debug.Log($"Server {_localEndPointTcp}: Starting connection listener");
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Socket incomingSocket = _serverTcp.Accept();
                    AcceptNewClient(incomingSocket);
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server {_localEndPointTcp}: exception:");
                Debug.LogException(e);
            }
        }

        private void AcceptNewClient(Socket incomingConnection)
        {
            
            // Check if the socket is valid
            if (incomingConnection.Handle == IntPtr.Zero || incomingConnection.Connected == false)
            {
                Debug.LogError($"Server {_localEndPointTcp}: Incoming connection is not valid");
                return;
            }
            
            IPEndPoint ipEndPoint = incomingConnection.RemoteEndPoint as IPEndPoint;
            
            // Check if the socket is connected
            if (!IsSocketConnected(incomingConnection))
            {
                Debug.LogWarning($"Server {_localEndPointTcp}: Incoming connection {ipEndPoint} is not connected, closing it");
                incomingConnection.Close();
                return;
            }

            // Check that the incoming socket is not being process twice
            foreach (ClientData client in _clientsList)
            {
                if (client.endPointTcp.Equals(ipEndPoint))
                {
                    Debug.LogWarning($"Server {_localEndPointTcp}: Incoming connection {ipEndPoint} is being processed twice, closing it");
                    incomingConnection.Close();
                    return;
                }
            }
            
            // Check that the incoming socket is not being authenticated twice
            foreach (ServerAuthenticator authProcess in _authenticationProcesses)
            {
                if (authProcess.clientEndPointTcp.Equals(ipEndPoint))
                {
                    Debug.LogWarning($"Server {_localEndPointTcp}: Incoming connection {ipEndPoint} is already in _authenticationProcesses, closing it");
                    incomingConnection.Close();
                    return;
                }
            }
            
            // After all checks create a new client and put it on verification


            UInt64 newId = GetNextClientID();
            if (_clientsList.Count == 0 && ipEndPoint.Address.Equals(IPAddress.Loopback)) // Host client
            {
                Debug.Log($"Server {_localEndPointTcp}: Incoming connection is possible local host, authenticating possible host client");
                //serverAuthenticator = new ServerAuthenticator(incomingConnection, newClientData => StoreAuthenticatedClient(newClientData, true), null);
                ClientData hostClient = new ClientData();

                hostClient.connectionTcp = incomingConnection;
                hostClient.endPointTcp = incomingConnection.RemoteEndPoint as IPEndPoint;
                hostClient.endPointUdp = new IPEndPoint(IPAddress.Loopback,0000);
                hostClient.id = newId;
                StoreAuthenticatedClient(hostClient, true);
                Debug.Log($"Server {_localEndPointTcp}: Successfully created host!");
            }
            else
            {
                ServerAuthenticator serverAuthenticator;
                Debug.Log($"Server {_localEndPointTcp}: Incoming client: " + newId);
                serverAuthenticator = new ServerAuthenticator(incomingConnection, newClientData => StoreAuthenticatedClient(newClientData,false), null);

                serverAuthenticator.clientBeingAuthenticated.id = newId;
                serverAuthenticator.clientBeingAuthenticated.listenProcess =  new Process();;
                serverAuthenticator.clientBeingAuthenticated.listenProcess.Name = $"Handle Client {newId}";
                serverAuthenticator.clientBeingAuthenticated.listenProcess.cancellationToken = new CancellationTokenSource();
                serverAuthenticator.clientBeingAuthenticated.listenProcess.thread = new Thread(() => ListenDataFromClient(serverAuthenticator.clientBeingAuthenticated));
                serverAuthenticator.clientBeingAuthenticated.listenProcess.thread.IsBackground = true;
                serverAuthenticator.clientBeingAuthenticated.listenProcess.thread.Name = $"Handle Client {newId}";
                serverAuthenticator.clientBeingAuthenticated.listenProcess.thread.Start();
                serverAuthenticator.RequestClientToStartAuthentication();
                
                _authenticationProcesses.Add(serverAuthenticator);
            }
            _connectionListenerEvent.Set(); // Set the event to allow the loop to continue waiting for connections
        }
        private UInt64 StoreAuthenticatedClient(ClientData clientData, bool isHost = false)
        {
            lock (_clientsList)
            {
                clientData.isHost = isHost;
                //add a time out exeption for when the client disconnects or has lag or something
                clientData.connectionTcp.ReceiveTimeout = Timeout.Infinite;
                clientData.connectionTcp.SendTimeout = Timeout.Infinite;
                _clientsList.Add(clientData);

                lock (_authenticationProcesses)
                {
                    ServerAuthenticator toRemove = null;
                    foreach (ServerAuthenticator process in _authenticationProcesses)
                    {
                        if (clientData.id == process.clientBeingAuthenticated.id)
                        {
                            toRemove = process;
                            break;
                        }
                    }
                    
                    if(toRemove != null)
                        _authenticationProcesses.Remove(toRemove);
                }
                
                Debug.Log($"Server {_localEndPointTcp}: Client {clientData.userName} stored with Id: {clientData.id} and EP: {clientData.endPointTcp}");
                return clientData.id;
            }
        }

        private void RemoveClient(ClientData clientData)
        {
            Debug.Log($"Server {_localEndPointTcp}: Client {clientData.userName} trying to remove with Id: {clientData.id} and EP: {clientData.endPointTcp}");
            try
            {
                // Remove the client from the list of connected clients    
                lock (_clientsList)
                {
                    // Shutdown client thread
                    clientData.listenProcess.Shutdown();

                    // Close the client's socket
                    if (clientData.connectionTcp.Connected)
                    {
                        clientData.connectionTcp.Shutdown(SocketShutdown.Both);
                    }

                    clientData.connectionTcp.Close();
                    _clientsToRemove.Add(clientData);
                    Debug.Log($"Server {_localEndPointTcp}: Client {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp} disconnected successfully");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Server {_localEndPointTcp}: Client {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp} failed to be removed: {e}");
                throw;
            }
           
        }

        private bool IsSocketConnected(Socket socket)
        {
            try
            {
                // Poll the socket for readability and if it's not readable, it means the socket is closed.
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException se)
            {
                Debug.LogError($"Server {_localEndPointTcp}: {socket} is closed! {se}");
                return false;
            }
        }
        #endregion
        #region Authentication

        private UInt64 GetNextClientID()
        {
            UInt64 currentID = _nextClientId;
            _nextClientId++;
            return currentID;
        }
        public void HandleAuthentication(MemoryStream stream, BinaryReader reader)
        {
            long posToReset = reader.BaseStream.Position;
            
            string ip = reader.ReadString();
            int port = reader.ReadInt32();
            
            IPAddress address = IPAddress.Any;
            if (!IPAddress.TryParse(ip, out address))
            {
                Debug.LogError("Authenticator: Couldn't deserialize IEP!");
            }
            IPEndPoint ipEndPoint = new IPEndPoint(address, port);

            //reset stream position to read the ip and port again on the authenticator
            reader.BaseStream.Position = posToReset;
            
            foreach (ServerAuthenticator authenticator in _authenticationProcesses)
            {
                if (authenticator.clientEndPointTcp.Equals(ipEndPoint))
                {
                    authenticator.HandleAuthentication(stream, reader);
                }
            }

        }
        #endregion
    }
}
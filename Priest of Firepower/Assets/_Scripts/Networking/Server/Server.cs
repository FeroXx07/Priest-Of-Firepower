using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using _Scripts.Networking.Authentication;
using _Scripts.Networking.Client;
using Debug = UnityEngine.Debug;
using Process = _Scripts.Networking.Utility.Process;

namespace _Scripts.Networking.Server
{
    public class Server
    {
        public Server(IPEndPoint localEndPointTcp, IPEndPoint localEndPointUdp)
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
        public ushort _currentTick { get; private set; } = 0;
        public bool isServerInitialized { get; private set; } = false;
        private UInt64 _nextClientId = 0;
        private List<ClientData> _clientsList = new List<ClientData>();
        private List<ClientData> _clientsToRemove = new List<ClientData>();
        public Dictionary<ClientData, DeliveryNotificationManager> deliveryNotificationManagers = new();
        private Process _listenConnectionsProcess;
        private ManualResetEvent _connectionListenerEvent = new ManualResetEvent(false);

        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        private List<ServerAuthenticator> _authenticationProcesses = new List<ServerAuthenticator>();
        private Dictionary<IPEndPoint, Socket> _incomingConnections = new Dictionary<IPEndPoint, Socket>();

        #endregion

        #region Actions

        public Action onClientConnected;
        public Action<UInt64, string> onClientDisconnected;

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
            lock (_clientsList)
            {
                Debug.Log($"Server {_localEndPointTcp}: Disconnecting all clients");
                foreach (ClientData client in _clientsList)
                {
                    RemoveClient(client);
                }
            }
        }

        public void UpdatePendingDisconnections() // executed on network manager update
        {
            lock (_clientsToRemove)
            {
                if (_clientsToRemove.Count > 0)
                {
                    foreach (ClientData clientToRemove in _clientsToRemove)
                    {
                        RemoveClient(clientToRemove);
                    }

                    _clientsToRemove.Clear();
                }
            }
        }

        #endregion

        #region Init and Disable

        public void InitServer()
        {
            Debug.Log(
                $"Server: Starting server... TCP LOCAL ENDPOINT:{_localEndPointTcp}, UDP LOCAL ENDPOINT:{_localEndPointUdp}:");
            _serverTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverTcp.Bind(_localEndPointTcp);
            _serverUdp.Bind(_localEndPointUdp);
            _serverTcp.Listen(4);
            _listenConnectionsProcess = new Process { cancellationToken = new CancellationTokenSource() };
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
            _serverTcp.Shutdown(SocketShutdown.Both);
            _serverUdp.Shutdown(SocketShutdown.Both);
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
            //Debug.Log($"Server {_localEndPointTcp}: Sending Udp data to all...");
            foreach (ClientData client in _clientsList)
            {
                if (client.id != 0)
                {
                    // For UDP SendTo() is obligatory, or just use Connect() before if not using SendTo.
                    //Debug.Log($"Server {_localEndPointTcp}: Sending Udp data to client {client.userName} with Id: {client.id} and EP: {client.endPointUdp}:");
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
                    Debug.Log(
                        $"Server {_localEndPointTcp}: Sending critical Tcp data to client {client.userName} with Id: {client.id} and EP: {client.endPointTcp}:");
                    try
                    {
                        client.connectionTcp
                            .Send(data); // For TCP SendTo isn't necessary since connection is established already.
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Server {_localEndPointTcp}: Error sending critical Tcp data to all clients: {e.Message}");
                    }
                }
            }
        }

        public void SendTcp(UInt64 id, byte[] data)
        {
            ClientData client = _clientsList.First(cl => cl.id == id);
            Debug.Log(
                $"Server {_localEndPointTcp}: Sending critical Tcp data to client {client.userName} with Id: {client.id} and EP: {client.endPointTcp}:");
            try
            {
                client.connectionTcp.Send(data);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Server {_localEndPointTcp}: Error sending critical Tcp data to client {client.userName} with Id: {client.id} and EP: {client.endPointTcp}: {e}");
            }
        }

        public void SendUdp(UInt64 clientId, byte[] data)
        {
            foreach (ClientData client in _clientsList)
            {
                if (client.id == clientId)
                {
                    Debug.Log(
                        $"Server {_localEndPointTcp}: Sending Udp data to client {client.userName} with Id: {client.id} and EP: {client.endPointUdp}:");
                    // For UDP SendTo() is obligatory, or just use Connect() before if not using SendTo.
                    try
                    {
                        _serverUdp.SendTo(data, data.Length, SocketFlags.None, client.endPointUdp);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Server {_localEndPointTcp}: Error sending Ucp data to Client {client.userName} with Id: {client.id} and EP: {client.endPointUdp}: {e}");
                    }

                    //client.ConnectionUDP.SendTo(data, data.Length, SocketFlags.None, client.ConnectionUDP.RemoteEndPoint);
                    return;
                }
            }
        }

        private void ListenDataFromClient(ClientData clientData)
        {
            Debug.Log(
                $"Server {_localEndPointTcp}: Starting client thread {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp}");
            try
            {
                while (!clientData.listenProcess.cancellationToken.Token.IsCancellationRequested)
                {
                    //Debug.Log(".");
                    if (!clientData.connectionTcp.Connected)
                    {
                        Debug.Log(
                            $"Server {_localEndPointTcp}: Tcp is not connected client {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp}");
                        _clientsToRemove.Add(clientData);
                        // Handle the case where TCP is not connected if needed
                        break; // Exit the loop if TCP is not connected
                    }

                    //check if time out has passed then add to remove client
                    if (NetworkManager.Instance.IsHost() && clientData.heartBeatStopwatch != null &&
                        clientData.state != ClientSate.DISCONNECTED && clientData.disconnectTimeout <
                        clientData.heartBeatStopwatch.ElapsedMilliseconds)
                    {
                        Debug.Log("Server: Heartbeat timeout ...");
                        clientData.state = ClientSate.DISCONNECTED;
                        _clientsToRemove.Add(clientData);
                    }

                    while (clientData.connectionTcp.Available > 0)
                    {
                        ReceiveTcpSocketData(clientData.connectionTcp);
                    }

                    // if (_serverUdp.Available > 0) // For connectionless protocols (UDP), the available property won't work as intended like in TCP.
                    if (clientData.endPointUdp != null) ReceiveUDPSocketData(_serverUdp, clientData);
                    Thread.Sleep(1);
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset ||
                    se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // Handle client disconnection (optional)
                    Debug.Log(
                        $"Server {_localEndPointTcp}: Client {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp} just disconnected: {se.Message}");
                    AddClientToRemove(clientData);
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
                lock (NetworkManager.Instance.incomingStreamLock)
                {
                    Debug.Log($"Server {_localEndPointTcp}: Has received Tcp Data from {socket.RemoteEndPoint}");
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
                lock (NetworkManager.Instance.incomingStreamLock)
                {
                    if (socket.Poll(1000, SelectMode.SelectRead)) // Wait up to 1 seconds for data to arrive
                    {
                        //Debug.Log($"Server {_localEndPointTcp}: Has received Tcp Data from {clientData.endPointUdp}");
                        byte[] buffer = new byte[1500];
                        EndPoint senderEndPoint = clientData.endPointUdp;
                        int size = socket.ReceiveFrom(buffer, ref senderEndPoint);
                        MemoryStream stream = new MemoryStream(buffer, 0, size);
                        NetworkManager.Instance.AddIncomingDataQueue(stream);
                    }
                }
            }
            catch (SocketException se)
            {
                // Handle other socket exceptions
                Debug.LogError($"Server {_localEndPointTcp}: SocketException: {se.SocketErrorCode}, {se.Message}");
                AddClientToRemove(clientData);
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Server {_localEndPointTcp}: Exception: {e.Message}");
                AddClientToRemove(clientData);
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
            //check if that conencion already exist
            if (_incomingConnections.ContainsKey(incomingConnection.RemoteEndPoint as IPEndPoint)) return;
            _incomingConnections[incomingConnection.RemoteEndPoint as IPEndPoint] = incomingConnection;

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
                Debug.LogWarning(
                    $"Server {_localEndPointTcp}: Incoming connection {ipEndPoint} is connected, closing it");
                incomingConnection.Close();
                return;
            }

            // Check that the incoming socket is not being process twice
            foreach (ClientData client in _clientsList)
            {
                if (client.endPointTcp.Equals(ipEndPoint))
                {
                    Debug.LogWarning(
                        $"Server {_localEndPointTcp}: Incoming connection {ipEndPoint} is being processed twice, closing it");
                    incomingConnection.Close();
                    return;
                }
            }

            // Check that the incoming socket is not being authenticated twice
            foreach (ServerAuthenticator authProcess in _authenticationProcesses)
            {
                if (authProcess.clientEndPointTcp.Equals(ipEndPoint))
                {
                    Debug.LogWarning(
                        $"Server {_localEndPointTcp}: Incoming connection {ipEndPoint} is already in _authenticationProcesses, closing it");
                    incomingConnection.Close();
                    return;
                }
            }

            // After all checks create a new client and put it on verification
            UInt64 newId = GetNextClientID();
            if (_clientsList.Count == 0 && ipEndPoint.Address.Equals(IPAddress.Loopback)) // Host client
            {
                Debug.Log(
                    $"Server {_localEndPointTcp}: Incoming connection is possible local host, authenticating possible host client");
                ClientData hostClient = new ClientData();
                hostClient.userName = NetworkManager.Instance.PlayerName;
                hostClient.connectionTcp = incomingConnection;
                hostClient.endPointTcp = incomingConnection.RemoteEndPoint as IPEndPoint;
                hostClient.endPointUdp = new IPEndPoint(IPAddress.Loopback, 0000);
                hostClient.id = newId;
                StoreAuthenticatedClient(hostClient, true);
                Debug.Log($"Server {_localEndPointTcp}: Successfully created host!");
            }
            else
            {
                ServerAuthenticator serverAuthenticator;
                Debug.Log($"Server {_localEndPointTcp}: Incoming client: " + newId);
                serverAuthenticator = new ServerAuthenticator(incomingConnection,
                    newClientData => StoreAuthenticatedClient(newClientData, false), null);
                serverAuthenticator.clientBeingAuthenticated.id = newId;
                serverAuthenticator.clientBeingAuthenticated.listenProcess = new Process();
                ;
                serverAuthenticator.clientBeingAuthenticated.listenProcess.Name = $"Handle Client {newId}";
                serverAuthenticator.clientBeingAuthenticated.listenProcess.cancellationToken =
                    new CancellationTokenSource();
                serverAuthenticator.clientBeingAuthenticated.listenProcess.thread = new Thread(() =>
                    ListenDataFromClient(serverAuthenticator.clientBeingAuthenticated));
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
                clientData.connectionTcp.ReceiveTimeout = Timeout.Infinite;
                clientData.connectionTcp.SendTimeout = Timeout.Infinite;
                clientData.state = ClientSate.AUTHENTICATED;

                //start heartbeat 
                if (!clientData.isHost)
                {
                    clientData.heartBeatStopwatch = new Stopwatch();
                    clientData.heartBeatStopwatch.Start();
                }

                //add connected client to the list
                _clientsList.Add(clientData);

                //Call event that the client is connected successfully
                UnityMainThreadDispatcher.Dispatcher.Enqueue(onClientConnected);

                //shutdown authentication process
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

                    if (toRemove != null)
                        UnityMainThreadDispatcher.Dispatcher.Enqueue(() => _authenticationProcesses.Remove(toRemove));
                }
                
                deliveryNotificationManagers.Add(clientData, new DeliveryNotificationManager());
                Debug.Log(
                    $"Server {_localEndPointTcp}: Client {clientData.userName} stored with Id: {clientData.id} and EP: {clientData.endPointTcp}");
                return clientData.id;
            }
        }

        public void AddClientToRemove(ClientData client)
        {
            if (!_clientsToRemove.Contains(client)) _clientsToRemove.Add(client);
        }

        private void RemoveClient(ClientData clientData)
        {
            Debug.Log(
                $"Server {_localEndPointTcp}: Client {clientData.userName} trying to remove with Id: {clientData.id} and EP: {clientData.endPointTcp}");
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
                    Debug.Log(
                        $"Server {_localEndPointTcp}: Client {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp} disconnected successfully");
                    deliveryNotificationManagers.Remove(clientData);
                    _clientsList.Remove(clientData);
                    UnityMainThreadDispatcher.Dispatcher.Enqueue(() =>
                        onClientDisconnected(clientData.id, clientData.userName));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Server {_localEndPointTcp}: Client {clientData.userName} with Id: {clientData.id} and EP: {clientData.endPointTcp} failed to be removed: {e}");
                throw;
            }
        }

        public List<ClientData> GetClients()
        {
            return _clientsList;
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

        public void HandleAuthentication(BinaryReader reader)
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
            lock (_authenticationProcesses)
            {
                foreach (ServerAuthenticator authenticator in _authenticationProcesses)
                {
                    if (authenticator.clientEndPointTcp.Equals(ipEndPoint))
                    {
                        authenticator.HandleAuthentication(reader);
                    }
                }
            }
        }

        #endregion

        #region TickSync

        public void FixedUpdate()
        {
            //send every 200 ticks
            if (_currentTick % 200 == 0)
            {
                SendSyncTick();
                //Debug.Log($"Sending tick: {_currentTick}");
            }

            _currentTick++;
        }

        void SendSyncTick()
        {
            MemoryStream newStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(newStream);
            writer.Write(_currentTick);
            Packet syncPacket = new Packet(PacketType.SYNC, ulong.MinValue, ulong.MinValue, long.MinValue,
                Int32.MinValue, false, newStream.ToArray());
            SendUdpToAll(syncPacket.allData);
        }

        #endregion

        #region Heart Beat

        public void HandleHeartBeat(Packet packet, BinaryReader reader)
        {
            UInt64 id = reader.ReadUInt64();
            foreach (ClientData clientData in _clientsList)
            {
                if (clientData.id == id)
                {
                    if (NetworkManager.Instance.debugShowPingPackets)
                        Debug.Log("Server: Received heartbeat client:" + id);
                    if (clientData.heartBeatStopwatch != null)
                    {
                        clientData.Ping = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - packet.timeStamp;
                        clientData.heartBeatStopwatch.Restart();
                        //Debug.Log($"client: {clientData.userName} ping: {clientData.Ping}ms");
                    }
                }
            }
        }

        public void SendHeartBeat()
        {
            MemoryStream newStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(newStream);
            writer.Write((int)PacketType.PING);
            writer.Write(0);
            Packet syncPacket = new Packet(PacketType.PING, ulong.MinValue, ulong.MinValue,
                DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond, Int32.MinValue, false, newStream.ToArray());
            SendUdpToAll(syncPacket.allData);
        }

        #endregion
    }
}
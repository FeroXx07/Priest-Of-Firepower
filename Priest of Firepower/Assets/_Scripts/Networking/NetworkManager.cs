using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Networking
{
    public enum PacketType
    {
        PING,
        OBJECT_STATE,
        INPUT,
        AUTHENTICATION,
        GAME_EVENT
    }

//this class will work as a client or server or both at the same time
    public class NetworkManager : GenericSingleton<NetworkManager>
    {
        #region Fields

        #region Default Ep Fields

        public IPAddress serverAdress = IPAddress.Any;
        public int defaultServerTcpPort = 12345;
        public int defaultServerUdpPort = 12443;

        #endregion

        #region Server/Client Fields

        private Client _client;
        private Server _server;
        private bool _isHost = false;
        private bool _isClient = false;
        public static readonly UInt64 UNKNOWN_ID = 69;
        public UInt64 getId => IsClient() ? _client.GetId() : 0;
        public string PlayerName = "testeo";
        
        public GameObject player { get; set; }
        public List<GameObject> instantiatablesPrefabs = new List<GameObject>();
        private ReplicationManager _replicationManager = new ReplicationManager();
        #endregion

        #region Buffers

        uint _mtu = 1400;
        int _stateBufferTimeout = 1000; // time with no activity to send not fulled packets
        int _inputBufferTimeout = 50; // time with no activity to send not fulled packets
        int _heartBeatRate = 1000; // beat rate to send to the server 

        // store all state streams to send
        private Queue<MemoryStream> _stateStreamBuffer = new Queue<MemoryStream>();
        [SerializeField] private UInt64 sequenceNumberState = 0;
        [SerializeField] private UInt64 receivedSequenceNumberState = 0;

        // store all input streams to send
        private Queue<MemoryStream> _inputStreamBuffer = new Queue<MemoryStream>();
        [SerializeField] private UInt64 sequenceNumberInput= 0;
        [SerializeField] private UInt64 receivedSequenceNumberInput = 0;

        // store all critical data streams to send (TCP)
        private Queue<MemoryStream> _reliableStreamBuffer = new Queue<MemoryStream>();

        // Mutex for thread safety
        private readonly object _stateQueueLock = new object();
        private readonly object _inputQueueLock = new object();
        private readonly object _realiableQueueLock = new object();

        // store all data in streams received
        private Queue<MemoryStream> _incomingStreamBuffer = new Queue<MemoryStream>();
        public readonly object IncomingStreamLock = new object();
        private Process _receiveData;
        private Process _sendData;
        #endregion

        #region Utility
        // Debug log packets
        public bool debugShowPingPackets = false;
        public bool debugShowObjectStatePackets = false;
        public bool debugShowInputPackets = false;
        public bool debugShowAuthenticationPackets = false;
        public bool debugShowMessagePackets = false;
        public static bool IsServerOnSameMachine(string serverIpAddress, int serverPort)
        {
            try
            {
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                IPAddress loopback = IPAddress.Parse("127.0.0.1");

                // Check if any local IP matches the server's IP
                foreach (IPAddress localIP in localIPs)
                {
                    if (localIP.Equals(IPAddress.Parse(serverIpAddress)))
                    {
                        return true; // Server IP matches one of the local IPs, so it's on the same machine
                    }
                }

                // Attempt a connection using loopback address
                Socket loopbackSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                loopbackSocket.Connect(loopback, serverPort);
                loopbackSocket.Close();
                return true; // Loopback connection successful, server is on the same machine
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false; // Error occurred or connection failed, server might not be on the same machine
            }
        }
        IPEndPoint ParseNetworkEndpoint(string address, ushort port) => new IPEndPoint(IPAddress.Parse(address), port);
        public bool isServerOnSameMachine => IsServerOnSameMachine(serverAdress.ToString(), defaultServerTcpPort);

        #endregion

        #region Actions

        //  Invoked when a new client is connected
        public Action OnClientConnected;
        
        //  Invoken when a client is disconnected
        public Action OnClientDisconnected;
        public Action<int> OnHostCreated;
        public Action<GameObject> OnHostPlayerCreated;

        // Message sendId, message string, message timestamp
        public Action<UInt64, string, long> OnGameEventMessageReceived;

        #endregion
        #endregion

        #region Enable/Disable
        private void Awake()
        {
            base.Awake();
            Debug.Log("Network Manager: Awake");
        }
        private void Start()
        {
            Debug.Log("Network Manager: Starting");
            _receiveData.Name = "NW receive data";
            _receiveData.cancellationToken = new CancellationTokenSource();
            _receiveData.thread = new Thread(() => ReceiveDataThread(_receiveData.cancellationToken.Token));
            _receiveData.thread.Start();
            _sendData.Name = "NW send data";
            _sendData.cancellationToken = new CancellationTokenSource();
            _sendData.thread = new Thread(() => SendDataThread(_receiveData.cancellationToken.Token));
            _sendData.thread.Start();
        }

        private void OnEnable()
        {
            // Debug.developerConsoleEnabled = true;
            // Debug.developerConsoleVisible = true;
            // Debug.LogError("Network Manager: Console Enabled");
            SceneManager.sceneLoaded += ResetNetworkIds;
        }
        
        public void SpawnPlayers()
        {
            if (SceneManager.GetActiveScene().name == "Game_Networking_Test")
            {
                if (_isHost)
                {
                    var prefab = instantiatablesPrefabs.Find(p => p.name == "PlayerPrefab");
                    foreach (ClientData clientData in _server.GetClients())
                    {
                        if (clientData.playerInstantiated) continue;
                        GameObject go = _replicationManager.Server_InstantiateNetworkObject(prefab, clientData);
                        go.gameObject.name = clientData.userName;
                        Player.Player player = go.GetComponent<Player.Player>();
                        player.SetName(clientData.userName);
                        player.SetPlayerId(clientData.id);
                        clientData.playerInstantiated = true;
                    }
                }
            }
        }
        
        public void Reset()
        {
            Debug.Log("NW reset ...");
            _receiveData.Shutdown();
            _sendData.Shutdown();
            
            if (_client != null)
            {
                try
                {
                    _client.Shutdown();
                    _client = null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Network Manager: Shutting down exception {e}");
                }
            }

            if (_server != null)
            {
                try
                {
                    _server.onClientConnected -= ClientConnected;
                    _server.onClientDisconnected -= ClientDisconected;
                    _server.Shutdown();
                    _server = null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Network Manager: Shutting down exception {e}");
                }
            }
            
            _isHost = false;
            _isClient = false;
            _replicationManager = new ReplicationManager();
            Start();
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= ResetNetworkIds;
            Debug.Log("Network Manager: Shutting down");
            _receiveData.Shutdown();
            _sendData.Shutdown();
            if (_client != null)
            {
                try
                {
                    _client.Shutdown();
                    _client = null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Network Manager: Shutting down exception {e}");
                }
            }

            if (_server != null)
            {
                try
                {
                    _server.onClientConnected -= ClientConnected;
                    _server.onClientDisconnected -= ClientDisconected;
                    _server.Shutdown();
                    _server = null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Network Manager: Shutting down exception {e}");
                }
            }
        }

        #endregion

        #region Connection Initializers

        public void StartClient()
        {
            if (_client == null) _client = new Client(PlayerName, new IPEndPoint(IPAddress.Any, 0), ClientConnected);
            if (isServerOnSameMachine)
            {
                serverAdress = IPAddress.Parse("127.0.0.1");
            }

            _client.ConnectToServer(new IPEndPoint(serverAdress, defaultServerTcpPort),
                new IPEndPoint(serverAdress, defaultServerUdpPort));
            _isClient = true;
        }

        public void StartHost()
        {
            _server = new Server(new IPEndPoint(IPAddress.Any, defaultServerTcpPort),
                new IPEndPoint(IPAddress.Any, defaultServerUdpPort));
            _server.onClientConnected += ClientConnected;
            _server.onClientDisconnected += ClientDisconected;
            _client = new Client(PlayerName, new IPEndPoint(IPAddress.Any, 0), ClientConnected);
            _isHost = true;
            if (_server.isServerInitialized)
            {
                // Localhost client!
                _client.ConnectToServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), defaultServerTcpPort),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), defaultServerUdpPort));
            }

            serverAdress = IPAddress.Loopback;
            Debug.Log("Network Manager: OnEnable -> _client: " + _client);
            Debug.Log("Network Manager: OnEnable -> _server: " + _server);
        }

        private void Update()
        {
            if (_isHost) _server.UpdatePendingDisconnections();
        }

        #endregion

        #region Output Streams

        public void AddStateStreamQueue(MemoryStream stream)
        {
            lock (_stateQueueLock)
            {
                _stateStreamBuffer.Enqueue(stream);
            }
        }

        public void AddInputStreamQueue(MemoryStream stream)
        {
            lock (_inputQueueLock)
            {
                _inputStreamBuffer.Enqueue(stream);
            }
        }

        public void AddReliableStreamQueue(MemoryStream stream)
        {
            lock (_realiableQueueLock)
            {
                _reliableStreamBuffer.Enqueue(stream);
            }
        }

        public void SendGameEventMessage(MemoryStream stream)
        {
            UInt64 senderid = IsClient() ? _client.GetId() : 0;
            byte[] buffer = InsertHeaderMemoryStreams(senderid, PacketType.GAME_EVENT, stream);
            if (_isClient)
            {
                Debug.Log($"Network Manager: Sending message as client, buffer size {buffer.Length}");
                _client.SendUdpPacket(buffer);
            }
            else if (_isHost)
            {
                Debug.Log($"Network Manager: Sending message as host, buffer size {buffer.Length}");
                _server.SendUdpToAll(buffer);
            }
        }

        private void SendDataThread(CancellationToken token)
        {
            try
            {
                Debug.Log("Network Manager: Send data thread started");
                float stateTimeout = _stateBufferTimeout;
                float inputTimeout = _inputBufferTimeout;
                System.Diagnostics.Stopwatch stateStopwatch = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch inputStopwatch = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch heartBeatStopwatch = new System.Diagnostics.Stopwatch();
                stateStopwatch.Start();
                inputStopwatch.Start();
                heartBeatStopwatch.Start();
                while (!token.IsCancellationRequested)
                {
                    UInt64 senderid = IsClient() ? _client.GetId() : 0;
                    
                     lock (_inputQueueLock)
                    {
                        //Input buffer
                        if (_inputStreamBuffer.Count > 0)
                        {
                            //Debug.Log("Network Manager: Preparing input stream buffer");
                            int totalSize = 0;
                            List<MemoryStream> streamsToSend = new List<MemoryStream>();

                            //check if the totalsize + the next stream total size is less than the specified size
                            while (_inputStreamBuffer.Count > 0 &&
                                   totalSize + (int)_inputStreamBuffer.Peek().Length <= _mtu)
                            {
                                //inputTimeout -= stopwatch.ElapsedMilliseconds;
                                MemoryStream nextStream = _inputStreamBuffer.Dequeue();
                                totalSize += (int)nextStream.Length;
                                streamsToSend.Add(nextStream);

                                // Adjust timeout based on elapsed time
                                inputTimeout -= inputStopwatch.ElapsedMilliseconds;
                                inputStopwatch.Restart();
                                //Debug.Log($"Network Manager: Timeout input stream buffer {inputTimeout}");
                                if (totalSize > 0 && (totalSize <= _mtu || inputTimeout <= 0.0f))
                                {
                                    inputTimeout = _inputBufferTimeout;
                                    byte[] buffer =
                                        InsertHeaderMemoryStreams(senderid, PacketType.INPUT, streamsToSend);
                                    if (_isClient)
                                    {
                                        Debug.Log($"Network Manager: Sending input as client, SIZE {buffer.Length}, TIMEOUT {inputTimeout} and SEQ INPUT: {sequenceNumberInput}");
                                        _client.SendUdpPacket(buffer);
                                        sequenceNumberInput++;
                                        if (sequenceNumberInput == ulong.MaxValue - 1) sequenceNumberInput = 0;
                                    }
                                    else if (_isHost)
                                    {
                                        Debug.Log($"Network Manager: Sending input as server, SIZE {buffer.Length}, TIMEOUT {inputTimeout} and SEQ INPUT: {sequenceNumberInput}");
                                        _server.SendUdpToAll(buffer);
                                        sequenceNumberInput++;
                                        if (sequenceNumberInput == ulong.MaxValue - 1) sequenceNumberInput = 0;
                                    }
                                }
                            }
                        }
                    }
                    
                    //State buffer
                    lock (_stateQueueLock)
                    {
                        if (_stateStreamBuffer.Count > 0)
                        {
                            //Debug.Log("Network Manager: Preparing state stream buffer");
                            int totalSize = 0;
                            List<MemoryStream> streamsToSend = new List<MemoryStream>();

                            //check if the totalsize + the next stream total size is less than the specified size
                            //while (_stateStreamBuffer.Count > 0 && totalSize + (int)_stateStreamBuffer.Peek().Length <= _mtu && _stateBufferTimeout > 0)
                            // Accumulate data packets until exceeding the MTU or the buffer is empty
                            while (_stateStreamBuffer.Count > 0 &&
                                   totalSize + (int)_stateStreamBuffer.Peek().Length <= _mtu)
                            {
                                MemoryStream nextStream = _stateStreamBuffer.Dequeue();
                                totalSize += (int)nextStream.Length;
                                streamsToSend.Add(nextStream);

                                // Adjust timeout based on elapsed time
                                stateTimeout -= stateStopwatch.ElapsedMilliseconds;
                                stateStopwatch.Restart();
                                //Debug.Log($"Network Manager: Timeout state stream buffer {stateTimeout}");
                                if (totalSize > 0 && (totalSize <= _mtu || stateTimeout <= 0.0f))
                                {
                                    stateTimeout = _stateBufferTimeout;
                                    byte[] buffer = InsertHeaderMemoryStreams(senderid, PacketType.OBJECT_STATE,
                                        streamsToSend);
                                    if (_isClient)
                                    {
                                        Debug.Log( $"Network Manager: Sending state as client, SIZE {buffer.Length}, TIMEOUT {stateTimeout}, and SEQ STATE: {sequenceNumberState}");
                                        _client.SendUdpPacket(buffer);
                                        sequenceNumberState++;
                                        if (sequenceNumberState == ulong.MaxValue - 1) sequenceNumberState = 0;
                                    }
                                    else if (_isHost)
                                    {
                                        Debug.Log($"Network Manager: Sending state as server, SIZE {buffer.Length}, TIMEOUT {stateTimeout}, and SEQ STATE: {sequenceNumberState}");
                                        _server.SendUdpToAll(buffer);
                                        sequenceNumberState++;
                                        if (sequenceNumberState == ulong.MaxValue - 1) sequenceNumberState = 0;
                                    }

                                    // Clear the streams to send for the next iteration
                                    streamsToSend.Clear();
                                    totalSize = 0;
                                }
                            }
                        }
                    }

                    lock (_realiableQueueLock)
                    {
                        // reliable buffer
                        if (_reliableStreamBuffer.Count > 0)
                        {
                            List<MemoryStream> streamsToSend = new List<MemoryStream>();
                            while (_reliableStreamBuffer.Count > 0)
                            {
                                MemoryStream nextStream = _reliableStreamBuffer.Dequeue();
                                MemoryStream newStream = new MemoryStream();
                                BinaryWriter writer = new BinaryWriter(newStream);
                                ///writer.Write((UInt64)senderid);
                                nextStream.CopyTo(newStream);
                                byte[] buffer = nextStream.ToArray();
                                if (_isClient)
                                {
                                    _client.SendTcp(buffer);
                                }
                                else if (_isHost)
                                {
                                    _server.SendTcpToAll(buffer);
                                }
                            }
                        }
                    }

                    //handle heart beats
                    if (heartBeatStopwatch.ElapsedMilliseconds >= _heartBeatRate)
                    {
                        if(_isClient)
                            _client.SendHeartBeat();
                        if(_isHost)
                            _server.SendHeartBeat();
                        
                        heartBeatStopwatch.Restart();
                    }

                    if (inputTimeout < 0) inputTimeout = _inputBufferTimeout;
                    if (stateTimeout < 0) stateTimeout = _stateBufferTimeout;
                    Thread.Sleep(1);
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogError($"Network Manager: OperationCanceledException {e}");
            }
            finally
            {
                // Clean up resources
                lock (_stateQueueLock)
                {
                    Debug.Log($"Network Manager: Disposing state stream buffer");
                    foreach (MemoryStream incomingStream in _stateStreamBuffer)
                    {
                        incomingStream.Dispose();
                    }

                    _stateStreamBuffer.Clear(); // Clear the queue
                }

                lock (_inputQueueLock)
                {
                    Debug.Log($"Network Manager: Disposing input stream buffer");
                    foreach (MemoryStream incomingStream in _inputStreamBuffer)
                    {
                        incomingStream.Dispose();
                    }

                    _inputStreamBuffer.Clear(); // Clear the queue
                }

                lock (_reliableStreamBuffer)
                {
                    Debug.Log($"Network Manager: Disposing reliable stream buffer");
                    foreach (MemoryStream incomingStream in _reliableStreamBuffer)
                    {
                        incomingStream.Dispose();
                    }

                    _reliableStreamBuffer.Clear(); // Clear the queue
                }
            }
        }

        #endregion

        #region Input Streams

        public void AddIncomingDataQueue(MemoryStream stream)
        {
            _incomingStreamBuffer.Enqueue(stream);
        }

        private void ReceiveDataThread(CancellationToken token)
        {
            try
            {
                Debug.Log("Network Manager: Receive data thread started...");
                while (!token.IsCancellationRequested)
                {
                    if (_incomingStreamBuffer.Count > 0)
                    {
                        lock (IncomingStreamLock)
                        {
                            while (_incomingStreamBuffer.Count > 0)
                            {
                                ProcessIncomingPacket(_incomingStreamBuffer.Dequeue());
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogException(e);
            }
            finally
            {
                // Clean up resources
                lock (IncomingStreamLock)
                {
                    foreach (MemoryStream incomingStream in _incomingStreamBuffer)
                    {
                        incomingStream.Dispose();
                    }

                    _incomingStreamBuffer.Clear(); // Clear the queue
                }
            }
        }

        public void ProcessIncomingPacket(MemoryStream stream)
        {
            // Reset stream position incase
            stream.Position = 0;
            BinaryReader reader = new BinaryReader(stream);
            
            // Check if packet is higher than 1500 bytes, it shouldn't be!
            if (stream.Length > 1500)
            {
                Debug.LogWarning(
                    "Network Manager: Received packet with size exceeding the maximum (1500 bytes). Discarding...");
                return;
            }
            
            // Check for packet type
            PacketType type = (PacketType)reader.ReadInt32();
            
            switch (type)
            {
                case PacketType.PING:
                {
                    if (debugShowPingPackets) Debug.Log($"Network Manager: Received packet {type} with stream array lenght {stream.ToArray().Length}");
                    if (_isHost) _server.HandleHeartBeat(reader);
                    else if (_isClient) _client.HandleHeartBeat(reader);
                }
                    break;
                case PacketType.INPUT:
                {
                    if (debugShowInputPackets) Debug.Log($"Network Manager: Received packet {type} with stream array lenght {stream.ToArray().Length}");
                    receivedSequenceNumberInput = reader.ReadUInt64();
                    UInt64 packetSenderId = reader.ReadUInt64();
                    long packetTimeStamp = reader.ReadInt64();
                    UnityMainThreadDispatcher.Dispatcher.Enqueue(() => HandleInput(reader, packetSenderId, packetTimeStamp, receivedSequenceNumberInput));
                }
                    break;
                case PacketType.OBJECT_STATE:
                {
                    if (debugShowObjectStatePackets) Debug.Log($"Network Manager: Received packet {type} with stream array lenght {stream.ToArray().Length}");
                    receivedSequenceNumberState = reader.ReadUInt64();
                    UInt64 packetSenderId = reader.ReadUInt64();
                    long packetTimeStamp = reader.ReadInt64();
                    UnityMainThreadDispatcher.Dispatcher.Enqueue(() => HandleObjectState(reader, packetSenderId, packetTimeStamp, receivedSequenceNumberState));
                    // Maybe this reading of packets in actions is a problem for tranforms
                }
                    break;
                case PacketType.AUTHENTICATION:
                {
                    if (debugShowAuthenticationPackets)Debug.Log($"Network Manager: Received packet {type} with stream array lenght {stream.ToArray().Length}");
                    if (_isClient) {
                        Debug.Log("Network Manager: Client auth message received");
                        _client.authenticator.HandleAuthentication(reader);
                    }
                    else if (_isHost) {
                        Debug.Log("Network Manager: Host auth message received");
                        _server.HandleAuthentication(reader);
                    }
                }
                    break;
                case PacketType.GAME_EVENT:
                {
                    if (debugShowMessagePackets)  Debug.Log($"Network Manager: Received packet {type} with stream array lenght {stream.ToArray().Length}");
                    UInt64 packetSenderId = reader.ReadUInt64();
                    long packetTimeStamp = reader.ReadInt64();
                    string message = reader.ReadString();
                    OnGameEventMessageReceived?.Invoke(packetSenderId, message, packetTimeStamp);
                }
                    break;
                default:
                    Debug.LogError("Network Manager: Unknown packet type!");
                    break;
            }
        }

        void HandleObjectState(BinaryReader reader, UInt64 packetSender, Int64 timeStamp, UInt64 sequenceNumInput)
        {
            try
            {
                //Debug.Log("base stream: " + reader.BaseStream.Length);
                //Debug.Log("Reader position:" + reader.BaseStream.Position);
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // [Object Class][Object ID]
                    string objClass = reader.ReadString();
                    UInt64 id = reader.ReadUInt64();
                    ReplicationAction replicationAction = (ReplicationAction)reader.ReadInt32();
                    //read rest of the stream
                    _replicationManager.HandleReplication(reader, id, timeStamp, sequenceNumInput, replicationAction, Type.GetType(objClass));
                }
            }
            catch (EndOfStreamException ex)
            {
                Debug.LogError($"Network Manager: EndOfStreamException: {ex.Message}");
            }
        }

        void HandleInput(BinaryReader reader, UInt64 packetSender, Int64 timeStamp, UInt64 sequenceNumInput)
        {
            try
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    string objClass = reader.ReadString();
                    UInt64 netObjId = reader.ReadUInt64();
                    _replicationManager.networkObjectMap[netObjId].HandleNetworkInput(reader, packetSender, timeStamp, sequenceNumInput, Type.GetType(objClass));
                }
            }
            catch (EndOfStreamException ex)
            {
                Debug.LogError($"Network Manager: EndOfStreamException: {ex.Message}");
            }
        }

        #endregion

        private byte[] InsertHeaderMemoryStreams(UInt64 senderId, PacketType type, List<MemoryStream> streamsList)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);
            
            // Packet type
            writer.Write((int)type);
            
            // Packet sequence number
            if (type == PacketType.OBJECT_STATE)
            {
                writer.Write(sequenceNumberState);
            }
            else if (type == PacketType.INPUT)
            {
                writer.Write(sequenceNumberInput);
            }
            
            // Packet sender id
            writer.Write(senderId);
            
            // Packet timestamp
            writer.Write(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            
            // Packet contents
            foreach (MemoryStream stream in streamsList)
            {
                stream.Position = 0;
                stream.CopyTo(output);
            }
            
            return output.ToArray();
        }

        private byte[] InsertHeaderMemoryStreams(UInt64 senderId, PacketType type, MemoryStream stream)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);
            
            // Packet type
            writer.Write((int)type);
            
            // Packet sequence number
            if (type == PacketType.OBJECT_STATE)
            {
                writer.Write(sequenceNumberState);
            }
            else if (type == PacketType.INPUT)
            {
                writer.Write(sequenceNumberInput);
            }
            
            // Packet sender id
            writer.Write(senderId);
            
            // Packet timestamp
            writer.Write(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            
            // Packet contents
            stream.Position = 0;
            stream.CopyTo(output);
            return output.ToArray();
        }

        #region Client Events Interface

        public void ClientConnected()
        {
            OnClientConnected?.Invoke();
        }

        public void ClientDisconected()
        {
            OnClientDisconnected?.Invoke();
        }

        public void RemoveClient(ClientData clientToRemove)
        {
            if (_isHost)
            {
                _server.AddClientToRemove(clientToRemove);
            }
        }

        #endregion

        #region Getters

        public bool IsHost()
        {
            return _isHost;
        }

        public bool IsClient()
        {
            return _isClient;
        }

        public Client GetClient()
        {
            return _client;
        }

        public Server GetServer()
        {
            if (_isHost)
                return _server;
            else
                return null;
        }

        #endregion

        #region address

        void ResetNetworkIds(Scene scene, LoadSceneMode mode)
        {
            List<NetworkObject> list = FindObjectsOfType<NetworkObject>(true).ToList();
            _replicationManager.InitManager(list);
        }

        #endregion
    }
}
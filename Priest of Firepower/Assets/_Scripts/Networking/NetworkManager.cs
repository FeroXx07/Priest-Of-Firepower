using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace _Scripts.Networking
{
    public enum PacketType
    {
        PING,
        OBJECT_STATE,
        INPUT,
        AUTHENTICATION,
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

        #region Name Generator Fields
        static readonly string[] firstNames = {"John","Paul","Ringo","George"};
        private static readonly string[] lastNames = {"Lennon","McCartney","Starr","Harrison"};
        public static string GenerateName()
        {
            var random = new Random();
            string firstName = firstNames[random.Next(0, firstNames.Length)];
            string lastName = lastNames[random.Next(0, firstNames.Length)];

            return $"{firstName}_{lastName}";
        }
        #endregion

        #region Server/Client Fields
        private AClient _client;
        private AServer _server;
        
        private bool _isHost = false;
        private bool _isServer = false;
        private bool _isClient = false;
        
        public static readonly UInt64 UNKNOWN_ID = 69;
        #endregion

        #region Buffers
        uint _mtu = 1400;
        int _stateBufferTimeout = 1000; // time with no activity to send not fulled packets
        int _inputBufferTimeout = 100; // time with no activity to send not fulled packets

        // store all state streams to send
        private Queue<MemoryStream> _stateStreamBuffer = new Queue<MemoryStream>();
        // store all input streams to send
        private Queue<MemoryStream> _inputStreamBuffer = new Queue<MemoryStream>();
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
        
        [SerializeField] private GameObject player;
        public ReplicationManager _replicationManager = new ReplicationManager();

        #region Actions
        //  Invoked when a new client is connected
        public Action OnClientConnected;
        //  Invoken when a client is disconnected
        public Action OnClientDisconnected;
        // Invoken when client recieves server data
        public Action<byte[]> OnRecivedServerData;
        // Invoken when server recives data from clients
        public Action<byte[]> OnRecivedClientData;
        #endregion
        #endregion
        

        #region Enable/Disable
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
            Debug.developerConsoleEnabled = true;
            Debug.developerConsoleVisible = true;
            Debug.LogError("Network Manager: Console Enabled");
            SceneManager.sceneLoaded += ResetNetworkIds;
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
                    _server.Shutdown();
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
            string clientName = GenerateName();
            
            _client = new AClient(clientName, new IPEndPoint(IPAddress.Any, 0), ClientConnected);
            
            if (isServerOnSameMachine)
            {
                serverAdress = IPAddress.Parse("127.0.0.1");
            }
            
            _client.ConnectToServer(new IPEndPoint(serverAdress, defaultServerTcpPort), new IPEndPoint(serverAdress, defaultServerUdpPort));
            _isClient = true;
        }

        public void StartHost()
        {
            string clientName = GenerateName();
            
            _server = new AServer(new IPEndPoint(IPAddress.Any, 0), new IPEndPoint(IPAddress.Any, 0));
            _client = new AClient(clientName, new IPEndPoint(IPAddress.Any, 0), ClientConnected);
            _isHost = true;
            
            if (_server.isServerInitialized)
            {
                // Localhost client!
                _client.ConnectToServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), defaultServerTcpPort), new IPEndPoint(IPAddress.Parse("127.0.0.1"), defaultServerUdpPort));
            }

            Debug.Log("Network Manager: OnEnable -> _client: " + _client);
            Debug.Log("Network Manager: OnEnable -> _server: " + _server);
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
        
        private void SendDataThread(CancellationToken token)
        {
            try
            {
                Debug.Log("Network Manager: Send data thread started");
                float stateTimeout = _stateBufferTimeout;
                float inputTimeout = _inputBufferTimeout;
                System.Diagnostics.Stopwatch stateStopwatch = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch inputStopwatch = new System.Diagnostics.Stopwatch();
                stateStopwatch.Start();
                inputStopwatch.Start();
                while (!token.IsCancellationRequested)
                {
                    UInt64 senderid = IsClient() ? _client.GetId() : 0;

                    //State buffer
                    lock (_stateQueueLock)
                    {
                        if (_stateStreamBuffer.Count > 0)
                        {
                            Debug.Log("Network Manager: Preparing state stream buffer");
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
                                
                                Debug.Log($"Network Manager: Timeout state stream buffer {stateTimeout}");
                                if (totalSize > 0 && (totalSize <= _mtu || stateTimeout <= 0.0f))
                                {
                                    stateTimeout = _stateBufferTimeout;
                                    byte[] buffer = ConcatenateMemoryStreams(senderid, PacketType.OBJECT_STATE,
                                        streamsToSend);
                                    if (_isClient)
                                    {
                                        Debug.Log($"Network Manager: Sending state stream buffer as client, buffer size {buffer.Length} and timeout {stateTimeout}");
                                        _client.SendUdpPacket(buffer);
                                    }
                                    else if (_isHost)
                                    {
                                        Debug.Log($"Network Manager: Sending state stream buffer as host, buffer size {buffer.Length} and timeout {stateTimeout}");
                                        _server.SendUdpToAll(buffer);
                                    }
                                    else if (_isServer)
                                    {
                                        Debug.Log($"Network Manager: Sending state stream buffer as server, buffer size {buffer.Length} and timeout {stateTimeout}");
                                        _server.SendUdpToAll(buffer);
                                    }

                                    // Clear the streams to send for the next iteration
                                    streamsToSend.Clear();
                                    totalSize = 0;
                                }
                            }
                        }
                    }

                    lock (_inputQueueLock)
                    {
                        //Input buffer
                        if (_inputStreamBuffer.Count > 0)
                        {
                            Debug.Log("Network Manager: Preparing input stream buffer");
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
                                
                                Debug.Log($"Network Manager: Timeout input stream buffer {inputTimeout}");
                                if (totalSize > 0 && (totalSize <= _mtu || inputTimeout <= 0.0f))
                                {
                                    inputTimeout = _inputBufferTimeout;
                                    byte[] buffer = ConcatenateMemoryStreams(senderid, PacketType.INPUT, streamsToSend);
                                    if (_isClient)
                                    {
                                        Debug.Log($"Network Manager: Sending input stream buffer as client, buffer size {buffer.Length} and timeout {inputTimeout}");
                                        _client.SendUdpPacket(buffer);
                                    }
                                    else if (_isServer)
                                    {
                                        Debug.Log($"Network Manager: Sending input stream buffer as client, buffer size {buffer.Length} and timeout {inputTimeout}");
                                        _server.SendUdpToAll(buffer);
                                    }
                                    else if (_isHost)
                                    {
                                        Debug.Log($"Network Manager: Sending input stream buffer as client, buffer size {buffer.Length} and timeout {inputTimeout}");
                                        _server.SendUdpToAll(buffer);
                                    }
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
                                else if (_isServer)
                                {
                                    _server.SendTcpToAll(buffer);
                                }
                                else if (_isHost)
                                {
                                    _server.SendTcpToAll(buffer);
                                }
                            }
                        }
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
                                ProcessIncomingData(_incomingStreamBuffer.Dequeue());
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

        public void ProcessIncomingData(MemoryStream stream)
        {
            // Create a replication manager
            //[packet type]
            stream.Position = 0;
            BinaryReader reader = new BinaryReader(stream);
            if (stream.Length > 1500)
            {
                Debug.LogWarning("Network Manager: Received packet with size exceeding the maximum (1500 bytes). Discarding...");
                return;
            }

            // UInt64 senderId = reader.ReadUInt64();
            PacketType type = (PacketType)reader.ReadInt32();
            Debug.Log($"Network Manager: Received packet {type} with stream array lenght {stream.ToArray().Length}");

            switch (type)
            {
                case PacketType.PING:
                    Debug.Log("Network Manager: PING message received");
                    break;
                case PacketType.INPUT:
                    Debug.Log("Network Manager: INPUT message received");
                    break;
                case PacketType.OBJECT_STATE:
                {
                    Debug.Log("Network Manager: OBJECT_STATE message received");
                    MainThreadDispatcher.EnqueueAction(() => HandleObjectState(stream, reader));
                }
                    break;
                case PacketType.AUTHENTICATION:
                {
                    if (_isClient)
                    {
                        Debug.Log("Network Manager: Client auth message received");
                        _client.authenticator.HandleAuthentication(stream, reader);
                    }
                    else if (_isHost)
                    {
                        Debug.Log("Network Manager: Host auth message received");
                        _server.HandleAuthentication(stream, reader);
                    }
                }
                    break;
                default:
                    Debug.LogError("Network Manager: Unknown packet type!");
                    break;
            }
        }

        void HandleObjectState(MemoryStream stream, BinaryReader reader)
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
                    NetworkAction networkAction = (NetworkAction)reader.ReadInt32();
                    //read rest of the stream
                    _replicationManager.HandleNetworkAction(id, networkAction, Type.GetType(objClass), reader);
                }
            }
            catch (EndOfStreamException ex)
            {
                Debug.LogError($"Network Manager: EndOfStreamException: {ex.Message}");
            }
        }
        #endregion
       

        private byte[] ConcatenateMemoryStreams(UInt64 senderId, PacketType type, List<MemoryStream> streamsList)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);

            // writer.Write(senderId);
            writer.Write((int)type);
            foreach (MemoryStream stream in streamsList)
            {
                stream.Position = 0;
                stream.CopyTo(output);
            }

            return output.ToArray();
        }

        #region Client Events Interface

        //Client Events Interface
        public void ClientConnected()
        {
            OnClientConnected?.Invoke();
        }

        public void ClientDisconected()
        {
            OnClientDisconnected?.Invoke();
        }

        #endregion

        #region Getters

        public bool IsHost()
        {
            return _isHost;
        }

        public bool IsServer()
        {
            return _isServer;
        }

        public bool IsClient()
        {
            return _isClient;
        }

        #endregion

        // Structure to store the address to connect to

        #region address
        void ResetNetworkIds(Scene scene, LoadSceneMode mode)
        {
            List<NetworkObject> list = FindObjectsOfType<NetworkObject>(true).ToList();
            _replicationManager.InitManager(list);
        }

        #endregion
    }

    public enum NetworkAction
    {
        CREATE,
        UPDATE,
        DESTROY,
        TRANSFORM,
        EVENT
    }

    public class ReplicationManager
    {
        public Dictionary<UInt64, NetworkObject> networkObjectMap = new Dictionary<ulong, NetworkObject>();

        public void InitManager(List<NetworkObject> listNetObj)
        {
            networkObjectMap.Clear();
            UInt64 id = 0;
            foreach (var networkObject in listNetObj)
            {
                networkObject.SetNetworkId(id);
                networkObjectMap.Add(id, networkObject);
                id++;
            }
        }

        public void HandleNetworkAction(UInt64 id, NetworkAction action, Type type, BinaryReader reader)
        {
            Debug.Log(
                $"Network Manager: HandlingNetworkAction: ID: {id}, Action: {action}, Type: {type.FullName}, Stream Position: {reader.BaseStream.Position}");
            switch (action)
            {
                case NetworkAction.CREATE:
                    HandleObjectCreation(id, action, type, reader);
                    break;
                case NetworkAction.UPDATE:
                    HandleObjectUpdate(id, action, type, reader);
                    break;
                case NetworkAction.DESTROY:
                    HandleObjectDeSpawn(id, action, type, reader);
                    break;
                case NetworkAction.EVENT:
                    HandleObjectEvent(id, action, type, reader);
                    break;
                case NetworkAction.TRANSFORM:
                    HandleObjectTransform(id, action, type, reader);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private void HandleObjectCreation(UInt64 id, NetworkAction action, Type type, BinaryReader reader)
        {
            /*
            i) Instantiate new object
            ii) Register in the linking context
            iii) Deserialize fields
             */
        }

        private void HandleObjectUpdate(UInt64 id, NetworkAction action, Type type, BinaryReader reader)
        {
            networkObjectMap[id].HandleNetworkBehaviour(type, reader);
        }

        private void HandleObjectDeSpawn(UInt64 id, NetworkAction action, Type type, BinaryReader reader)
        {
            // Destroy or return to pool
        }

        private void HandleObjectEvent(UInt64 id, NetworkAction action, Type type, BinaryReader reader)
        {
        }

        private void HandleObjectTransform(UInt64 id, NetworkAction action, Type type, BinaryReader reader)
        {
            if (networkObjectMap[id].synchronizeTransform) networkObjectMap[id].HandleNetworkTransform(reader);
        }
    }
}


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using _Scripts.Networking.Client;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Networking
{
    public enum PacketType
    {
        SYNC,
        PING,
        OBJECT_STATE,
        INPUT,
        AUTHENTICATION
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

        private Client.Client _client;
        private Server.Server _server;
        private bool _isHost = false;
        private bool _isClient = false;
        public static readonly UInt64 UNKNOWN_ID = 69;
        public UInt64 getId => IsClient() ? _client.GetId() : 0;
        public string PlayerName = "testeo";
        public GameObject player { get; set; }
        public List<GameObject> instantiatablesPrefabs = new List<GameObject>();
        public ReplicationManager replicationManager = new ReplicationManager();

        #endregion

        #region Buffers

        private uint _mtu = 1300;
        private int _stateBufferTimeout = 100; // time with no activity to send not fulled packets
        private int _inputBufferTimeout = 10; // time with no activity to send not fulled packets
        private int _heartBeatRate = 1000; // beat rate to send to the server 
        private System.Diagnostics.Stopwatch _inputStopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _heartBeatStopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _stateStopwatch = new System.Diagnostics.Stopwatch();

        // store all state streams to send
        private ConcurrentQueue<ReplicationItem> _stateStreamBuffer = new ConcurrentQueue<ReplicationItem>();
        private List<ReplicationItem> _replicationItemsToSend = new List<ReplicationItem>();
        [SerializeField] private UInt64 sendSeqNumState = 0;
        [SerializeField] private UInt64 recSeqNumState = 0;
        public UInt64 currSeqNumStateRead { get; private set; } = 0;

        // store all input streams to send
        private ConcurrentQueue<InputItem> _inputStreamBuffer = new ConcurrentQueue<InputItem>();
        private List<InputItem> _inputItemsToSend = new List<InputItem>();
        [SerializeField] private UInt64 sendSeqNumInput = 0;
        [SerializeField] private UInt64 recSeqNumInput = 0;
        private ConcurrentQueue<InputItem> _inputRealiableStreamBuffer = new ConcurrentQueue<InputItem>();
        private List<InputItem> _inputReliableItemsToSend = new List<InputItem>();

        // store all critical data streams to send (TCP)
        private ConcurrentQueue<MemoryStream> _reliableStreamBuffer = new ConcurrentQueue<MemoryStream>();

        // Mutex for thread safety
        private readonly object _stateQueueLock = new object();
        private readonly object _inputQueueLock = new object();
        private readonly object _reliableInputQueueLock = new object();
        private readonly object _reliableQueueLock = new object();

        // store all data in streams received
        private ConcurrentQueue<MemoryStream> _incomingStreamBuffer = new ConcurrentQueue<MemoryStream>();
        public readonly object incomingStreamLock = new object();
        private Process _receiveData;
        private Process _sendData;
        private int thresholdToStartDiscardingPackets = 3;
        private bool isBehindThreshold = false;

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
        private List<GameObject> playerInstancesList = new List<GameObject>();

        #endregion

        #endregion

        #region Enable/Disable

        public override void Awake()
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
                        MemoryStream mem = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(mem);
                        writer.Write(clientData.userName);
                        writer.Write(clientData.id);
                        ReplicationHeader header = new ReplicationHeader(0, this.GetType().FullName,
                            ReplicationAction.CREATE, mem.ToArray().Length);
                        GameObject go = replicationManager.Server_InstantiateNetworkObject(prefab, header, mem);
                        go.gameObject.name = clientData.userName;
                        playerInstancesList.Add(go);
                        Player.Player player = go.GetComponent<Player.Player>();
                        player.SetName(clientData.userName);
                        player.SetPlayerId(clientData.id);
                        clientData.playerInstantiated = true;
                    }
                }
            }
        }

        public void OwnerPlayerCreated(GameObject playerGameObject)
        {
            OnHostPlayerCreated?.Invoke(playerGameObject);
        }
        
        void DespawnPlayer(UInt64 id, string userName)
        {
            if (_isHost)
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(userName);
                writer.Write(id);
                ReplicationHeader header = new ReplicationHeader(0, this.GetType().FullName, ReplicationAction.DESTROY,
                    stream.ToArray().Length);
                GameObject obj = null;
                foreach (GameObject player in playerInstancesList)
                {
                    if (player.name == userName)
                    {
                        obj = player;
                        break;
                    }
                }

                if (obj == null)
                {
                    Debug.LogError("Something happend when removing player instace ... ");
                    return;
                }

                playerInstancesList.Remove(obj);
                NetworkObject nObj = obj.GetComponent<NetworkObject>();
                replicationManager.Server_DeSpawnNetworkObject(nObj, header, stream);
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
                    _client.onServerConnectionLost -= Reset;
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
            replicationManager = new ReplicationManager();
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
                    _client.onServerConnectionLost -= Reset;
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
            if (_client == null) _client = new Client.Client(PlayerName, new IPEndPoint(IPAddress.Any, 0), ClientConnected);
            if (isServerOnSameMachine)
            {
                serverAdress = IPAddress.Parse("127.0.0.1");
            }

            _client.ConnectToServer(new IPEndPoint(serverAdress, defaultServerTcpPort),
                new IPEndPoint(serverAdress, defaultServerUdpPort));
            _isClient = true;
            _client.onServerConnectionLost += Reset;
        }

        public void StartHost()
        {
            _server = new Server.Server(new IPEndPoint(IPAddress.Any, defaultServerTcpPort),
                new IPEndPoint(IPAddress.Any, defaultServerUdpPort));
            _server.onClientConnected += ClientConnected;
            _server.onClientDisconnected += ClientDisconected;
            _client = new Client.Client(PlayerName, new IPEndPoint(IPAddress.Any, 0), ClientConnected);
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

        private void FixedUpdate()
        {
            if (_client != null) _client.FixedUpdate();
            if (_server != null) _server.FixedUpdate();
        }

        #endregion

        #region Output Streams

        public void AddStateStreamQueue(ReplicationHeader replicationHeader, MemoryStream stream)
        {
            lock (_stateQueueLock)
            {
                //// Check if replication header contains already contains an exact replication header id, obj, an action, if true then replace with the most recent.
                //bool headerExistsAndIsReplaced = false;
                bool isDuplicate = IsReplicationItemDuplicate(replicationHeader, out ReplicationItem alreadyExistingItem);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (isDuplicate)
                    alreadyExistingItem.ReplaceMemoryStream(stream);
                else //ReSharper disable once HeuristicUnreachableCode
                    _stateStreamBuffer.Enqueue(new ReplicationItem(replicationHeader, stream));
            }
        }

        private bool IsReplicationItemDuplicate(ReplicationHeader replicationHeader,
            out ReplicationItem alreadyExistingItem)
        {
            foreach (var item in _stateStreamBuffer)
            {
                if (item.header.id == replicationHeader.id &&
                    item.header.objectFullName == replicationHeader.objectFullName &&
                    item.header.replicationAction == replicationHeader.replicationAction)
                {
                    Debug.LogWarning($"Header is duplicate, id: {item.header.id}, name: {item.header.objectFullName}, action: {item.header.replicationAction}, size: {item.header.memoryStreamSize}");
                    alreadyExistingItem = item;
                    return true;
                }
            }

            alreadyExistingItem = null;
            return false;
        }

        public void AddInputStreamQueue(InputHeader inputHeader, MemoryStream stream)
        {
            lock (_inputQueueLock)
            {
                _inputStreamBuffer.Enqueue(new InputItem(inputHeader, stream));
            }
        }

        public void AddReliableInputStreamQueue(InputHeader inputHeader, MemoryStream stream)
        {
            lock (_reliableInputQueueLock)
            {
                _inputRealiableStreamBuffer.Enqueue(new InputItem(inputHeader, stream));
            }
        }

        public void AddReliableStreamQueue(MemoryStream stream)
        {
            lock (_reliableQueueLock)
            {
                _reliableStreamBuffer.Enqueue(stream);
            }
        }

        // public void SendGameEventMessage(MemoryStream stream)
        // {
        //     UInt64 senderid = IsClient() ? _client.GetId() : 0;
        //     byte[] buffer = InsertHeaderMemoryStreams(senderid, PacketType.GAME_EVENT, stream);
        //     if (_isClient)
        //     {
        //         Debug.Log($"Network Manager: Sending message as client, buffer size {buffer.Length}");
        //         _client.SendUdpPacket(buffer);
        //     }
        //     else if (_isHost)
        //     {
        //         Debug.Log($"Network Manager: Sending message as host, buffer size {buffer.Length}");
        //         _server.SendUdpToAll(buffer);
        //     }
        // }

        private void SendDataThread(CancellationToken token)
        {
            try
            {
                Debug.Log("Network Manager: Send data thread started");
                float stateTimeout = _stateBufferTimeout;
                float inputTimeout = _inputBufferTimeout;
                _heartBeatStopwatch.Start();
                _stateStopwatch.Start();
                _inputStopwatch.Start();
                while (!token.IsCancellationRequested)
                {
                    UInt64 senderid = IsClient() ? _client.GetId() : 0;
                    lock (_inputQueueLock)
                    {
                        if (_inputStreamBuffer.Count > 0)
                        {
                            int totalSize = 0;
                            bool mtuFull = false;
                            //Debug.Log( $"Network Manager: Starting STATE loop, elapsedTime: {_stateStopwatch.ElapsedMilliseconds}");
                            while (_inputStreamBuffer.Count > 0 &&
                                   mtuFull == false /*&& _stateStopwatch.ElapsedMilliseconds < _stateBufferTimeout)*/)
                            {
                                if (_inputStreamBuffer.TryPeek(out InputItem nextItem))
                                {
                                    // Able to peek
                                    MemoryStream currentItemHeaderStream = nextItem.header.GetSerializedHeader();
                                    if (totalSize + (int)nextItem.memoryStream.Length +
                                        (int)currentItemHeaderStream.Length <= _mtu)
                                    {
                                        // Able to insert next header into mtu
                                        if (_inputStreamBuffer.TryDequeue(out InputItem inputItem))
                                        {
                                            totalSize += (int)inputItem.memoryStream.Length +
                                                         (int)currentItemHeaderStream.Length;
                                            _inputItemsToSend.Add(inputItem);
                                        }
                                    }
                                    else
                                    {
                                        // Unable to insert next header into mtu because it's full
                                        mtuFull = true;
                                    }
                                }

                                if (totalSize > 0 || mtuFull)
                                {
                                    byte[] buffer = InsertHeaderMemoryStreams(senderid, PacketType.INPUT,
                                        _inputItemsToSend);
                                    if (_isClient)
                                    {
                                        if (debugShowInputPackets)
                                            Debug.Log(
                                                $"Network Manager: Sending input as client, SIZE {buffer.Length}, TIMEOUT {_inputStopwatch.ElapsedMilliseconds} - {inputTimeout} and SEQ INPUT: {sendSeqNumInput}");
                                        _client.SendUdpPacket(buffer);
                                        sendSeqNumInput++;
                                        if (sendSeqNumInput == ulong.MaxValue - 1) sendSeqNumInput = 0;
                                    }
                                    else if (_isHost)
                                    {
                                        if (debugShowInputPackets)
                                            Debug.Log(
                                                $"Network Manager: Sending input as server, SIZE {buffer.Length}, TIMEOUT {_inputStopwatch.ElapsedMilliseconds} - {inputTimeout} and SEQ INPUT: {sendSeqNumInput}");
                                        _server.SendUdpToAll(buffer);
                                        sendSeqNumInput++;
                                        if (sendSeqNumInput == ulong.MaxValue - 1) sendSeqNumInput = 0;
                                    }

                                    _inputItemsToSend.Clear();
                                    _inputStopwatch.Restart();
                                    totalSize = 0;
                                }
                            }
                        }
                    }

                    lock (_reliableInputQueueLock)
                    {
                        if (_inputRealiableStreamBuffer.Count > 0)
                        {
                            int totalSize = 0;
                            bool mtuFull = false;
                            bool isTimeout = false;
                            //Debug.Log( $"Network Manager: Starting STATE loop, elapsedTime: {_stateStopwatch.ElapsedMilliseconds}");
                            while (_inputRealiableStreamBuffer.Count > 0 &&
                                   mtuFull == false /*&& _stateStopwatch.ElapsedMilliseconds < _stateBufferTimeout)*/)
                            {
                                if (_inputRealiableStreamBuffer.TryPeek(out InputItem nextItem))
                                {
                                    // Able to peek
                                    MemoryStream currentItemHeaderStream = nextItem.header.GetSerializedHeader();
                                    if (totalSize + (int)nextItem.memoryStream.Length +
                                        (int)currentItemHeaderStream.Length <= _mtu)
                                    {
                                        // Able to insert next header into mtu
                                        if (_inputRealiableStreamBuffer.TryDequeue(out InputItem inputItem))
                                        {
                                            totalSize += (int)inputItem.memoryStream.Length +
                                                         (int)currentItemHeaderStream.Length;
                                            _inputReliableItemsToSend.Add(inputItem);
                                        }
                                    }
                                    else
                                    {
                                        // Unable to insert next header into mtu because it's full
                                        mtuFull = true;
                                    }
                                }

                                if (_inputStopwatch.ElapsedMilliseconds >= inputTimeout)
                                {
                                    isTimeout = true;
                                }

                                if (totalSize > 0 && mtuFull || isTimeout)
                                {
                                    _inputStopwatch.Restart();
                                    byte[] buffer = InsertHeaderMemoryStreams(senderid, PacketType.INPUT,
                                        _inputReliableItemsToSend);
                                    if (_isClient)
                                    {
                                        if (debugShowInputPackets)
                                            Debug.Log(
                                                $"Network Manager: Sending input as client, SIZE {buffer.Length}, TIMEOUT {inputTimeout} and SEQ INPUT: {sendSeqNumInput}");
                                        _client.SendTcp(buffer);
                                        sendSeqNumInput++;
                                        if (sendSeqNumInput == ulong.MaxValue - 1) sendSeqNumInput = 0;
                                    }
                                    else if (_isHost)
                                    {
                                        if (debugShowInputPackets)
                                            Debug.Log(
                                                $"Network Manager: Sending input as server, SIZE {buffer.Length}, TIMEOUT {inputTimeout} and SEQ INPUT: {sendSeqNumInput}");
                                        _server.SendTcpToAll(buffer);
                                        sendSeqNumInput++;
                                        if (sendSeqNumInput == ulong.MaxValue - 1) sendSeqNumInput = 0;
                                    }

                                    _inputReliableItemsToSend.Clear();
                                    totalSize = 0;
                                }
                            }
                        }
                    }

                    //State buffer
                    lock (_stateQueueLock)
                    {
                        if (_stateStreamBuffer.Count > 0)
                        {
                            int totalSize = 0;
                            bool mtuFull = false;
                            bool isTimeout = false;
                            //Debug.Log( $"Network Manager: Starting STATE loop, elapsedTime: {_stateStopwatch.ElapsedMilliseconds}");
                            while (_stateStreamBuffer.Count > 0 &&
                                   mtuFull == false /*&& _stateStopwatch.ElapsedMilliseconds < _stateBufferTimeout)*/)
                            {
                                if (_stateStreamBuffer.TryPeek(out ReplicationItem nextItem))
                                {
                                    // Able to peek
                                    MemoryStream currentItemHeaderStream = nextItem.header.GetSerializedHeader();
                                    if (totalSize + (int)nextItem.memoryStream.Length +
                                        (int)currentItemHeaderStream.Length <= _mtu)
                                    {
                                        // Able to insert next header into mtu
                                        if (_stateStreamBuffer.TryDequeue(out ReplicationItem replicationItem))
                                        {
                                            if (!IsReplicationItemDuplicate(nextItem.header,
                                                    out ReplicationItem alreadyExistingItem))
                                            {
                                                totalSize += (int)replicationItem.memoryStream.Length +
                                                             (int)currentItemHeaderStream.Length;
                                                _replicationItemsToSend.Add(replicationItem);
                                                if (debugShowObjectStatePackets)
                                                    Debug.Log(
                                                        $"Network Manager: Inserting item into STATE loop, elapsedTime: {_stateStopwatch.ElapsedMilliseconds}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Unable to insert next header into mtu because it's full
                                        mtuFull = true;
                                        if (debugShowObjectStatePackets)
                                            Debug.Log(
                                                $"Network Manager: MTU is full! STATE loop, elapsedTime: {_stateStopwatch.ElapsedMilliseconds}");
                                    }
                                }

                                if (_stateStopwatch.ElapsedMilliseconds >= stateTimeout)
                                {
                                    isTimeout = true;
                                }

                                if (totalSize > 0 && mtuFull || isTimeout)
                                {
                                    _stateStopwatch.Restart();
                                    byte[] buffer = InsertHeaderMemoryStreams(senderid, PacketType.OBJECT_STATE,
                                        _replicationItemsToSend);
                                    if (_isClient)
                                    {
                                        if (debugShowObjectStatePackets)
                                            Debug.Log(
                                                $"Network Manager: Sending state as client, Items:{_replicationItemsToSend.Count()}, SIZE {buffer.Length}, TIMEOUT {stateTimeout}, and SEQ STATE: {sendSeqNumState}");
                                        _client.SendUdpPacket(buffer);
                                        sendSeqNumState++;
                                        if (sendSeqNumState == ulong.MaxValue - 1) sendSeqNumState = 0;
                                    }
                                    else if (_isHost)
                                    {
                                        if (debugShowObjectStatePackets)
                                            Debug.Log(
                                                $"Network Manager: Sending state as server, Items:{_replicationItemsToSend.Count()}, SIZE {buffer.Length}, TIMEOUT {stateTimeout}, and SEQ STATE: {sendSeqNumState}");
                                        _server.SendUdpToAll(buffer);
                                        sendSeqNumState++;
                                        if (sendSeqNumState == ulong.MaxValue - 1) sendSeqNumState = 0;
                                    }

                                    _replicationItemsToSend.Clear();
                                    totalSize = 0;
                                }
                            }
                        }
                    }

                    // lock (_inputQueueLock)
                    // {
                    //    //Input buffer
                    //    if (_inputStreamBuffer.Count > 0)
                    //    {
                    //        //Debug.Log("Network Manager: Preparing input stream buffer");
                    //        int totalSize = 0;
                    //        List<MemoryStream> streamsToSend = new List<MemoryStream>();
                    //
                    //        //check if the totalsize + the next stream total size is less than the specified size
                    //        while (_inputStreamBuffer.Count > 0)
                    //        {
                    //            if (_inputStreamBuffer.TryPeek(out MemoryStream memoryStream) == false) break;
                    //            if (totalSize + (int)memoryStream.Length >= _mtu) break;
                    //            
                    //            if (_inputStreamBuffer.TryDequeue(out MemoryStream nextStream) == false) break;
                    //            totalSize += (int)nextStream.Length;
                    //            streamsToSend.Add(nextStream);
                    //
                    //            // Adjust timeout based on elapsed time
                    //            inputTimeout -= _inputStopwatch.ElapsedMilliseconds;
                    //            _inputStopwatch.Restart();
                    //            //Debug.Log($"Network Manager: Timeout input stream buffer {inputTimeout}");
                    //            if (totalSize > 0 && (totalSize <= _mtu || inputTimeout < _inputStopwatch.ElapsedMilliseconds))
                    //            {
                    //                inputTimeout = _inputBufferTimeout;
                    //                byte[] buffer =
                    //                    InsertHeaderMemoryStreams(senderid, PacketType.INPUT, streamsToSend);
                    //                if (_isClient)
                    //                {
                    //                    if(debugShowInputPackets) Debug.Log($"Network Manager: Sending input as client, SIZE {buffer.Length}, TIMEOUT {inputTimeout} and SEQ INPUT: {sendSeqNumInput}");
                    //                    _client.SendUdpPacket(buffer);
                    //                    sendSeqNumInput++;
                    //                    if (sendSeqNumInput == ulong.MaxValue - 1) sendSeqNumInput = 0;
                    //                }
                    //                else if (_isHost)
                    //                {
                    //                    if(debugShowInputPackets) Debug.Log($"Network Manager: Sending input as server, SIZE {buffer.Length}, TIMEOUT {inputTimeout} and SEQ INPUT: {sendSeqNumInput}");
                    //                    _server.SendUdpToAll(buffer);
                    //                    sendSeqNumInput++;
                    //                    if (sendSeqNumInput == ulong.MaxValue - 1) sendSeqNumInput = 0;
                    //                }
                    //            }
                    //        }
                    //    }
                    // }

                    // lock (_reliableInputQueueLock)
                    // {
                    //     //Reliable Input buffer
                    //     if (_inputRealiableStreamBuffer.Count > 0)
                    //     {
                    //         //Debug.Log("Network Manager: Preparing input stream buffer");
                    //         int totalSize = 0;
                    //         List<MemoryStream> streamsToSend = new List<MemoryStream>();
                    //
                    //         //check if the totalsize + the next stream total size is less than the specified size
                    //         while (_inputRealiableStreamBuffer.Count > 0)
                    //         {
                    //             if (_inputRealiableStreamBuffer.TryDequeue(out MemoryStream nextStream) == false) break;
                    //             totalSize += (int)nextStream.Length;
                    //             streamsToSend.Add(nextStream);
                    //
                    //             // Adjust timeout based on elapsed time
                    //             inputTimeout -= _inputStopwatch.ElapsedMilliseconds;
                    //             _inputStopwatch.Restart();
                    //             if(inputTimeout < _inputStopwatch.ElapsedMilliseconds)
                    //             {                                
                    //                 byte[] buffer = InsertHeaderMemoryStreams(senderid, PacketType.INPUT, streamsToSend);
                    //
                    //                 if (_isClient)
                    //                 {
                    //                     if (debugShowInputPackets) Debug.Log($"Network Manager: Sending input as client, SIZE {buffer.Length}, TIMEOUT {inputTimeout} and SEQ INPUT: {sendSeqNumInput}");
                    //                     _client.SendTcp(buffer);
                    //                     sendSeqNumInput++;
                    //                     if (sendSeqNumInput == ulong.MaxValue - 1) sendSeqNumInput = 0;
                    //                 }
                    //                 else if (_isHost)
                    //                 {
                    //                     if (debugShowInputPackets) Debug.Log($"Network Manager: Sending input as server, SIZE {buffer.Length}, TIMEOUT {inputTimeout} and SEQ INPUT: {sendSeqNumInput}");
                    //                     _server.SendTcpToAll(buffer);
                    //                     sendSeqNumInput++;
                    //                     if (sendSeqNumInput == ulong.MaxValue - 1) sendSeqNumInput = 0;
                    //                 }
                    //             }
                    //             
                    //         }
                    //     }
                    // }
                    lock (_reliableQueueLock)
                    {
                        // reliable buffer
                        if (_reliableStreamBuffer.Count > 0)
                        {
                            List<MemoryStream> streamsToSend = new List<MemoryStream>();
                            while (_reliableStreamBuffer.Count > 0)
                            {
                                if (_reliableStreamBuffer.TryDequeue(out MemoryStream nextStream) == false) break;
                                MemoryStream newStream = new MemoryStream();
                                BinaryWriter writer = new BinaryWriter(newStream);
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
                    if (_heartBeatStopwatch.ElapsedMilliseconds >= _heartBeatRate)
                    {
                        if (_isClient)
                        {
                            if (debugShowPingPackets) Debug.Log("Network Manager: Client HeartBeat");
                            _client.SendHeartBeat();
                        }
                        else if (_isHost)
                        {
                            if (debugShowPingPackets) Debug.Log("Network Manager: Server HeartBeat");
                            _server.SendHeartBeat();
                        }

                        _heartBeatStopwatch.Restart();
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
                    foreach (ReplicationItem replicationItem in _stateStreamBuffer)
                    {
                        replicationItem.memoryStream.Dispose();
                    }

                    _stateStreamBuffer.Clear(); // Clear the queue
                }

                lock (_inputQueueLock)
                {
                    Debug.Log($"Network Manager: Disposing input stream buffer");
                    foreach (InputItem incomingStream in _inputStreamBuffer)
                    {
                        incomingStream.memoryStream.Dispose();
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
                        lock (incomingStreamLock)
                        {
                            while (_incomingStreamBuffer.Count > 0)
                            {
                                if (_incomingStreamBuffer.TryDequeue(out MemoryStream memoryStream) == false) break;
                                ProcessIncomingPacket(memoryStream);
                            }
                        }
                    }

                    Thread.Sleep(1);
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogException(e);
            }
            finally
            {
                // Clean up resources
                lock (incomingStreamLock)
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
                case PacketType.SYNC:
                {
                    if (_isClient)
                    {
                        _client.SetTick(reader.ReadUInt16());
                    }
                }
                    break;
                case PacketType.PING:
                {
                    if (debugShowPingPackets) Debug.Log($"Network Manager: Received packet {type}");
                    if (_isHost)
                        _server.HandleHeartBeat(reader);
                    else if (_isClient) _client.HandleHeartBeat(reader);
                }
                    break;
                case PacketType.INPUT:
                {
                    if (debugShowInputPackets) Debug.Log($"Network Manager: Received packet {type}");
                    recSeqNumInput = reader.ReadUInt64();
                    UInt64 packetSenderId = reader.ReadUInt64();
                    long packetTimeStamp = reader.ReadInt64();
                    int inputItemsCount = reader.ReadInt32();
                    UnityMainThreadDispatcher.Dispatcher.Enqueue(() => HandleInput(stream, stream.Position,
                        packetSenderId, packetTimeStamp, recSeqNumInput, inputItemsCount));
                }
                    break;
                case PacketType.OBJECT_STATE:
                {
                    if (debugShowObjectStatePackets) Debug.Log($"Network Manager: Received packet {type}");
                    recSeqNumState = reader.ReadUInt64();
                    UInt64 packetSenderId = reader.ReadUInt64();
                    long packetTimeStamp = reader.ReadInt64();
                    int replicationItemsCount = reader.ReadInt32();
                    UnityMainThreadDispatcher.Dispatcher.Enqueue(() => HandleObjectState(stream, stream.Position,
                        packetSenderId, packetTimeStamp, recSeqNumState, replicationItemsCount));
                }
                    break;
                case PacketType.AUTHENTICATION:
                {
                    if (debugShowAuthenticationPackets) Debug.Log($"Network Manager: Received packet {type}");
                    if (_isClient)
                    {
                        Debug.Log("Network Manager: Client auth message received");
                        _client.authenticator.HandleAuthentication(reader);
                    }
                    else if (_isHost)
                    {
                        Debug.Log("Network Manager: Host auth message received");
                        _server.HandleAuthentication(reader);
                    }
                }
                    break;
                default:
                    Debug.LogError("Network Manager: Unknown packet type!");
                    break;
            }
        }

        void HandleObjectState(MemoryStream stream, Int64 streamPosition, UInt64 packetSender, Int64 timeStamp,
            UInt64 seqNum, int replicationItemsCount)
        {
            BinaryReader reader = new BinaryReader(stream);
            List<ReplicationHeader> replicationHeaders =
                ReplicationHeader.DeSerializeHeadersList(reader, replicationItemsCount);

            // TODO: If influx of packets is high, start discarding data and only read creates, destroy, updates, important events.
            currSeqNumStateRead = seqNum;
            foreach (ReplicationHeader header in replicationHeaders)
            {
                if (recSeqNumState - currSeqNumStateRead >= (ulong)thresholdToStartDiscardingPackets)
                    isBehindThreshold = true;
                else
                    isBehindThreshold = false;
                if (isBehindThreshold && (header.replicationAction == ReplicationAction.TRANSFORM ||
                                          header.replicationAction == ReplicationAction.EVENT))
                {
                    Debug.Log(
                        $"Network Manager: Is behind threshold, rec:{recSeqNumState} - read:{currSeqNumStateRead} >= {(ulong)thresholdToStartDiscardingPackets}, discarding {header.id}, {header.objectFullName},  {header.replicationAction},  {header.memoryStreamSize}");
                    reader.BaseStream.Seek(header.memoryStreamSize, SeekOrigin.Current);
                    continue;
                }

                try
                {
                    replicationManager.HandleReplication(reader, header.id, timeStamp, seqNum, header.replicationAction,
                        Type.GetType(header.objectFullName), header.memoryStreamSize);
                }
                catch (EndOfStreamException ex)
                {
                    Debug.LogError($"Network Manager: EndOfStreamException: {ex.Message}");
                }
            }

            isBehindThreshold = false;
        }

        void HandleInput(MemoryStream stream, Int64 streamPosition, UInt64 packetSender, Int64 timeStamp,
            UInt64 sequenceNumInput, int replicationItemsCount)
        {
            BinaryReader reader = new BinaryReader(stream);
            List<InputHeader> inputHeaders = InputHeader.DeSerializeHeadersList(reader, replicationItemsCount);
            if (inputHeaders.Count == 0) Debug.LogError("Error in input packet");
            foreach (InputHeader header in inputHeaders)
            {
                try
                {
                    if (header.objectFullName == "_Scripts.HitManager")
                    {
                        Debug.Log("Stop");
                    }
                    replicationManager.networkObjectMap[header.id]
                        .HandleNetworkInput(reader, packetSender, timeStamp, sequenceNumInput, header);
                }
                catch (EndOfStreamException ex)
                {
                    Debug.LogError($"Network Manager: EndOfStreamException: {ex.Message}");
                }
            }
        }

        #endregion

        private byte[] InsertHeaderMemoryStreams(UInt64 senderId, PacketType type, List<ReplicationItem> streamsList)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);

            // Packet type
            writer.Write((int)type);

            // Packet sequence number
            if (type == PacketType.OBJECT_STATE)
            {
                writer.Write(sendSeqNumState);
            }
            else if (type == PacketType.INPUT)
            {
                writer.Write(sendSeqNumInput);
            }

            // Packet sender id
            writer.Write(senderId);

            // Packet timestamp
            writer.Write(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);

            // Count of total replication items in this packet
            writer.Write(streamsList.Count);

            // Packet header contents
            foreach (ReplicationItem item in streamsList)
            {
                MemoryStream headerStream = item.header.GetSerializedHeader();
                headerStream.Position = 0;
                headerStream.CopyTo(output);
            }

            // Packet fields data contents
            foreach (ReplicationItem item in streamsList)
            {
                item.memoryStream.Position = 0;
                item.memoryStream.CopyTo(output);
            }

            return output.ToArray();
        }

        private byte[] InsertHeaderMemoryStreams(UInt64 senderId, PacketType type, List<InputItem> streamsList)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);

            // Packet type
            writer.Write((int)type);

            // Packet sequence number
            if (type == PacketType.OBJECT_STATE)
            {
                writer.Write(sendSeqNumState);
            }
            else if (type == PacketType.INPUT)
            {
                writer.Write(sendSeqNumInput);
            }

            // Packet sender id
            writer.Write(senderId);

            // Packet timestamp
            writer.Write(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);

            // Count of total replication items in this packet
            writer.Write(streamsList.Count);

            // Packet header contents
            foreach (InputItem item in streamsList)
            {
                MemoryStream headerStream = item.header.GetSerializedHeader();
                headerStream.Position = 0;
                headerStream.CopyTo(output);
            }

            // Packet fields data contents
            foreach (InputItem item in streamsList)
            {
                item.memoryStream.Position = 0;
                item.memoryStream.CopyTo(output);
            }

            return output.ToArray();
        }

        #region Client Events Interface

        public void ClientConnected()
        {
            OnClientConnected?.Invoke();
        }

        public void ClientDisconected(UInt64 id, string userName)
        {
            OnClientDisconnected?.Invoke();
            DespawnPlayer(id, userName);
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

        public Client.Client GetClient()
        {
            return _client;
        }

        public Server.Server GetServer()
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
            replicationManager.InitManager(list);
        }

        #endregion
    }
}
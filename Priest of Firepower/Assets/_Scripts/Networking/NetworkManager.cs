using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Networking
{
    public enum PacketType
    {
        PING,
        OBJECT_STATE,
        INPUT,
        AUTHENTICATION
    }

//this class will work as a client or server or both at the same time
    public class NetworkManager : GenericSingleton<NetworkManager>
    {
        AClient _client;
        AServer _server;

        private bool _isHost = false;
        private bool _isServer = false;
        private bool _isClient = false;

        uint _mtu = 1400;
        int _stateBufferTimeout = 1000; // time with no activity to send not fulled packets
        int _inputBufferTimeout = 100; // time with no activity to send not fulled packets
        // store all state streams to send
        Queue<MemoryStream> _stateStreamBuffer = new Queue<MemoryStream>();
        // store all input streams to send
        Queue<MemoryStream> _inputStreamBuffer = new Queue<MemoryStream>();
        // store all critical data streams to send (TCP)
        Queue<MemoryStream> _reliableStreamBuffer = new Queue<MemoryStream>();
        // Mutex for thread safety
        private readonly object _stateQueueLock = new object();
        private readonly object _inputQueueLock = new object();
        private readonly object _realiableQueueLock = new object();

        Queue<MemoryStream>_incomingStreamBuffer = new Queue<MemoryStream>();
        public readonly object IncomingStreamLock = new object();
        struct Process
        {
            public Thread Thread;
            public CancellationTokenSource CancellationToken;
        }

        Process _receiveData = new Process();
        Process _sendData = new Process();

        [FormerlySerializedAs("Player")] [SerializeField] GameObject player;
        
        [SerializeField]
        ConnectionAddressData connectionAddress;

        private ReplicationManager _replicationManager = new ReplicationManager();
        //Actions
        //  Invoked when a new client is connected
        public Action OnClientConnected;
        //  Invoken when a client is disconnected
        public Action OnClientDisconnected;
        // Invoken when client recieves server data
        public Action<byte[]> OnRecivedServerData;
        // Invoken when server recives data from clients
        public Action<byte[]> OnRecivedClientData;


        private void Start()
        {
            Debug.Log("Starting Network Manager ...");
            _receiveData.CancellationToken = new CancellationTokenSource();
            _receiveData.Thread = new Thread(() => ReceiveDataThread(_receiveData.CancellationToken.Token));
            _receiveData.Thread.Start();

            _sendData.CancellationToken = new CancellationTokenSource();
            _sendData.Thread = new Thread(() => SendDataThread(_receiveData.CancellationToken.Token));
            _sendData.Thread.Start();
            
            List<NetworkObject> list = FindObjectsOfType<NetworkObject>(true).ToList();
            _replicationManager.InitManager(list);
        }
        private void OnDisable()
        {
            Debug.Log("Stopping NetworkManger threads...");
            // When you want to stop the thread, you call cancellationTokenSource.Cancel(),
            // and the thread will stop executing the loop.
            _receiveData.CancellationToken?.Cancel();
            _sendData.CancellationToken?.Cancel();
            // You then wait for the thread to finish using thread.Join().
            _receiveData.Thread.Join();
            _sendData.Thread.Join();

            if (_receiveData.Thread.IsAlive)
                _receiveData.Thread.Abort();

            if (_sendData.Thread.IsAlive)
                _sendData.Thread.Abort();
        }

        #region Connection Initializers
        public void StartClient()
        {
            CreateClient();
            _isClient = true;
        }

        public void StartHost()
        {
            CreateServer();
            CreateClient();

            _isHost = true;

            _server.InitServer(); 
        
            if (_server.GetServerInit())
            {
                _client.Connect(IPAddress.Loopback);
            }
        }
        void StartServer()
        {
            CreateServer();
            _isServer = true;
        }
        #endregion

        //Initializers for the actions
        void CreateClient()
        {
            _client = new AClient();
            _client.OnConnected += ClientConnected;
        
        }
        void CreateServer()
        {
            _server = new AServer();        
        }

        public void AddStateStreamQueue(MemoryStream stream)
        {
            _stateStreamBuffer.Enqueue(stream);
        }
        public void AddInputStreamQueue(MemoryStream stream)
        {
            _inputStreamBuffer.Enqueue(stream);
        }
        public void AddReliableStreamQueue(MemoryStream stream)
        {
            _reliableStreamBuffer.Enqueue(stream);
        }

        public void AddIncomingDataQueue(MemoryStream stream)
        {
            _incomingStreamBuffer.Enqueue(stream);
        }

        private void SendDataThread(CancellationToken token)
        {
            try
            {
                Debug.Log("Netwrok Manager Send data thread started...");
                float stateTimeout = _stateBufferTimeout;
                float inputTimeout = _inputBufferTimeout;

                while (!token.IsCancellationRequested)
                {
                    //State buffer

                    if (_stateStreamBuffer.Count > 0)
                    {
                        lock (_stateQueueLock)
                        {
                            int totalSize = 0;
                            List<MemoryStream> streamsToSend = new List<MemoryStream>();

                            //check if the totalsize + the next stream total size is less than the specified size
                            while (_stateStreamBuffer.Count > 0 && totalSize + (int)_stateStreamBuffer.Peek().Length <= _mtu && _stateBufferTimeout > 0)
                            {
                                stateTimeout -= Time.deltaTime * 1000f;
                                MemoryStream nextStream = _stateStreamBuffer.Dequeue();
                                totalSize += (int)nextStream.Length;
                                streamsToSend.Add(nextStream);
                            }

                            byte[] buffer = ConcatenateMemoryStreams(PacketType.OBJECT_STATE, streamsToSend);

                            if (_isClient)
                            {
                                _client.SendPacket(buffer);
                            }
                            else if (_isServer)
                            {
                                _server.SendToAll(buffer);
                            }
                            else if (_isHost)
                            {
                                _server.SendToAll(buffer);
                            }
                        }
                    }

                    //Input buffer

                    if (_inputStreamBuffer.Count > 0)
                    {
                        lock (_inputQueueLock)
                        {
                            int totalSize = 0;
                            List<MemoryStream> streamsToSend = new List<MemoryStream>();

                            //check if the totalsize + the next stream total size is less than the specified size
                            while (_inputStreamBuffer.Count > 0 && totalSize + (int)_inputStreamBuffer.Peek().Length <= _mtu && _inputBufferTimeout > 0)
                            {
                                inputTimeout -= Time.deltaTime * 1000;
                                MemoryStream nextStream = _inputStreamBuffer.Dequeue();
                                totalSize += (int)nextStream.Length;
                                streamsToSend.Add(nextStream);
                            }
                            byte[] buffer = ConcatenateMemoryStreams(PacketType.INPUT, streamsToSend);

                            if (_isClient)
                            {
                                _client.SendPacket(buffer);
                            }
                            else if (_isServer)
                            {
                                _server.SendToAll(buffer);
                            }
                            else if (_isHost)
                            {
                                _server.SendToAll(buffer);
                            }
                        }
                    }

                    // reliable buffer

                    if (_reliableStreamBuffer.Count > 0)
                    {
                        lock (_realiableQueueLock)
                        {

                            List<MemoryStream> streamsToSend = new List<MemoryStream>();

                            while (_reliableStreamBuffer.Count > 0)
                            {
                            
                                MemoryStream nextStream = _reliableStreamBuffer.Dequeue();

                                byte[] buffer = nextStream.ToArray();

                                if (_isClient)
                                {
                                    _client.SendCriticalPacket(buffer);
                                }
                                else if (_isServer)
                                {
                                    _server.SendCriticalToAll(buffer);
                                }
                                else if (_isHost)
                                {
                                    _server.SendCriticalToAll(buffer);
                                }
                            }
                        }
                    }


                    if (inputTimeout < 0)
                        inputTimeout = _inputBufferTimeout;
                    if (stateTimeout < 0)
                        stateTimeout = _stateBufferTimeout;

                    Thread.Sleep(10);
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogException(e);
            }
            finally
            {
                // Clean up resources
                lock (_stateQueueLock)
                {
                    foreach (MemoryStream incomingStream in _stateStreamBuffer)
                    {
                        incomingStream.Dispose();
                    }
                    _stateStreamBuffer.Clear(); // Clear the queue
                }
                lock (_inputQueueLock)
                {
                    foreach (MemoryStream incomingStream in _inputStreamBuffer)
                    {
                        incomingStream.Dispose();
                    }
                    _inputStreamBuffer.Clear(); // Clear the queue
                }
                lock (_reliableStreamBuffer)
                {
                    foreach (MemoryStream incomingStream in _reliableStreamBuffer)
                    {
                        incomingStream.Dispose();
                    }
                    _reliableStreamBuffer.Clear(); // Clear the queue
                }
            }
        }


        private void ReceiveDataThread(CancellationToken token)
        {
            try
            {
                Debug.Log("Netwrok Manager receive data thread started...");
                while (!token.IsCancellationRequested)
                {
                    if (_incomingStreamBuffer.Count > 0)
                    {
                        Debug.Log("helo");
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
            BinaryReader reader = new BinaryReader(stream);

            PacketType type = (PacketType)reader.ReadInt32();

            switch (type)
            {
                case PacketType.PING:
                    break;
                case PacketType.INPUT:
                    break;
                case PacketType.OBJECT_STATE:
                    HandleObjectState(stream,reader);
                    break;
                case PacketType.AUTHENTICATION:
                    if(_isClient)
                    {
                        _client.GetAuthenticator().HandleAuthentication(stream, reader);
                    }
                    else if(_isHost)
                    {
                        _server.GetAuthenticator().HandleAuthentication(stream, reader);   
                    }
                    break;
                default:
                    break;
            }

        }
        void HandleObjectState(MemoryStream stream, BinaryReader reader)
        {
            while(reader.BaseStream.Position < reader.BaseStream.Length)
            {
                // [Object Class][Object ID]
                string objClass = reader.ReadString();
                UInt64 id = reader.ReadUInt64();
                NetworkAction networkAction = (NetworkAction)reader.ReadInt32();
                //read rest of the stream
                _replicationManager.HandleNetworkAction(id, networkAction, Type.GetType(objClass), reader);
            }
        }



        private byte[] ConcatenateMemoryStreams(PacketType type,List<MemoryStream> streams)
        {
            MemoryStream buffer = new MemoryStream();
        
            BinaryWriter writer = new BinaryWriter(buffer);
        
            writer.Write((int) type);
        
            foreach(MemoryStream stream in streams)
            {
                stream.CopyTo(buffer);
        
            }

            return buffer.ToArray();
        }

        #region Client Events Interface
        //Client Events Interface
        public void ClientConnected()
        {
            Debug.Log("Client Connected to server ...");
            OnClientConnected?.Invoke();
        }
        public void ClientDisconected()
        {
            OnClientDisconnected?.Invoke();
        }
        #endregion
        #region Server Events Interface
        //Server Events Interface
        public void ConnectClient(IPAddress address)
        {
            _client.Connect(address);
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

        [Serializable]
        public struct ConnectionAddressData
        {
            // IP address of the server (address to which clients will connect to).
            [FormerlySerializedAs("Address")]
            [Tooltip("IP address of the server (address to which clients will connect to).")]
            [SerializeField]
            public string address;

            // UDP port of the server.

            [FormerlySerializedAs("Port")]
            [Tooltip("UDP port of the server.")]
            [SerializeField]
            public ushort port;

            // IP address the server will listen on. If not provided, will use 'Address'.
            [FormerlySerializedAs("ServerListenAddress")]
            [Tooltip("IP address the server will listen on. If not provided, will use 'Address'.")]
            [SerializeField]
            public string serverListenAddress;

            private static EndPoint ParseNetworkEndpoint(string ip, ushort port)
            {
                IPAddress address = IPAddress.Parse(ip);
                if (address == null)
                {
                    Debug.Log(ip + " address is not valid ...");
                    return null;
                }
                return new IPEndPoint(address, port);
            }

            /// Endpoint (IP address and port) clients will connect to.
            public EndPoint ServerEndPoint => ParseNetworkEndpoint(address, port);

            /// Endpoint (IP address and port) server will listen/bind on.
            public EndPoint ListenEndPoint => ParseNetworkEndpoint((serverListenAddress == string.Empty) ? address : serverListenAddress, port);
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
            networkObjectMap[id].HandleNetworkBehaviour(type,reader);
        }

        private void HandleObjectDeSpawn(UInt64 id, NetworkAction action, Type type, BinaryReader reader)
        {
            // Destroy or return to pool
        }
        
        private void HandleObjectEvent(UInt64 id, NetworkAction action, Type type, BinaryReader reader){}

        private void HandleObjectTransform(UInt64 id, NetworkAction action, Type type, BinaryReader reader)
        {
            if (!networkObjectMap[id].synchronizeTransform)
            networkObjectMap[id].HandleNetworkTransform(reader);
        }
    }
}
using ClientA;
using ServerA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;

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
    AClient client;
    AServer server;

    private bool isHost = false;
    private bool isServer = false;
    private bool isClient = false;

    uint MTU = 1400;
    int stateBufferTimeout = 1000; // time with no activity to send not fulled packets
    int inputBufferTimeout = 100; // time with no activity to send not fulled packets
    // store all state streams to send
    Queue<MemoryStream> stateStreamBuffer = new Queue<MemoryStream>();
    // store all input streams to send
    Queue<MemoryStream> inputStreamBuffer = new Queue<MemoryStream>();
    // store all critical data streams to send (TCP)
    Queue<MemoryStream> reliableStreamBuffer = new Queue<MemoryStream>();
    // Mutex for thread safety
    private readonly object stateQueueLock = new object();
    private readonly object inputQueueLock = new object();
    private readonly object realiableQueueLock = new object();

    Queue<MemoryStream>incomingStreamBuffer = new Queue<MemoryStream>();
    public readonly object incomingStreamLock = new object();

    struct Process
    {
       public Thread thread;
       public CancellationTokenSource cancellationToken;
    }

    Process receiveData = new Process();
    Process sendData = new Process();

    [SerializeField] GameObject Player;
       

    [SerializeField]
    ConnectionAddressData connectionAddress;

    //Actions
    //  Invoked when a new client is connected
    public Action OnClientConnected;
    //  Invoken when a client is disconnected
    public Action OnClientDisconnected;
    // Invoken when client recieves server data
    public Action<byte[]> OnRecivedServerData;
    // Invoken when server recives data from clients
    public Action<byte[]> OnRecivedClientData;



    Dictionary<Int64, NetworkObject> networkObjectMap;



    
    
    private void Start()
    {

        Debug.Log("Starting Netwrok Manager ...");
        receiveData.cancellationToken = new CancellationTokenSource();
        receiveData.thread = new Thread(() => ReceiveDataThread(receiveData.cancellationToken.Token));
        receiveData.thread.Start();

        sendData.cancellationToken = new CancellationTokenSource();
        sendData.thread = new Thread(() => SendDataThread(receiveData.cancellationToken.Token));
        sendData.thread.Start();
    }
    private void OnDisable()
    {
        Debug.Log("Stopping NetworkManger threads...");
        // When you want to stop the thread, you call cancellationTokenSource.Cancel(),
        // and the thread will stop executing the loop.
        receiveData.cancellationToken?.Cancel();
        sendData.cancellationToken?.Cancel();
        // You then wait for the thread to finish using thread.Join().
        receiveData.thread.Join();
        sendData.thread.Join();
    }

    #region Connection Initializers
    public void StartClient()
    {
        CreateClient();
        isClient = true;
    }

    public void StartHost()
    {
        CreateServer();
        CreateClient();

        isHost = true;

        server.InitServer(); 
        
        if (server.GetServerInit())
        {
            client.Connect(IPAddress.Loopback);
        }
    }
    void StartServer()
    {
        CreateServer();
        isServer = true;
    }
    #endregion

    //Initializers for the actions
    void CreateClient()
    {
        client = new AClient();
        client.OnConnected += ClientConnected;
        
    }
    void CreateServer()
    {
        server = new AServer();        
    }

    public void AddStateStreamQueue(MemoryStream stream)
    {
       stateStreamBuffer.Enqueue(stream);
    }
    public void AddInputStreamQueue(MemoryStream stream)
    {
        inputStreamBuffer.Enqueue(stream);
    }
    public void AddReliableStreamQueue(MemoryStream stream)
    {
        reliableStreamBuffer.Enqueue(stream);
    }

    public void AddIncomingDataQueue(MemoryStream stream)
    {
        incomingStreamBuffer.Enqueue(stream);
    }

    private void SendDataThread(CancellationToken token)
    {
        try
        {
            Debug.Log("Netwrok Manager Send data thread started...");
            float stateTimeout = stateBufferTimeout;
            float inputTimeout = inputBufferTimeout;

            while (!token.IsCancellationRequested)
            {
                //State buffer

                if (stateStreamBuffer.Count > 0)
                {
                    lock (stateQueueLock)
                    {
                        int totalSize = 0;
                        List<MemoryStream> streamsToSend = new List<MemoryStream>();

                        //check if the totalsize + the next stream total size is less than the specified size
                        while (stateStreamBuffer.Count > 0 && totalSize + (int)stateStreamBuffer.Peek().Length <= MTU && stateBufferTimeout > 0)
                        {
                            stateTimeout -= Time.deltaTime * 1000f;
                            MemoryStream nextStream = stateStreamBuffer.Dequeue();
                            totalSize += (int)nextStream.Length;
                            streamsToSend.Add(nextStream);
                        }

                        byte[] buffer = ConcatenateMemoryStreams(PacketType.OBJECT_STATE, streamsToSend);

                        if (isClient)
                        {
                            client.SendPacket(buffer);
                        }
                        else if (isServer)
                        {
                            server.SendToAll(buffer);
                        }
                        else if (isHost)
                        {
                            server.SendToAll(buffer);
                        }
                    }
                }

                //Input buffer

                if (inputStreamBuffer.Count > 0)
                {
                    lock (inputQueueLock)
                    {
                        int totalSize = 0;
                        List<MemoryStream> streamsToSend = new List<MemoryStream>();

                        //check if the totalsize + the next stream total size is less than the specified size
                        while (inputStreamBuffer.Count > 0 && totalSize + (int)inputStreamBuffer.Peek().Length <= MTU && inputBufferTimeout > 0)
                        {
                            inputTimeout -= Time.deltaTime * 1000;
                            MemoryStream nextStream = inputStreamBuffer.Dequeue();
                            totalSize += (int)nextStream.Length;
                            streamsToSend.Add(nextStream);
                        }
                        byte[] buffer = ConcatenateMemoryStreams(PacketType.INPUT, streamsToSend);

                        if (isClient)
                        {
                            client.SendPacket(buffer);
                        }
                        else if (isServer)
                        {
                            server.SendToAll(buffer);
                        }
                        else if (isHost)
                        {
                            server.SendToAll(buffer);
                        }
                    }
                }

                // reliable buffer

                if (reliableStreamBuffer.Count > 0)
                {
                    lock (realiableQueueLock)
                    {

                        List<MemoryStream> streamsToSend = new List<MemoryStream>();

                        while (reliableStreamBuffer.Count > 0)
                        {
                            
                            MemoryStream nextStream = reliableStreamBuffer.Dequeue();

                            byte[] buffer = nextStream.ToArray();

                            if (isClient)
                            {
                                client.SendCriticalPacket(buffer);
                            }
                            else if (isServer)
                            {
                                server.SendCriticalToAll(buffer);
                            }
                            else if (isHost)
                            {
                                server.SendCriticalToAll(buffer);
                            }
                        }
                    }
                }


                if (inputTimeout < 0)
                    inputTimeout = inputBufferTimeout;
                if (stateTimeout < 0)
                    stateTimeout = stateBufferTimeout;

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
            lock (stateQueueLock)
            {
                foreach (MemoryStream incomingStream in stateStreamBuffer)
                {
                    incomingStream.Dispose();
                }
                stateStreamBuffer.Clear(); // Clear the queue
            }
            lock (inputQueueLock)
            {
                foreach (MemoryStream incomingStream in inputStreamBuffer)
                {
                    incomingStream.Dispose();
                }
                inputStreamBuffer.Clear(); // Clear the queue
            }
            lock (reliableStreamBuffer)
            {
                foreach (MemoryStream incomingStream in reliableStreamBuffer)
                {
                    incomingStream.Dispose();
                }
                reliableStreamBuffer.Clear(); // Clear the queue
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
                if (incomingStreamBuffer.Count > 0)
                {
                    lock (incomingStreamLock)
                    {
                        while (incomingStreamBuffer.Count > 0)
                        {
                            ProcessIncomingData(incomingStreamBuffer.Dequeue());
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
            lock (incomingStreamLock)
            {
                foreach (MemoryStream incomingStream in incomingStreamBuffer)
                {
                    incomingStream.Dispose();
                }
                incomingStreamBuffer.Clear(); // Clear the queue
            }
        }

    }
    public void ProcessIncomingData(MemoryStream stream)
    {

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
                if(isClient)
                {
                    client.GetAuthenticator().HandleAuthentication(stream, reader);
                }
                else if(isHost)
                {
                    server.GetAuthenticator().HandleAuthentication(stream, reader);   
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
            Int64 id = reader.ReadInt64();    

            //read rest of the stream
            networkObjectMap[id].HandleNetworkBehaviour(Type.GetType(objClass), reader);
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
        client.Connect(address);
    }

    #endregion
    #region Getters
    public bool IsHost()
    {
        return isHost;
    }

    public bool IsServer()
    {
        return isServer;
    }

    public bool IsClient()
    {
        return isClient;
    }
    #endregion


    // Structure to store the address to connect to

    #region address

    [Serializable]
    public struct ConnectionAddressData
    {
        // IP address of the server (address to which clients will connect to).
        [Tooltip("IP address of the server (address to which clients will connect to).")]
        [SerializeField]
        public string Address;

        // UDP port of the server.

        [Tooltip("UDP port of the server.")]
        [SerializeField]
        public ushort Port;

        // IP address the server will listen on. If not provided, will use 'Address'.
        [Tooltip("IP address the server will listen on. If not provided, will use 'Address'.")]
        [SerializeField]
        public string ServerListenAddress;

        private static EndPoint ParseNetworkEndpoint(string ip, ushort port)
        {
            IPAddress address = IPAddress.Parse(ip);
            if (address == null)
            {
                Debug.LogError(ip + " address is not valid ...");
                return null;
            }
            return new IPEndPoint(address, port);
        }

        /// Endpoint (IP address and port) clients will connect to.
        public EndPoint ServerEndPoint => ParseNetworkEndpoint(Address, Port);

        /// Endpoint (IP address and port) server will listen/bind on.
        public EndPoint ListenEndPoint => ParseNetworkEndpoint((ServerListenAddress == string.Empty) ? Address : ServerListenAddress, Port);
    }
    #endregion
}


public class ObjectRegistery
{

}
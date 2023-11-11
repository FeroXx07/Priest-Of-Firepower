using ClientA;
using ServerA;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public enum PacketType
{
    PING,
    OBJECT_STATE,
    INPUT
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
    int sendBufferTimeout = 1000; // time with no activity to send not fulled packets
    Queue<MemoryStream> stateStreamBuffer = new Queue<MemoryStream>();
    Queue<MemoryStream> inputStreamBuffer = new Queue<MemoryStream>();

    // Queue to store MemoryStreams
    private Queue<MemoryStream> dataQueue = new Queue<MemoryStream>();
    // Mutex for thread safety
    private readonly object queueLock = new object();



    [SerializeField] GameObject Player;

    IPAddress serverIP;
    IPEndPoint endPoint;

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

    private void OnEnable()
    {
        
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
    //last one todo (optional)
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


    public void AddDataToQueue(MemoryStream stream)
    {
       dataQueue.Enqueue(stream);
    }
    
    private void SendDataThread(CancellationTokenSource token)
    {
        float timeout = sendBufferTimeout;
        while(!token.IsCancellationRequested)
        {
           

            //State buffer
            if(dataQueue.Count > 0)
            {
                lock(queueLock)
                {
                    int totalSize = 0;
                    List<MemoryStream> streamsToSend = new List<MemoryStream>();

                    //check if the totalsize + the next stream total size is less than the specified size
                    while (dataQueue.Count > 0 && totalSize + (int)dataQueue.Peek().Length <= MTU || timeout <= 0)
                    {
                        timeout -= Time.deltaTime;
                        MemoryStream nextStream = dataQueue.Dequeue();
                        totalSize += (int)nextStream.Length;
                        streamsToSend.Add(nextStream);
                    }
                    byte[] buffer = ConcatenateMemoryStreams(streamsToSend);

                    if(isClient)
                    {
                        client.SendPacket(buffer);
                    }
                    else if(isServer)
                    {
                        server.SendToAll(buffer);
                    }
                    else if(isHost)
                    {
                        server.SendToAll(buffer);
                    }                  
                }
            }

            //Input buffer


            Thread.Sleep(10);
        }
    }
    private byte[] ConcatenateMemoryStreams(List<MemoryStream> streams)
    {
        MemoryStream buffer = new MemoryStream();
        foreach(MemoryStream stream in streams)
        {
            stream.CopyTo(buffer);
        
        }

        return buffer.ToArray();
    }

    #region Client Functions
    void SendDataToServer(byte[] data)
    {
        client.SendPacket(data);
    }
    #endregion
    #region Server Funtions
    void SendDataToAllClients(byte[] data)
    {

    }
    #endregion
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
    private bool IsHost()
    {
        return isHost;
    }

    private bool IsServer()
    {
        return isServer;
    }

    private bool IsClient()
    {
        return isClient;
    }
    #endregion


    // Structure to store the address to connect to



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
}

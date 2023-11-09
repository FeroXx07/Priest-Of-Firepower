using ClientA;
using ServerA;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;


//this class will work as a client or server or both at the same time
public class NetworkManager : GenericSingleton<NetworkManager>
{
    AClient client;
    AServer server;

    [SerializeField] GameObject Player;

    IPAddress serverIP;
    IPEndPoint endPoint;

    [SerializeField]
    ConnectionAddressData connectionAddress;

    //Actions
    public Action OnClientConnected;

    private void OnEnable()
    {
        
    }
    #region Connection Initializers
    public void StartClient()
    {
        CreateClient();
    }

    public void StartHost()
    {
        CreateServer();
        CreateClient();

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
    }
    #endregion

    //
    void CreateClient()
    {
        client = new AClient();
        client.OnConnected += ClientConnected;
    }
    void CreateServer()
    {
        server = new AServer();
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

    }
    #endregion
    #region Server Events Interface
    //Server Events Interface
    public void ConnectClient(IPAddress address)
    {
        client.Connect(address);
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
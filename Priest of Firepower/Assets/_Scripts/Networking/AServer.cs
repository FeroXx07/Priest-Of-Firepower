
#define AUTHENTICATION_CODE
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace _Scripts.Networking
{

    public class ClientManager
    {
        private int nextClientId = 0;

        public int GetNextClientId()
        {
            int clientId = nextClientId;
            nextClientId++;
            return clientId;
        }
    }
    public class AServer : GenericSingleton<AServer>
    {
        #region variables
        IPEndPoint endPoint;
        //[SerializeField] int port = 12345;
        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        private CancellationTokenSource authenticationToken;
        private Thread authenticationThread;

        ClientManager clientManager;

        private List<ClientProcess> clientThreads = new List<ClientProcess>();
        private List<ClientData> clientList = new List<ClientData>();
        private List<ClientData> clientListToRemove = new List<ClientData>();
        //private ConcurrentBag<ClientData> clientList = new ConcurrentBag<ClientData>();

        //actions
        Action<int> OnClientAccepted;
        Action OnClientRemoved;
        Action<int> OnClientDisconnected;
        Action<byte[]> OnDataRecieved;

        //handeles connection with clients
        Socket serverTCP;
        Socket serverUDP;

        ServerAuthenticator authenticator = new ServerAuthenticator();

        struct ClientProcess
        {
            public Thread thread;
            public CancellationTokenSource token;
        }

        private bool IsServerInitialized  = false;
        #endregion

        #region client data
        class ClientData
        {
            public int ID = -1;
            public string username = "";
            public ClientMetadata metaData;
            public ClientSate state;
            public Socket connectionTCP;
            public Socket connectionUDP;
            public CancellationTokenSource authenticationToken; //if disconnection request invoke cancellation token to shutdown all related processes
            public bool IsHost = false;
        }
        struct ClientMetadata
        {
            public int port;
            public IPAddress IP;
            //add time stamp
        }
        public enum ClientSate
        {
            Connected,
            Authenticated,
            InGame
        }
        #endregion

        #region enable/disable functions
        private void OnDisable()
        {

            Debug.Log("Stopping server ...");

            StopAuthenticationThread();

            DisconnectAllClients();

            StopAllClientThreads();

            Debug.Log("Closing server connection ...");

            if (serverTCP.Connected)
            {
                serverTCP.Shutdown(SocketShutdown.Both);
            }
            serverTCP.Close();
        }
        private void Update()
        {
            RemoveDisconectedClient();
        }
        #endregion

        #region helper funcitons
        void StopAuthenticationThread()
        {

            authenticationToken.Cancel();
            if (authenticationThread != null)
            {
                if (authenticationThread.IsAlive)
                {
                    authenticationThread.Join();
                }
                //make sure it is not alive
                if (authenticationThread.IsAlive)
                {
                    authenticationThread.Abort();
                }
            }
        }
        void DisconnectAllClients()
        {
            foreach (ClientData client in clientList)
            {
                RemoveClient(client);
            }
        }
        void StopAllClientThreads()
        {

            Debug.Log("Server: Waiting for all threads to terminate.");
            foreach (ClientProcess p in clientThreads)
            {
                p.token.Cancel();
                if (p.thread.IsAlive)
                    p.thread.Join();
            }
            foreach (ClientProcess p in clientThreads)
            {
                if (p.thread.IsAlive)
                    p.thread.Abort();
            }
        }
        void RemoveDisconectedClient()
        {
            if (clientListToRemove.Count > 0)
            {
                lock (clientList)
                {
                    foreach (ClientData clientToRemove in clientListToRemove)
                    {
                        clientList.Remove(clientToRemove);
                    }
                }
                Debug.Log("removed " + clientListToRemove.Count + " clients");
                clientListToRemove.Clear();
            }
        }
        #endregion

        #region getter setter funtions
        public bool GetServerInit() { return IsServerInitialized; }
        #endregion

        #region core functions
        public void InitServer()
        {
            clientManager = new ClientManager();
            //start server
            StartConnectionListenerTCP();
        }

        public void SendToAll(byte[] data)
        {
            foreach(ClientData client in clientList)
            {
                client.connectionUDP.SendTo(data, data.Length, SocketFlags.None, endPoint);
            }
        }
        public  void SendCriticalToAll(byte[] data)
        {
            foreach (ClientData client in clientList)
            {
                client.connectionTCP.SendTo(data, data.Length, SocketFlags.None, endPoint);
            }
        }
        public void SendToClient(int clientId, byte[] data)
        {
            foreach (ClientData client in clientList)
            {
                if (client.ID == clientId)
                {
                    client.connectionUDP.SendTo(data,data.Length, SocketFlags.None, endPoint);

                    return;
                }
            }
        }
        void StartConnectionListenerTCP()
        {
            try
            {
                Debug.Log("Starting server ...");
                //create listener tcp
                serverTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //create end point
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                //So for the ip any it listens to all directions ipv4 local LAN and 
                //also the public ip. TOconnect from the client use any of the ips
                endPoint = new IPEndPoint(IPAddress.Any, 12345);
                //bind to ip and port to listen to
                serverTCP.Bind(endPoint);
                // Using ListenForConnections() method we create 
                // the Client list that will want
                // to connect to Server
                serverTCP.Listen(4);

                authenticationToken = new CancellationTokenSource();
                authenticationThread = new Thread(() => Authenticate(authenticationToken.Token));
                authenticationThread.Start();

                IsServerInitialized = true;

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }  
        void HandleClient(ClientData clientData)
        {
            Debug.Log("Starting Client Thread " + clientData.ID + " ...");
            try
            {
                while (!clientData.authenticationToken.IsCancellationRequested)
                {

                    if (clientData.connectionTCP.Available > 0)
                    {
                        ReceiveSocketData(clientData.connectionTCP);
                    }
                    if (clientData.connectionUDP.Available > 0)
                    {
                        ReceiveSocketData(clientData.connectionUDP);
                    }
                    // Add some delay to avoid busy-waiting
                    Thread.Sleep(10);
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset ||
                    se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // Handle client disconnection (optional)
                    Debug.Log($"Client {clientData.ID} disconnected: {se.Message}");
                    RemoveClient(clientData);
                }
                else
                {
                    // Handle other socket exceptions
                    Debug.Log($"SocketException: {se.SocketErrorCode}, {se.Message}");
                }
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.Log($"Exception: {e.Message}");
            }
        }
        void ReceiveSocketData(Socket socket)
        {
            try
            {
                lock(NetworkManager.Instance.incomingStreamLock)
                {
                    byte[] buffer = new byte[1500];

                    Debug.Log("Server: waiting for authentication ... ");
                    // Receive data from the client
                    socket.Receive(buffer);
                    Debug.Log("Server: recieved data ... ");
                    MemoryStream stream = new MemoryStream(buffer);

                    NetworkManager.Instance.AddIncomingDataQueue(stream);
                }
            }
            catch (SocketException se)
            {
               // Handle other socket exceptions
               Debug.Log($"SocketException: {se.SocketErrorCode}, {se.Message}");

            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.Log($"Exception: {e.Message}");
            }
        }
        int CreateClient(Socket clientSocket, string userName)
        {
            lock (clientList)
            {
                ClientData clientData = new ClientData();

                clientData.ID = clientManager.GetNextClientId();
                clientData.username = userName;

                clientData.connectionTCP = clientSocket;
                //add a time out exeption for when the client disconnects or has lag or something
                clientData.connectionTCP.ReceiveTimeout = 1000;
                clientData.connectionTCP.SendTimeout = 1000;

                //create udp connection
                clientData.connectionUDP = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
                clientData.connectionUDP.Bind(clientSocket.RemoteEndPoint);
                clientData.connectionUDP.ReceiveTimeout = 100;
                clientData.connectionUDP.SendTimeout = 100;

                //store endpoint
                IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                clientData.metaData.IP = clientEndPoint.Address;
                clientData.metaData.port = clientEndPoint.Port;

                clientData.authenticationToken = new CancellationTokenSource();

                clientList.Add(clientData);

                Debug.Log("Connected client Id: " + clientData.ID);


                //Create the handeler of the chat for that client
                //create a hole thread to recive important data from server-client
                //like game state, caharacter selection, map etc

                ClientProcess process = new ClientProcess();

                process.token = new CancellationTokenSource();

                process.thread = new Thread(() => HandleClient(clientData));

                process.thread.IsBackground = true;
                process.thread.Name = clientData.ID.ToString();
                process.thread.Start();



                clientThreads.Add(process);

                return clientData.ID;
            }
        }
        void RemoveClient(ClientData clientData)
        {
            // Remove the client from the list of connected clients    
            lock (clientList)
            {
                // Cancel the client's cancellation token source
                clientData.authenticationToken.Cancel();

                // Close the client's socket
                if (clientData.connectionTCP.Connected)
                {
                    clientData.connectionTCP.Shutdown(SocketShutdown.Both);
                }
                clientData.connectionTCP.Close();

                clientListToRemove.Add(clientData);

                Debug.Log("Client " + clientData.ID + " disconnected.");
            }
        }
        #endregion

        #region Authentication
        void Authenticate(CancellationToken cancellationToken)
        {
            try
            {
                //accept new Connections ... 
                while (!cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Server: Waiting connection ... ");
                    Socket clientSocket = serverTCP.Accept();
                    Debug.Log("Server: new socket accepted ...  ");
                    if (clientSocket.Available > 0)
                        ReceiveSocketData(clientSocket);                

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                authenticationToken.Cancel();
                Debug.Log("Shutting down authentication process ...");
            }
            finally
            {

            }
        }

        public ServerAuthenticator GetAuthenticator() { return authenticator; }
        #endregion
    }
}

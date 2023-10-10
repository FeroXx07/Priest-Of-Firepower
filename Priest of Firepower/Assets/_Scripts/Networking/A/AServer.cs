using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ServerA
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
    public class AServer : MonoBehaviour
    {
        AServer instance;
        AServer Instance { get { return instance; } }
        IPEndPoint endPoint;
        [SerializeField] int port = 12345;
        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        private CancellationTokenSource connectionCancellationTokenSource;
        private Task listenerTask;
        private Thread listenerThread;

        ClientManager clientManager;

        private List<Thread> clientThreads = new List<Thread>();
        private List<ClientData> clientList = new List<ClientData>();
        private List<ClientData> clientListToRemove = new List<ClientData>();
        //private ConcurrentBag<ClientData> clientList = new ConcurrentBag<ClientData>();

        //actions
        Action<int> OnClientAccepted;
        Action OnClientRemoved;
        Action<string> OnDataRecieved;

        //handeles connection with clients
        Socket server;
         class ClientData
        {
            public int ID;
            public ClientMetadata metaData;
            public ClientSate state;
            public Socket socket;
            public CancellationTokenSource cancellationTokenSource;
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

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if(instance != null)
                DestroyImmediate(instance);
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            clientManager = new ClientManager();
            //start server
            StartConnectionListenerTPC();
        }
        private void OnEnable()
        {

        }

        private void OnDisable()
        {
          
            StopListening();

            DisconnectAllClients();

            if(server.Connected)
            {
                server.Shutdown(SocketShutdown.Both);
            }
            server.Close();
        }

        private void Update()
        {
            RemoveDisconectedClient();
        }

        void StartConnectionListenerTPC()
        {
            try
            {
                Debug.Log("Starting server ...");
                //create listener tcp
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //create end point
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                //So for the ip any it listens to all directions ipv4 local LAN and 
                //also the public ip. TOconnect from the client use any of the ips
                endPoint = new IPEndPoint(IPAddress.Any, 12345);
                //bind to ip and port to listen to
                server.Bind(endPoint);
                // Using ListenForConnections() method we create 
                // the Client list that will want
                // to connect to Server
                server.Listen(4);
                
                connectionCancellationTokenSource = new CancellationTokenSource();
                listenerThread = new Thread(() => ConnectionListenerTCP(connectionCancellationTokenSource.Token));
                listenerThread.Start();
              
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void ConnectionListenerTCP(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Waiting connection ... ");

                    Socket clientSocket = server.Accept();


                    //add this accepted client into the client list
                    int clientID = CreateClient(clientSocket);
                    
                    //call any related action to this event
                    OnClientAccepted?.Invoke(clientID);

                    // Add some delay to avoid busy-waiting
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        void HandleChat(ClientData clientData)
        {
            Debug.Log("Starting chat thread " +clientData.ID + " ...");
            
            while (!clientData.cancellationTokenSource.IsCancellationRequested)
            {
                Socket clientSocket = clientData.socket;
                string data = null;
                try
                {
                    byte[] buffer = new byte[1024];
                    
                    // Receive data from the client
                    int bufferSize = clientSocket.Receive(buffer);
                    data = Encoding.ASCII.GetString(buffer, 0, bufferSize);
                
                    if (!string.IsNullOrEmpty(data))
                    {
                        Debug.Log("message recived: "+ data);
                        // Process the received message (e.g., broadcast to all clients)
                        BroadcastMessage(data, clientData.ID);
                    }
                   
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.ConnectionReset ||
                        se.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        // Handle client disconnection (optional)
                        Debug.LogError($"Client {clientData.ID} disconnected: {se.Message}");
                        RemoveClient(clientData);
                    }
                    else
                    {
                        // Handle other socket exceptions
                        Debug.LogError($"SocketException: {se.SocketErrorCode}, {se.Message}");
                    }
                    continue;
                }
                catch (Exception e)
                {
                    // Handle other exceptions
                    Debug.LogError($"Exception: {e.Message}");

                    continue;
                }
                // Add some delay to avoid busy-waiting
                Thread.Sleep(100);
            }
        }
         
        int CreateClient(Socket clientSocket)
        {
            lock(clientList)
            {
                ClientData clientData = new ClientData();


                clientSocket.ReceiveTimeout = 100;
                clientSocket.SendTimeout = 100;

                clientData.socket = clientSocket;

                IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                clientData.metaData.IP = clientEndPoint.Address;
                clientData.metaData.port = clientEndPoint.Port;
                clientData.cancellationTokenSource = new CancellationTokenSource();  
                clientData.ID = clientManager.GetNextClientId();

                clientList.Add(clientData);

                Debug.Log("Connected client Id: " + clientData.ID);


                //Create the handeler of the caht for that client
                Thread t = new Thread(() => HandleChat(clientData));

                t.IsBackground = true;
                t.Name = clientData.ID.ToString();
                t.Start();

                clientThreads.Add(t);

                return clientData.ID;
            }
        }
        void RemoveClient(ClientData clientData)
        {
            // Remove the client from the list of connected clients    
            lock (clientList)
            {
                // Cancel the client's cancellation token source
                clientData.cancellationTokenSource.Cancel();

                // Close the client's socket
                if (clientData.socket.Connected)
                {
                    clientData.socket.Shutdown(SocketShutdown.Both);
                }
                clientData.socket.Close();
                clientListToRemove.Add(clientData);

                Debug.Log("Client " + clientData.ID + " disconnected.");
            }            
        }

        void BroadcastMessage(string message, int senderID)
        {
            List<ClientData> clients = new List<ClientData>();
            lock (clientList)
            {
                clients = new List<ClientData>(clientList);
            }
            foreach (ClientData clientData in clients)
            {
                try
                {
                    // Skip broadcasting to the sender
                    if (clientData.ID == senderID)
                    {
                        continue;
                    }

                    Socket clientSocket = clientData.socket;

                    // Send the message to other clients
                    byte[] data = Encoding.ASCII.GetBytes($"Client {senderID}: {message}");
                    clientSocket.Send(data);

                    Debug.Log("message broadcasted ...");
                }
                catch (SocketException se)
                {
                    // Handle socket exceptions (e.g., client disconnection)
                    Debug.LogError($"Error broadcasting message to client {clientData.ID}: {se.Message}");
                    RemoveClient(clientData);
                }
                catch (Exception e)
                {
                    // Handle other exceptions
                    Debug.LogError($"Error broadcasting message: {e.Message}");
                }
            }
            
        }
        void StopListening()
        {
            if (listenerThread != null && listenerThread.IsAlive)
            {
                // Signal the thread to exit 
                connectionCancellationTokenSource.Cancel();
                listenerThread.Abort();
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
            foreach(Thread t in clientThreads)
            {
                t.Abort();
            }
            // Print status message.
            Debug.Log("Server: Waiting for all threads to terminate.");

            // Wait for all client threads to really terminate.
            foreach (Thread t in clientThreads)
            {
                t.Join();
            }
        }
        void HandleClient(Socket clientSocket, CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[1024];
                string data = null;

                while (!cancellationToken.IsCancellationRequested)
                {
                    //get the buffer size
                    int bufferSize = clientSocket.Receive(buffer);

                    data += Encoding.ASCII.GetString(buffer, 0, bufferSize);

                    //check end of the socket
                    if (data.IndexOf("<EOF>") > -1)
                        break;
                }
                Debug.Log("Text received -> " + data);
                byte[] message = Encoding.ASCII.GetBytes("Recived data server A");

                // Send a message to Client 
                // using Send() method
                clientSocket.Send(message);

            }
            catch (Exception e)
            {
                Debug.LogError(e);
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

#region Async


        private async Task Heartbeat()
        {
            //foreach (ClientData client in clientList)
            //{
            //    client.socket.SendAsync();
            //}
        }
        async Task ListenerTCPAsync(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log("Starting server ...");
                //create listener tcp
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //create end point
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                //So for the ip any it listens to all directions ipv4 local LAN and 
                //also the public ip. TOconnect from the client use any of the ips
                endPoint = new IPEndPoint(IPAddress.Any, 12345);
                //bind to ip and port to listen to
                listener.Bind(endPoint);
                // Using ListenForConnections() method we create 
                // the Client list that will want
                // to connect to Server
                listener.Listen(4);

                Debug.Log("Server listening ...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Waiting connection ... ");

                    Socket clientSocket = await listener.AcceptAsync();
                    IPEndPoint endPoint = (IPEndPoint)clientSocket.LocalEndPoint;
                    int localPort = endPoint.Port;
                    Debug.Log("Connection accepted -> " + localPort);

                    int clientID = CreateClient(clientSocket);
                    //add this accepted client into the client list
                    OnClientAccepted?.Invoke(clientID);
                    // Add some delay to avoid busy-waiting
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        async Task HandleChatAsync(CancellationToken cancellationToken)
        {
            Debug.Log("Starting chat thread ...");

            while (!cancellationToken.IsCancellationRequested)
            {
                // Check for incoming messages from all clients
                foreach (ClientData clientData in clientList)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break; // Exit if cancellation is requested
                    }

                    Socket clientSocket = clientData.socket;

                    try
                    {
                        byte[] buffer = new byte[1024];

                        // Use ReceiveAsync to asynchronously receive data from the client
                        int bufferSize = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None, cancellationToken);
                        string data = Encoding.ASCII.GetString(buffer, 0, bufferSize);

                        if (!string.IsNullOrEmpty(data))
                        {
                            Debug.Log("message received: " + data);
                            // Process the received message (e.g., broadcast to all clients)
                            BroadcastMessage(data, clientData.ID);
                        }
                    }
                    catch (SocketException se)
                    {
                        if (se.SocketErrorCode == SocketError.ConnectionReset ||
                            se.SocketErrorCode == SocketError.ConnectionAborted)
                        {
                            // Handle client disconnection (optional)
                            Debug.LogError($"Client {clientData.ID} disconnected: {se.Message}");
                            RemoveClient(clientData);
                        }
                        else
                        {
                            // Handle other socket exceptions
                            Debug.LogError($"SocketException: {se.SocketErrorCode}, {se.Message}");
                        }
                    }
                    catch (Exception e)
                    {
                        // Handle other exceptions
                        Debug.LogError($"Exception: {e.Message}");
                    }
                }

                // Add some delay to avoid busy-waiting
                await Task.Delay(100);
            }
        }
        async Task BroadcastMessageAsync(string message, int senderID)
        {
            foreach (ClientData clientData in clientList)
            {
                try
                {
                    // Skip broadcasting to the sender
                    if (clientData.ID == senderID)
                    {
                        continue;
                    }

                    Socket clientSocket = clientData.socket;

                    // Send the message to other clients
                    byte[] data = Encoding.ASCII.GetBytes($"Client {senderID}: {message}");
                    await clientSocket.SendAsync(new ArraySegment<byte>(data),SocketFlags.None);

                    Debug.Log("message broadcasted ...");
                }
                catch (SocketException se)
                {
                    // Handle socket exceptions (e.g., client disconnection)
                    Debug.LogError($"Error broadcasting message to client {clientData.ID}: {se.Message}");
                    RemoveClient(clientData);
                }
                catch (Exception e)
                {
                    // Handle other exceptions
                    Debug.LogError($"Error broadcasting message: {e.Message}");
                }
            }
        }
        #endregion
        void ListenerUDP()
        {
            try
            {
                //create listener udp
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                endPoint = new IPEndPoint(IPAddress.Any, 0);
                //bind to ip and port to listen to
                listener.Bind(endPoint);

                while (true)
                {
                    try
                    {
                        listener.Listen(1);
                        Debug.Log("waiting for clients....");
                        Socket client = listener.Accept();
                        IPEndPoint clientIP = (IPEndPoint)client.RemoteEndPoint;


                    }
                    catch (SocketException e)
                    {
                        Debug.LogException(e);
                    }

                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

    }
}

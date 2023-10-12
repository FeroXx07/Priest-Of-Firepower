
#define AUTHENTICATION_CODE 
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
        //[SerializeField] int port = 12345;
        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        private CancellationTokenSource listenerToken;
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
        Socket serverTCP;
        Socket serverUDP;

        //Authentication 
        private string authenticationCode = "IM_VALID_USER_LOVE_ME";

        class ClientData
        {
            public int ID = -1;
            public string username = "";
            public ClientMetadata metaData;
            public ClientSate state;
            public Socket connectionTCP;
            public Socket connectionUDP;
            public CancellationTokenSource authenticationToken; //if disconnection request invoke cancellation token to shutdown all related processes
            public Thread gameThread;
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
            else if (instance != null)
                DestroyImmediate(instance);
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            clientManager = new ClientManager();
            //start server
            StartConnectionListenerTCP();
        }
        private void OnEnable()
        {

        }

        private void OnDisable()
        {

            StopListening();

            DisconnectAllClients();

            StopAllClientThreads();

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

                listenerToken = new CancellationTokenSource();
                listenerThread = new Thread(() => AuthenticationTCP(listenerToken.Token));
                listenerThread.Start();

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void AuthenticationTCP(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Waiting connection ... ");

                    Socket clientSocket = serverTCP.Accept();

                    //ping client to send confirmation code
                    byte[] msg = Encoding.ASCII.GetBytes("ok");
                    clientSocket.Send(msg);

                    //set a timeout to recive the verification code
                    clientSocket.ReceiveTimeout = 1000;

                    byte[] buffer = new byte[1024];
                    int bufferSize = clientSocket.Receive(buffer);

                    string code = Encoding.ASCII.GetString(buffer, 0, bufferSize);

                    //ping client to send username
                    byte[] confirmation = Encoding.ASCII.GetBytes("ok");
                    clientSocket.Send(msg);

                    buffer = new byte[1024];
                    bufferSize = clientSocket.Receive(buffer);

                    string username = Encoding.ASCII.GetString(buffer, 0, bufferSize);

                    //username validation
                    bool validUsername = true;
                    if (username.Length > 15 || username.Length == 0)
                        validUsername = false;

                    //before create add the client check if it an actual connection we want
                    if (code == authenticationCode && validUsername)
                    {
                        //add this accepted client into the client list
                        int clientID = CreateClient(clientSocket, username);

                        //call any related action to this event
                        OnClientAccepted?.Invoke(clientID);
                    }
                    else
                    {
                        //disconnect that connection as it is a non wanted connection
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                    }
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
            Debug.Log("Starting chat thread " + clientData.ID + " ...");

            while (!clientData.authenticationToken.IsCancellationRequested)
            {
                Socket clientSocket = clientData.connectionTCP;
                string data = null;
                try
                {
                    byte[] buffer = new byte[1024];

                    // Receive data from the client
                    int bufferSize = clientSocket.Receive(buffer);
                    data = Encoding.ASCII.GetString(buffer, 0, bufferSize);

                    if (!string.IsNullOrEmpty(data))
                    {
                        Debug.Log("message recived: " + data);
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

        int CreateClient(Socket clientSocket, string userName)
        {
            lock (clientList)
            {
                ClientData clientData = new ClientData();

                clientData.ID = clientManager.GetNextClientId();
                clientData.username = userName;

                clientData.connectionTCP = clientSocket;
                clientData.connectionTCP.ReceiveTimeout = 100;
                clientData.connectionTCP.SendTimeout = 100;

                IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                clientData.metaData.IP = clientEndPoint.Address;
                clientData.metaData.port = clientEndPoint.Port;

                clientData.authenticationToken = new CancellationTokenSource();

                clientList.Add(clientData);

                Debug.Log("Connected client Id: " + clientData.ID);


                //Create the handeler of the chat for that client
                //create a hole thread to recive important data from server-client
                //like game state, caharacter selection, map etc
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

                    Socket clientSocket = clientData.connectionTCP;

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
            if (listenerThread != null)
            {
                if (listenerThread.IsAlive)
                {// Signal the thread to exit 
                    listenerToken.Cancel();
                }
                listenerThread.Join();

                //make sure it is not alive
                if (listenerThread.IsAlive)
                {
                    listenerThread.Abort();
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

            // Wait for all client threads to really terminate.
            foreach (Thread t in clientThreads)
            {
                t.Join();
            }
            foreach (Thread t in clientThreads)
            {
                if (t.IsAlive)
                    t.Abort();
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

                    //int clientID = CreateClient(clientSocket, username);
                    ////add this accepted client into the client list
                    //OnClientAccepted?.Invoke(clientID);
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

                    Socket clientSocket = clientData.connectionTCP;

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

                    Socket clientSocket = clientData.connectionTCP;

                    // Send the message to other clients
                    byte[] data = Encoding.ASCII.GetBytes($"Client {senderID}: {message}");
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);

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

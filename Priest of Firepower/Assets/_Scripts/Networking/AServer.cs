
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
using ClientA;
using UnityEngine.Rendering;
using System.Xml.Serialization;

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

        private List<Thread> clientThreads = new List<Thread>();
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

        //Authentication 
        private string authenticationCode = "IM_VALID_USER_LOVE_ME";
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
            public Thread gameThread;
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
            if (authenticationThread != null)
            {
                if (authenticationThread.IsAlive)
                {// Signal the thread to exit 
                    authenticationToken.Cancel();
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
            foreach (Thread t in clientThreads)
            {
                if (t.IsAlive)
                    t.Join();
            }
            foreach (Thread t in clientThreads)
            {
                if (t.IsAlive)
                    t.Abort();
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
                client.connectionUDP.Send(data);
            }
        }

        public void SendToClient(int clientId, PacketType packetType, byte[] data)
        {
            foreach (ClientData client in clientList)
            {
                if (client.ID == clientId)
                {
                    client.connectionUDP.Send(data);

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
        void Authenticate(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Server: Waiting connection ... ");
                    Socket clientSocket = serverTCP.Accept();
                    //ping client to send confirmation code
                    byte[] msg = Encoding.ASCII.GetBytes("ok");
                    clientSocket.Send(msg);

                    //set a timeout to recive the verification code
                    clientSocket.ReceiveTimeout = 5000;

                    byte[] buffer = new byte[1024];
                    int bufferSize = clientSocket.Receive(buffer);

                    string code = Encoding.ASCII.GetString(buffer, 0, bufferSize);
                    Debug.Log("Server: "+code + " recieved");
                    //ping client to send username
                    byte[] confirmation = Encoding.ASCII.GetBytes("ok");
                    clientSocket.Send(msg);

                    buffer = new byte[1024];
                    bufferSize = clientSocket.Receive(buffer);

                    string username = Encoding.ASCII.GetString(buffer, 0, bufferSize);
                    Debug.Log("Server: recieved username: " + username);


                    //username validation
                    bool validUsername = true;
                    if (username.Length > 15 || username.Length == 0)
                        validUsername = false;

                    confirmation = Encoding.ASCII.GetBytes("ok");
                    clientSocket.Send(msg);

                    //before create add the client check if it an actual connection we want
                    if (code == authenticationCode && validUsername)
                    {
                        //ping client that is authenticated
                        confirmation = Encoding.ASCII.GetBytes("ok");
                        clientSocket.Send(msg);

                        //add this accepted client into the client list
                        int clientID = CreateClient(clientSocket, username);

                        //call any related action to this event
                        OnClientAccepted?.Invoke(clientID);

                        authenticationToken.Cancel();
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
                authenticationToken.Cancel();
                Debug.Log("Shutting down authentication process ...");
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
                //add a time out exeption for when the client disconnects or has lag or something
                clientData.connectionTCP.ReceiveTimeout = Timeout.Infinite;
                clientData.connectionTCP.SendTimeout = Timeout.Infinite;

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
        #endregion
    }
}

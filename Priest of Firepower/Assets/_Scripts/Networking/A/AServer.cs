using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace ServerA
{


    public class ClientManager
    {
        private int nextClientId = -1;

        public int GetNextClientId()
        {
            int clientId = nextClientId;
            nextClientId++;
            return clientId;
        }
    }
    public class AServer : MonoBehaviour
    {
        AServer Instance;
        IPEndPoint endPoint;
        [SerializeField] int port = 12345;
        // It's used to signal to an asynchronous operation that it should stop or be interrupted.
        // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
        // or any other condition that requires the operation to terminate prematurely.
        private CancellationTokenSource connectionCancellationTokenSource;
        private CancellationTokenSource chatCancellationTokenSource;
        private Task listenerTask;
        private Thread listenerThread;
        private Thread chatThread;

        ClientManager clientManager;

        List<ClientData> clientList = new List<ClientData>();

        //actions
        Action<Socket> OnClientAccepted;


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
            Instance = this;
        }
        private void Start()
        {
            clientManager = new ClientManager();
        
        }
        private void OnEnable()
        {

            OnClientAccepted += CreateClient;

            connectionCancellationTokenSource = new CancellationTokenSource();
            listenerThread = new Thread(() => ListenerTCP(connectionCancellationTokenSource.Token));
            listenerThread.Start();

            chatCancellationTokenSource = new CancellationTokenSource();
            chatThread = new Thread(() => HandleChat(chatCancellationTokenSource.Token));
            chatThread.Start();
        }

        private void OnDisable()
        {
            OnClientAccepted -= CreateClient;

            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Abort();
            }

            foreach(ClientData client in clientList)
            {
                RemoveClient(client);
            }
            StopListening();
            StopChat();
        }
        void ListenerTCP(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log("Starting server ...");
                //create listener tcp
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //create end point
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                endPoint = new IPEndPoint(IPAddress.Any, 12345);
                //bind to ip and port to listen to
                listener.Bind(endPoint);
                // Using Listen() method we create 
                // the Client list that will want
                // to connect to Server
                listener.Listen(4);

                Debug.Log("Server listening ...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Waiting connection ... ");

                    Socket clientSocket = listener.Accept();
                    IPEndPoint endPoint = (IPEndPoint)clientSocket.LocalEndPoint;
                    int localPort = endPoint.Port;
                    Debug.Log("Connection accepted -> " + localPort);

                    //add this accepted client into the client list
                    OnClientAccepted?.Invoke(clientSocket);
                    // Add some delay to avoid busy-waiting
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void DisconnectClient(Socket clientSocket)
        {

            // Disable the connection with this client
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }


        void HandleChat(CancellationToken cancellationToken)
        {
            Debug.Log("Starting chat thred ...");

            while (!cancellationToken.IsCancellationRequested)
            {
                List<ClientData> clients= new List<ClientData>(clientList);   
                // Check for incoming messages from all clients
                foreach (ClientData clientData in clients)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break; // Exit if cancellation is requested
                    }

                    Socket clientSocket = clientData.socket;

                    try
                    {
                        byte[] buffer = new byte[1024];
                        string data = null;

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
                            DisconnectClient(clientData.socket);
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
                Thread.Sleep(100);
            }
        }
         
        void CreateClient(Socket clientSocket)
        {
            ClientData clientData = new ClientData();

            clientData.socket = clientSocket;

            IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            clientData.metaData.IP = clientEndPoint.Address;
            clientData.metaData.port = clientEndPoint.Port;

            clientData.ID = clientManager.GetNextClientId();

            clientList.Add(clientData);

            Debug.Log("Connected client count: " + clientList.Count );

            //clientData.cancellationTokenSource = new CancellationTokenSource();
            //Thread clientHandeler = new Thread(() => HandleClient(clientSocket, clientData.cancellationTokenSource.Token));
            //clientHandeler.Start();
        }
        void RemoveClient(ClientData clientData)
        {
            // Remove the client from the list of connected clients
            clientList.Remove(clientData);

            // Optionally, perform cleanup or logging for the disconnected client
            Debug.Log($"Client {clientData.ID} disconnected.");

            // Cancel the client's cancellation token source
            clientData.cancellationTokenSource.Cancel();

            // Close the client's socket
            if (clientData.socket.Connected)
            {
                clientData.socket.Shutdown(SocketShutdown.Both);
            }
            clientData.socket.Close();
        }
        void BroadcastMessage(string message, int senderID)
        {
            List<ClientData> clients = new List<ClientData>(clientList);
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
                // Signal the thread to exit gracefully
                connectionCancellationTokenSource.Cancel();

                // Wait for the thread to finish before proceeding (optional)
                listenerThread.Join();
            }
        }

        void StopChat()
        {

            if (chatThread != null && chatThread.IsAlive)
            {
                // Signal the thread to exit gracefully
                chatCancellationTokenSource.Cancel();

                // Wait for the thread to finish before proceeding (optional)
                chatThread.Join();
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

        async Task ListenerTCPAsync(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log("Starting server...");
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //Port to be listening to listen to any ip address as we dont know the client ip
                endPoint = new IPEndPoint(IPAddress.Any, 12345);
                //bind() is used to bind a socket to a specific address and port,
                //making the socket listen for incoming connection requests on that address and port,
                //while connect() is used by a client to initiate a connection to a server.
                listener.Bind(endPoint);
                listener.Listen(4);
                while (!cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Waiting connection ... ");

                    Socket clientSocket = await listener.AcceptAsync();

                    Debug.Log("Connection accepted ->" + clientSocket.ToString());

                    byte[] buffer = new byte[1024];
                    string data = null;

                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break; // Exit if cancellation is requested
                        }
                        int bufferSize = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);

                        data += Encoding.ASCII.GetString(buffer, 0, bufferSize);

                        if (data.IndexOf("<EOF>") > -1)
                            break;
                    }

                    Debug.Log("Text received -> " + data);
                    byte[] message = Encoding.ASCII.GetBytes("Received data server A");

                    await clientSocket.SendAsync(message, SocketFlags.None);

                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }


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

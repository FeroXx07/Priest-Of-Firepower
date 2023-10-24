using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.SceneManagement;
public class Br_TCP_Server : MonoBehaviour
{

    private SynchronizationContext synchronizationContext;

    public int port = 5000;
    //string serverIpAddress = " 192.168.104.17";
    //bool createRoomRequested = false;
    Socket newSocket;
    [SerializeField]
    float waitTimeLimit;
    [SerializeField]
    float timer;
    Thread listenClients;
    bool serverActive = false;

    //general buffer to store user incoming data
    byte[] clientData = new Byte[256];
    string roomName = "";

    private static Br_TCP_Server tcpServerInstance;

    [SerializeField]
    List<Br_ServerInfoPackaging.ClientInfo> connectedClients = new List<Br_ServerInfoPackaging.ClientInfo>();
    [SerializeField]
    List<string> connectedClientsStr = new List<string>();

    //this should be a single instance with the device info as if it was a client
    Br_ServerInfoPackaging.ClientInfo serverClient = new Br_ServerInfoPackaging.ClientInfo();
    private void Awake()
    {

        // Check if an instance already exists
        if (tcpServerInstance != null && tcpServerInstance != this)
        {
            // If an instance already exists, destroy this duplicate GameObject
            Destroy(gameObject);
            return;
        }

        // Set this instance as the singleton
        tcpServerInstance = this;

        // Don't destroy this GameObject when loading new scenes
        DontDestroyOnLoad(gameObject);

        Application.runInBackground = true;
    }

    private void OnEnable()
    {
        Br_ICreateRoomUI.OnCreateRoom += CreateRoomRequest;
        Br_IServer.OnSendMessageToClient += SendMessageToClient;
    }

    

    void Start()
    {
        if (Screen.fullScreen)
            Screen.fullScreen = false;
    }
    

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else
        {
            if (listenClients != null && listenClients.IsAlive)
            {
                print("TCP: Waiting has exceeded time limit. Aborting...");
                AbortListenForClients();
            }
        }
    }

    public void CreateRoomRequest()
    {
        if (!enabled) return;

        timer = waitTimeLimit;
        //createRoomRequested = true;

        synchronizationContext = SynchronizationContext.Current;
        serverActive = !serverActive;

        //go to hub
        SceneManager.LoadScene("BHub");

        if (serverActive)
        {
            listenClients = new Thread(ListenForClients);
            listenClients.Start();
        }
        else
        {
            AbortListenForClients();
        }
        
    }



    void ListenForClients()
    {
        

        
        try
        {
            //  "create" server
            print("TCP: Starting Server.");
            //Create and bind socket so that nobody can use it until unbinding
            newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            newSocket.Bind(endPoint);
            newSocket.Listen(10);
            print("TCP: Waiting to receive datagrams from client...");

            serverClient.socket = newSocket;
            serverClient.username = "Server";

            //blocks the program until receiving a client connection attempt
            var client = newSocket.BeginAccept(new AsyncCallback(AcceptClient), null);
            

        }
        catch (System.Exception e)
        {
            Debug.Log("TCP: Connection failed. Error:" + e);
        }


    }

    void AcceptClient(IAsyncResult ar)
    {
        try
        {
            Socket clientSocket = newSocket.EndAccept(ar);
            EndPoint clientEp = clientSocket.RemoteEndPoint;
            
            //block program until data from user is received
            var receiveResult = clientSocket.BeginReceive(clientData, 0, clientData.Length, SocketFlags.None, new AsyncCallback(ReceiveMessage), clientSocket);

            //loop to check for client's connection attempts
            newSocket.BeginAccept(new AsyncCallback(AcceptClient), null);
            

        }
        catch (System.Exception e)
        {
            Debug.Log("TCP: Accep Cliend Failed. Error:" + e);
        }
    }

    void ReceiveMessage(IAsyncResult ar)
    {
        try
        {
            Socket clientSocket = (Socket)ar.AsyncState;
            EndPoint clientEp = clientSocket.RemoteEndPoint;
            Br_ServerInfoPackaging.ClientInfo refClient = new Br_ServerInfoPackaging.ClientInfo();
            ref Br_ServerInfoPackaging.ClientInfo incomingClient = ref refClient;

            if (serverActive)
            {
                int bytesRead = clientSocket.EndReceive(ar);
                byte[] receivedData = new byte[bytesRead];

                if (bytesRead > 0)
                {
                    bool clientAlreadyConnected = false;
                    int clientIndex = 0;
                    //check if client is new
                    for (int i = 0; i < connectedClients.Count; i++)
                    {
                        if (connectedClients[i].socket == clientSocket)
                        {
                            incomingClient = connectedClients[i];
                            clientAlreadyConnected = true;
                            clientIndex = i;
                            break;
                        }
                    }
                    
                    if (!clientAlreadyConnected)
                    {
                        //create new client data contaniner
                        Br_ServerInfoPackaging.ClientInfo newClient = new Br_ServerInfoPackaging.ClientInfo();
                        newClient.socket = clientSocket;
                        incomingClient = newClient;

                        connectedClients.Add(newClient);
                        connectedClientsStr.Add(newClient.socket.RemoteEndPoint.ToString());
                        clientIndex = connectedClients.Count - 1;

                        //send confirmation message that client entered server
                        Br_ServerInfoPackaging.ChatMessage welcomeMessage = new Br_ServerInfoPackaging.ChatMessage();
                        welcomeMessage.user = serverClient;
                        welcomeMessage.message = "Welcome to " + roomName;
                        byte[] responseBytes = PackData(welcomeMessage, Br_ServerInfoPackaging.InfoPackageType.CHAT_MESSAGE);

                        //string response = "Welcome to " + roomName;
                        //print("TCP: Sending response: " + response);
                        //byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);

                        clientSocket.Send(responseBytes);
                    }
                    


                    //copy data from general user data buffer [clientData] to single data buffer [receivedData] to use
                    Array.Copy(clientData, receivedData, bytesRead);

                    //post function to be executed in main thread
                    synchronizationContext.Post(_ => HandleReceivedData(clientIndex, receivedData), null);                    

                    //keep receiving data from the accepted client
                    clientSocket.BeginReceive(clientData, 0, clientData.Length, SocketFlags.None, new AsyncCallback(ReceiveMessage), clientSocket);

                }

            }
            else
            {
                print("TCP: Client disconnected: " + clientSocket.RemoteEndPoint.ToString());
                clientSocket.Close();

            }

        }
        catch (System.Exception e)
        {
            Debug.Log("TCP: Receive Message Failed. Error:" + e);
        }
    }



    void AbortListenForClients()
    {
        if (listenClients != null)
            listenClients.Abort();
        if (newSocket != null && newSocket.IsBound) newSocket.Close();
        serverActive = false;

    }
    private void OnDisable()
    {
        if (listenClients != null)
            listenClients.Abort();

        if (newSocket != null && newSocket.IsBound) newSocket.Close();
        serverActive = false;
    }


    //Executed in main thread

    //Multiuse that acts when data is received
    void HandleReceivedData(int clientIndex, byte[] data)
    {
        //string message = System.Text.Encoding.UTF8.GetString(data);
        //print("Received Data: " + message);
        //Br_IServer.OnReceiveMessageFromClient?.Invoke(message);

        ////redistribute to other clients
        //RedistributeMessageFromClient(sender, message);

        
        //Unpack Data

        Br_ServerInfoPackaging.InfoPackage infoPackage = UnpackData<Br_ServerInfoPackaging.InfoPackage>(data);

        switch (infoPackage.infoType)
        {
            case Br_ServerInfoPackaging.InfoPackageType.SERVER_INFO:
                //Br_ServerInfoPackaging.ServerInfo serverInfo = Br_ServerInfoPackaging.DeserializeData<Br_ServerInfoPackaging.ServerInfo>(data);
                break;
           
            case Br_ServerInfoPackaging.InfoPackageType.CLIENT_INFO:
                Br_ServerInfoPackaging.ClientInfo clientInfo = Br_ServerInfoPackaging.DeserializeData<Br_ServerInfoPackaging.ClientInfo>(data);
                HandleClientInfo( clientInfo);
                break;
            
            case Br_ServerInfoPackaging.InfoPackageType.CHAT_MESSAGE:
                Br_ServerInfoPackaging.ChatMessage chatMessage = Br_ServerInfoPackaging.DeserializeData<Br_ServerInfoPackaging.ChatMessage>(data);
                HandleChatMessages(chatMessage);
                break;
            default:
                break;
        }

        //Read Header

        //Work on the data depending on its type
    }

    void HandleClientInfo(int clientIndex, Br_ServerInfoPackaging.ClientInfo clientInfo)
    {

    }

    void HandleChatMessages(Br_ServerInfoPackaging.ChatMessage chatMessage)
    {
        print("Received Data: " + chatMessage.message);
        Br_IServer.OnReceiveMessageFromClient?.Invoke(chatMessage);

        //redistribute to other clients
        RedistributeMessageFromClient(chatMessage.user.socket, chatMessage.message);
    }



    private void SendMessageToClient(string message)
    {
        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(message);

        for (int i = 0; i < connectedClients.Count; i++)
        {
            print("sending message [" + message + "] to: " + connectedClients[i]);
            connectedClients[i].socket.Send(responseBytes);

        }
    }

    void RedistributeMessageFromClient(Socket originalSender, string message)
    {
        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(message);

        for (int i = 0; i < connectedClients.Count; i++)
        {
            if (connectedClients[i].socket != originalSender)
            {
                print("sending message [" + message + "] to: " + connectedClients[i]);
                connectedClients[i].socket.Send(responseBytes);
            }
        }
    }

    byte[] PackData<T>(T data, Br_ServerInfoPackaging.InfoPackageType dataType)
    {
        return Br_ServerInfoPackaging.PackData(data, dataType);
    }

    private Br_ServerInfoPackaging.InfoPackage UnpackData<T>(byte[] data)
    {
        return Br_ServerInfoPackaging.UnpackData<Br_ServerInfoPackaging.InfoPackage>(data);

    }

    public void SetRoomName(string roomName)
    {
        this.roomName = roomName;
    }

    public string GetRoomName()
    {
        return roomName;
    }

}

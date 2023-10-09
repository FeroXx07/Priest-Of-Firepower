using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.SceneManagement;
public class Br_UDP_Server : MonoBehaviour
{

    private SynchronizationContext synchronizationContext;

    public int port = 5000;
    Socket newSocket;

    [SerializeField]
    float waitTimeLimit;

    [SerializeField]
    float timer;

    Thread listenClients;
    bool serverActive = false;

    string roomName = "";

    private static Br_UDP_Server udpServerInstance;
    private void Awake()
    {
        // Check if an instance already exists
        if (udpServerInstance != null && udpServerInstance != this)
        {
            // If an instance already exists, destroy this duplicate GameObject
            Destroy(gameObject);
            return;
        }

        // Set this instance as the singleton
        udpServerInstance = this;

        // Don't destroy this GameObject when loading new scenes
        DontDestroyOnLoad(gameObject);

        Application.runInBackground = true;
        Br_ICreateRoomUI.OnCreateRoom += CreateRoomRequest;
        Br_IServer.OnReceiveMessageFromClient += ReceiveMessageFromClient;
        Br_IServer.OnSendMessageToClient += SendMessageToClient;
    }

    // Start is called before the first frame update
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
                print("UDP: Waiting has exceeded time limit. Aborting...");
                AbortListenForClients();
            }
        }
    }

    public void CreateRoomRequest()
    {
        if (!enabled) return;
        timer = waitTimeLimit;


        print("UDP: Starting to listen for clients...");

        synchronizationContext = SynchronizationContext.Current;
        serverActive = !serverActive;

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
        print("UDP: Starting Server.");
        //Create and bind socket so that nobody can use it until unbinding
        newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
        newSocket.Bind(endPoint);

        print("UDP: Waiting to receive datagrams from client...");

        while (serverActive)
        {
            try
            {

                byte[] msg = new Byte[256];
                EndPoint senderRemote = new IPEndPoint(IPAddress.Any, 0);

                //this blocks the program until receiving an answer from a client
                newSocket.ReceiveFrom(msg, msg.Length, SocketFlags.None, ref senderRemote);

                string response = "Welcome to " + roomName;
                print("TCP: Sending response: " + response);

                byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                newSocket.SendTo(responseBytes, senderRemote);

                if (serverActive)
                {
                    //post function to be executed in main thread
                    synchronizationContext.Post(_ => HandleReceivedData(msg), null);
                }

               

                print("UDP: Message Received");

            }
            catch (System.Exception e)
            {
                Debug.Log("UDP: Connection failed.. trying again. Error:" + e);
            }
        }

        print("UDP: Closing Server.");
        newSocket.Close();


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
    void HandleReceivedData(byte[] msg)
    {

        InvokeCreateMessage(msg);
    }

    //Spawn floating text action
    void InvokeCreateMessage(byte[] msg)
    {
        //decode data
        string message = System.Text.Encoding.UTF8.GetString(msg);
        Br_IServer.OnSendMessageToClient?.Invoke(message);

    }

    void ReceiveMessageFromClient(string message)
    {

    }

    void SendMessageToClient(string message)
    {

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

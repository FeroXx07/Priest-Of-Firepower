using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Net.Sockets;
using System.Threading;
using TMPro;
public class Br_UDP_Server : MonoBehaviour
{

    private SynchronizationContext synchronizationContext;

    [SerializeField]
    TextMeshProUGUI serverName;

    public int port = 5000;
    string serverIpAddress = " 192.168.104.17";
    bool createRoomRequested = false;
    Socket newSocket;
    [SerializeField]
    float waitTimeLimit;
    [SerializeField]
    float timer;
    Thread listenClients;
    bool serverActive = false;

    // Start is called before the first frame update
    void Start()
    {
        if (Screen.fullScreen)
            Screen.fullScreen = false;
    }
    private void Awake()
    {
        Application.runInBackground = true;
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
        createRoomRequested = true;


        print("UDP: Starting to listen for clients...");

        synchronizationContext = SynchronizationContext.Current;
        serverActive = !serverActive;

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

        //newSocket.Listen(10); --> for tcp
        print("UDP: Waiting to receive datagrams from client...");

        //var client = newSocket.Accept(); //blocks the thread until a client connects to the socket // for tcp
        //EndPoint senderRemote = client.RemoteEndPoint; //Retrieves the endpoint info (IP address and port of the client)
        //newSocket.Bind(senderRemote); unnecessary

        while (serverActive)
        {
            try
            {

                byte[] msg = new Byte[256];
                EndPoint senderRemote = new IPEndPoint(IPAddress.Any, 0);

                //this blocks the program until receiving an answer from a client
                newSocket.ReceiveFrom(msg, msg.Length, SocketFlags.None, ref senderRemote);

                string response = "Welcome to " + serverName.text;
                print("TCP: Sending response: " + response);

                byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                newSocket.SendTo(responseBytes, senderRemote);

                if (serverActive)
                {
                    //post function to be executed in main thread
                    synchronizationContext.Post(_ => InvokeCreateMessage(msg), null);
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
    void InvokeCreateMessage(byte[] msg)
    {
        //decode data
        string message = System.Text.Encoding.UTF8.GetString(msg);
        Br_IServer.OnCreateMessage.Invoke(message);

    }


}

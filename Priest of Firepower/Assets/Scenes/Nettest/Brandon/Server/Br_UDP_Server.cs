using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Net.Sockets;
using System.Threading;

public class Br_UDP_Server : MonoBehaviour
{
    public static event System.Action<byte[]> OnCreateMessage;

    [SerializeField]
    GameObject floatingText;
    public int port = 5000;
    string serverIpAddress = " 192.168.104.17";
    bool createRoomRequested = false;
    Socket newSocket;
    [SerializeField]
    int listenCalls;
    [SerializeField]
    float waitTimeLimit;
    [SerializeField]
    float timer;
    Thread listenClients;
    // Start is called before the first frame update
    void Start()
    {
        OnCreateMessage += CreateMessage;
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
                AbortListenForClients();
            }
        }
    }

    public void CreateRoomRequest()
    {
        timer = waitTimeLimit;
        createRoomRequested = true;

        

        print("Starting to listen for clients...");
        listenClients = new Thread(ListenForClients);
        listenClients.Start();

        GameObject text = Instantiate(floatingText);
        text.GetComponent<TMPro.TextMeshPro>().text = "test message";
    }



    void ListenForClients()
    {
        try
        {
            
            //Create and bind socket so that nobody can use it until unbinding
            newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            newSocket.Bind(endPoint);

            //newSocket.Listen(10); --> for tcp
            print("Waiting to receive datagrams from client...");

            //var client = newSocket.Accept(); //blocks the thread until a client connects to the socket // for tcp
            //EndPoint senderRemote = client.RemoteEndPoint; //Retrieves the endpoint info (IP address and port of the client)
            //newSocket.Bind(senderRemote); unnecessary
            listenCalls++;

            byte[] msg = new Byte[256];
            EndPoint senderRemote = new IPEndPoint(IPAddress.Any, 0);

            //this blocks the program until receiving an answer from a client
            newSocket.ReceiveFrom(msg, msg.Length, SocketFlags.None, ref senderRemote);

            InvokeCreateMessage(msg);

            listenCalls--;
            print("Waiting has stopped.");
            newSocket.Close();
            
        }
        catch (System.Exception e)
        {
            Debug.Log("Connection failed.. trying again. Error:" + e);
        }
    }

    void AbortListenForClients()
    {
        print("Waiting has exceeded time limit. Aborting...");
        if (listenClients != null)
            listenClients.Abort();
        if (newSocket != null && newSocket.IsBound) newSocket.Close();

    }
    private void OnDisable()
    {
        if (listenClients != null)
            listenClients.Abort();

        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }

    void InvokeCreateMessage(byte[] msg)
    {
        OnCreateMessage?.Invoke(msg);
    }

    void CreateMessage(byte[] msg)
    {
        string message = System.Text.Encoding.ASCII.GetString(msg);
        GameObject text = Instantiate(floatingText);
        text.GetComponent<TMPro.TextMeshPro>().text = message;

    }
}

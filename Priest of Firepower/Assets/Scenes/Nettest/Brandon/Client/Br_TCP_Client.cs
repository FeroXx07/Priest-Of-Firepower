using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class Br_TCP_Client : MonoBehaviour
{
    private SynchronizationContext synchronizationContext;

    public int serverPort;


    [SerializeField]
    float waitTimeLimit;
    [SerializeField]
    float timer;


    Thread connectToServerThread;
    Socket newSocket;

    Thread responseThread;

    string username;
    string serverIp;
    private static Br_TCP_Client tcpClientInstance;
    //bool connectedToServer = false;


    private void Awake()
    {
        // Check if an instance already exists
        if (tcpClientInstance != null && tcpClientInstance != this)
        {
            // If an instance already exists, destroy this duplicate GameObject
            Destroy(gameObject);
            return;
        }

        // Set this instance as the singleton
        tcpClientInstance = this;

        // Don't destroy this GameObject when loading new scenes
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;

    }

    private void OnEnable()
    {
        Br_IJoinRoomUI.OnJoinRoom += JoinRoom;
        Br_IServer.OnSendMessageToServer += SendMessageToServer;
    }

    void Start()
    {
        if (Screen.fullScreen)
            Screen.fullScreen = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else
        {
            if (connectToServerThread != null && connectToServerThread.IsAlive)
            {
                AbortConnectToServer();
            }
        }
    }

    public void JoinRoom()
    {
        if (!enabled) return;

        try
        {
            print("TCP: conectig to ip: " + this.serverIp);
            string serverIp = this.serverIp.Remove(this.serverIp.Length - 1);

            //Create and bind socket so that nobody can use it until unbinding
            newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            string message = username + " joined.";


            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

            IPAddress ipAddress;
            if (!IPAddress.TryParse(serverIp, out ipAddress))
            {
                // Handle invalid IP address input
                print("TCP: Invalid IP address: " + serverIp);
                return;
            }
            print("TCP: ipAddress: " + ipAddress);

            IPEndPoint serverEp = new IPEndPoint(ipAddress, serverPort);
            newSocket.Connect(serverEp);
            print("TCP: Connected to server at: " + serverEp);

            SceneManager.LoadScene("BHub");

            newSocket.Send(messageBytes);

            synchronizationContext = SynchronizationContext.Current;
            responseThread = new Thread(WaitForServerAnswer);
            responseThread.Start();



        }
        catch (System.Exception e)
        {
            Debug.Log("TCP: Connection failed.. trying again. Error: " + e);
        }
    }

    void AbortConnectToServer()
    {
        print("TCP: Waiting has exceeded time limit. Aborting...");
        if (connectToServerThread != null)
            connectToServerThread.Abort();
        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }
    private void OnDisable()
    {
        if (connectToServerThread != null)
            connectToServerThread.Abort();

        if (responseThread != null)
            responseThread.Abort();

        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }

    void WaitForServerAnswer()
    {
        byte[] serverAnswerData = new byte[256];
        int responseByteCount = newSocket.Receive(serverAnswerData);
        if (responseByteCount > 0)
        {
            //connectedToServer = true;

            //Announce connection to server
            synchronizationContext.Post(_ => InvokeReceiveMessageFromServer(serverAnswerData), null);
        }
        WaitForServerAnswer();
    }

    private void InvokeReceiveMessageFromServer(byte[] msg)
    {
        string message = System.Text.Encoding.UTF8.GetString(msg);
        print("server Answer: " + message);
        Br_IServer.OnReceiveMessageFromServer?.Invoke(message);
    }


    void SendMessageToServer(string message)
    {
        if (!this.enabled) return;
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
        newSocket.Send(messageBytes);

    }

    public void SetUsername(string username)
    {
        this.username = username;
    }

    public void SetServerIp(string ip)
    {
        this.serverIp = ip;
    }

    public string GetUsername()
    {
        return username;
    }
}

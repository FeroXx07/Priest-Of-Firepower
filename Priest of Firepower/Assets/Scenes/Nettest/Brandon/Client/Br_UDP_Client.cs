using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using TMPro;
using UnityEngine.SceneManagement;

public class Br_UDP_Client : MonoBehaviour
{
    private SynchronizationContext synchronizationContext;

    public int serverPort;


    [SerializeField]
    float waitTimeLimit;
    [SerializeField]
    float timer;


    Thread connectToServerThread;
    Thread recieveResponseThread;
    Socket newSocket;
    EndPoint serverEndpoint;

    private static Br_UDP_Client udpClientInstance;
    string username;
    string serverIp;
    bool connectedToServer = false;
    private void Awake()
    {
        // Check if client already exists
        if (udpClientInstance != null && udpClientInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        udpClientInstance = this;

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
            print("UDP: conectig to ip: " + this.serverIp);
            string serverIp = this.serverIp.Remove(this.serverIp.Length - 1);

            //Create and bind socket so that nobody can use it until unbinding
            newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);

            string message = username + " joined.";
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

            IPAddress ipAddress;
            if (!IPAddress.TryParse(serverIp, out ipAddress))
            {
                // Handle invalid IP address input
                print("UDP: Invalid IP address: " + serverIp);
                return;
            }
            print("UDP: ipAddress: " + ipAddress);

            serverEndpoint = new IPEndPoint(ipAddress, serverPort);

            SceneManager.LoadScene("BHub");


            newSocket.SendTo(messageBytes, serverEndpoint);


            synchronizationContext = SynchronizationContext.Current;
            recieveResponseThread = new Thread(WaitForServerAnswer);
            recieveResponseThread.Start();


        }
        catch (System.Exception e)
        {
            Debug.Log("UDP: Connection failed.. trying again. Error: " + e);
            print("error");
        }
    }

    void SendMessageToServer(string message)
    {
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
        newSocket.SendTo(messageBytes, serverEndpoint);

    }

    //wait for server response to our attempt at connecting
    void WaitForServerAnswer()
    {
        byte[] serverAnswerData = new byte[256];
        int responseByteCount = newSocket.ReceiveFrom(serverAnswerData, serverAnswerData.Length, SocketFlags.None, ref serverEndpoint);
        if (responseByteCount > 0)
        {
            connectedToServer = true;

            //Announce connection to server
            synchronizationContext.Post(_ => InvokeReceiveMessageFromServer(serverAnswerData), null);

            //keep listening for data
            KeepListeningToServer();
        }
    }

    //loop that listens for messages from the server always
    void KeepListeningToServer()
    {
        if (connectedToServer)
        {
            byte[] serverData = new byte[256];
            print("waiting to receive response");

            int responseByteCount = newSocket.ReceiveFrom(serverData, serverData.Length, SocketFlags.None, ref serverEndpoint);
            if (responseByteCount > 0)
            {
                //synchronizationContext.Post(_ => InvokeCreateResponse(response), null);
                synchronizationContext.Post(_ => InvokeReceiveMessageFromServer(serverData), null);

            }
            KeepListeningToServer();
        }
        //newSocket.Close();
    }

    void InvokeReceiveMessageFromServer(byte[] msg)
    {
        string message = System.Text.Encoding.UTF8.GetString(msg);
        print("server Answer: " + message);
        Br_IServer.OnReceiveMessageFromServer?.Invoke(message);

    }

    void AbortConnectToServer()
    {
        print("UDP: Waiting has exceeded time limit. Aborting...");
        if (connectToServerThread != null)
            connectToServerThread.Abort();
        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }
    private void OnDisable()
    {
        if (connectToServerThread != null)
            connectToServerThread.Abort();
        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }

    //todo: quitar unused connectToServer thread;

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

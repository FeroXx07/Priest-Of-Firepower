using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using TMPro;

public class Br_TCP_Client : MonoBehaviour
{
    private SynchronizationContext synchronizationContext;

    [SerializeField]
    TextMeshProUGUI inputFieldText;
    [SerializeField]
    TextMeshProUGUI inputFieldMessage;
    public int serverPort;


    [SerializeField]
    float waitTimeLimit;
    [SerializeField]
    float timer;


    Thread connectToServer;
    Socket newSocket;

    Thread responseThread;
    // Start is called before the first frame update
    void Start()
    {
        if (Screen.fullScreen)
            Screen.fullScreen = false;
    }
    private void Awake()
    {
        Application.runInBackground = true;
        DontDestroyOnLoad(transform.gameObject);
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
            if (connectToServer != null && connectToServer.IsAlive)
            {
                AbortConnectToServer();
            }
        }
    }

    public void ConnectToServer()
    {
        if (!enabled) return;

        try
        {
            print("TCP: conectig to ip: " + inputFieldText.text);
            string serverIp = inputFieldText.text.Remove(inputFieldText.text.Length - 1);

            //Create and bind socket so that nobody can use it until unbinding
            newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(inputFieldMessage.text);

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

            newSocket.Send(messageBytes);

            synchronizationContext = SynchronizationContext.Current;
            responseThread = new Thread(HandleServerResponse);
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
        if (connectToServer != null)
            connectToServer.Abort();
        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }
    private void OnDisable()
    {
        if (connectToServer != null)
            connectToServer.Abort();

        if (responseThread != null)
            responseThread.Abort();

        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }

    void HandleServerResponse()
    {
        byte[] response = new byte[256];
        int responseByteCount = newSocket.Receive(response);
        if (responseByteCount > 0)
        {
            synchronizationContext.Post(_ => InvokeCreateResponse(response), null);
        }
        newSocket.Close();
    }



    void InvokeCreateResponse(byte[] msg)
    {
        //decode data
        string message = System.Text.Encoding.UTF8.GetString(msg);
        Br_IServer.OnCreateResponse?.Invoke(message);

    }
}

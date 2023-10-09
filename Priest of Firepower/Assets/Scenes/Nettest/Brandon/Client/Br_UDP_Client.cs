using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using TMPro;

public class Br_UDP_Client : MonoBehaviour
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
    Thread recieveResponseThread;
    Socket newSocket;
    EndPoint serverEndpoint;
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
            print("UDP: conectig to ip: " + inputFieldText.text);
            string serverIp = inputFieldText.text.Remove(inputFieldText.text.Length - 1);

            //Create and bind socket so that nobody can use it until unbinding
            newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);


            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(inputFieldMessage.text);

            IPAddress ipAddress;
            if (!IPAddress.TryParse(serverIp, out ipAddress))
            {
                // Handle invalid IP address input
                print("UDP: Invalid IP address: " + serverIp);
                return;
            }
            print("UDP: ipAddress: " + ipAddress);

            serverEndpoint = new IPEndPoint(ipAddress, serverPort);

            newSocket.SendTo(messageBytes, serverEndpoint);

            synchronizationContext = SynchronizationContext.Current;
            recieveResponseThread = new Thread(HandleServerResponse);
            recieveResponseThread.Start();


        }
        catch (System.Exception e)
        {
            Debug.Log("UDP: Connection failed.. trying again. Error: " + e);
        }
    }

    void HandleServerResponse()
    {
        byte[] response = new byte[256];
        int responseByteCount = newSocket.ReceiveFrom(response, response.Length, SocketFlags.None, ref serverEndpoint);
        if (responseByteCount > 0)
        {
            synchronizationContext.Post(_ => InvokeCreateResponse(response), null);
            string message = System.Text.Encoding.UTF8.GetString(response);
            print(message);
        }
        newSocket.Close();
    }

    void InvokeCreateResponse(byte[] msg)
    {
        //decode data
        string message = System.Text.Encoding.UTF8.GetString(msg);
        Br_IServer.OnCreateResponse?.Invoke(message);

    }

    void AbortConnectToServer()
    {
        print("UDP: Waiting has exceeded time limit. Aborting...");
        if (connectToServer != null)
            connectToServer.Abort();
        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }
    private void OnDisable()
    {
        if (connectToServer != null)
            connectToServer.Abort();
        if (newSocket != null && newSocket.IsBound) newSocket.Close();
    }

    //todo: quitar unused connectToServer thread;

}

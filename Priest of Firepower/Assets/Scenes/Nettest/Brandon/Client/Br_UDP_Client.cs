using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using TMPro;

public class Br_UDP_Client : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI inputFieldText;
    public int serverPort;


    [SerializeField]
    float waitTimeLimit;
    [SerializeField]
    float timer;


    Thread connectToServer;
    Socket newSocket;
    // Start is called before the first frame update
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
            if (connectToServer != null && connectToServer.IsAlive)
            {
                AbortConnectToServer();
            }
        }
    }

    public void ConnectToServer()
    {
        try
        {
            print("conectig to ip: " + inputFieldText.text);
            string serverIp = inputFieldText.text;

            //Create and bind socket so that nobody can use it until unbinding
            newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);


            byte[] messageBytes = System.Text.Encoding.ASCII.GetBytes("ola ke ase");

            serverIp = "127.0.0.1";
            IPAddress ipAddress;
            if (!IPAddress.TryParse(serverIp, out ipAddress))
            {
                // Handle invalid IP address input
                print("Invalid IP address: " + serverIp);
                return;
            }
            print("ipAddress: " + ipAddress);

            IPEndPoint ipep = new IPEndPoint(ipAddress, serverPort);

            newSocket.SendTo(messageBytes, ipep);
            newSocket.Close();

        }
        catch (System.Exception e)
        {
            Debug.Log("Connection failed.. trying again. Error: " + e);
        }
    }

    void AbortConnectToServer()
    {
        print("Waiting has exceeded time limit. Aborting...");
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
}

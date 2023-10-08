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
    [SerializeField]
    TextMeshProUGUI inputFieldMessage;
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

            IPEndPoint ipep = new IPEndPoint(ipAddress, serverPort);

            newSocket.SendTo(messageBytes, ipep);
            newSocket.Close();

        }
        catch (System.Exception e)
        {
            Debug.Log("UDP: Connection failed.. trying again. Error: " + e);
        }
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


}

using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

public class AServer : MonoBehaviour
{
    AServer Instance;
    IPEndPoint endPoint;
    [SerializeField]int port  = 12345;
    // It's used to signal to an asynchronous operation that it should stop or be interrupted.
    // Cancellation tokens are particularly useful when you want to stop an ongoing operation due to user input, a timeout,
    // or any other condition that requires the operation to terminate prematurely.
    private CancellationTokenSource tokenSource;
    private Task listenerTask;
    private void Awake()
    {
        Instance = this;
    }
    private async void OnEnable()
    { 
        tokenSource = new CancellationTokenSource();
        listenerTask = ListenerTCPAsync(tokenSource.Token);
        await listenerTask;
        //listen = new Thread(()=> ListenerTCP()); 
        //listen.Start(); 
    }
    async Task ListenerTCPAsync(CancellationToken cancellationToken)
    {
        try
        {
            Debug.Log("Starting server...");
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            endPoint = new IPEndPoint(IPAddress.Any, port);
            listener.Bind(endPoint);
            listener.Listen(4);
            //try
            //{
            //    //bind the socket to the port and if not aviable find another one and send response to the client

            //    Debug.Log("Server listening on port " + port);
            //    Socket clientSocket = await Task.Run(() => listener.Accept(), cancellationToken);
            //    // Send the server's port to the client
            //    byte[] portMessage = Encoding.ASCII.GetBytes("ServerPort:" + port);
            //    clientSocket.Send(portMessage);
            //}
            //catch (SocketException se)
            //{
            //    if (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
            //    {
            //        // Handle the case where the port is already in use
            //        Debug.LogError("Port " + port + " is already in use.");
            //    }
            //    else
            //    {
            //        Debug.LogError("SocketException: " + se.SocketErrorCode);
            //    }
            //    return; // Exit the server or take appropriate action
            //}

            while (!cancellationToken.IsCancellationRequested)
            {
                Debug.Log("Waiting connection ... ");

                Socket clientSocket = await Task.Run(() => listener.Accept(), cancellationToken);

                byte[] buffer = new byte[1024];
                string data = null;

                while (true)
                {
                    int bufferSize = await Task.Run(() => clientSocket.Receive(buffer), cancellationToken);

                    data += Encoding.ASCII.GetString(buffer, 0, bufferSize);

                    if (data.IndexOf("<EOF>") > -1)
                        break;
                }

                Debug.Log("Text received -> " + data);
                byte[] message = Encoding.ASCII.GetBytes("Received data server A");

                await Task.Run(() => clientSocket.Send(message), cancellationToken);

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    async void Bind()
    {

    }
    void ListenerTCP()
    {
        try
        {
            Debug.Log("Starting server...");
            //create listener tcp
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //create end point

            //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
            //In this case the operating system (TCP/IP stack) assigns a free port number for you.
            endPoint = new IPEndPoint(IPAddress.Any, 0);
            //bind to ip and port to listen to
            listener.Bind(endPoint);
            // Using Listen() method we create 
            // the Client list that will want
            // to connect to Server
            listener.Listen(4);
            while (true)
            {
                Console.WriteLine("Waiting connection ... ");

                Socket clientSocket = listener.Accept();

                byte[] buffer = new byte[1024];
                string data = null;

                while(true)
                {
                    //get the buffer size
                    int bufferSize = clientSocket.Receive(buffer);

                    data += Encoding.ASCII.GetString(buffer,0, bufferSize);

                    //check end of the socket
                    if (data.IndexOf("<EOF>") > -1)
                        break;
                }
                Debug.Log("Text received -> "+ data);
                byte[] message = Encoding.ASCII.GetBytes("Recived data server A");

                // Send a message to Client 
                // using Send() method
                clientSocket.Send(message);

                // Close client Socket using the
                // Close() method. After closing,
                // we can use the closed Socket 
                // for a new Client Connection
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
      
    }

    void ListenerUDP()
    {
        try
        {
            //create listener udp
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            endPoint = new IPEndPoint(IPAddress.Any, 0);
            //bind to ip and port to listen to
            listener.Bind(endPoint);

            while (true)
            {
                try
                {
                    listener.Listen(1);
                    Debug.Log("waiting for clients....");
                    Socket client = listener.Accept();
                    IPEndPoint clientIP = (IPEndPoint)client.RemoteEndPoint;


                }
                catch (SocketException e)
                {
                    Debug.LogException(e);
                }

            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}

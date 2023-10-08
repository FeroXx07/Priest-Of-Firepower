using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using static ServerA.AServer;

namespace ClientA
{
    public class AClient : MonoBehaviour
    {
        public static AClient Instance { get; set; }
        IPEndPoint endPoint;
        [SerializeField]public string IPaddress;

        private CancellationTokenSource sendToken;
        private Task senderTask;

        private Thread senderThread;

        private CancellationTokenSource listenerToken;
        private Thread listenServerThread;

        private Socket connection;

        public Action<string> OnMessageRecived;

        public void Connect()
        {
            sendToken = new CancellationTokenSource();           
            senderThread = new Thread(()=> ConnectTCP(sendToken.Token));
            senderThread.Start();

            
        }

        public void SendMessageUI(string text)
        {
            Thread message = new Thread(() => SendMessageText(text));
            message.Start();
        }

        public void SetIpAddress(string ip)
        {
            IPaddress = ip;
        }

        private void Start()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if(senderThread != null && senderThread.IsAlive)
            {
                senderThread.Abort();
            }
            if (listenServerThread != null && listenServerThread.IsAlive)
            {
                listenServerThread.Abort();
            }
            Disconnect();

        }
        void CancelThread(Thread thread)
        {
            if (thread != null && thread.IsAlive)
            {
                // Signal the thread to exit gracefully
                sendToken.Cancel();
                
                // Wait for the thread to finish before proceeding (optional)
                thread.Join();
            }
        }
        void ConnectTCP(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log("Creating connetion ...");
                connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream,ProtocolType.Tcp);

                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                IPAddress serverIP = IPAddress.Parse(IPaddress);
                int serverPort = 12345; // Replace with your server's port
                endPoint = new IPEndPoint(serverIP, serverPort);

                connection.Connect(endPoint);

                if (!connection.Connected)
                {
                    Debug.LogError("Socket connection failed.");
                    return;
                }

                Debug.Log("Socket connected to -> " + connection.RemoteEndPoint.ToString());

                //start listening for server data
                StartListening();
             
            }catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void SendMessageText(string message)
        {
            try
            {
                // Creation of message that
                // we will send to Server
                byte[] sendBytes = Encoding.ASCII.GetBytes(message);
                connection.SendTo(sendBytes, sendBytes.Length, SocketFlags.None, endPoint);

                byte[] dataBuffer = new byte[1024];

                // get the data size and fill the databuffer
                int bytesRecived = connection.Receive(dataBuffer);
                Debug.Log(Encoding.ASCII.GetString(dataBuffer, 0, bytesRecived));

            }
            catch (ArgumentNullException ane)
            {

                Debug.LogError("ArgumentNullException : " + ane.ToString());
            }
            catch (SocketException se)
            {

                Debug.LogError("SocketException: " + se.SocketErrorCode); // Log the error code
                Debug.LogError("SocketException: " + se.Message); // Log the error message

            }

            catch (Exception e)
            {
                Debug.LogError("Unexpected exception : " + e.ToString());
            }
        }

        void ListenServer(CancellationToken cancellationToken)
        {
          while(!cancellationToken.IsCancellationRequested)
          {

              try
              {
                  Debug.Log("Listening server ...");
                  byte[] buffer = new byte[1024];
                  string data = null;

                  // Receive data from the client
                  int bufferSize = connection.Receive(buffer);
                  data = Encoding.ASCII.GetString(buffer, 0, bufferSize);

                  if (!string.IsNullOrEmpty(data))
                  {
                      Debug.Log("msg recived ..." + data);
                      OnMessageRecived?.Invoke(data);
                  }

             
              }
              catch (SocketException se)
              {
                  if (se.SocketErrorCode == SocketError.ConnectionReset ||
                      se.SocketErrorCode == SocketError.ConnectionAborted)
                  {
                      // Handle client disconnection (optional)
                      Debug.LogError(se);
                  }
                  else
                  {
                      // Handle other socket exceptions
                      Debug.LogError($"SocketException: {se.SocketErrorCode}, {se.Message}");
                  }
              }
              catch (Exception e)
              {
                  // Handle other exceptions
                  Debug.LogError($"Exception: {e.Message}");
              }

              Thread.Sleep(100);
          }
        }

        void StartListening()
        {
            listenerToken = new CancellationTokenSource();
            listenServerThread = new Thread(() => ListenServer(listenerToken.Token));
            listenServerThread.Start();
        }

        void Disconnect()
        { 
            if (connection != null)
            {
                connection.Shutdown(SocketShutdown.Both);
                connection.Close();
            }
        }
        void SendUDP()
        {
            try
            {
                Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                endPoint = new IPEndPoint(IPAddress.Any, 0);
                // We print EndPoint information 
                // that we are connected
                Debug.Log("Socket connected to -> " + sender.RemoteEndPoint.ToString());


            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        async Task SenderTCPAsync(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log("Creating connetion");
                Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //For a client socket, you should specify the IP address and port of the server you want to connect to.
                IPAddress serverIP = IPAddress.Parse(IPaddress);
                int serverPort = 12345; // Replace with your server's port
                endPoint = new IPEndPoint(serverIP, serverPort);
                await sender.ConnectAsync(endPoint);

                if (!sender.Connected)
                {
                    Debug.LogError("Socket connection failed.");
                    return;
                }

                Debug.Log("Socket connected to -> " + sender.RemoteEndPoint.ToString());

                try
                {

                    // Continue with sending and receiving data
                    Debug.Log("Preparing message ...");

                    byte[] sendBytes = Encoding.ASCII.GetBytes("hello from clientA");
                    await sender.SendAsync(new ArraySegment<byte>(sendBytes), SocketFlags.None);

                    byte[] receivedBytes = new byte[1024];
                    string data = null;
                    while (true)
                    {
                        // Check the cancellation token before receiving
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break; // Exit if cancellation is requested
                        }

                        int bufferSize = await sender.ReceiveAsync(new ArraySegment<byte>(receivedBytes), SocketFlags.None);

                        data += Encoding.ASCII.GetString(receivedBytes, 0, bufferSize);

                        if (data.IndexOf("<EOF>") > -1)
                            break;
                    }
                    Debug.Log("Text received -> " + data);

                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                catch (ArgumentNullException ane)
                {

                    Debug.LogError("ArgumentNullException : " + ane.ToString());
                }
                catch (SocketException se)
                {
                    Debug.LogError("SocketException : " + se.SocketErrorCode); // Log the error code
                    Debug.LogError("SocketException : " + se.Message); // Log the error message

                    // Handle the exception based on the error code
                    switch (se.SocketErrorCode)
                    {
                        case SocketError.ConnectionRefused:
                            // The server refused the connection (e.g., server not running)
                            Debug.LogError("Connection to the server was refused.");
                            break;

                        case SocketError.HostNotFound:
                            // The hostname couldn't be resolved
                            Debug.LogError("Host not found. Check the server's IP address.");
                            break;

                        default:
                            // Handle other error cases as needed
                            Debug.LogError("Unknown socket error.");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Unexpected exception : " + e.ToString());
                }

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

    }
}
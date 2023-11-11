using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using static ServerA.AServer;
using System.Collections;
using System.Collections.Generic;

namespace ClientA
{
    public class AClient : GenericSingleton<AClient>
    {
        #region variables
        IPEndPoint endPoint;
        string IPaddress;
        IPAddress serverIP;
        private Thread connectionThread;

        private CancellationTokenSource listenerToken;
        private Thread listenServerThread;

        private Socket connectionTCP;
        private Socket connectionUDP;

        public Action OnConnected;
        public Action<string> OnMessageRecived;
        private Queue<string> messageQueue = new Queue<string>();

        private int serverPort = 12345; // Replace with your server's port
        //private bool IsConnected = false;
        #endregion


        #region Enable/Disable funcitons
        private void Start()
        {
            OnConnected += StartListening;
        }
        private void OnDisable()
        {
            Disconnect();
        }
        private void Update()
        {
            if (messageQueue.Count > 0)
            {
                OnMessageRecived?.Invoke(messageQueue.Dequeue());
            }
        }
        #endregion
        #region Get/Setters
        public string GetIpAddress()
        {
            return IPaddress;
        }
        public void SetIpAddress(IPAddress adress)
        {
            serverIP = adress;
        }
        #endregion

        #region Core Functions
        public void Connect(IPAddress address)
        {
            serverIP = address;
            connectionThread = new Thread(() => Authenticate());
            connectionThread.Start();
        }
        void Authenticate()
        {
            try
            {
                //disconnect if there is a previous connection
                if (connectionTCP != null && connectionTCP.Connected)
                {
                    Disconnect();
                    Thread.Sleep(100);
                }
                //if ip is empty exit connection attempt TODO sow popup or something to user
                if (IPaddress == "")
                {
                    Debug.LogError("IP is empty ...");

                    return;
                }

                Debug.Log("Creating connetion ...");
                connectionTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connectionTCP.ReceiveTimeout = 5000;
                connectionTCP.SendTimeout = 5000;
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                if (serverIP == null)
                {
                    Debug.LogError("server Ip is null ...");
                    return;
                }
                endPoint = new IPEndPoint(serverIP, serverPort);

                connectionTCP.Connect(endPoint);

                if (!connectionTCP.Connected)
                {
                    Debug.LogError("Socket connection failed.");
                    return;
                }

                Debug.Log("Client:  Socket connected to -> " + connectionTCP.RemoteEndPoint.ToString());

                byte[] buffer = new byte[1024];
                int bufferSize;
                string msg;
                bufferSize = connectionTCP.Receive(buffer);
                msg = Encoding.ASCII.GetString(buffer, 0, bufferSize);
                Debug.Log("Client: " + msg);
                if (msg == "ok")
                {
                    Debug.Log("Client: Sending authentication token");
                    connectionTCP.Send(Encoding.ASCII.GetBytes("IM_VALID_USER_LOVE_ME"));

                }

                bufferSize = connectionTCP.Receive(buffer);

                msg = Encoding.ASCII.GetString(buffer, 0, bufferSize);
                Debug.Log("Client: " + msg);
                if (msg == "ok")
                {
                    connectionTCP.Send(Encoding.ASCII.GetBytes("Juan Caballo"));
                }

                bufferSize = connectionTCP.Receive(buffer);

                msg = Encoding.ASCII.GetString(buffer, 0, bufferSize);
                Debug.Log("Client: " + msg);

                if (msg == "ok")
                {
                    //add action dispatcher for main thread
                    MainThreadDispatcher.EnqueueAction(OnConnected);
                }
                else
                {
                    Debug.LogError("Failed on authentication process ...");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        void StartListening()
        {
            listenerToken = new CancellationTokenSource();
            listenServerThread = new Thread(() => ListenServer(listenerToken.Token));
            listenServerThread.Start();
        }
        void ListenServer(CancellationToken cancellationToken)
        {
            connectionTCP.ReceiveTimeout = Timeout.Infinite;
            connectionTCP.SendTimeout = Timeout.Infinite;
            Debug.Log("Listening server ...");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {

                    //byte[] buffer = new byte[1024];
                    //string data = null;

                    //// Receive data from the client
                    //int bufferSize = connectionTCP.Receive(buffer);
                    //data = Encoding.ASCII.GetString(buffer, 0, bufferSize);

                    //if (!string.IsNullOrEmpty(data))
                    //{
                    //    Debug.Log("msg recived ..." + data);

                    //    //queue the message to be processed on the main thread
                    //    EnqueueMessage(data);
                    //}

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
        void Disconnect()
        {
            Debug.Log("Disconnecting client ...");
            if (connectionThread != null && connectionThread.IsAlive)
            {
                connectionThread.Abort();
            }
            if (listenServerThread != null && listenServerThread.IsAlive)
            {
                CancelThread(listenServerThread, listenerToken);
            }
            if (connectionTCP != null)
            {
                connectionTCP.Shutdown(SocketShutdown.Both);
                connectionTCP.Close();
            }
        }
        void CancelThread(Thread thread, CancellationTokenSource token)
        {
            if (thread != null && thread.IsAlive)
            {
                // Signal the thread to exit gracefully
                token.Cancel();

                // Wait for the thread to finish before proceeding
                thread.Join();
            }
        }
        #endregion
        
        public void SendPacket(byte[]data)
        {
            try
            {
                if (connectionUDP == null) return;                
                connectionUDP.SendTo(data, data.Length, SocketFlags.None, endPoint);
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
        #region Helper functions
        #endregion
    }
}
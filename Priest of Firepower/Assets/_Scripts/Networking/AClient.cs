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
        public Action<byte[]> OnDataRecieved;
        private Queue<byte[]> messageQueue = new Queue<byte[]>();

        private int serverPort = 12345; // Replace with your server's port
 
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
                    if(connectionUDP.Available > 0)
                    {
                        byte[] data= new byte[1024];
                        connectionUDP.Receive(data);
                        NetworkManager.Instance.ProcessIncomingData(data);
                    }     
                    if(connectionTCP.Available > 0)
                    {
                        byte[] data = new byte[1024];
                        connectionTCP.Receive(data);
                        NetworkManager.Instance.ProcessIncomingData(data);
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
        public void SendCriticalPacket(byte[] data)
        {
            try
            {
                if (connectionTCP == null) return;

                connectionTCP.SendTo(data, data.Length, SocketFlags.None, endPoint);
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
                connectionTCP.ReceiveTimeout = 1000;
                connectionTCP.SendTimeout = 1000;
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

                bool authenticated = AuthenticateStep("ok", "IM_VALID_USER_LOVE_ME");

                if(authenticated)
                {
                    authenticated = AuthenticateStep("ok", "User name");

                    if(authenticated)
                    {
                        //add action dispatcher for main thread
                        MainThreadDispatcher.EnqueueAction(OnConnected);
                    }
                    else
                    {
                        Debug.LogError("Failed on authentication step 2");
                    }
                }else
                {
                    Debug.LogError("Failed on authentication step 1");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        bool AuthenticateStep(string expectedResponse, string messageToSend)
        {
            byte[] buffer = new byte[1024];
            int bufferSize;

            bufferSize = connectionTCP.Receive(buffer);
            string receivedMsg = Encoding.ASCII.GetString(buffer, 0, bufferSize);
            Debug.Log("Client: " + receivedMsg);

            if (receivedMsg == expectedResponse)
            {
                Debug.Log($"Client: Sending authentication token - {messageToSend}");
                connectionTCP.Send(Encoding.ASCII.GetBytes(messageToSend));
                return true;
            }
            else
            {
                Debug.LogError($"Authentication failed. Expected: {expectedResponse}, Received: {receivedMsg}");
                return false;
            }
        }
    }
}
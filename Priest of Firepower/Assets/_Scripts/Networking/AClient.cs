using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.IO;

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

        ClientAuthenticator authenticator = new ClientAuthenticator();
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

            Debug.Log("Creating connetion ...");
            connectionTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectionTCP.ReceiveTimeout = 1000;
            connectionTCP.SendTimeout = 1000;
            //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
            //In this case the operating system (TCP/IP stack) assigns a free port number for you.
            if (serverIP == null)
            {
                Debug.Log("server Ip is null ...");
                return;
            }
            endPoint = new IPEndPoint(serverIP, serverPort);

            connectionTCP.Connect(endPoint);

            if (!connectionTCP.Connected)
            {
                Debug.Log("Socket connection failed.");
                return;
            }

            Debug.Log("Client:  Socket connected to -> " + connectionTCP.RemoteEndPoint.ToString());

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
                        byte[] data= new byte[1500];
                        connectionUDP.Receive(data);
                        MemoryStream stream = new MemoryStream(data);
                        NetworkManager.Instance.AddIncomingDataQueue(stream);
                    }     
                    if(connectionTCP.Available > 0)
                    {
                        byte[] data = new byte[1500];
                        connectionTCP.Receive(data);
                        MemoryStream stream = new MemoryStream(data);
                        NetworkManager.Instance.AddIncomingDataQueue(stream);
                    }
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.ConnectionReset ||
                        se.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        // Handle client disconnection (optional)
                        Debug.Log(se);
                    }
                    else
                    {
                        // Handle other socket exceptions
                        Debug.Log($"SocketException: {se.SocketErrorCode}, {se.Message}");
                    }
                }
                catch (Exception e)
                {
                    // Handle other exceptions
                    Debug.Log($"Exception: {e.Message}");
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

                Debug.Log("ArgumentNullException : " + ane.ToString());
            }
            catch (SocketException se)
            {

                Debug.Log("SocketException: " + se.SocketErrorCode); // Log the error code
                Debug.Log("SocketException: " + se.Message); // Log the error message

            }

            catch (Exception e)
            {
                Debug.Log("Unexpected exception : " + e.ToString());
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

                Debug.Log("ArgumentNullException : " + ane.ToString());
            }
            catch (SocketException se)
            {

                Debug.Log("SocketException: " + se.SocketErrorCode); // Log the error code
                Debug.Log("SocketException: " + se.Message); // Log the error message

            }

            catch (Exception e)
            {
                Debug.Log("Unexpected exception : " + e.ToString());
            }
        }
        #region Helper functions
        #endregion

        void Authenticate()
        {
            try
            {
                authenticator.SendAuthenticationRequest("Yololo");


                //if (authenticated)
                //{
                //    //add action dispatcher for main thread
                //    MainThreadDispatcher.EnqueueAction(OnConnected);
                //}
                //else
                //{
                //    Debug.Log("Failed on authentication");
                //}

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public ClientAuthenticator GetAuthenticator() { return authenticator; }

    }
}
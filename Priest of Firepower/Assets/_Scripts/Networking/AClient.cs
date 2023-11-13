using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

namespace _Scripts.Networking
{
    public class AClient : GenericSingleton<AClient>
    {
        #region variables
        IPEndPoint _endPoint;
        private Thread _connectionThread;

        private CancellationTokenSource _listenerToken;
        private Thread _listenServerThread;

        private Socket _connectionTcp;
        private Socket _connectionUDP;

        public Action OnConnected;
        public Action<byte[]> OnDataRecieved;

        ClientAuthenticator _authenticator = new ClientAuthenticator();
        #endregion

       

        #region Enable/Disable funcitons
        private void Start()
        {
            OnConnected += StartListening;
            OnConnected += StopAuthenticationProcess;
        }
        private void OnDisable()
        {
            Disconnect();
        }
        #endregion
        #region Get/Setters
        public string GetIpAddress()
        {
            return _endPoint.ToString();
        }
        #endregion

        #region Core Functions
        public void Connect(IPEndPoint address)
        {
            _endPoint = address;

            Debug.Log("Creating connetion ...");
            _connectionTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _connectionTcp.ReceiveTimeout = 1000;
            _connectionTcp.SendTimeout = 1000;
            //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
            //In this case the operating system (TCP/IP stack) assigns a free port number for you.
            if (_endPoint == null)
            {
                Debug.Log("server Ip is null ...");
                return;
            }
            
            _connectionTcp.Connect(_endPoint);

            if (!_connectionTcp.Connected)
            {
                Debug.Log("Socket connection failed.");
                return;
            }

            Debug.Log("Client:  Socket connected to -> " + _connectionTcp.RemoteEndPoint.ToString());
            if(!NetworkManager.Instance.IsHost())
            {
                _connectionThread = new Thread(() => Authenticate());
                _connectionThread.Start();
            }
            else
            {
                Debug.Log("Local host created ...");
            }


        }
  
        void StartListening()
        {
            _listenerToken = new CancellationTokenSource();
            _listenServerThread = new Thread(() => ListenServer(_listenerToken.Token));
            _listenServerThread.Start();
        }
        void ListenServer(CancellationToken cancellationToken)
        {
            _connectionTcp.ReceiveTimeout = Timeout.Infinite;
            _connectionTcp.SendTimeout = Timeout.Infinite;
            Debug.Log("Listening server ...");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if(_connectionUDP.Available > 0)
                    {
                        byte[] data= new byte[1500];
                        _connectionUDP.Receive(data);
                        MemoryStream stream = new MemoryStream(data);
                        NetworkManager.Instance.AddIncomingDataQueue(stream);
                    }     
                    if(_connectionTcp.Available > 0)
                    {
                        byte[] data = new byte[1500];
                        _connectionTcp.Receive(data);
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
            StopAuthenticationProcess();

            if (_listenServerThread != null && _listenServerThread.IsAlive)
            {
                CancelThread(_listenServerThread, _listenerToken);
            }
            if (_connectionTcp != null)
            {
                _connectionTcp.Shutdown(SocketShutdown.Both);
                _connectionTcp.Close();
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

        void StopAuthenticationProcess()
        {
            _connectionThread.Abort();
        }
        #endregion
        public void SendCriticalPacket(byte[] data)
        {
            try
            {
               
                if (_connectionTcp == null) return;

                Debug.Log("Client: sending critical packet...");

                _connectionTcp.SendTo(data, data.Length, SocketFlags.None, _endPoint);
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
                if (_connectionUDP == null) return;   
                
                _connectionUDP.SendTo(data, data.Length, SocketFlags.None, _endPoint);
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
            _connectionTcp.ReceiveTimeout = 1000;
            try
            {
                _authenticator.SendAuthenticationRequest("Yololo");
                while (_connectionTcp.Connected)
                {


                    // Create an authentication packet
                    MemoryStream authStream = new MemoryStream();
                    BinaryWriter authWriter = new BinaryWriter(authStream);

                    authWriter.Write((int)PacketType.AUTHENTICATION);
                    authWriter.Write("hello");
                                     
                    NetworkManager.Instance.AddReliableStreamQueue(authStream);

                    //if (authenticated)
                    //{
                    //    //add action dispatcher for main thread
                    //    MainThreadDispatcher.EnqueueAction(OnConnected);
                    //}
                    //else
                    //{
                    //    Debug.Log("Failed on authentication");
                    //}

                    Thread.Sleep(100);
                }
  


            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public ClientAuthenticator GetAuthenticator() { return _authenticator; }

    }
}
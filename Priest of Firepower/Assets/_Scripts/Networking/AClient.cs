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
        
        Process _authenticationProcess = new Process();
        Process _serverListenerProcess = new Process();

        private Socket _connectionTCP;
        private Socket _connectionUDP;

        public Action OnConnected;
        public Action<byte[]> OnDataRecieved;

        ClientAuthenticator _authenticator = new ClientAuthenticator();

        private UInt64 _ID = 69;

        #endregion            
        #region Enable/Disable funcitons
        private void Start()
        {
            OnConnected += StartListening;
            OnConnected += _authenticationProcess.Shutdown;
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
            try
            {
                _endPoint = address;

                //Debug.Log("Creating connetion ...");
                _connectionTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _connectionTCP.ReceiveTimeout = 1000;
                _connectionTCP.SendTimeout = 1000;
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                if (_endPoint == null)
                {
                    Debug.Log("server Ip is null ...");
                    return;
                }

                _connectionTCP.Connect(_endPoint);

                if (!_connectionTCP.Connected)
                {
                    Debug.Log("Socket connection failed.");
                    return;
                }

                _connectionTCP.ReceiveTimeout = 5000;
                _connectionTCP.SendTimeout = 5000;

                //create new udp connection
                _connectionUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                _connectionUDP.Bind(_endPoint);

                Debug.Log("Client:  Socket connected to -> " + _connectionTCP.RemoteEndPoint.ToString());

                if (!NetworkManager.Instance.IsHost())
                {
                    _authenticationProcess.cancellationToken = new CancellationTokenSource();
                    _authenticationProcess.thread = new Thread(() => Authenticate(_authenticationProcess.cancellationToken.Token));
                    _authenticationProcess.thread.Start();
                    _authenticator.SetEndPoint(_endPoint);
                }
                else
                {
                    OnConnected?.Invoke();

                    Debug.Log("Local host created ...");
                }
            }catch(Exception e)
            {
                Debug.LogWarning(e);
            }
    
        }
        void StartListening()
        {
            Debug.Log("Client: listening to server ...");
            _serverListenerProcess.cancellationToken = new CancellationTokenSource();
            _serverListenerProcess.thread = new Thread(() => ListenServer(_serverListenerProcess.cancellationToken.Token));
            _serverListenerProcess.thread.Start();
        }

        void ListenServer(CancellationToken cancellationToken)
        {
            Debug.Log("Listening server ...");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if(_connectionUDP.Available > 0)
                    {
                        ReceiveSocketData(_connectionUDP);
                    }     
                    if(_connectionTCP.Available > 0)
                    {
                        ReceiveSocketData(_connectionTCP);
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


            _serverListenerProcess.Shutdown();
            _authenticationProcess.Shutdown();

            if (_connectionTCP != null)
            {
                _connectionTCP.Shutdown(SocketShutdown.Both);
                _connectionTCP.Close();
                _connectionUDP.Close();
            }
        }
        #endregion
        public void SendCriticalPacket(byte[] data)
        {
            try
            {               
                if (_connectionTCP == null) return;
                _connectionTCP.SendTo(data, data.Length, SocketFlags.None, _endPoint);
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
        public void SendPacket(byte[] data)
        {
            try
            {
                Debug.Log($"Client sending data: {_endPoint} - Length: {data.Length}");

                _connectionUDP.SendTo(data, data.Length, SocketFlags.None, _endPoint);
            }
            catch (ArgumentNullException ane)
            {
                Debug.LogError("ArgumentNullException: " + ane.ToString());
            }
            catch (SocketException se)
            {
                Debug.LogError($"SocketException - Error Code: {se.SocketErrorCode}, Message: {se.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError("Unexpected exception: " + e.ToString());
            }
        }
        #region Helper functions
        #endregion


        public void HandleRecivingID(BinaryReader reader)
        { 
            _ID = reader.ReadUInt64();
            Debug.Log("recived ID :" + _ID);
        }
        public UInt64 ID() { return _ID; }
        void Authenticate(CancellationToken token)
        {
            _connectionTCP.ReceiveTimeout = 1000;
            bool authenticated = false;
            try
            {
                _authenticator.SendAuthenticationRequest("Yololo");
                while (!token.IsCancellationRequested)
                {

                    if (_connectionTCP.Available > 0)
                    {
                        Debug.Log("Authentication message recieved ...");
                        ReceiveSocketData(_connectionTCP);
                    }

                    if (_authenticator.Authenticated() && !authenticated)
                    {
                        authenticated = true;
                        //add action dispatcher for main thread
                        MainThreadDispatcher.EnqueueAction(OnConnected);                    
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        void ReceiveSocketData(Socket socket)
        {
            try
            {
                lock (NetworkManager.Instance.IncomingStreamLock)
                {
                    byte[] buffer = new byte[1500];

                    // Receive data from the client
                    int size = socket.Receive(buffer, buffer.Length, SocketFlags.None);

                    MemoryStream stream = new MemoryStream(buffer, 0, size);

                    NetworkManager.Instance.AddIncomingDataQueue(stream);
                }
            }
            catch (SocketException se)
            {
                // Handle other socket exceptions
                Debug.Log($"SocketException: {se.SocketErrorCode}, {se.Message}");

            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.Log($"Exception: {e.Message}");
            }
        }
        public ClientAuthenticator GetAuthenticator() { return _authenticator; }

    }
}
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
        #region Fields
        private UInt64 _id = 69;
        private string _userName = "Yololo";
        
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;
        
        private Process _authenticationProcess = new Process();
        private Process _serverListenerProcess = new Process();
        
        private Socket _connectionTcp;
        private Socket _connectionUdp;
        
        public Action OnConnected;
        public Action<byte[]> OnDataRecieved;
        
        private ClientAuthenticator _authenticator = new ClientAuthenticator();
        #endregion

        #region Enable/Disable funcitons

        private void OnEnable()
        {
            //OnConnected += StartListening;
            //OnConnected += _authenticationProcess.Shutdown;
            _authenticator.userName = _userName;
        }

        private void OnDisable()
        {
            //OnConnected -= StartListening;
            //OnConnected -= _authenticationProcess.Shutdown;
            Disconnect();
        }

        public void Shutdown()
        {
            Disconnect();
        }

        #endregion

        // #region Get/Setters
        //
        // public string GetIpAddress()
        // {
        //     return _endPoint.ToString();
        // }
        //
        // public void SetUsername(string username)
        // {
        //     userName = username;
        // }
        //
        // #endregion

        #region Core Functions

        public void Connect(IPEndPoint serverEndPoint)
        {
            try
            {
                _remoteEndPoint = serverEndPoint;
                _localEndPoint = new IPEndPoint(IPAddress.Any, 0);

                //Debug.Log("Creating connetion ...");
                _connectionTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _connectionTcp.Bind(_localEndPoint);
                _connectionTcp.ReceiveTimeout = 1000;
                _connectionTcp.SendTimeout = 1000;
    
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                if (_remoteEndPoint == null)
                {
                    Debug.Log("server Ip is null ...");
                    return;
                }

                _connectionTcp.Connect(_remoteEndPoint);
                if (!_connectionTcp.Connected)
                {
                    Debug.Log("Socket connection failed.");
                    return;
                }

                _connectionTcp.ReceiveTimeout = 5000;
                _connectionTcp.SendTimeout = 5000;

                //create new udp connection
                _connectionUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _connectionUdp.Bind(_localEndPoint);
                _connectionUdp.Connect(_remoteEndPoint);
                
                Debug.Log("Client:  Socket connected to -> " + _connectionTcp.RemoteEndPoint.ToString());
                if (!NetworkManager.Instance.IsHost())
                {
                    Debug.Log("Invoke ... ");
                    OnConnected?.Invoke();
                    StartListening();
                    //_authenticationProcess.cancellationToken = new CancellationTokenSource();
                    //_authenticationProcess.thread = new Thread(() => Authenticate(_authenticationProcess.cancellationToken.Token));
                    //_authenticationProcess.thread.Start();
                    //_authenticator.SetEndPoint(_endPoint);
                }
                else
                {
                    OnConnected?.Invoke();
                    Debug.Log("Local host created ...");
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        void StartListening()
        {
            Debug.Log("Client: listening to server ...");
            _serverListenerProcess.cancellationToken = new CancellationTokenSource();
            _serverListenerProcess.thread =
                new Thread(() => ListenServer(_serverListenerProcess.cancellationToken.Token));
            _serverListenerProcess.thread.Start();
        }

        void ListenServer(CancellationToken cancellationToken)
        {
            Debug.Log("Listening server ...");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // if (_serverUdp.Available > 0) // For connectionless protocols (UDP), the available property won't work as intended like in TCP.
                    ReceiveUDPSocketData(_connectionUdp);

                    if (_connectionTcp.Available > 0)
                    {
                        Debug.Log("Client: TCP data received ...");
                        ReceiveSocketData(_connectionTcp);
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
                    Debug.LogException(e);
                }

                Thread.Sleep(100);
            }

            Debug.Log("Ending listening server ...");
        }

        void Disconnect()
        {
            Debug.Log("Disconnecting client ...");
            _serverListenerProcess.Shutdown();
            _authenticationProcess.Shutdown();
            if (_connectionTcp != null)
            {
                _connectionTcp.Shutdown(SocketShutdown.Both);
                _connectionTcp.Close();
                _connectionUdp.Close();
            }
        }

        #endregion

        public void SendCriticalPacket(byte[] data)
        {
            try
            {
                if (_connectionTcp == null) return;
                _connectionTcp.SendTo(data, data.Length, SocketFlags.None, _remoteEndPoint);
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

        public void SendPacket(byte[] data)
        {
            try
            {
                Debug.Log($"Client sending data: {_remoteEndPoint} - Length: {data.Length}");
                _connectionUdp.Send(data);
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
            _id = reader.ReadUInt64();
            Debug.Log("recived ID :" + _id);
        }

        public UInt64 ID()
        {
            return _id;
        }

        void Authenticate(CancellationToken token)
        {
            _connectionTcp.ReceiveTimeout = 1000;
            bool authenticated = false;
            try
            {
                _authenticator.SendAuthenticationRequest(_userName);
                while (!token.IsCancellationRequested)
                {
                    if (_connectionTcp.Available > 0)
                    {
                        Debug.Log("?????");
                        ReceiveSocketData(_connectionTcp);
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
                Debug.LogError($"SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Exception: {e.Message}");
            }
        }
        
        void ReceiveUDPSocketData(Socket socket)
        {
            try
            {
                lock (NetworkManager.Instance.IncomingStreamLock)
                {
                    if (socket.Poll(1000, SelectMode.SelectRead)) // Wait up to 1 seconds for data to arrive
                    {
                        byte[] buffer = new byte[1500];
                        int size = socket.Receive(buffer);
                        MemoryStream stream = new MemoryStream(buffer, 0, size);
                        NetworkManager.Instance.AddIncomingDataQueue(stream);
                    }
                    /*If you are using a connectionless protocol such as UDP, you do not have to call Connect before sending and receiving data.
                     You can use SendTo and ReceiveFrom to synchronously communicate with a remote host. 
                     If you do call Connect, any datagrams that arrive from an address other than the specified default will be discarded. */
                }
            }
            catch (SocketException se)
            {
                // Handle other socket exceptions
                Debug.LogError($"SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Exception: {e.Message}");
            }
        }

        public ClientAuthenticator GetAuthenticator()
        {
            return _authenticator;
        }
    }
}
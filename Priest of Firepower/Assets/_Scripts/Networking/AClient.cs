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
    public class AClient 
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

        public AClient(string name, IPEndPoint localEndPoint, Action onConnected)
        {
            _userName = name;
            _localEndPoint = localEndPoint;
            OnConnected += onConnected;
        }
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

                _connectionTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _connectionTcp.Bind(_localEndPoint);
                _connectionTcp.ReceiveTimeout = 1000;
                _connectionTcp.SendTimeout = 1000;
                
                
                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                if (_remoteEndPoint == null)
                {
                    Debug.Log($"Client {_userName}_{_id}: {_remoteEndPoint} is null");
                    return;
                }
                
                Debug.Log($"Client {_userName}_{_id}: Trying to connect TCP local EP {_localEndPoint} to server EP {_remoteEndPoint}.");
                _connectionTcp.Connect(_remoteEndPoint);
                
                if (_connectionTcp.Connected)
                {
                    Debug.Log($"Client {_userName}_{_id}: Successfully connected TCP local EP {_connectionTcp.LocalEndPoint} to server EP {_connectionTcp.RemoteEndPoint}.");
                }
                else
                {
                    Debug.Log($"Client {_userName}_{_id}: Failed to connected TCP local EP {_connectionTcp.LocalEndPoint} to server EP {_connectionTcp.RemoteEndPoint}.");
                }

                _connectionTcp.ReceiveTimeout = 5000;
                _connectionTcp.SendTimeout = 5000;

                //create new udp connection
                _connectionUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _connectionUdp.Bind(_localEndPoint);
                
                Debug.Log($"Client {_userName}_{_id}: Trying to connect UDP local EP {_localEndPoint} to server EP {_remoteEndPoint}.");
                _connectionUdp.Connect(_remoteEndPoint);
                
                if (_connectionTcp.Connected)
                {
                    Debug.Log($"Client {_userName}_{_id}: Successfully connected UDP local EP {_connectionUdp.LocalEndPoint} to server EP {_connectionUdp.RemoteEndPoint}.");
                }
                else
                {
                    Debug.Log($"Client {_userName}_{_id}: Failed to connected UDP local EP {_connectionUdp.LocalEndPoint} to server EP {_connectionUdp.RemoteEndPoint}.");
                }
                
                if (!NetworkManager.Instance.IsHost())
                {
                    Debug.Log($"Client {_userName}_{_id}: Is NOT hosting and invoking.");
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
                    Debug.Log($"Client {_userName}_{_id}: Is Hosting and invoking.");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Client {_userName}_{_id}: Has exception!");
                Debug.LogException(e);
            }
        }

        void StartListening()
        {
            Debug.Log($"Client {_userName}_{_id}: Will be starting to listen server.");
            _serverListenerProcess.cancellationToken = new CancellationTokenSource();
            _serverListenerProcess.thread =
                new Thread(() => ListenServer(_serverListenerProcess.cancellationToken.Token));
            _serverListenerProcess.thread.Start();
        }

        void ListenServer(CancellationToken cancellationToken)
        {
            Debug.Log($"Client {_userName}_{_id}: Is about to starting to listen server.");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // if (_serverUdp.Available > 0) // For connectionless protocols (UDP), the available property won't work as intended like in TCP.
                    ReceiveUdpSocketData(_connectionUdp);

                    if (_connectionTcp.Available > 0)
                    {
                        ReceiveTcpSocketData(_connectionTcp);
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
                        Debug.Log($"Client {_userName}_{_id}: SocketException: {se.SocketErrorCode}, {se.Message}");
                    }
                }
                catch (Exception e)
                {
                    // Handle other exceptions
                    Debug.LogException(e);
                }

                Thread.Sleep(100);
            }

            Debug.Log($"Client {_userName}_{_id}: Is ending listening server.");
        }

        void Disconnect()
        {
            Debug.Log($"Client {_userName}_{_id}: Is disconnecting and shutting down the sockets.");
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
                Debug.Log($"Client {_userName}_{_id}: Is sending critical packet");
                if (_connectionTcp == null) return;
                _connectionTcp.SendTo(data, data.Length, SocketFlags.None, _remoteEndPoint);
            }
            catch (ArgumentNullException ane)
            {
                Debug.LogError($"Client {_userName}_{_id}: ArgumentNullException : {ane.ToString()}");
            }
            catch (SocketException se)
            {
                Debug.LogError($"Client {_userName}_{_id}: SocketException: " + se.SocketErrorCode); // Log the error code
                Debug.LogError($"Client {_userName}_{_id}: SocketException: " + se.Message); // Log the error message
            }
            catch (Exception e)
            {
                Debug.LogError($"Client {_userName}_{_id}: Unexpected exception : {e.ToString()}");
            }
        }

        public void SendPacket(byte[] data)
        {
            try
            {
                Debug.Log($"Client {_userName}_{_id}: Sending data to {_remoteEndPoint} - Length: {data.Length}");
                _connectionUdp.Send(data);
            }
            catch (ArgumentNullException ane)
            {
                Debug.LogError($"Client {_userName}_{_id}: ArgumentNullException: {ane.ToString()}");
            }
            catch (SocketException se)
            {
                Debug.LogError($"Client {_userName}_{_id}: SocketException - Error Code: {se.SocketErrorCode}, Message: {se.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Client {_userName}_{_id}: Unexpected exception: {e.ToString()}" );
            }
        }

        public void HandleRecivingID(BinaryReader reader)
        {
            UInt64 oldId = _id;
            _id = reader.ReadUInt64();
            Debug.Log($"Client {_userName}_{oldId}: + has received new {_id}");
        }

        public UInt64 GetId()
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
                        ReceiveTcpSocketData(_connectionTcp);
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

        void ReceiveTcpSocketData(Socket socket)
        {
            try
            {
                Debug.Log($"Client {_userName}_{_id}: Has received Tcp Data");
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
                Debug.LogError($"Client {_userName}_{_id}: SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Client {_userName}_{_id}: Exception: {e.Message}");
            }
        }
        
        void ReceiveUdpSocketData(Socket socket)
        {
            try
            {
                Debug.Log($"Client {_userName}_{_id}: Has received Udp Data");
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
                MainThreadDispatcher.EnqueueAction(() =>  Debug.LogError($"Client {_userName}_{_id}: SocketException: {se.SocketErrorCode}, {se.Message}"));
            }
            catch (Exception e)
            {
                // Handle other exceptions
                MainThreadDispatcher.EnqueueAction(() => Debug.LogError($"Client {_userName}_{_id}: Exception: {e.Message}"));
            }
        }

        public ClientAuthenticator GetAuthenticator()
        {
            return _authenticator;
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using _Scripts.Networking.Authentication;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Process = _Scripts.Networking.Utility.Process;

namespace _Scripts.Networking.Client
{
    public class Client 
    {
        #region Fields
        public ClientData _clientData { get; private set; }
        private IPEndPoint _remoteEndPointTcp;
        private IPEndPoint _remoteEndPointUdp;
        
        private Process _authenticationProcess = new Process();
        private Process _serverListenerProcess = new Process();
        public DeliveryNotificationManager deliveryNotificationManager = new DeliveryNotificationManager();

        //tick sync
        private ushort _serverTick;
        public ushort ServerTick
        {
            get => _serverTick;
            private set
            {
                _serverTick = value;
                InterpolationTick = (ushort)(value - TicksBetweenPositionUpdates);
            }
        }
        public ushort InterpolationTick { get; private set; }
        private ushort _ticksBetweenPositionUpdates = 2;

        public ushort TicksBetweenPositionUpdates
        {
            get => _ticksBetweenPositionUpdates;
            private set
            {
                _ticksBetweenPositionUpdates = value;
                InterpolationTick = (ushort)(ServerTick - value);
            }
        }
        private ushort tickDivergencceTolerance = 1;

        public Action onConnected;
        public Action onServerConnectionLost;

        private ClientAuthenticator _authenticator;
        public ClientAuthenticator authenticator => _authenticator;

        public Client(string name, IPEndPoint localEndPointTcp, Action onConnected)
        {
            _clientData = new ClientData(69, name, localEndPointTcp, new IPEndPoint(IPAddress.Any, 0));
            this.onConnected += onConnected;
            ServerTick = 2;
        }
        #endregion

        #region Enable/Disable funcitons
        public void Shutdown()
        {
            DisconnectFromServer();
        }
        #endregion

        #region Core Functions
        public void ConnectToServer(IPEndPoint serverEndPointTcp, IPEndPoint serverEndPointUdp)
        {
            try
            {
                _remoteEndPointTcp = serverEndPointTcp;
                _remoteEndPointUdp = serverEndPointUdp;
                
                _clientData.connectionTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _clientData.connectionTcp.Bind(_clientData.endPointTcp);
                _clientData.connectionTcp.ReceiveTimeout = 1000;
                _clientData.connectionTcp.SendTimeout = 1000;
                
                if (_remoteEndPointTcp == null)
                {
                    Debug.Log($"Client {_clientData.userName}_{_clientData.id}: {_remoteEndPointTcp} is null");
                    return;
                }
                
                Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Trying to connect TCP local EP {_clientData.endPointTcp} to server EP {_remoteEndPointTcp}.");
                _clientData.connectionTcp.Connect(_remoteEndPointTcp);
                _clientData.endPointTcp = _clientData.connectionTcp.LocalEndPoint as IPEndPoint; // Caching the newly assigned TCP socket address.
                
                if (_clientData.connectionTcp.Connected)
                {
                    Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Successfully connected TCP local EP {_clientData.connectionTcp.LocalEndPoint} to server EP {_clientData.connectionTcp.RemoteEndPoint}.");
                }
                else
                {
                    Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: Failed to connected TCP local EP {_clientData.connectionTcp.LocalEndPoint} to server EP {_clientData.connectionTcp.RemoteEndPoint}.");
                }

                _clientData.connectionTcp.ReceiveTimeout = 5000;
                _clientData.connectionTcp.SendTimeout = 5000;
                
                _clientData.connectionUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _clientData.connectionUdp.Bind(_clientData.endPointUdp);
                    
                Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Trying to connect UDP local EP {_clientData.endPointUdp} to server EP {_remoteEndPointUdp}.");
                _clientData.connectionUdp.Connect(_remoteEndPointUdp);
                _clientData.endPointUdp = _clientData.connectionUdp.LocalEndPoint as IPEndPoint;// Caching the newly assigned UDP socket address.
                
                if (_clientData.connectionTcp.Connected)
                {
                    Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Successfully connected UDP local EP {_clientData.connectionUdp.LocalEndPoint} to server EP {_clientData.connectionUdp.RemoteEndPoint}.");
                }
                else
                {
                    Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Failed to connected UDP local EP {_clientData.connectionUdp.LocalEndPoint} to server EP {_clientData.connectionUdp.RemoteEndPoint}.");
                }
                
                Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Starting authentication process to server EP {_clientData.connectionUdp.RemoteEndPoint}.");
                
                _authenticator = new ClientAuthenticator(_clientData, _clientData.connectionTcp,  onConnected, null);
                Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Will be starting to listen server.");
                _serverListenerProcess.cancellationToken = new CancellationTokenSource();
                _serverListenerProcess.thread =
                    new Thread(() => ListenToServer(_serverListenerProcess.cancellationToken.Token));
                _serverListenerProcess.thread.Start();

                //start hearbeat stopwatch
                _clientData.heartBeatStopwatch = new Stopwatch();
                _clientData.heartBeatStopwatch.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: Has exception! {e}");
            }
        }
        void ListenToServer(CancellationToken cancellationToken)
        {
            Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Is about to starting to listen server.");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (NetworkManager.Instance.IsClient() && _clientData.disconnectTimeout < _clientData.heartBeatStopwatch.ElapsedMilliseconds)
                    {
                        Debug.Log("Client: server connection lost ...");
                        MainThreadDispatcher.EnqueueAction(()=>NetworkManager.Instance.Reset());
                        MainThreadDispatcher.EnqueueAction(()=>SceneManager.LoadScene("ConnectToLobby"));
                    }
                    
                    // if (_serverUdp.Available > 0) // For connectionless protocols (UDP), the available property won't work as intended like in TCP.
                    ReceiveUdpSocketData(_clientData.connectionUdp);

                    while (_clientData.connectionTcp.Available > 0)
                    {
                        ReceiveTcpSocketData(_clientData.connectionTcp);
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
                        Debug.Log($"Client {_clientData.userName}_{_clientData.id}: SocketException: {se.SocketErrorCode}, {se.Message}");
                    }
                }
                catch (Exception e)
                {
                    // Handle other exceptions
                    Debug.LogException(e);
                }

                Thread.Sleep(1);
            }

            Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Is ending listening server.");
        }

        void DisconnectFromServer()
        {
            Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Is disconnecting and shutting down the sockets.");
            
            _serverListenerProcess.Shutdown();
            _authenticationProcess.Shutdown();
            if (_clientData.connectionTcp != null)
            {
                _clientData.connectionTcp.Shutdown(SocketShutdown.Both);
                _clientData.connectionTcp.Close();
            }

            if (_clientData.connectionUdp != null)
            {
                _clientData.connectionUdp.Shutdown(SocketShutdown.Both);
                _clientData.connectionUdp.Close();
            }
        }

        #endregion

        #region Data Transmission
         public void SendTcp(byte[] data)
         {
            try
            {
                Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Sending Tcp packet from {_clientData.connectionTcp.LocalEndPoint}to {_clientData.connectionTcp.RemoteEndPoint} - Length: {data.Length}");
                _clientData.connectionTcp.Send(data);
            }
            catch (ArgumentNullException ane)
            {
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: ArgumentNullException : {ane.ToString()}");
            }
            catch (SocketException se)
            {
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: SocketException: " + se.SocketErrorCode); // Log the error code
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: SocketException: " + se.Message); // Log the error message
            }
            catch (Exception e)
            {
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: Unexpected exception : {e.ToString()}");
            }
         }

        public void SendUdpPacket(byte[] data)
        {
            try
            {
                //Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Sending Udp packet from {_clientData.connectionUdp.LocalEndPoint}to {_clientData.connectionUdp.RemoteEndPoint} - Length: {data.Length}");
                _clientData.connectionUdp.Send(data);
            }
            catch (ArgumentNullException ane)
            {
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: ArgumentNullException: {ane.ToString()}");
            }
            catch (SocketException se)
            {
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: SocketException - Error Code: {se.SocketErrorCode}, Message: {se.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: Unexpected exception: {e.ToString()}" );
            }
        }
        void ReceiveTcpSocketData(Socket socket)
        {
            try
            {
                Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Has received Tcp Data");
                lock (NetworkManager.Instance.incomingStreamLock)
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
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: SocketException: {se.SocketErrorCode}, {se.Message}");
            }
            catch (Exception e)
            {
                // Handle other exceptions
                Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: Exception: {e.Message}");
            }
        }
        
        void ReceiveUdpSocketData(Socket socket)
        {
            try
            {
                lock (NetworkManager.Instance.incomingStreamLock)
                {
                    if (socket.Poll(1000, SelectMode.SelectRead)) // Wait up to 1 seconds for data to arrive
                    {
                        //Debug.Log($"Client {_clientData.userName}_{_clientData.id}: Has received Udp Data from {socket.RemoteEndPoint}");
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
                UnityMainThreadDispatcher.Dispatcher.Enqueue(onServerConnectionLost);
                UnityMainThreadDispatcher.Dispatcher.Enqueue(()=>SceneManager.LoadScene("ConnectToLobby"));
                UnityMainThreadDispatcher.Dispatcher.Enqueue(() => Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: SocketException: {se.SocketErrorCode}, {se.Message}"));
            }
            catch (Exception e)
            {
                // Handle other exceptions
                UnityMainThreadDispatcher.Dispatcher.Enqueue(() => Debug.LogError($"Client {_clientData.userName}_{_clientData.id}: Exception: {e.Message}"));
            }
        }
        #endregion

        #region TickSync

        public void SetTick(ushort serverTick)
        {
            if (MathF.Abs(ServerTick - serverTick) > tickDivergencceTolerance)
            {
                //Debug.Log($"Sync Client Tick: {ServerTick} -> {serverTick}");
                ServerTick = serverTick;
            }
        }
        public void FixedUpdate()
        {
            ServerTick++;
        }
        #endregion

        #region Getter/Setter
        public UInt64 GetId()
        {
            return _clientData.id;
        }
        #endregion
        #region Heart Beat
        public void HandleHeartBeat(BinaryReader reader)
        {
            _clientData.heartBeatStopwatch.Restart();
        }

        public void SendHeartBeat()
        {
            MemoryStream newStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(newStream);
            writer.Write(_clientData.id);
            writer.Write(0);
            Packet syncPacket = new Packet(PacketType.PING, ulong.MinValue, ulong.MinValue, long.MinValue,
                Int32.MinValue,false, newStream.ToArray());
            SendUdpPacket(syncPacket.allData);
        }
        #endregion
    }
}
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using ServerAli;
using System.Threading;
using System.Text;

namespace ServerAli
{
    public class Client_TCP : Socket_Connection
    {
        #region Fields
        private Thread _writeServerThread;
        private Thread _readServerThread;
        public string serverIP = "127.0.0.1";
        #endregion

        #region Initializers and Cleanup
        private void Awake()
        {
            // Make sure in localhost client doesn't have the same port as server
            while (_port == 61111)
            {
                _port = Utilities.FreeTcpPort();
            }

            InitSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, null);
        }

        private void OnDisable()
        {
            DisconnectFromServer();
        }

        #endregion

        #region Core func
        public void InitServerConnection()
        {
            if (_writeServerThread != null)
            {
                _writeServerThread.Abort();
                _writeServerThread = null;
            }

            if (_readServerThread != null)
            {
                _readServerThread.Abort();
                _readServerThread = null;
            }

            if (Utilities.ValidateIPAdress(serverIP, out string cleanedIp))
            {
                _writeServerThread = new Thread(() => ConnectToServer(cleanedIp));
                _writeServerThread.Start();
            }
            else
            {
                Debug.LogAssertion($"CLIENT TCP: Insert an valid IP Adress, {serverIP} is not a valid IP address");
            }
        }

        public void DisconnectFromServer()
        {
            Debug.Log("CLIENT TCP: Init Server Disconnection");
            if (_writeServerThread != null) _writeServerThread.Abort();
            if (_readServerThread != null) _readServerThread.Abort();

            Utilities.CloseConnection(_localSocket);
        }

        void ConnectToServer(string ip)
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), 61111);
            ConnectToSocket(serverEndPoint, () =>
            {
                SendData(_localSocket, Encoding.ASCII.GetBytes($"Hello {serverEndPoint}, my user name is: {_socketName}"), null);

                _readServerThread = new Thread(() => ListenData(_localSocket, null, null));
                _readServerThread.Name = "_readServerThread";
                _readServerThread.Start();
            });
        }
        #endregion
    }
}
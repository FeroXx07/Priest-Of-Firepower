using ServerAli;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ServerAli
{
    public class Socket_Connection : MonoBehaviour
    {
        #region Fields
        protected Socket _localSocket;
        protected IPEndPoint _iPEndPointlocal;

        protected IPAddress _address = IPAddress.Parse("127.0.0.1");
        [SerializeField] protected int _port = Utilities.FreeTcpPort();

        protected List<Thread> _activeThreads = new List<Thread>();
        protected List<Socket> _connectedSockets = new List<Socket>();
        [SerializeField] public string _socketName;
        [SerializeField] protected bool _isHost;
        #endregion

        #region Events
        public Action<string> onRemoteIPAssignated;
        public Action<Socket> OnNewConnection;
        #endregion

        private void OnDisable()
        {
            foreach (var thread in _activeThreads) 
                thread.Abort();
            _activeThreads.Clear();

            foreach (var socket in _connectedSockets)
                Utilities.CloseConnection(socket);
            _connectedSockets.Clear();
        }

        #region Virtual
        protected virtual void InitSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, Action callback) 
        {
            // Init
            _localSocket = new Socket(addressFamily, socketType, protocolType);
            _iPEndPointlocal = new IPEndPoint(_address, _port);

            /*Binding is only necessary to receive data from arbitrary hosts (otherwise, thesocket doesn’t need to know!)
             * 
             * Q: I am wondering why the client doesn't need to bind with its ip address and port number.
             A: Because there is an internal bind() as part of connect(), if the socket isn't already bound, 
             and because the server doesn't care what the client's port number is: it doesn't need to be fixed like the server's port number.*/

            callback?.Invoke();
        }
        protected virtual void ConnectToSocket(IPEndPoint iPEndPoint, Action callback) 
        {
            try
            {
                // Connect throws an exception if unsuccessful
                _localSocket.Connect(iPEndPoint);
            }
            catch (System.Exception e)
            {
                Debug.Log($"Connection to {iPEndPoint} failed" + e.ToString());
            }

            Debug.Log($"Connection to {iPEndPoint}: {_localSocket.Connected}");

            callback?.Invoke();
        }
        protected virtual void SendData(Socket socketToSend, byte[] data, Action callback) 
        {
            // This is how you can determine whether a socket is still connected.
            bool blockingState = socketToSend.Blocking;
            try
            {
                socketToSend.Blocking = false;
                int i = socketToSend.Send(data, data.Length, SocketFlags.None);
                Debug.Log($"Sent {i} bytes to: {socketToSend.RemoteEndPoint}!");
            }
            catch (SocketException e)
            {
                // 10035 == WSAEWOULDBLOCK
                if (e.NativeErrorCode.Equals(10035))
                {
                    Debug.Log("Still Connected, but the Send would block");
                }
                else
                {
                    Debug.Log($"Disconnected: error code {e.NativeErrorCode}!");
                }
            }
            finally
            {
                socketToSend.Blocking = blockingState;
                callback?.Invoke();
            }
        }
        protected virtual void ListenData(Socket socketToListen, Action<byte[]> callback, Action<Socket> disconnectionCallback) 
        {
            while (socketToListen != null)
            {
                try
                {
                    //if (_socket.Poll(100000, SelectMode.SelectRead)) //check if there's data available for reading on the socket without blocking
                    //{

                    //}
                    socketToListen.Blocking = true;
                    byte[] b = new byte[100];
                    int count = socketToListen.Receive(b);

                    callback?.Invoke(b);

                    if (count == 0)
                    {
                        Debug.Log($"{(IPEndPoint)socketToListen.RemoteEndPoint} is disconnected ");
                        disconnectionCallback?.Invoke(socketToListen);
                    }

                    string szReceived = Encoding.ASCII.GetString(b, 0, count);

                    Debug.Log($"Received data from {(IPEndPoint)socketToListen.RemoteEndPoint}: {szReceived}");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"Error trying to listen to {(IPEndPoint)socketToListen.RemoteEndPoint}" + e.ToString());
                }
            }
        }
        protected virtual void ListenForConnections(Action<Socket> callback)
        {
            while (_localSocket != null)
            {
                //bool a = _localSocket == null ? false : true;
                //Debug.Log(a);
                try
                {
                    Debug.Log("Listening... Waiting for clients...");
                    if (_localSocket.Poll(100000, SelectMode.SelectRead)) //check if there's data available for reading on the socket without blocking
                    {
                        // Check if there's data available for reading (100000 microseconds = 100 milliseconds)
                        Socket client = _localSocket.Accept();// Contrary to socket.Accept(), async server socket.BeginAccept() starts a new thread for each client socket assigning a new port

                        if (client != null && client.Connected)
                            callback(client);
                    }
                    else
                    {
                        // No incoming connections, continue waiting
                    }
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.WouldBlock || se.SocketErrorCode == SocketError.IOPending)
                    {
                        // Non-blocking operation would block, continue waiting
                    }
                    else
                    {
                        Debug.Log("Connection failed.. trying again... " + se.ToString());
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log("Connection failed.. trying again... " + e.ToString());
                }
            }
        }
        protected virtual void HandleDisconnection(Socket socket) { }
        #endregion
    }
}

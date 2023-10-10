using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

namespace ServerAli
{
    public class SupportClass
    {
        public static void BindSocket(Socket socket, IPEndPoint endPoint)
        {
            try
            {
                socket.Bind(endPoint);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Winsock error: " + e.ToString());
            }
        }

        public static void CloseConnection(Socket socket)
        {
            try
            {
                if (socket != null)
                {
                    socket.Shutdown(SocketShutdown.Both); // Disable both sending and receiving on this Socket.
                    socket = null;
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Error closing socket connection: " + e.ToString());
            }
            finally
            {
                socket.Close(); // Closes the Socket connection and releases all associated resources.
                socket.Dispose(); //Releases all resources used by the current instance of the Socket class.
            }
        }

        public static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}

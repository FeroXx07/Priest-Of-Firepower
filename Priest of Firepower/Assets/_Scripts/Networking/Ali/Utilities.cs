using System;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

namespace ServerAli
{
    public class Utilities
    {
        public static void BindSocket(Socket socket, IPEndPoint endPoint)
        {
            try
            {
                socket.Bind(endPoint);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log("Winsock error: " + e.ToString());
            }
        }

        public static void CloseConnection(Socket socket)
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both); // Disable both sending and receiving on this Socket.
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log("Error closing socket connection: " + e.ToString());
            }
            finally
            {
                socket.Close(); // Closes the Socket connection and releases all associated resources.
                socket.Dispose(); //Releases all resources used by the current instance of the Socket class.
                socket = null;
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

        public static bool ValidateIPAdress(string ipToValidate, out string cleanedIp)
        {
            // Remove non-numeric characters and leading/trailing spaces
            cleanedIp = Regex.Replace(ipToValidate, @"[^\d.]", "");

            //if (String.Equals(cleanedIp.Trim(), localhost, StringComparison.InvariantCultureIgnoreCase)) 
            //    return true;// Special localhost case

            if (cleanedIp.Count(c => c == '.') != 3) 
                return false;

            IPAddress address;
            return IPAddress.TryParse(cleanedIp, out address);
        }
    }
}

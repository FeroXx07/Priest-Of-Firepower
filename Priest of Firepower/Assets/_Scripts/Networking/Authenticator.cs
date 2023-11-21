using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace _Scripts.Networking
{
    public abstract class Authenticator 
    {
        protected Authenticator(Socket client)
        {
            socketClientTcp = client;
            clientEndPointTcp = socketClientTcp.RemoteEndPoint as IPEndPoint;
        }
        
        #region Fields
        protected const string AuthenticationCode = "IM_VALID_USER_LOVE_ME";
        protected const string HandshakeOne = "HandshakeOne";
        protected const string AcknowledgmentOne = "AcknowledgmentOne";
        public IPEndPoint clientEndPointTcp { get; protected set; }
        public Socket socketClientTcp{ get; protected set; }
        protected AuthenticationState state;
        public Process process;
        public bool isAuthenticated => (state == AuthenticationState.CONFIRMED);
        #endregion
        
        protected enum AuthenticationState
        {
            REQUESTED,
            RESPONSE,
            CONFIRMED
        }

        public abstract void HandleAuthentication(MemoryStream stream, BinaryReader reader);
        
        protected void SerializeIPEndPoint(IPEndPoint endpoint, BinaryWriter writer)
        {
            // Serialize IP Address
            byte[] ipAddressBytes = endpoint.Address.GetAddressBytes();
            writer.Write(endpoint.Address.ToString());
            writer.Write(endpoint.Port);
        }
        protected IPEndPoint DeserializeIPEndPoint(BinaryReader reader)
        {
            // Deserialize IP Address
            string ipString = reader.ReadString();
            int tcpPort = reader.ReadInt32();
            IPAddress address = IPAddress.Any;
            if (!IPAddress.TryParse(ipString, out address))
            {
                Debug.LogError("Authenticator: Couldn't deserialize IEP!");
            }
            IPEndPoint ipEndPoint = new IPEndPoint(address, tcpPort);
            return ipEndPoint;
        }
    }
}

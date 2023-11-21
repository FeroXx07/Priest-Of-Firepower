using System;
using System.Net;
using System.Net.Sockets;

namespace _Scripts.Networking
{
    public enum ClientSate
    {
        CONNECTED,
        AUTHENTICATED,
        IN_GAME
    }
    public class ClientData
    {
        public ClientData()
        {
            
        }
        public ClientData(UInt64 id, string userName, IPEndPoint endPointTcp, IPEndPoint endPointUdp)
        {
            this.id = id;
            this.userName = userName;
            this.endPointTcp = endPointTcp;
            this.endPointUdp = endPointUdp;
        }

        public UInt64 id;
        public string userName = "NoName";
        public IPEndPoint endPointTcp;
        public IPEndPoint endPointUdp;
        public ClientSate state;
        public Socket connectionTcp;
        public Socket connectionUdp;

        public Process
            listenProcess;

        public bool isHost = false;
    }
}
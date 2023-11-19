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

    /// <summary>
    /// A class that holds information about the clients from the server side.
    /// </summary>
    public class ClientData
    {
        public ClientData(UInt64 id, string username, IPEndPoint endPointTcp, IPEndPoint endPointUdp)
        {
            this.id = id;
            this.username = username;
            this.endPointTcp = endPointTcp;
            this.endPointUdp = endPointUdp;
        }

        public UInt64 id;
        public string username = "";
        public IPEndPoint endPointTcp;
        public IPEndPoint endPointUdp;
        public ClientSate state;
        public Socket connectionTcp;
            
        public Process
            listenProcess;

        public bool isHost = false;
    }

    // public class ClientInfo
    // {
    //     public ClientInfo(UInt64 id, string username, IPEndPoint endPointTcp, IPEndPoint endPointUdp)
    //     {
    //         this.id = id;
    //         this.username = username;
    //         this.endPointTcp = endPointTcp;
    //         this.endPointUdp = endPointUdp;
    //     }
    //     public UInt64 id { get; private set; }
    //     public string username  { get; private set; }
    //     public IPEndPoint endPointTcp { get; private set; }
    //     public IPEndPoint endPointUdp { get; private set; }
    // }
}
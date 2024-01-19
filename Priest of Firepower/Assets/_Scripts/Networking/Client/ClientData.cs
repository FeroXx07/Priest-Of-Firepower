using System;
using System.Net;
using System.Net.Sockets;
using _Scripts.Networking.Utility;

namespace _Scripts.Networking.Client
{
    public enum ClientSate
    {
        DISCONNECTED,
        CONNECTED,
        AUTHENTICATED,
    }
    [Serializable]
    public class ClientData
    {
        public ClientData(string userName = "NoName")
        {
            id = ulong.MaxValue;
            this.userName = userName;
        }
        public ClientData(UInt64 id, string userName, IPEndPoint endPointTcp, IPEndPoint endPointUdp)
        {
            this.id = id;
            this.userName = userName;
            this.endPointTcp = endPointTcp;
            this.endPointUdp = endPointUdp;
        }

        public UInt64 id;
        public string userName;
        public IPEndPoint endPointTcp;
        public IPEndPoint endPointUdp;
        public ClientSate state = ClientSate.CONNECTED;
        public Socket connectionTcp;
        public Socket connectionUdp;

        public Process
            listenProcess;

        public bool isHost = false;
        public bool playerInstantiated = false;

        public long Ping;

        public System.Diagnostics.Stopwatch heartBeatStopwatch;
        #if UNITY_EDITOR
                public float disconnectTimeout = 12500;
        #else
                public float disconnectTimeout = 2000;
        #endif
    }
}
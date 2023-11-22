using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace _Scripts.Networking
{
    public class ServerAuthenticator : Authenticator
    {
        public ClientData clientBeingAuthenticated;

   
        public ServerAuthenticator(Socket tcp, Action<ClientData> onAuthenticationSuccessful, Action<IPEndPoint> onAuthenticationFailed) : base(tcp)
        {
            clientBeingAuthenticated = new ClientData();
            clientBeingAuthenticated.connectionTcp = tcp;
            clientBeingAuthenticated.endPointTcp = clientBeingAuthenticated.connectionTcp.RemoteEndPoint as IPEndPoint;
            clientEndPointTcp = socketTcp.RemoteEndPoint as IPEndPoint;
        }
        public override void HandleAuthentication(BinaryReader reader)
        {
            IPEndPoint localEndPointTcp  = DeserializeIPEndPoint(reader);
            
            if (!clientEndPointTcp.Equals(localEndPointTcp))
            {
                reader.BaseStream.Position = reader.BaseStream.Length;
                return;
            }
            
            state = (AuthenticationState)reader.ReadInt32();
            Debug.Log(state);
            MemoryStream authStream = new MemoryStream();
            BinaryWriter authWriter = new BinaryWriter(authStream);
            switch (state)
            {
                case AuthenticationState.REQUESTED:
                {
                    string clientCode = reader.ReadString();
                    bool isSuccess = (clientCode == AuthenticationCode);
                    
                    authWriter.Write((int)PacketType.AUTHENTICATION);
                    SerializeIPEndPoint(clientEndPointTcp, authWriter);
                    authWriter.Write((int)AuthenticationState.RESPONSE);
                    authWriter.Write(isSuccess);
                    authWriter.Write(clientBeingAuthenticated.id);
                    
                    Debug.Log($"Server Authenticator {clientBeingAuthenticated.connectionTcp.LocalEndPoint}: Request client to send response ");
                    clientBeingAuthenticated.connectionTcp.Send(authStream.ToArray());
                    //NetworkManager.Instance.AddReliableStreamQueue(authStream);
                }
                    break;
                case AuthenticationState.RESPONSE:
                {
                    string confirmation =  reader.ReadString();
                    if (confirmation.Equals(HandshakeOne))
                    {
                        Debug.Log($"Server Authenticator {clientBeingAuthenticated.connectionTcp.LocalEndPoint}:  Request client to send confirm ");
                        
                        UInt64 id = reader.ReadUInt64();
                        string userName = reader.ReadString();
                        string epTcp = reader.ReadString();
                        int portTcp = reader.ReadInt32();
                        string epUdp = reader.ReadString();
                        int portUdp = reader.ReadInt32();

                        clientBeingAuthenticated.userName = userName;
                        clientBeingAuthenticated.endPointUdp = new IPEndPoint(IPAddress.Parse(epUdp), portUdp);
                        
                        authWriter.Write((int)PacketType.AUTHENTICATION);
                        SerializeIPEndPoint(clientEndPointTcp, authWriter);
                        authWriter.Write((int)AuthenticationState.CONFIRMED);
                        clientBeingAuthenticated.connectionTcp.Send(authStream.ToArray());
                    }
                    else
                    {
                        Debug.Log($"Server Authenticator {clientBeingAuthenticated.connectionTcp.LocalEndPoint}: authentication response failed");
                    }
                }
                    break;
                case AuthenticationState.CONFIRMED:
                {
                    string ack =  reader.ReadString();
                    if (ack.Equals(AcknowledgmentOne))
                    {
                        Debug.Log($"Server Authenticator {clientBeingAuthenticated.connectionTcp.LocalEndPoint}:  authentication confirmation successful");
                        MainThreadDispatcher.EnqueueAction(NetworkManager.Instance.OnClientConnected);
                    }
                    else
                    {
                        Debug.Log($"Server Authenticator {clientBeingAuthenticated.connectionTcp.LocalEndPoint}:  authentication confirmation failed");
                    }
                }
                    break;
            }
        }

        public void RequestClientToStartAuthentication()
        {
            MemoryStream authStream = new MemoryStream();
            BinaryWriter authWriter = new BinaryWriter(authStream);
            authWriter.Write((int)PacketType.AUTHENTICATION);
            SerializeIPEndPoint(clientEndPointTcp, authWriter);
            authWriter.Write((int)AuthenticationState.REQUESTED);
            clientBeingAuthenticated.connectionTcp.Send(authStream.ToArray());
            Debug.Log($"Server Authenticator: Request Client To Start Authentication!");
        }
    }
}

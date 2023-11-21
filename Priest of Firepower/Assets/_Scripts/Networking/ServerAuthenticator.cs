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
        public Action<ClientData> onAuthenticationSuccessful;
        public Action<IPEndPoint> onAuthenticationFailed;
   
        public ServerAuthenticator(Socket tcp, Action<ClientData> onAuthenticationSuccessful, Action<IPEndPoint> onAuthenticationFailed) : base(tcp)
        {
            this.onAuthenticationSuccessful += onAuthenticationSuccessful;
            this.onAuthenticationFailed += onAuthenticationFailed;
            clientBeingAuthenticated = new ClientData();
            clientBeingAuthenticated.connectionTcp = tcp;
            clientBeingAuthenticated.endPointTcp = clientBeingAuthenticated.connectionTcp.RemoteEndPoint as IPEndPoint;
        }
        public override void HandleAuthentication(MemoryStream stream, BinaryReader reader)
        {
            IPEndPoint localEndPointTcp  = DeserializeIPEndPoint(reader);
            
            if (!clientEndPointTcp.Equals(localEndPointTcp))
            {
                reader.BaseStream.Position = reader.BaseStream.Length;
                return;
            }
            
            state = (AuthenticationState)reader.ReadInt32();
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
                    
                    Debug.Log($"Server Authenticator {localEndPointTcp}: Replying authentication request");
                    clientBeingAuthenticated.connectionTcp.Send(stream.ToArray());
                    //NetworkManager.Instance.AddReliableStreamQueue(authStream);
                }
                    break;
                case AuthenticationState.RESPONSE:
                {
                    string confirmation =  reader.ReadString();
                    if (confirmation.Equals(HandshakeOne))
                    {
                        Debug.Log($"Server Authenticator {localEndPointTcp}: Replying authentication response");
                        
                        UInt64 id = reader.ReadUInt64();
                        string userName = reader.ReadString();
                        string epTcp = reader.ReadString();
                        int portTcp = reader.ReadInt32();
                        string epUdp = reader.ReadString();
                        int portUdp = reader.ReadInt32();
                        
                        authWriter.Write((int)PacketType.AUTHENTICATION);
                        SerializeIPEndPoint(clientEndPointTcp, authWriter);
                        authWriter.Write((int)AuthenticationState.CONFIRMED);
                        clientBeingAuthenticated.connectionTcp.Send(stream.ToArray());
                        //NetworkManager.Instance.AddReliableStreamQueue(authStream);
                    }
                    else
                    {
                        Debug.Log($"Server Authenticator {localEndPointTcp}: Replying authentication response failed");
                        onAuthenticationFailed?.Invoke(localEndPointTcp);
                    }
                }
                    break;
                case AuthenticationState.CONFIRMED:
                {
                    string ack =  reader.ReadString();
                    if (ack.Equals(AcknowledgmentOne))
                    {
                        Debug.Log($"Server Authenticator {localEndPointTcp}: Replying authentication confirmation");
                        onAuthenticationSuccessful?.Invoke(clientBeingAuthenticated);
                    }
                    else
                    {
                        Debug.Log($"Server Authenticator {localEndPointTcp}: Replying authentication confirmation failed");
                        onAuthenticationFailed?.Invoke(localEndPointTcp);
                    }
                }
                    break;
            }
        }
    }
}

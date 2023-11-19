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
        private UInt64 id = 0;
        private string userName = "none";
        private string epTcp = "none";
        private int portTcp = 0;
        public ServerAuthenticator(Socket client, Action<ClientData> onAuthenticationSuccessful, Action<IPEndPoint> onAuthenticationFailed) : base(client, onAuthenticationSuccessful, onAuthenticationFailed)
        {
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
                    Debug.Log($"Authentication {localEndPointTcp}: Replying authentication request");
                    NetworkManager.Instance.AddReliableStreamQueue(authStream);
                }
                    break;
                case AuthenticationState.RESPONSE:
                {
                    string confirmation =  reader.ReadString();
                    if (confirmation.Equals(HandshakeOne))
                    {
                        Debug.Log($"Authentication {localEndPointTcp}: Replying authentication response");
                        
                        id = reader.ReadUInt64();
                        userName = reader.ReadString();
                        epTcp = reader.ReadString();
                        portTcp = reader.ReadInt32();
                        string epUdp = reader.ReadString();
                        int portUdp = reader.ReadInt32();
                        
                        authWriter.Write((int)PacketType.AUTHENTICATION);
                        SerializeIPEndPoint(clientEndPointTcp, authWriter);
                        authWriter.Write((int)AuthenticationState.CONFIRMED);
                        NetworkManager.Instance.AddReliableStreamQueue(authStream);
                    }
                    else
                    {
                        Debug.Log($"Authentication {localEndPointTcp}: Replying authentication response failed");
                        onAuthenticationFailed?.Invoke(localEndPointTcp);
                    }
                }
                    break;
                case AuthenticationState.CONFIRMED:
                {
                    string ack =  reader.ReadString();
                    if (ack.Equals(AcknowledgmentOne))
                    {
                        Debug.Log($"Authentication {localEndPointTcp}: Replying authentication confirmation");
                        onAuthenticationSuccessful?.Invoke(new ClientData(2, "22", localEndPointTcp, localEndPointTcp));
                    }
                    else
                    {
                        Debug.Log($"Authentication {localEndPointTcp}: Replying authentication confirmation failed");
                        onAuthenticationFailed?.Invoke(localEndPointTcp);
                    }
                }
                    break;
            }
        }
    }
}

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using _Scripts.Networking.Client;
using UnityEngine;

namespace _Scripts.Networking.Authentication
{
    public class ClientAuthenticator : Authenticator
    {
        private ClientData _clientData;
        public Action onAuthenticationSuccessful;
        public Action onAuthenticationFailed;
        public ClientAuthenticator(ClientData clientData, Socket clientSocketTcp, Action onAuthenticationSuccessful, Action onAuthenticationFailed) : base(clientSocketTcp)
        {
            _clientData = clientData;
            this.onAuthenticationSuccessful += onAuthenticationSuccessful;
            this.onAuthenticationFailed += onAuthenticationFailed;
            clientEndPointTcp = socketTcp.LocalEndPoint as IPEndPoint;
        }
        public override void HandleAuthentication(BinaryReader reader)
        {
            IPEndPoint localEndPointTcp = DeserializeIPEndPoint(reader);
            
            if (!clientEndPointTcp.Equals(localEndPointTcp))
            {
                reader.BaseStream.Position = reader.BaseStream.Length;
                return;
            }

            state = (AuthenticationState)reader.ReadInt32();
            Debug.Log(state);
            MemoryStream authStream = new MemoryStream();
            BinaryWriter authWriter = new BinaryWriter(authStream);
            switch(state)
            {
                case AuthenticationState.REQUESTED:
                {
                    SerializeIPEndPoint(_clientData.connectionTcp.LocalEndPoint as IPEndPoint,authWriter);
                    authWriter.Write((int)AuthenticationState.REQUESTED);
                    authWriter.Write(AuthenticationCode);
                    Debug.Log($"Client Authenticator {_clientData.connectionTcp.LocalEndPoint}: Responding authentication request");
                }
                    break;
                case AuthenticationState.RESPONSE:
                {
                    bool isSuccess = reader.ReadBoolean();
                    _clientData.id = reader.ReadUInt64();
                    
                    if (isSuccess)
                    {
                        Debug.Log($"Client Authenticator {localEndPointTcp}: Responding authentication response");
                        
                        // Create an authentication packet
                        SerializeIPEndPoint(localEndPointTcp,authWriter);
                        authWriter.Write((int)AuthenticationState.RESPONSE);
                        authWriter.Write(HandshakeOne);
                        authWriter.Write(_clientData.id);
                        authWriter.Write(_clientData.userName);
                        authWriter.Write(_clientData.endPointTcp.Address.ToString());
                        authWriter.Write(_clientData.endPointTcp.Port);
                        authWriter.Write(_clientData.endPointUdp.Address.ToString());
                        authWriter.Write(_clientData.endPointUdp.Port);
                    }
                    else
                    {
                        Debug.LogError($"Client Authenticator {localEndPointTcp}: has failed!");
                    }
                }
                    break;
                case AuthenticationState.CONFIRMED:
                {
                    SerializeIPEndPoint(localEndPointTcp,authWriter);
                    authWriter.Write((int)AuthenticationState.CONFIRMED);
                    authWriter.Write(AcknowledgmentOne);
                    UnityMainThreadDispatcher.Dispatcher.Enqueue(onAuthenticationSuccessful);
                    Debug.Log($"Client Authenticator {localEndPointTcp}: Responding authentication confirmation");
                }
                    break;
            }
            
            Packet authPacket = new Packet(PacketType.AUTHENTICATION, ulong.MinValue, ulong.MinValue, long.MinValue,
                Int32.MinValue, authStream.ToArray());
            _clientData.connectionTcp.Send(authPacket.allData);
        }
    }
}

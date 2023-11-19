using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace _Scripts.Networking
{
    public class ClientAuthenticator : Authenticator
    {
        private ClientData _clientData;
        public ClientAuthenticator(ClientData clientData, Socket client, Action<ClientData> onAuthenticationSuccessful, Action<IPEndPoint> onAuthenticationFailed) : base(client, onAuthenticationSuccessful, onAuthenticationFailed)
        {
            _clientData = clientData;
        }
        public override void HandleAuthentication(MemoryStream stream, BinaryReader reader)
        {
            IPEndPoint localEndPointTcp = DeserializeIPEndPoint(reader);
            
            if (!clientEndPointTcp.Equals(localEndPointTcp))
            {
                reader.BaseStream.Position = reader.BaseStream.Length;
                return;
            }

            state = (AuthenticationState)reader.ReadInt32();
            MemoryStream authStream = new MemoryStream();
            BinaryWriter authWriter = new BinaryWriter(authStream);
            switch(state)
            {
                case AuthenticationState.REQUESTED:
                {
                    // Create an authentication packet
                    authWriter.Write((int)PacketType.AUTHENTICATION);
                    SerializeIPEndPoint(localEndPointTcp,authWriter);
                    authWriter.Write((int)AuthenticationState.REQUESTED);
                    authWriter.Write(AuthenticationCode);
                    Debug.Log($"Authentication {localEndPointTcp}: Starting authentication request");
                    NetworkManager.Instance.AddReliableStreamQueue(authStream);
                }
                    break;
                case AuthenticationState.RESPONSE:
                {
                    bool isSuccess = reader.ReadBoolean();
                    if (isSuccess)
                    {
                        Debug.Log($"Authentication {localEndPointTcp}: Sending authentication response");
                        
                        // Create an authentication packet
                        authWriter.Write((int)PacketType.AUTHENTICATION);
                        SerializeIPEndPoint(localEndPointTcp,authWriter);
                        authWriter.Write((int)AuthenticationState.RESPONSE);
                        authWriter.Write(HandshakeOne);
                        authWriter.Write(_clientData.id);
                        authWriter.Write(_clientData.username);
                        authWriter.Write(_clientData.endPointTcp.Address.ToString());
                        authWriter.Write(_clientData.endPointTcp.Port);
                        authWriter.Write(_clientData.endPointUdp.Address.ToString());
                        authWriter.Write(_clientData.endPointUdp.Port);
                        
                        NetworkManager.Instance.AddReliableStreamQueue(authStream);
                    }
                    else
                    {
                        Debug.LogError($"Authentication {localEndPointTcp}: has failed!");
                    }
                }
                    break;
                case AuthenticationState.CONFIRMED:
                {
                    authWriter.Write((int)PacketType.AUTHENTICATION);
                    SerializeIPEndPoint(localEndPointTcp,authWriter);
                    authWriter.Write((int)AuthenticationState.CONFIRMED);
                    authWriter.Write(AcknowledgmentOne);
                    NetworkManager.Instance.AddReliableStreamQueue(authStream);
                    onAuthenticationSuccessful?.Invoke(_clientData);
                }
                    break;
            }
        }
    }
}

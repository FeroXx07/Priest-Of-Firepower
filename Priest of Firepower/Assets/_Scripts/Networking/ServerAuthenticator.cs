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
        private string _authenticationCode = "IM_VALID_USER_LOVE_ME";
        public Action<IPEndPoint, string> _onAuthenticated;
        public Action<IPEndPoint> _onAuthenticationFailed;
        IPEndPoint _endPointTmp;//stores temporal endpoint to retrun message

        AuthenticationState _state; 
        public void HandleAuthentication(MemoryStream stream, BinaryReader reader)
        {
            // check who is sending the request
            _endPointTmp = DeserializeIPEndPoint(reader);

            //get in what state is the authoritation process
            _state = (AuthenticationState)reader.ReadInt32();

            switch (_state)
            {
                case AuthenticationState.REQUESTED:              

                    // Receive username and code from the client
                    string username = reader.ReadString();
                    string clientCode = reader.ReadString();

                    // Validate the received code
                    bool isSuccess = (clientCode == _authenticationCode);

                    // Send authentication response to the client
                    SendAuthenticationResponse(isSuccess);

                    if (isSuccess)
                    {
                        Debug.Log($"Authentication successful for client: {username}");
                        _onAuthenticated?.Invoke(_endPointTmp, username);
                    }
                    else
                    {
                        Debug.Log("Authentication failed!");
                        _onAuthenticationFailed?.Invoke(_endPointTmp);
                    }

                    
                    break;
                case AuthenticationState.CONFIRMATION:
                    //hum

                    break;
                default:
                    break;
           
            }
            _endPointTmp = null;
        }
        //create new functions to send messages
        private void SendAuthenticationResponse(bool isSuccess)
        {
            // Create an authentication response packet
            MemoryStream responseStream = new MemoryStream();
            BinaryWriter responseWriter = new BinaryWriter(responseStream);

            responseWriter.Write((int)PacketType.AUTHENTICATION);
            SerializeIPEndPoint(_endPointTmp, responseWriter);
            responseWriter.Write((int)AuthenticationState.CONFIRMATION);
            responseWriter.Write(isSuccess);
            responseWriter.Write((int)_state);

            NetworkManager.Instance.AddReliableStreamQueue(responseStream);
        }
        
    }
}

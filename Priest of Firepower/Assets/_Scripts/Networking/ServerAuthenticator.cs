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

            Debug.Log(_state);

            switch (_state)
            {
                case AuthenticationState.REQUESTED:              

                    // Receive username and code from the client
                    string clientCode = reader.ReadString();

                    // Validate the received code
                    bool isSuccess = (clientCode == _authenticationCode);

                    // Send authentication response to the client
                    SendAuthenticationResponse(isSuccess);

                    break;
                case AuthenticationState.CONFIRMATION:
                    //hum
                    string confirmation =  reader.ReadString();
                    string username = reader.ReadString();
                    if (confirmation == "ok")
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
  
            Debug.Log("Sending success:" + isSuccess);

            NetworkManager.Instance.AddReliableStreamQueue(responseStream);
        }
        
    }
}

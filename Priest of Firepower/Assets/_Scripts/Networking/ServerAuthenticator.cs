using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

namespace _Scripts.Networking
{
    public class ServerAuthenticator : MonoBehaviour
    {
        private string _authenticationCode = "IM_VALID_USER_LOVE_ME";
        Action<string> _onAuthenticated;

    
        public void HandleAuthentication(MemoryStream stream, BinaryReader reader)
        {

            Debug.Log("authentication data");
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
                _onAuthenticated?.Invoke(username);
            }
            else
            {
                Debug.Log("Authentication failed!");
            }
        }
        //create new functions to send messages
        private void SendAuthenticationResponse(bool isSuccess)
        {
            // Create an authentication response packet
            MemoryStream responseStream = new MemoryStream();
            BinaryWriter responseWriter = new BinaryWriter(responseStream);


            responseWriter.Write((int)PacketType.AUTHENTICATION);
            responseWriter.Write(isSuccess);


            NetworkManager.Instance.AddReliableStreamQueue(responseStream);
        }

        public void RegisterSocket(Socket socket)
        {

        }
    }
}

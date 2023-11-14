using System.IO;
using System.Net;
using UnityEngine;

namespace _Scripts.Networking
{
    public class ClientAuthenticator : Authenticator
    {
        private string _authenticationCode = "IM_VALID_USER_LOVE_ME";
        private bool _authenticated = false;
        private IPEndPoint endPoint;
        AuthenticationState _state = AuthenticationState.REQUESTED;
        public void HandleAuthentication(MemoryStream stream, BinaryReader reader)
        {
            IPEndPoint enpoint = GetIPEndPoint(reader);
            //get the end point if it is the same then the message is for this client 

            if (!endPoint.Equals(enpoint))
            {
                reader.BaseStream.Position = reader.BaseStream.Length;
                return;
            }

            _state = (AuthenticationState)reader.ReadInt32();


            switch(_state)
            {
                case AuthenticationState.REQUESTED:

                    break;
                case AuthenticationState.CONFIRMATION:
                    //check response to the authorization request
                    bool isSuccess = reader.ReadBoolean();

                    if (isSuccess)
                    {
                        Debug.Log("Authentication successful!");
                        _authenticated = true;
                        ConfirmSuccess();
                    }
                    else
                    {
                        Debug.Log("Authentication failed!");
                    }
                    break;
                default:
                    break;
            }
        }
        //send a request with the state(request) username and endpoint(id)
        public void SendAuthenticationRequest(string username)
        {
            // Create an authentication packet
            MemoryStream authStream = new MemoryStream();
            BinaryWriter authWriter = new BinaryWriter(authStream);

            authWriter.Write((int)PacketType.AUTHENTICATION);

            SendIPEndPoint(endPoint,authWriter);

            authWriter.Write((int)_state);

            authWriter.Write(username);
            authWriter.Write(_authenticationCode);

            Debug.Log("Client: Starting authetication request ...");

            NetworkManager.Instance.AddReliableStreamQueue(authStream);
        }

        public void ConfirmSuccess()
        {
            // Create an authentication packet
            MemoryStream authStream = new MemoryStream();
            BinaryWriter authWriter = new BinaryWriter(authStream);

            authWriter.Write((int)PacketType.AUTHENTICATION);
            SendIPEndPoint(endPoint,authWriter);
            authWriter.Write((int)_state);
            authWriter.Write("ok");

            NetworkManager.Instance.AddReliableStreamQueue(authStream);
        }

        public bool Authenticated()
        {
            return _authenticated;
        }

        public void SetEndPoint(IPEndPoint endpoint)
        {
            endPoint = endpoint;           
        }
    }
}

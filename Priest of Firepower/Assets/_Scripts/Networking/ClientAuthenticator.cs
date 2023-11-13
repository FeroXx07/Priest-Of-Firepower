using System.IO;
using UnityEngine;

namespace _Scripts.Networking
{
    public class ClientAuthenticator : MonoBehaviour
    {
        private string authenticationCode = "IM_VALID_USER_LOVE_ME";

        public void HandleAuthentication(MemoryStream stream, BinaryReader reader)
        {
            bool isSuccess = reader.ReadBoolean();

            if (isSuccess)
            {
                Debug.Log("Authentication successful!");
            }
            else
            {
                Debug.Log("Authentication failed!");
            }
        }
        //create new functions to send messages
        public void SendAuthenticationRequest(string username)
        {
            // Create an authentication packet
            MemoryStream authStream = new MemoryStream();
            BinaryWriter authWriter = new BinaryWriter(authStream);

            authWriter.Write((int)PacketType.AUTHENTICATION);
            authWriter.Write(username);
            authWriter.Write(authenticationCode);

            Debug.Log("Client: Starting authetication request ...");

            NetworkManager.Instance.AddReliableStreamQueue(authStream);
        }
    }
}

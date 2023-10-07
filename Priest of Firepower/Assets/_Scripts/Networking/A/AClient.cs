using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

namespace ClientA
{
    public class AClient : MonoBehaviour
    {
        IPEndPoint endPoint;
        [SerializeField] string IPaddress;

        private CancellationTokenSource tokenSource;
        private Task senderTask;

        public void Connect()
        {
            tokenSource = new CancellationTokenSource();
            senderTask = SenderTCP(tokenSource.Token);
        }

        async Task SenderTCP(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log("Creating connetion");
                Socket sender = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
                //for the local endpoint, which is typically not what you'd use for a client socket.
                //For a client socket, you should specify the IP address and port of the server you want to connect to.
                IPAddress serverIP = IPAddress.Parse(IPaddress);
                int serverPort = 12345; // Replace with your server's port
                endPoint = new IPEndPoint(serverIP, serverPort);
               

                try
                {
                    await sender.ConnectAsync(endPoint);

                    if (sender.Connected)
                    {
                        Debug.Log("Socket connected to -> " + sender.RemoteEndPoint.ToString());

                        // Continue with sending and receiving data
                    }
                    else
                    {
                        Debug.LogError("Socket connection failed.");
                        return; // Handle the failure or return from the method
                    }


                    byte[] sendBytes = Encoding.ASCII.GetBytes("hello from clientA");
                    await  sender.SendAsync(new ArraySegment<byte>(sendBytes),SocketFlags.None);

                    byte[] receivedBytes = new byte[1024];
                    string data = null;
                    while (true)
                    {
                        // Check the cancellation token before receiving
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break; // Exit if cancellation is requested
                        }

                        int bufferSize = await sender.ReceiveAsync(new ArraySegment<byte>(receivedBytes), SocketFlags.None);

                        data += Encoding.ASCII.GetString(receivedBytes, 0, bufferSize);

                        if (data.IndexOf("<EOF>") > -1)
                            break;
                    }
                    Debug.Log("Text received -> " + data);

                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                catch (ArgumentNullException ane)
                {

                    Debug.Log("ArgumentNullException : "+ ane.ToString());
                }
                catch (SocketException se)
                {
                    Debug.LogError("SocketException : " + se.SocketErrorCode); // Log the error code
                    Debug.LogError("SocketException : " + se.Message); // Log the error message

                    // Handle the exception based on the error code
                    switch (se.SocketErrorCode)
                    {
                        case SocketError.ConnectionRefused:
                            // The server refused the connection (e.g., server not running)
                            Debug.LogError("Connection to the server was refused.");
                            break;

                        case SocketError.HostNotFound:
                            // The hostname couldn't be resolved
                            Debug.LogError("Host not found. Check the server's IP address.");
                            break;

                        default:
                            // Handle other error cases as needed
                            Debug.LogError("Unknown socket error.");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Unexpected exception : "+ e.ToString());
                }

            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        //bind() is used to bind a socket to a specific address and port,
        //making the socket listen for incoming connection requests on that address and port,
        //while connect() is used by a client to initiate a connection to a server.
        void SendTCP()
        {

            try
            {
                Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream,ProtocolType.Tcp);

                //If the port number doesn't matter you could pass 0 for the port to the IPEndPoint.
                //In this case the operating system (TCP/IP stack) assigns a free port number for you.
                endPoint = new IPEndPoint(IPAddress.Any, 0);
                // We print EndPoint information 
                // that we are connected
                Debug.Log("Socket connected to -> "+sender.RemoteEndPoint.ToString());
                try
                {
                    //connect socket to the end point
                    sender.Connect(endPoint);
                    // Creation of message that
                    // we will send to Server
                    byte[] sendBytes = Encoding.ASCII.GetBytes("Hello there! from clientA");
                    sender.SendTo(sendBytes, sendBytes.Length, SocketFlags.None, endPoint);


                    byte[] dataBuffer = new byte[1024];

                    // get the data size and fill the databuffer
                    int bytesRecived =  sender.Receive(dataBuffer);
                    Debug.Log(Encoding.ASCII.GetString(dataBuffer,0, bytesRecived));


                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                }
                catch (ArgumentNullException ane) {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                 catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        void SendUDP()
        {
            try
            {
                Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                endPoint = new IPEndPoint(IPAddress.Any, 0);
                // We print EndPoint information 
                // that we are connected
                Debug.Log("Socket connected to -> " + sender.RemoteEndPoint.ToString());


            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
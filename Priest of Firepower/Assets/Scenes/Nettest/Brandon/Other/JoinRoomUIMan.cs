using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class JoinRoomUIMan : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI username;
    [SerializeField]
    TextMeshProUGUI serverIp;

    [SerializeField]
    Br_UDP_Client udpClient;
    [SerializeField]
    Br_TCP_Client tcpClient;

    public void JoinRoom()
    {
        if (username.text != "")
        {
            udpClient.SetUsername(username.text);
            udpClient.SetServerIp(serverIp.text);

            tcpClient.SetUsername(username.text);
            tcpClient.SetServerIp(serverIp.text);

            Br_IJoinRoomUI.OnJoinRoom?.Invoke();

        }
    }
}

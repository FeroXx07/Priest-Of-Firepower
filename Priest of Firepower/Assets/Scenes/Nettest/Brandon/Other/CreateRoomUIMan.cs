using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class CreateRoomUIMan : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI serverName;

    [SerializeField]
    Br_UDP_Server udpServer;
    [SerializeField]
    Br_TCP_Server tcpServer;

    public void CreateRoom()
    {
        udpServer.SetRoomName(serverName.text);
        tcpServer.SetRoomName(serverName.text);
        Br_ICreateRoomUI.OnCreateRoom?.Invoke();
    }
}

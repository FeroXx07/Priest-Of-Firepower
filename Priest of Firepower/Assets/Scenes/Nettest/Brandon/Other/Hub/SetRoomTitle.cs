using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class SetRoomTitle : MonoBehaviour
{
    [SerializeField]
    Br_TCP_Server tcpServer;
    [SerializeField]
    Br_UDP_Server udpServer;
    [SerializeField]
    TextMeshProUGUI roomTitleText;
    private void Awake()
    {
        roomTitleText = gameObject.GetComponent<TextMeshProUGUI>();
        GameObject serverGO = GameObject.Find("server");
        if (serverGO != null)
        {
            tcpServer = serverGO.GetComponent<Br_TCP_Server>();
            udpServer = serverGO.GetComponent<Br_UDP_Server>();

            if (tcpServer.enabled) roomTitleText.text = tcpServer.GetRoomName();
            if (udpServer.enabled) roomTitleText.text = udpServer.GetRoomName();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UDP_TCP_change_server : MonoBehaviour
{
    public Toggle toggle;
    public Text toggleText;
    public Br_UDP_Server udpServer;
    public Br_TCP_Server tcpServer;

    bool useUDP = true;

    private void OnEnable()
    {
        ChangeProtocol();
    }
    public void ChangeProtocol()
    {
        useUDP = toggle.isOn;


        if (useUDP)
        {
            udpServer.enabled = true;
            tcpServer.enabled = false;
            toggleText.text = "Using UDP";
        }
        else
        {
            udpServer.enabled = false;
            tcpServer.enabled = true;
            toggleText.text = "Using TCP";
        }
    }
}

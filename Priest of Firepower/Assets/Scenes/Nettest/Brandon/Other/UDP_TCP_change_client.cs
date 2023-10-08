using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UDP_TCP_change_client : MonoBehaviour
{
    public Toggle toggle;
    public Text toggleText;
    public Br_UDP_Client udpClient;
    public Br_TCP_Client tcpClient;

    bool useUDP = false;
    private void OnEnable()
    {
        ChangeProtocol();
    }
    public void ChangeProtocol()
    {
        useUDP = toggle.isOn;

        if (useUDP)
        {
            udpClient.enabled = true;
            tcpClient.enabled = false;
            toggleText.text = "Using UDP";
        }
        else
        {
            udpClient.enabled = false;
            tcpClient.enabled = true;
            toggleText.text = "Using TCP";
        }
    }
}

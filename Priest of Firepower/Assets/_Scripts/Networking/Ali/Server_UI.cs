using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Server_UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textServerIP;

    public Server_TCP serverTCP;

    private void Awake()
    {
        SetIPText("server_ip");
        TryGetComponent<Server_TCP>(out serverTCP);
    }

    private void OnEnable()
    {
        if (serverTCP != null)
        {
            serverTCP.OnServerIPAssignated += SetIPText;
        }
    }

    private void OnDisable()
    {
        if (serverTCP != null)
        {
            serverTCP.OnServerIPAssignated -= SetIPText;
        }
    }

    private void SetIPText(string newIP)
    {
        _textServerIP.text = $"Your server IP is <color=green>{newIP}</color> with envy";
    }
}

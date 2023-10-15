using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Server_UI : MonoBehaviour
{
    [SerializeField] private TMP_Text _textServerIP;
    [SerializeField] private TMP_InputField _inputFieldServerName;
    [SerializeField] private Button _createGameButton;
    
    public Server_TCP serverTCP;

    private void Awake()
    {
        SetIPText("server_ip");
    }

    private void OnEnable()
    {
        if (serverTCP != null)
        {
            serverTCP.OnServerIPAssignated += SetIPText;
            _inputFieldServerName.onEndEdit.AddListener(SetServerName);
            _createGameButton.onClick.AddListener(OnCreateGamePressed);
        }
    }

    void OnDisable()
    {
        if (serverTCP != null)
        {
            serverTCP.OnServerIPAssignated -= SetIPText;
            _inputFieldServerName.onEndEdit.RemoveListener(SetServerName);
            _createGameButton.onClick.RemoveListener(OnCreateGamePressed);
        }
    }

    void SetIPText(string newIP)
    {
        _textServerIP.text = $"Your server IP is <color=green>{newIP}</color> with envy";
    }

    void SetServerName(string newName)
    {
        serverTCP.serverName = newName;
    }

    void OnCreateGamePressed()
    {
        serverTCP.TriggerCreateGame();
    }
}

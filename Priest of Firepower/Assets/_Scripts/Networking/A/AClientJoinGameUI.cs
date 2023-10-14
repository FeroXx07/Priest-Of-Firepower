using ClientA;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;

public class AClientJoinGameUI : MonoBehaviour
{
    [SerializeField]TMP_InputField ipInputField;

    private void Awake()
    {
        ipInputField.onEndEdit.AddListener(ConnectToServer);
    }

    private void ConnectToServer(string ip)
    {
        IPAddress adddress = IPAddress.Parse(ip);
        if (adddress != null)
        { 
            AClient.Instance.Connect(adddress);
        }
        else
        {
            Debug.LogError(ip +" address is not valid ...");
        }
    }
}

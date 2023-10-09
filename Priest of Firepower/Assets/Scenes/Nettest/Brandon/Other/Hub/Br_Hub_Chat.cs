using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Br_Hub_Chat : MonoBehaviour
{
    [SerializeField]
    GameObject chatMessagePrefab;
    [SerializeField]
    TMP_InputField inputFieldText;
    [SerializeField]
    GameObject chatMessageContent;
    GameObject server;
    GameObject client;
    private void OnEnable()
    {
        server = GameObject.Find("server");
        client = GameObject.Find("client");

        if (client != null) Br_IServer.OnSendMessageToClient += ReceiveMessage;
        if (server != null) Br_IServer.OnSendMessageToServer += SendMessageToServer;
    }

    public void SendChatMessage()
    {
        if (inputFieldText.text != "")
        {
            GameObject chatMessage = Instantiate(chatMessagePrefab);
           if (server != null) chatMessage.GetComponent<TextMeshProUGUI>().text = "Server: " + inputFieldText.text;
           if (client != null) chatMessage.GetComponent<TextMeshProUGUI>().text = inputFieldText.text;


            chatMessage.transform.parent = chatMessageContent.transform;


            if (client != null) SendMessageToServer(inputFieldText.text);
            if (server != null) SendMessageToClient(inputFieldText.text);

            inputFieldText.SetTextWithoutNotify("");
        }
    }

    void ReceiveMessage(string message)
    {
        GameObject chatMessage = Instantiate(chatMessagePrefab);
        chatMessage.GetComponent<TextMeshProUGUI>().text = message;
        chatMessage.transform.parent = chatMessageContent.transform;
    }


    void SendMessageToServer(string message)
    {
        Br_IServer.OnSendMessageToServer?.Invoke(message);
    }

    void SendMessageToClient(string message)
    {
        Br_IServer.OnSendMessageToClient?.Invoke(message);
    }

}

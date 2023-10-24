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

        //entered as client
        if (client != null)
        {
            //Br_IServer.OnSendMessageToServer += SendMessageToServer;
            Br_IServer.OnReceiveMessageFromServer += ReceiveMessage;

        }

        //entered as server
        if (server != null)
        {
            //Br_IServer.OnSendMessageToClient += SendMessageToClient;
            Br_IServer.OnReceiveMessageFromClient += ReceiveMessage;

        }
    }

    public void SendChatMessage()
    {
        if (inputFieldText.text != "")
        {
            GameObject chatMessage = Instantiate(chatMessagePrefab);
            if (server != null) chatMessage.GetComponent<TextMeshProUGUI>().text = inputFieldText.text;
            if (client != null) chatMessage.GetComponent<TextMeshProUGUI>().text = inputFieldText.text;


            chatMessage.transform.parent = chatMessageContent.transform;


            if (client != null) SendMessageToServer(inputFieldText.text);
            if (server != null) SendMessageToClient(inputFieldText.text);

            inputFieldText.SetTextWithoutNotify("");
        }
    }

    void ReceiveMessage(string message)
    {
        print("creating message: " + message);
        GameObject chatMessage = Instantiate(chatMessagePrefab);
        chatMessage.GetComponent<TextMeshProUGUI>().text = message;
        chatMessage.transform.parent = chatMessageContent.transform;
    }


    void SendMessageToServer(string message)
    {
        print("sending message to server: " + message);
        Br_IServer.OnSendMessageToServer?.Invoke(message);
    }

    void SendMessageToClient(string message)
    {
        print("sending message to clients: " + message);
        Br_IServer.OnSendMessageToClient?.Invoke(message);
    }

}

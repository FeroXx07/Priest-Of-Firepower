using ClientA;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientInputHandelerUI : MonoBehaviour
{
    [SerializeField] TMP_Text connectionInfo;
    [SerializeField]TMP_InputField ipInputField,mesageInputField;
    [SerializeField] GameObject chatPanel;
    [SerializeField] GameObject chatTextElementUi;
    [SerializeField] int maxMessages = 25;
    List<MSG> messages = new List<MSG>();

    void Start()
    {
        if(ipInputField != null)
        ipInputField.onEndEdit.AddListener(OnConnect);
        if(mesageInputField != null)
        mesageInputField.onSubmit.AddListener(OnSendMessage);
        if (chatPanel == null)
        {
            Debug.LogError("chatPanel is not assigned in the Inspector.");
        }

    }
    private void OnEnable()
    {
        AClient.Instance.OnMessageRecived += OnMessageRecived;
        AClient.Instance.OnConnected += OnConnected;
    }
    private void OnDisable()
    {
        AClient.Instance.OnMessageRecived -= OnMessageRecived;
        AClient.Instance.OnConnected -= OnConnected;
    }
    private void OnConnect(string text)
    {
        IPAddress address = IPAddress.Parse(text);
        if(address != null)
        {
            AClient.Instance.Connect(address);
        }else
        {
            Debug.LogError(text + " IP not valid");
        }
    }

    private void OnSendMessage(string text)
    {
        if (text != "")
        {
            AClient.Instance.SendMessageUI(text);
            OnMessageRecived(text);
            mesageInputField.text = "";
        }
    }

    void OnMessageRecived(string text)
    {
        Debug.Log("post message: " +text);
        if(messages.Count > maxMessages)
        {
            Destroy(messages[0].textObj.gameObject);
            messages.Remove(messages[0]);
        }

        GameObject textObj = Instantiate(chatTextElementUi,chatPanel.transform);

        MSG newMsg = new MSG();

        newMsg.textObj = textObj.GetComponent<TMP_Text>();
        newMsg.text = text;
        newMsg.textObj.text = text;

        messages.Add(newMsg);

    }

    private void OnConnected()
    {
        connectionInfo.text = "Connected to:\n"+AClient.Instance.GetIpAddress();
    }

    class MSG
    {
        public string text;
        public TMP_Text textObj;
    }
}

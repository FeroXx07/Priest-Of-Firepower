using ClientA;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;

public class AClientChatHandelerUI : MonoBehaviour
{
    [SerializeField] TMP_InputField mesageInputField;
    [SerializeField] GameObject chatPanel;
    [SerializeField] GameObject chatTextElementUi;
    [SerializeField] int maxMessages = 25;
    List<MSG> messages = new List<MSG>();

    void Start()
    {
        if (mesageInputField != null)
            mesageInputField.onSubmit.AddListener(OnSendMessage);
        if (chatPanel == null)
        {
            Debug.LogError("chatPanel is not assigned in the Inspector.");
        }

    }
    private void OnEnable()
    {
        AClient.Instance.OnMessageRecived += OnMessageRecived;
    }
    private void OnDisable()
    {
        AClient.Instance.OnMessageRecived -= OnMessageRecived;
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
        Debug.Log("post message: " + text);
        if (messages.Count > maxMessages)
        {
            Destroy(messages[0].textObj.gameObject);
            messages.Remove(messages[0]);
        }

        GameObject textObj = Instantiate(chatTextElementUi, chatPanel.transform);

        MSG newMsg = new MSG();

        newMsg.textObj = textObj.GetComponent<TMP_Text>();
        newMsg.text = text;
        newMsg.textObj.text = text;

        messages.Add(newMsg);

    }
    class MSG
    {
        public string text;
        public TMP_Text textObj;
    }
}

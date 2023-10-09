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
    
    public void SendChatMessage()
    {
        if (inputFieldText.text != "")
        {
            GameObject chatMessage = Instantiate(chatMessagePrefab);
            chatMessage.GetComponent<TextMeshProUGUI>().text = "Server: " + inputFieldText.text;
            chatMessage.transform.parent = chatMessageContent.transform;
            inputFieldText.SetTextWithoutNotify("");
        }
    }
}

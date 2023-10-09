using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextSpawner : MonoBehaviour
{

    [SerializeField]
    GameObject floatingText;

    // Start is called before the first frame update
    void Start()
    {

    }

    private void OnEnable()
    {
        Br_IServer.OnSendMessageToClient += CreateMessage;
        Br_IServer.OnSendMessageToServer += CreateResponse;


    }


    void CreateMessage(string message)
    {
        GameObject text = Instantiate(floatingText);
        text.GetComponent<TMPro.TextMeshPro>().text = message;

    }

    void CreateResponse(string message)
    {
        GameObject text = Instantiate(floatingText);
        text.transform.position = new Vector3(-text.transform.position.x, text.transform.position.y, text.transform.position.z);
        text.GetComponent<TMPro.TextMeshPro>().text = message;

    }
}

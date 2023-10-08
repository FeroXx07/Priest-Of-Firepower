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
        Br_IServer.OnCreateMessage += CreateMessage;


    }


    void CreateMessage(string message)
    {
        GameObject text = Instantiate(floatingText);
        text.GetComponent<TMPro.TextMeshPro>().text = message;

    }
}

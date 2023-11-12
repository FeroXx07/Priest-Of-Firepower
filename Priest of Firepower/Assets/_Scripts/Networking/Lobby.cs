using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Lobby : NetworkBehaviour
{
    [SerializeField] GameObject clientUiPrefab;
    [SerializeField] Button startGameBtn;


    private void Start()
    {
        NetworkManager.;
    }


    void OnClientConnected()
    {
        Instantiate(clientUiPrefab);
    }

    protected override MemoryStream Write(MemoryStream outputMemoryStream)
    {

    }

    protected override void Read(MemoryStream inputMemoryStream)
    {
        

    }
}

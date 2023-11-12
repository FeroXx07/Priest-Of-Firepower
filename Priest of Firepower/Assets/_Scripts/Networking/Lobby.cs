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
    
    }


    void OnClientConnected()
    {
        Instantiate(clientUiPrefab);
    }

    protected override MemoryStream Write(MemoryStream outputMemoryStream)
    {
        throw new System.NotImplementedException();
    }

    public override void Read(BinaryReader reader)
    {
        throw new System.NotImplementedException();
    }
}

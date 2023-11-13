using System.IO;
using _Scripts.Networking;
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

    protected override void InitNetworkVariablesList()
    {
        throw new System.NotImplementedException();
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

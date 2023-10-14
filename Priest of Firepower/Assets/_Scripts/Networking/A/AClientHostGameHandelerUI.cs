using ClientA;
using ServerA;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AClientHostGameHandelerUI : MonoBehaviour
{
    [SerializeField] Button hostBtn;
    private void Start()
    {
        AClient.Instance.OnConnected += Lobby;
        hostBtn.onClick.AddListener(HostGame); 
    }
    void HostGame()
    {
        GameObject serverObj = new GameObject("Server");
        serverObj.AddComponent<AServer>();

        AServer.Instance.InitServer();

        if(AServer.Instance.GetServerInit())
        {
            AClient.Instance.Connect(IPAddress.Loopback);
        }
    }

    //if client connected send to lobby
    void Lobby()
    {
        SceneManager.LoadScene(1);
    }
}

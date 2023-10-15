using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    const string _serverSceneName = "Ali_Server";
    const string _clientSceneName = "Ali_Client";
    static bool loaded;

    public static void Load()
    {
        Debug.Log("Lobby Manager: Load();");
        if (loaded) return;
        Debug.Log("Lobby Manager: Loading server scene");
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            _serverSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        Debug.Log("Lobby Manager: Loading client scene");
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            _clientSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);

        loaded = true;
    }

    private void Start()
    {
        Load();
    }

    private void OnEnable()
    {
        Debug.Log("Lobby Manager: OnEnable()");
    }
    private void OnDisable()
    {
        Debug.Log("Lobby Manager: OnDisable()");
        loaded = false;
    }
}

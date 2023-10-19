using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public const string _serverSceneName = "Ali_CreateGame";
    public const string _clientSceneName = "Ali_JoinGame";

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

    public void LoadLobby(string arg)
    {
        Scene sceneToUnload = SceneManager.GetActiveScene();

        if (arg.Equals("host"))  {
            sceneToUnload = SceneManager.GetSceneByName(_clientSceneName);
        }
        else if (arg.Equals("client"))   {
            sceneToUnload = SceneManager.GetSceneByName(_serverSceneName);
        }

        if (sceneToUnload.isLoaded)  {
            SceneManager.UnloadSceneAsync(sceneToUnload);
        }
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

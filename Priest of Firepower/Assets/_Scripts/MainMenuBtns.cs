using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using _Scripts;

namespace _Scripts
{
    public class MainMenuBtns : MonoBehaviour
    {
        public void ChangeScene()
        {
            SceneManager.LoadScene("ConnectToLobby");
        }

        public void QuitApp()
        {
            Application.Quit();
        }
    }
}

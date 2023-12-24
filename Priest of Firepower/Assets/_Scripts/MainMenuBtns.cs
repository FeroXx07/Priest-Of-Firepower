using UnityEngine;
using UnityEngine.SceneManagement;

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

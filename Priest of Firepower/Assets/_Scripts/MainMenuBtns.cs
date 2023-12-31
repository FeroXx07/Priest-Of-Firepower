using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

namespace _Scripts
{
    public class MainMenuBtns : MonoBehaviour
    {
        public AudioClip audioClip; 
        private AudioSource audioSource; 

        void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.playOnAwake = false;
        }

        public void ChangeScene()
        {
            PlayAudio();
            SceneManager.LoadScene("ConnectToLobby");
        }

        public void QuitApp()
        {
            PlayAudio();
            Application.Quit();
        }

        public void PlayAudio()
        {
            if (audioClip != null)
            {
                audioSource.Play();
            }
        }
    }
}

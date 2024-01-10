using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundOnType : MonoBehaviour
{
    public AudioClip typingSound;

    private AudioSource audioSource;
    private TMP_InputField inputField;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        inputField = GetComponent<TMP_InputField>();
        inputField.onValueChanged.AddListener(OnInputValueChanged);
    }

    void OnInputValueChanged(string newText)
    {
        if (typingSound != null && newText.Length > 0 && !audioSource.isPlaying)
        {
            audioSource.clip = typingSound;
            audioSource.Play();
        }
    }
    void OnDisable()
    {
        inputField.onValueChanged.RemoveListener(OnInputValueChanged);
    }

    void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(OnInputValueChanged);
    }
}
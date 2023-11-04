using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{

    [SerializeField] TMP_Text promptText;
    public void Display()
    {
        gameObject.SetActive(true);
    }

    public void SetText(string prompt)
    {
        promptText.text = prompt;
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

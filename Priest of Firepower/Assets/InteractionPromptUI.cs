using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{

    [SerializeField] TMP_Text promptText;

    private void OnEnable()
    {
        
    }


    public void Display(string prompt)
    {
        promptText.text = prompt;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

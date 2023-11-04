using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{

    [SerializeField] string message;
    [SerializeField] float time;
    [SerializeField] InteractionPromptUI interactionPromptUI;
    [SerializeField] AudioClip audioClip;
    float timer;
    public string Prompt => message;

    public float InteractionTime => time;

    private void OnEnable()
    {
        interactionPromptUI.SetText(message);
        EnablePromptUI(false);

    }
    public void EnablePromptUI(bool show)
    {
        interactionPromptUI.gameObject.SetActive(show);
    }

    public void Interact(Interactor interactor, bool keyPressed)
    {
        if(keyPressed)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Debug.Log(Prompt);
                //TODO check update points
                timer = InteractionTime;
                EnablePromptUI(false);
                Debug.Log("Open door");
                gameObject.SetActive(false);
            }
        }
        else{
            EnablePromptUI(true);
            timer = time;          
        }
    }
}

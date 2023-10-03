using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] string message;
    [SerializeField] float time;
    [SerializeField] InteractionPromptUI interactionPromptUI;
    float timer;
    public string Prompt => message;

    public float InteractionTime => time;
    private void OnEnable()
    {
        interactionPromptUI.Display(message);
    }

    public void EnablePromptUI(bool show)
    {
        interactionPromptUI.gameObject.SetActive(show);
    }

    public void Interact(Interactor interactor)
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Debug.Log(Prompt);
            //TODO check update points
            timer = InteractionTime;
            EnablePromptUI(false);
            Debug.Log("Open chest");
        }
    }

}

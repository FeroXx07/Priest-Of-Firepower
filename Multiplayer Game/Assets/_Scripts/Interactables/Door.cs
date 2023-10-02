using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{

    [SerializeField] string message;
    [SerializeField] float time;
    [SerializeField] GameObject uiElement;
    public string Prompt => message;

    public float InteractionTime => time;

    public GameObject UiElement { get => uiElement; set => UiElement = value; }
    public void EnableUIPrompt(bool show)
    {
     
    }

    public void Interact(Interactor interactor)
    {
        Debug.Log("Open door");
    }
}

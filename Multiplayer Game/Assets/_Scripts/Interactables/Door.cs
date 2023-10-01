using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{

    [SerializeField] string message;
    [SerializeField] float time;
    public string prompt => message;

    public float interactionTime => time;

    public void Interact(Interactor interactor)
    {
        Debug.Log("Open door");
    }

    public void ShowUIMessage()
    {
        
    }
}

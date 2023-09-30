using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{

    [SerializeField] string message;
    public string prompt => message;

    public void Interact(Interactor interactor)
    {
        Debug.Log("Open door");
    }
}

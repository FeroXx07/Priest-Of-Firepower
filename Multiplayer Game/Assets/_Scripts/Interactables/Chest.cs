using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] string message;
    public string prompt => message;

    public void Interact(Interactor interactor)
    {
        Debug.Log(prompt);
    }
}

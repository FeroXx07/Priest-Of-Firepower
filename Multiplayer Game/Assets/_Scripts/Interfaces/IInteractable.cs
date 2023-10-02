using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable 
{
    public float interactionTime { get; }
    public string prompt { get; }
    public void Interact(Interactor interactor);
    public void EnableUIPrompt(bool show);
}

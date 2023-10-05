using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable 
{
    public float InteractionTime { get; }
    public string Prompt { get; }
    public void Interact(Interactor interactor);
    public void EnablePromptUI(bool show);
}

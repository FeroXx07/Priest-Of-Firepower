using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable 
{
    public string prompt { get; }
    public void Interact(Interactor interactor);
}

using System;
using System.IO;
using Unity.VisualScripting;

namespace _Scripts.Interfaces
{
    public enum InteractableState
    {
        INTERACTING,
        INTERRUPTED,
        INTERACTION_COMPLETE,
        NONE
    }
    public interface IInteractable
    {        
        public InteractableState state { get; set; }
        public UInt64 interactorId { get; set; }
        public bool IsBeingInteracted { get; }
        public float InteractionTime { get; }
        public string Prompt { get; }
        public int InteractionCost { get; }
        public void Interact(Interactor interactor, bool keyPressed);
        public void EnablePromptUI(bool show);
        public void InterruptInteraction();
    }
}

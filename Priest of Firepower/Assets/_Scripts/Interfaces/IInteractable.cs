using System.IO;
using Unity.VisualScripting;

namespace _Scripts.Interfaces
{
    public enum InteractableState
    {
        INTERACTING,
        INTERRUPTED,
        INTERACTION_COMPLETE
    }
    public interface IInteractable
    {        
        public InteractableState state { get; set; }
        public bool IsBeingInteracted { get; }
        public float InteractionTime { get; }
        public string Prompt { get; }
        public int InteractionCost { get; }
        public void Interact(State _state);
        public void ClientInteract(Interactor interactor, bool keyPressed);
        public void ClientHandleInteraction(MemoryStream stream);
        public void ServerInteract(Interactor interactor, bool keyPressed);       
        public void ServerHandleInteraction(MemoryStream stream);
        public void EnablePromptUI(bool show);
    }
}

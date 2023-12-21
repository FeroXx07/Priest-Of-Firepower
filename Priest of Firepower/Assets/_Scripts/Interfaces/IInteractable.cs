using System.IO;

namespace _Scripts.Interfaces
{
    public interface IInteractable
    {        
        public enum InteractableState
        {
            INTERACTING,
            INTERRUPTED,
            FINISH_INTERACTION
        }
        public bool IsBeingInteracted { get; }
        public float InteractionTime { get; }
        public string Prompt { get; }
        public int InteractionCost { get; }
        public void Interact(Interactor interactor, bool keyPressed);
        public void EnablePromptUI(bool show);

        public MemoryStream GetInteractionStream();
        public void ReadInteractionStream(MemoryStream stream);
    }
}

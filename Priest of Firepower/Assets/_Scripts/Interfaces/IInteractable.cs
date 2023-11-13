namespace _Scripts.Interfaces
{
    public interface IInteractable 
    {
        public float InteractionTime { get; }
        public string Prompt { get; }
        public int InteractionCost { get; }
        public void Interact(Interactor interactor, bool keyPressed);
        public void EnablePromptUI(bool show);
    }
}

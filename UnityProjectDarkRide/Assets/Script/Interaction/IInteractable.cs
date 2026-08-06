public interface IInteractable
{
    string GetInteractPrompt();
    float HoldDuration { get; }
    void OnInteract();
}
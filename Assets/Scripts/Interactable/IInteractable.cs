
public interface IInteractable
{
    string InteractionPrompt { get; }

    InteractionType Type { get; }

    bool Interact(UnityEngine.GameObject interactor);
}
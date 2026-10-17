
public interface IInteractable
{

    InteractionType Type { get; }

    bool Interact(UnityEngine.GameObject interactor);
}
using UnityEngine;

public class FakeDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioClip lockDoorClip;

    public string InteractionPrompt => "Drzwi";
    public InteractionType Type => InteractionType.Door;

    public bool Interact(GameObject interactor)
    {
        AudioManager.Instance.PlaySFX(lockDoorClip);
        return false;
    }
}

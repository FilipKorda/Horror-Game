using UnityEngine;

public class EscapeDoorInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private PauseController pauseController;
    [SerializeField] private AudioClip doorCloseOpenClip;
    public string InteractionPrompt => "Opuść dom";

    public InteractionType Type => InteractionType.Exit;

    private bool used;

    public bool Interact(GameObject interactor)
    {
        if (used || questManager == null)
            return false;

        used = true;

        questManager.Escape();

        AudioManager.Instance.PlaySFX(doorCloseOpenClip);

        AudioManager.Instance.StopBreathing();
        AudioManager.Instance.StopHeartbeat();

        pauseController.CanPause = false;
        return true;
    }
}
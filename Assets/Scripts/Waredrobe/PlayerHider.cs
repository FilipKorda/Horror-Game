using UnityEngine;

public class PlayerHider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer playerVisual; 
    [SerializeField] private FPSController fpsController;
    [SerializeField] private FlashlightLookController flashlightLookController;

    public static event System.Action<HidingSpot> OnPlayerHidden;

    public static event System.Action OnPlayerRevealed;

    public void SetHidden(HidingSpot hidingSpot)
    {
        if (playerVisual != null)
            playerVisual.enabled = false;

        OnPlayerHidden?.Invoke(hidingSpot);
    }

    public void SetVisible(bool visible)
    {
        if (playerVisual != null)
            playerVisual.enabled = visible;

        if (visible)
            OnPlayerRevealed?.Invoke();
    }

    public void SetMovementLocked(bool locked)
    {
        if (fpsController != null)
            fpsController.SetMovementLocked(locked);
    }

    public void SetLookLocked(bool locked)
    {
        if (flashlightLookController != null)
        {
            flashlightLookController.SetLookLocked(locked);
            return;
        }

        if (fpsController != null)
            fpsController.SetLookLocked(locked);
    }

}
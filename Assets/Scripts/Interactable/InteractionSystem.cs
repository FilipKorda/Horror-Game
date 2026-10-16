using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private Transform aimDirectionSource;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Detection")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayerMask = ~0;

    [SerializeField] private AudioClip pickUpClip;

    private IInteractable currentInteractable;
    private IHideable currentHideable;
    private InteractableHighlight currentHighlight;

    public IInteractable CurrentInteractable => currentInteractable;

    public bool IsHiding => currentHideable != null;

    private void OnEnable()
    {
        interactAction.action.Enable();
        interactAction.action.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        interactAction.action.performed -= OnInteractPerformed;
        interactAction.action.Disable();

        UpdateHighlight(currentInteractable, null);
        currentInteractable = null;
    }

    private void Update()
    {
        if (currentHideable == null)
            DetectInteractable();
    }

    private void DetectInteractable()
    {
        IInteractable previousInteractable = currentInteractable;
        currentInteractable = null;

        if (interactionOrigin == null)
        {
            UpdateHighlight(previousInteractable, null);
            return;
        }

        Vector3 aimDirection = aimDirectionSource != null
            ? aimDirectionSource.forward
            : interactionOrigin.forward;

        bool hitSomething = Physics.Raycast(
            interactionOrigin.position,
            aimDirection,
            out RaycastHit hit,
            interactionRange,
            interactableLayerMask);

        if (hitSomething)
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable is ItemSlot itemSlot && itemSlot.IsFilled)
            {
                currentInteractable = null;
            }
            else
            {
                currentInteractable = interactable;
            }
        }

        UpdateHighlight(previousInteractable, currentInteractable);
    }

    private void UpdateHighlight(IInteractable previous, IInteractable current)
    {
        if (previous == current)
            return;

        currentHighlight?.SetHighlighted(false);

        currentHighlight = current is Component component
            ? component.GetComponent<InteractableHighlight>()
            : null;

        currentHighlight?.SetHighlighted(true);
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (ChaseMinigameController.Instance != null && ChaseMinigameController.Instance.IsActive)
            return;

        if (currentHideable != null)
        {
            IHideable hideable = currentHideable;
            currentHideable = null;
            hideable.Exit(gameObject);
            return;
        }

        if (currentInteractable == null)
            return;

        bool success = currentInteractable.Interact(gameObject);

        if (success)
            AudioManager.Instance.PlaySFX(pickUpClip);

        if (currentInteractable is IHideable hideable2)
            currentHideable = hideable2;
    }

    public void CancelHiding()
    {
        currentHideable = null;
    }
}
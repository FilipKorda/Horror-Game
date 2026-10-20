using System.Collections;
using UnityEngine;

public class WardrobeController : MonoBehaviour, IInteractable, IHideable
{
    [Header("References")]
    [SerializeField] private PlayerFearMeter playerFearMeter;
    [SerializeField] private Transform rightDoorTransform;
    [SerializeField] private Transform leftDoorTransform;
    [SerializeField] private Transform hidePoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private HidingSpot hidingSpot;

    [Header("Right Door Angles (lokalna rotacja Y)")]
    [SerializeField] private float rightClosedAngle = 0f;
    [SerializeField] private float rightAjarAngle = 20f;
    [SerializeField] private float rightOpenAngle = 90f;

    [Header("Left Door Angles (lokalna rotacja Y)")]
    [SerializeField] private float leftClosedAngle = 0f;
    [SerializeField] private float leftAjarAngle = -20f;
    [SerializeField] private float leftOpenAngle = -90f;

    [Header("Timing")]
    [SerializeField] private float doorAnimationDuration = 0.5f;
    [SerializeField] private float playerMoveDuration = 0.4f;

    private enum DoorState { Closed, Ajar, Open }

    public InteractionType Type => InteractionType.Hide;
    [SerializeField] private AudioClip doorCloseOpenClip;
    [SerializeField] private float ejectDoorDuration = 0.12f;
    [SerializeField] private float ejectCloseDelay = 0.8f;
    private bool isTransitioning;
    private bool isOccupied;
    private GameObject occupant;
    public bool IsOccupied => isOccupied;
    public bool IsTransitioningWithPlayer => isTransitioning && occupant != null;

    public bool Interact(GameObject interactor)
    {
        if (isTransitioning || isOccupied || playerFearMeter.CurrentFearPoints >= playerFearMeter.MaxFearPoints)
            return false;

        if (JumpscareController.Instance != null && JumpscareController.Instance.IsPlaying)
            return false;

        StartCoroutine(EnterRoutine(interactor));
        AudioManager.Instance.PlaySFX(doorCloseOpenClip);

        return false;
    }

    public void Exit(GameObject interactor)
    {
        if (isTransitioning || !isOccupied)
            return;

        AudioManager.Instance.PlaySFX(doorCloseOpenClip);
        StartCoroutine(ExitRoutine(interactor));
    }

    public IEnumerator SlamOpen(float duration)
    {
        yield return RotateDoors(DoorState.Open, duration);
    }

    public void ReturnToAjar()
    {
        if (isOccupied && isActiveAndEnabled)
            StartCoroutine(RotateDoors(DoorState.Ajar, doorAnimationDuration));
    }

    public void EjectPlayer()
    {
        GameObject player = occupant;

        StopAllCoroutines();

        occupant = null;
        isOccupied = false;
        isTransitioning = false;

        if (player != null)
        {
            PlayerHider playerHider = player.GetComponent<PlayerHider>();
            if (playerHider != null)
                playerHider.SetVisible(true);

            if (exitPoint != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                bool controllerEnabled = controller != null && controller.enabled;

                if (controller != null)
                    controller.enabled = false;

                player.transform.position = exitPoint.position;

                if (controller != null)
                    controller.enabled = controllerEnabled;
            }
        }

        if (isActiveAndEnabled)
            StartCoroutine(EjectDoorsRoutine());
    }

    private IEnumerator EjectDoorsRoutine()
    {
        yield return RotateDoors(DoorState.Open, ejectDoorDuration);
        yield return new WaitForSeconds(ejectCloseDelay);
        yield return RotateDoors(DoorState.Closed, doorAnimationDuration);
    }

    private IEnumerator EnterRoutine(GameObject interactor)
    {
        isTransitioning = true;
        occupant = interactor;

        PlayerHider playerHider = interactor.GetComponent<PlayerHider>();
        if (playerHider == null)
        {
            isTransitioning = false;
            occupant = null;
            yield break;
        }

        playerHider.SetMovementLocked(true);
        playerHider.SetLookLocked(true);

        yield return RotateDoors(DoorState.Open, doorAnimationDuration);
        yield return MovePlayer(interactor.transform, hidePoint, playerMoveDuration);

        playerHider.SetHidden(hidingSpot);

        yield return RotateDoors(DoorState.Ajar, doorAnimationDuration);


        isOccupied = true;
        isTransitioning = false;
    }

    private IEnumerator ExitRoutine(GameObject interactor)
    {
        isTransitioning = true;

        PlayerHider playerHider = interactor.GetComponent<PlayerHider>();

        if (playerHider != null)
            playerHider.SetLookLocked(true);

        yield return RotateDoors(DoorState.Open, doorAnimationDuration);

        if (playerHider != null)
            playerHider.SetVisible(true);

        yield return MovePlayer(interactor.transform, exitPoint, playerMoveDuration);
        yield return RotateDoors(DoorState.Closed, doorAnimationDuration);

        if (playerHider != null)
        {
            playerHider.SetMovementLocked(false);
            playerHider.SetLookLocked(false);
        }

        isOccupied = false;
        isTransitioning = false;
        occupant = null;
    }

    private IEnumerator RotateDoors(DoorState state, float duration)
    {
        if (rightDoorTransform == null && leftDoorTransform == null)
            yield break;

        float rightTarget = GetRightAngle(state);
        float leftTarget = GetLeftAngle(state);

        float rightStart = rightDoorTransform != null ? rightDoorTransform.localEulerAngles.y : 0f;
        float leftStart = leftDoorTransform != null ? leftDoorTransform.localEulerAngles.y : 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (rightDoorTransform != null)
            {
                float angle = Mathf.LerpAngle(rightStart, rightTarget, t);
                rightDoorTransform.localRotation = Quaternion.Euler(0f, angle, 90f);
            }

            if (leftDoorTransform != null)
            {
                float angle = Mathf.LerpAngle(leftStart, leftTarget, t);
                leftDoorTransform.localRotation = Quaternion.Euler(0f, angle, 90f);
            }

            yield return null;
        }

        if (rightDoorTransform != null)
            rightDoorTransform.localRotation = Quaternion.Euler(0f, rightTarget, 90f);

        if (leftDoorTransform != null)
            leftDoorTransform.localRotation = Quaternion.Euler(0f, leftTarget, 90f);
    }

    private float GetRightAngle(DoorState state) => state switch
    {
        DoorState.Closed => rightClosedAngle,
        DoorState.Ajar => rightAjarAngle,
        DoorState.Open => rightOpenAngle,
        _ => rightClosedAngle
    };

    private float GetLeftAngle(DoorState state) => state switch
    {
        DoorState.Closed => leftClosedAngle,
        DoorState.Ajar => leftAjarAngle,
        DoorState.Open => leftOpenAngle,
        _ => leftClosedAngle
    };

    private IEnumerator MovePlayer(Transform playerTransform, Transform target, float duration)
    {
        if (target == null)
            yield break;

        Vector3 startPos = playerTransform.position;
        Quaternion startRot = playerTransform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerTransform.position = Vector3.Lerp(startPos, target.position, t);
            playerTransform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
            yield return null;
        }

        playerTransform.position = target.position;
        playerTransform.rotation = target.rotation;
    }
}
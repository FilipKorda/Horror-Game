using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private AudioClip doorCloseOpenClip;

    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;

    private Quaternion leftOpenRotation;
    private Quaternion rightOpenRotation;

    private bool isOpen;

    public string InteractionPrompt => "Drzwi";
    public InteractionType Type => InteractionType.Door;

    private void Awake()
    {
        leftClosedRotation = leftDoor.localRotation;
        rightClosedRotation = rightDoor.localRotation;

        leftOpenRotation = leftClosedRotation *
                           Quaternion.Euler(0f, -openAngle, 0f);

        rightOpenRotation = rightClosedRotation *
                            Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        Quaternion leftTarget = isOpen
            ? leftOpenRotation
            : leftClosedRotation;

        Quaternion rightTarget = isOpen
            ? rightOpenRotation
            : rightClosedRotation;

        leftDoor.localRotation = Quaternion.Slerp(
            leftDoor.localRotation,
            leftTarget,
            rotationSpeed * Time.deltaTime);

        rightDoor.localRotation = Quaternion.Slerp(
            rightDoor.localRotation,
            rightTarget,
            rotationSpeed * Time.deltaTime);
    }

    public bool Interact(GameObject interactor)
    {
        isOpen = !isOpen;
        AudioManager.Instance.PlaySFX(doorCloseOpenClip);
        return false;
    }
}
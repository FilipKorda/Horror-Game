using System.Collections;
using UnityEngine;

public class ItemPuzzleDoor : MonoBehaviour
{
    [Header("Sloty wymagane do otwarcia")]
    [SerializeField] private ItemSlot[] requiredSlots;
    [Header("Doors")]
    [SerializeField] private Transform rightDoorTransform;
    [SerializeField] private Transform leftDoorTransform;
    [SerializeField] private float doorAnimationDuration = 1f;
    [SerializeField] private float rightOpenAngle = 90f;
    [SerializeField] private float leftOpenAngle = -90f;
    [SerializeField] private AudioClip doorCloseOpenClip;

    private bool isOpen;

    private void OnEnable()
    {
        foreach (ItemSlot slot in requiredSlots)
        {
            if (slot != null)
                slot.OnItemPlaced += HandleItemPlaced;
        }
    }

    private void OnDisable()
    {
        foreach (ItemSlot slot in requiredSlots)
        {
            if (slot != null)
                slot.OnItemPlaced -= HandleItemPlaced;
        }
    }

    private void HandleItemPlaced(ItemSlot slot)
    {
        if (!isOpen && AreAllSlotsFilled())
            StartCoroutine(OpenDoors());
    }

    private bool AreAllSlotsFilled()
    {
        foreach (ItemSlot slot in requiredSlots)
        {
            if (slot == null || !slot.IsFilled)
                return false;
        }

        return true;
    }

    private IEnumerator OpenDoors()
    {
        isOpen = true;

        if (rightDoorTransform == null && leftDoorTransform == null)
            yield break;

        float rightStart = rightDoorTransform != null ? rightDoorTransform.localEulerAngles.y : 0f;
        float leftStart = leftDoorTransform != null ? leftDoorTransform.localEulerAngles.y : 0f;

        float elapsed = 0f;

        AudioManager.Instance.PlaySFXAtPosition(doorCloseOpenClip, transform.position);

        while (elapsed < doorAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / doorAnimationDuration;

            if (rightDoorTransform != null)
            {
                float angle = Mathf.LerpAngle(rightStart, rightOpenAngle, t);
                rightDoorTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            if (leftDoorTransform != null)
            {
                float angle = Mathf.LerpAngle(leftStart, leftOpenAngle, t);
                leftDoorTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            yield return null;
        }

        if (rightDoorTransform != null)
            rightDoorTransform.localRotation = Quaternion.Euler(0f, rightOpenAngle, 0f);

        if (leftDoorTransform != null)
            leftDoorTransform.localRotation = Quaternion.Euler(0f, leftOpenAngle, 0f);
    }
}
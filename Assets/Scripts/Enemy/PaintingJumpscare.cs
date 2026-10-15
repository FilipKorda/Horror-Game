using System.Collections;
using UnityEngine;

public class PaintingJumpscare : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Collider paintingCollider;
    [SerializeField] private SkinnedMeshRenderer eyeMaintingRenderer;

    [Header("Looking")]
    [SerializeField] private float requiredLookTime = 3f;
    [SerializeField] private float maxLookDistance = 10f;
    [SerializeField] private float maxLookAngle = 25f;

    [Header("Movement")]
    [SerializeField] private float moveDistanceFromCamera = 1f;
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float targetHeightOffset = -0.5f;
    [Header("Sfx")]
    [SerializeField] private AudioClip boomClip;

    [SerializeField] private Animator animator;
    [SerializeField] private Transform headBone;

    private float lookTimer;
    private bool isTriggered;


    private void Update()
    {
        if (isTriggered)
            return;

        if (IsPlayerLookingAtPainting())
        {
            lookTimer += Time.deltaTime;

            if (lookTimer >= requiredLookTime)
            {
                isTriggered = true;
                StartJumpscare();
            }
        }
        else
        {
            lookTimer = 0f;
        }
    }

    private bool IsPlayerLookingAtPainting()
    {
        if (playerCamera == null)
            return false;

        Vector3 targetPosition = paintingCollider != null
            ? paintingCollider.bounds.center
            : transform.position;

        Vector3 direction = targetPosition - playerCamera.transform.position;
        float distance = direction.magnitude;

        if (distance > maxLookDistance)
            return false;

        float angle = Vector3.Angle(
            playerCamera.transform.forward,
            direction.normalized
        );

        if (angle > maxLookAngle)
            return false;

        if (Physics.Raycast(
            playerCamera.transform.position,
            direction.normalized,
            out RaycastHit hit,
            distance + 0.1f))
        {
            if (paintingCollider != null)
                return hit.collider == paintingCollider;

            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        return false;
    }

    private IEnumerator MoveToCamera()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward * moveDistanceFromCamera;

        targetPosition.y += targetHeightOffset;

        Vector3 direction = targetPosition - startPosition;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        float elapsed = 0f;

        AudioManager.Instance.PlaySFX(boomClip);
        animator.SetTrigger("Attack");

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = moveCurve.Evaluate(elapsed / moveDuration);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        Destroy(gameObject);
    }

    private void StartJumpscare()
    {
        if (JumpscareController.Instance == null)
        {
            StartCoroutine(MoveToCamera());
            return;
        }

        if (headBone == null)
            headBone = FindChildRecursive(transform, "head");

        foreach (var material in eyeMaintingRenderer.materials)
        {
            material.SetFloat("_EmissionEnabled", 1f);
            material.EnableKeyword("_EMISSION");
        }

        JumpscareController.Instance.PlayAttack(
            transform,
            animator,
            headBone,
            () => Destroy(gameObject),
            false,
            boomClip,
            true,
            moveDuration,
            moveCurve);
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
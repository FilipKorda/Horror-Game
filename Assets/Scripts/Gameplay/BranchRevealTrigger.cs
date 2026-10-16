using System.Collections;
using UnityEngine;

public class RevealObjectTrigger : MonoBehaviour
{
    [Header("Obiekt")]
    [SerializeField] private GameObject objectToReveal;
    [SerializeField] private GameObject cameraLookPoint;
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Range(5f, 90f)] private float lookHalfAngle = 45f;
    [SerializeField] private FlashlightLookController lookController;
    [SerializeField, Min(0f)] private float turnDuration = 0.25f;

    [Header("Audio")]
    [SerializeField] private AudioClip revealClip;
    [SerializeField] private AudioClip jumpscareClip;

    [Header("Przebieg")]
    [SerializeField, Min(0)] private int flickerCount = 3;
    [SerializeField] private Vector2 flickerInterval = new Vector2(0.03f, 0.1f);
    [SerializeField, Min(0f)] private float revealDuration = 0f;

    [Header("Rzut na gracza (opcjonalnie)")]
    [SerializeField] private Transform lungeTarget;
    [SerializeField, Min(0.01f)] private float lungeDuration = 0.25f;
    [SerializeField] private AnimationCurve lungeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Strach")]
    [SerializeField] private FearEffectPreset fearEffect;
    [SerializeField] private PlayerFearMeter fearMeter;

    private bool hasEnteredCorrectly;
    private bool hasTriggered;

    private void Start()
    {
        if (objectToReveal != null)
            objectToReveal.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || hasEnteredCorrectly || !other.CompareTag(playerTag))
            return;

        Vector3 directionToPlayer = other.transform.position - transform.position;
        directionToPlayer.y = 0f;

        Vector3 triggerForward = transform.forward;
        triggerForward.y = 0f;

        if (Vector3.Dot(triggerForward.normalized, directionToPlayer.normalized) <= 0f)
            return;

        hasEnteredCorrectly = true;

        if (IsCameraLookingAtObject())
            BeginReveal();
    }

    private void Update()
    {
        if (!hasEnteredCorrectly || hasTriggered)
            return;

        if (IsCameraLookingAtObject())
            BeginReveal();
    }

    private bool IsCameraLookingAtObject()
    {
        if (playerCameraTransform == null || cameraLookPoint == null)
            return false;

        Vector3 directionToObject = cameraLookPoint.transform.position - playerCameraTransform.position;

        if (directionToObject.sqrMagnitude <= 0.001f)
            return true;

        return Vector3.Angle(playerCameraTransform.forward, directionToObject) <= lookHalfAngle;
    }

    private void BeginReveal()
    {
        hasTriggered = true;

        Transform aimPoint = objectToReveal.transform;
        lookController.LookAt(aimPoint.position + (Vector3.up * 1.5f), turnDuration);

        if (TryGetComponent<Collider>(out var triggerCollider))
            triggerCollider.enabled = false;

        StartCoroutine(RevealSequence());
    }

    private IEnumerator RevealSequence()
    {
        PlaySfx(revealClip);
        if (jumpscareClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayJumpscareSFX(jumpscareClip);

        Coroutine flicker = StartCoroutine(Flicker());

        if (fearEffect != null && FearCameraEffect.Instance != null)
            FearCameraEffect.Instance.Play(fearEffect);

        if (objectToReveal != null && lungeTarget != null)
            StartCoroutine(Lunge());

        if (revealDuration > 0f)
        {
            yield return new WaitForSeconds(revealDuration);

            if (flicker != null)
                StopCoroutine(flicker);
            objectToReveal.SetActive(false);
        }
    }

    private IEnumerator Flicker()
    {
        for (int i = 0; i < flickerCount; i++)
        {
            objectToReveal.SetActive(true);
            yield return new WaitForSeconds(Random.Range(flickerInterval.x, flickerInterval.y));

            objectToReveal.SetActive(false);
            yield return new WaitForSeconds(Random.Range(flickerInterval.x, flickerInterval.y));
        }

        objectToReveal.SetActive(true);
    }

    private IEnumerator Lunge()
    {
        Transform revealedTransform = objectToReveal.transform;
        Vector3 startPosition = revealedTransform.position;
        Vector3 endPosition = lungeTarget.position;

        yield return new WaitForSeconds(0.2f);
        float elapsed = 0f;
        while (elapsed < lungeDuration)
        {
            elapsed += Time.deltaTime;
            revealedTransform.position = Vector3.LerpUnclamped(startPosition, endPosition, lungeCurve.Evaluate(elapsed / lungeDuration));
            yield return null;
        }

        revealedTransform.position = endPosition;
    }

    private static void PlaySfx(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip);
    }
}
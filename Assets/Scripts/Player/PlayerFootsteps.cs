using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSController fpsController;
    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip[] walkFootstepClips;
    [SerializeField] private AudioClip[] sprintFootstepClips;

    [Header("Krok co ile metrów")]
    [SerializeField] private float stepDistance = 2f;

    [Header("Volume & Pitch")]
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float sprintVolume = 0.8f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f); 

    private CharacterController controller;
    private Vector3 lastPosition;
    private float distanceSinceLastStep;

    public float StepProgress => stepDistance > 0f ? Mathf.Clamp01(distanceSinceLastStep / stepDistance) : 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (fpsController != null && fpsController.IsSlowWalking)
        {
            lastPosition = transform.position;
            distanceSinceLastStep = 0f;
            return;
        }

        if (!controller.isGrounded)
            return;

        Vector3 horizontalDelta = transform.position - lastPosition;
        horizontalDelta.y = 0f; 

        float distanceMoved = horizontalDelta.magnitude;
        lastPosition = transform.position;

        if (distanceMoved < 0.0001f)
            return; 

        distanceSinceLastStep += distanceMoved;

        if (distanceSinceLastStep < stepDistance)
            return;

        distanceSinceLastStep = 0f;

        bool isSprinting = fpsController != null && fpsController.IsSprinting;
        PlayFootstep(isSprinting);
    }

    private void PlayFootstep(bool isSprinting)
    {
        AudioClip[] clips = isSprinting ? sprintFootstepClips : walkFootstepClips;
        if (clips == null || clips.Length == 0 || audioSource == null)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, isSprinting ? sprintVolume : walkVolume);
    }
}
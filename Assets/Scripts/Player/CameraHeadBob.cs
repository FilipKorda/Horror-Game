using System;
using UnityEngine;

[DefaultExecutionOrder(-1001)]
public class CameraHeadBob : MonoBehaviour
{
    [Serializable]
    private struct BobProfile
    {
        public float stepLength;
        public float bounce;
        public float sway;
        public float roll;
        public float nod;

        public static BobProfile Lerp(BobProfile a, BobProfile b, float t)
        {
            return new BobProfile
            {
                stepLength = Mathf.Lerp(a.stepLength, b.stepLength, t),
                bounce = Mathf.Lerp(a.bounce, b.bounce, t),
                sway = Mathf.Lerp(a.sway, b.sway, t),
                roll = Mathf.Lerp(a.roll, b.roll, t),
                nod = Mathf.Lerp(a.nod, b.nod, t)
            };
        }
    }

    [Header("Referencje")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private FPSController fpsController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerFootsteps footsteps;

    [Header("Wolne chodzenie")]
    [SerializeField] private BobProfile slowWalk = new BobProfile { stepLength = 0.75f, bounce = 0.005f, sway = 0.009f, roll = 0.4f, nod = 0.12f };

    [Header("Chodzenie")]
    [SerializeField] private BobProfile walk = new BobProfile { stepLength = 1.6f, bounce = 0.011f, sway = 0.007f, roll = 0.3f, nod = 0.2f };

    [Header("Bieganie")]
    [SerializeField] private BobProfile sprint = new BobProfile { stepLength = 1.6f, bounce = 0.02f, sway = 0.011f, roll = 0.45f, nod = 0.4f };

    [Header("Płynność")]
    [SerializeField, Min(0f)] private float speedSmoothing = 10f;
    [SerializeField, Min(0f)] private float fadeSpeed = 6f;
    [SerializeField, Min(0f)] private float airborneGrace = 0.2f;

    [Header("Synchronizacja z krokami")]
    [SerializeField, Min(0f)] private float footstepSync = 2f;
    [SerializeField, Range(0f, 1f)] private float maxSyncCorrection = 0.2f;

    private float smoothedSpeed;
    private float weight;
    private float stepPhase;
    private float airTime;

    private bool offsetApplied;
    private Vector3 basePosition;
    private Vector3 appliedPosition;
    private Quaternion baseRotation;
    private Quaternion appliedRotation;

    private void Awake()
    {
        if (fpsController == null)
            fpsController = GetComponent<FPSController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (footsteps == null)
            footsteps = GetComponent<PlayerFootsteps>();
    }

    private void OnDisable()
    {
        RestoreCamera();
        smoothedSpeed = 0f;
        weight = 0f;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || fpsController == null || characterController == null)
            return;

        RestoreCamera();

        float deltaTime = Time.deltaTime;
        bool canBob = fpsController.isActiveAndEnabled && characterController.enabled && !fpsController.IsMovementLocked;

        Vector3 velocity = canBob ? characterController.velocity : Vector3.zero;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-speedSmoothing * deltaTime));

        airTime = characterController.isGrounded ? 0f : airTime + deltaTime;
        bool grounded = airTime < airborneGrace;

        float slowSpeed = Mathf.Max(0.01f, fpsController.SlowWalkSpeed);
        float targetWeight = canBob && grounded ? Mathf.Clamp01(smoothedSpeed / slowSpeed) : 0f;
        weight = Mathf.Lerp(weight, targetWeight, 1f - Mathf.Exp(-fadeSpeed * deltaTime));

        BobProfile profile = ProfileForSpeed(smoothedSpeed);
        AdvancePhase(grounded ? speed : 0f, profile.stepLength, deltaTime);

        if (weight < 0.0001f)
            return;

        float bounceWave = -Mathf.Cos(stepPhase * 2f * Mathf.PI);
        float swayWave = Mathf.Sin(stepPhase * Mathf.PI);

        Vector3 positionOffset = new Vector3(profile.sway * swayWave, profile.bounce * bounceWave, 0f) * weight;
        Vector3 eulerOffset = new Vector3(-profile.nod * bounceWave, 0f, -profile.roll * swayWave) * weight;

        basePosition = cameraTransform.localPosition;
        baseRotation = cameraTransform.localRotation;

        appliedPosition = basePosition + positionOffset;
        appliedRotation = baseRotation * Quaternion.Euler(eulerOffset);

        cameraTransform.localPosition = appliedPosition;
        cameraTransform.localRotation = appliedRotation;

        offsetApplied = true;
    }

    private void AdvancePhase(float speed, float stepLength, float deltaTime)
    {
        float advance = speed * deltaTime / Mathf.Max(0.05f, stepLength);

        if (footsteps != null && !fpsController.IsSlowWalking && advance > 0f)
        {
            float error = Mathf.Repeat(footsteps.StepProgress - stepPhase + 0.5f, 1f) - 0.5f;
            advance *= 1f + Mathf.Clamp(error * footstepSync, -maxSyncCorrection, maxSyncCorrection);
        }

        stepPhase = Mathf.Repeat(stepPhase + advance, 2f);
    }

    private BobProfile ProfileForSpeed(float speed)
    {
        float walkSpeed = fpsController.WalkSpeed;

        if (speed <= walkSpeed)
            return BobProfile.Lerp(slowWalk, walk, Mathf.InverseLerp(fpsController.SlowWalkSpeed, walkSpeed, speed));

        return BobProfile.Lerp(walk, sprint, Mathf.InverseLerp(walkSpeed, fpsController.SprintSpeed, speed));
    }

    private void RestoreCamera()
    {
        if (!offsetApplied || cameraTransform == null)
            return;

        offsetApplied = false;

        if ((cameraTransform.localPosition - appliedPosition).sqrMagnitude < 1e-8f)
            cameraTransform.localPosition = basePosition;

        if (Quaternion.Angle(cameraTransform.localRotation, appliedRotation) < 0.01f)
            cameraTransform.localRotation = baseRotation;
    }
}

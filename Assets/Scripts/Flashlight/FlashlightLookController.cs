using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightLookController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSController fpsController;
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform flashlightPivot;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference lookAction;

    [Header("Mouse")]
    [SerializeField] private float mouseSensitivityX = 0.1f;
    [SerializeField] private float mouseSensitivityY = 0.1f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Gamepad")]
    [SerializeField] private float gamepadSensitivityX = 180f;
    [SerializeField] private float gamepadSensitivityY = 180f;

    [Header("Mouse Sensitivity (ustawienia)")]
    [SerializeField] private float minSensitivity = 0.02f;
    [SerializeField] private float maxSensitivity = 0.3f;

    [Header("Lag latarki")]
    [SerializeField] private float maxOffsetAngle = 20f;
    [SerializeField] private float cameraCatchUpSpeed = 8f;
    [SerializeField] private float gamepadPitchCatchUpSpeed = 20f;

    private float cameraYaw;
    private float cameraPitch;
    private float flashlightYaw;
    private float flashlightPitch;

    private bool lookLocked;

    private bool yawConstrained;
    private float yawConstraintCenter;
    private float yawConstraintRange;

    private void Awake()
    {
        if (playerBody != null)
            cameraYaw = playerBody.eulerAngles.y;

        flashlightYaw = cameraYaw;

        if (fpsController != null)
            fpsController.SetLookLocked(true);
    }

    private void Start()
    {
        if (SettingsManager.Instance != null)
        {
            SetMouseSensitivity(SettingsManager.Instance.Gameplay.mouseSensitivity);
            SetInvertY(SettingsManager.Instance.Gameplay.invertY);
        }
    }

    private void OnEnable()
    {
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
    }

    public void SetLookLocked(bool locked)
    {
        if (lookLocked && !locked)
            SyncRotationState();

        lookLocked = locked;
    }

    public void SetMouseSensitivity(float sensitivity01)
    {
        float sensitivity = Mathf.Lerp(minSensitivity, maxSensitivity, Mathf.Clamp01(sensitivity01));
        mouseSensitivityX = sensitivity;
        mouseSensitivityY = sensitivity;
    }

    public void SetInvertY(bool invert) => invertY = invert;

    private void SyncRotationState()
    {
        if (playerBody != null)
            cameraYaw = playerBody.eulerAngles.y;

        if (cameraTransform != null)
            cameraPitch = NormalizeAngle(cameraTransform.localEulerAngles.x);

        flashlightYaw = cameraYaw;
        flashlightPitch = cameraPitch;
    }

    private float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    public void SetYawConstraint(bool enabled, float centerYaw = 0f, float range = 45f)
    {
        yawConstrained = enabled;
        yawConstraintCenter = centerYaw;
        yawConstraintRange = range;
    }

    private void Update()
    {
        if (lookLocked)
            return;

        Vector2 look = lookAction.action.ReadValue<Vector2>();

        bool isGamepad = InputDeviceDetector.Instance != null &&
                         (InputDeviceDetector.Instance.CurrentDevice == InputDeviceType.Xbox ||
                          InputDeviceDetector.Instance.CurrentDevice == InputDeviceType.PlayStation ||
                          InputDeviceDetector.Instance.CurrentDevice == InputDeviceType.Generic);

        float mouseX;
        float mouseY;

        if (isGamepad)
        {
            mouseX = look.x * gamepadSensitivityX * Time.deltaTime;
            mouseY = look.y * gamepadSensitivityY * Time.deltaTime * (invertY ? 1f : -1f);
        }
        else
        {
            mouseX = look.x * mouseSensitivityX;
            mouseY = look.y * mouseSensitivityY * (invertY ? 1f : -1f);
        }

        flashlightYaw += mouseX;

        if (yawConstrained)
            flashlightYaw = ClampAngleAroundCenter(
                flashlightYaw,
                yawConstraintCenter,
                yawConstraintRange
            );

        flashlightPitch = Mathf.Clamp(
            flashlightPitch + mouseY,
            minPitch,
            maxPitch
        );

        UpdateCameraCatchUp(Time.deltaTime, isGamepad);
        ApplyRotations();
    }

    private void UpdateCameraCatchUp(float deltaTime, bool isGamepad)
    {
        float yawOffset = Mathf.DeltaAngle(cameraYaw, flashlightYaw);
        float excessYaw = Mathf.Max(Mathf.Abs(yawOffset) - maxOffsetAngle, 0f) * Mathf.Sign(yawOffset);

        if (Mathf.Abs(excessYaw) > 0.001f)
        {
            cameraYaw = Mathf.LerpAngle(
                cameraYaw,
                cameraYaw + excessYaw,
                cameraCatchUpSpeed * deltaTime
            );
        }

        float pitchOffset = flashlightPitch - cameraPitch;
        float excessPitch = Mathf.Max(Mathf.Abs(pitchOffset) - maxOffsetAngle, 0f) * Mathf.Sign(pitchOffset);

        if (Mathf.Abs(excessPitch) > 0.001f)
        {
            float catchUpSpeed = isGamepad
                ? gamepadPitchCatchUpSpeed
                : cameraCatchUpSpeed;

            cameraPitch = Mathf.Lerp(
                cameraPitch,
                cameraPitch + excessPitch,
                catchUpSpeed * deltaTime
            );
        }

        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        if (yawConstrained)
            cameraYaw = ClampAngleAroundCenter(
                cameraYaw,
                yawConstraintCenter,
                yawConstraintRange
            );
    }

    private float ClampAngleAroundCenter(float angle, float center, float range)
    {
        float delta = Mathf.DeltaAngle(center, angle);
        delta = Mathf.Clamp(delta, -range, range);
        return center + delta;
    }

    private void ApplyRotations()
    {
        if (playerBody != null)
            playerBody.rotation = Quaternion.Euler(0f, cameraYaw, 0f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        if (flashlightPivot != null)
        {
            float clampedYawOffset = Mathf.Clamp(
                Mathf.DeltaAngle(cameraYaw, flashlightYaw),
                -maxOffsetAngle,
                maxOffsetAngle
            );

            float clampedPitchOffset = Mathf.Clamp(
                flashlightPitch - cameraPitch,
                -maxOffsetAngle,
                maxOffsetAngle
            );

            flashlightPivot.localRotation = Quaternion.Euler(
                clampedPitchOffset,
                clampedYawOffset,
                0f
            );
        }
    }
}
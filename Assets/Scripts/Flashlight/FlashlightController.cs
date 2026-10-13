using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private BatteryInventory batteryInventory;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference toggleAction;
    [SerializeField] private InputActionReference adjustSizeUpAction;
    [SerializeField] private InputActionReference adjustSizeDownAction;
    [SerializeField] private InputActionReference reloadBatteryAction;

    [Header("Battery")]
    [SerializeField, Range(0f, 1f)] private float currentBattery = 1f;
    [SerializeField] private float batteryLifeAtMinSizeSeconds = 300f;
    [SerializeField] private float batteryLifeAtMaxSizeSeconds = 60f;
    [SerializeField, Range(0f, 1f)] private float maxBatteryLevelToReload = 0.75f;
    [SerializeField] private float reloadDuration = 1f;

    [Header("Reload Animation")]
    [Tooltip("Model latarki (dziecko kamery) animowany podczas przeładowania. Puste = brak animacji.")]
    [SerializeField] private Transform flashlightModel;
    [Tooltip("Przesunięcie w pełni opuszczonej pozie (krzywa = 1), w przestrzeni rodzica (kamery).")]
    [SerializeField] private Vector3 reloadPositionOffset = new Vector3(0f, -0.15f, 0f);
    [Tooltip("Obrót w pełni opuszczonej pozie (Euler), w osiach lokalnych modelu.")]
    [SerializeField] private Vector3 reloadRotationOffset = new Vector3(20f, 0f, 0f);
    [Tooltip("Ruch w czasie: X = postęp przeładowania (0-1), Y = 0 pozycja spoczynkowa, 1 = pełne opuszczenie, wartości ujemne = ponad spoczynek. Powinna zaczynać i kończyć się na 0.")]
    [SerializeField]
    private AnimationCurve reloadMotionCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.22f, 1f),
        new Keyframe(0.48f, 1f),
        new Keyframe(0.6f, -0.3f),
        new Keyframe(0.72f, 0.2f),
        new Keyframe(1f, 0f));

    [Header("Light Size (scroll)")]
    [SerializeField, Range(0f, 1f)] private float lightSize = 0.5f;
    [SerializeField] private float lightSizeStep = 0.1f;
    [SerializeField] private float minRange = 5f;
    [SerializeField] private float maxRange = 20f;
    [SerializeField] private float minIntensity = 1f;
    [SerializeField] private float maxIntensity = 5f;
    [SerializeField] private float minInnerSpotAngle = 0f;
    [SerializeField] private float maxInnerSpotAngle = 50f;

    [SerializeField] private float minOuterSpotAngle = 50f;
    [SerializeField] private float maxOuterSpotAngle = 80f;

    [SerializeField] private AudioClip flashlightOnClip;
    [SerializeField] private AudioClip flashlightOffClip;
    [SerializeField] private AudioClip reloadBatteryClip;
    [SerializeField] private AudioClip batteryIsOverClip;
    [Header("Addictional Light")]
    [SerializeField] private Light addictionallight;

    private bool isOn;
    public bool IsOn => isOn;
    private bool isReloading;
    private bool isReloadAnimating;
    private Vector3 reloadBasePosition;
    private Quaternion reloadBaseRotation;

    public float BatteryLevel01 => currentBattery;
    public float LightSize01 => lightSize;

    public bool CanReload => !isReloading
        && batteryInventory != null
        && batteryInventory.CurrentBatteries > 0
        && currentBattery < maxBatteryLevelToReload;

    public event Action<bool> OnCanReloadChanged;
    private bool lastCanReload;

    private void CheckCanReloadChanged()
    {
        bool current = CanReload;
        if (current == lastCanReload)
            return;

        lastCanReload = current;
        OnCanReloadChanged?.Invoke(current);
    }

    private void Awake()
    {
        if (flashlightLight == null)
            flashlightLight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        toggleAction.action.Enable();
        toggleAction.action.performed += OnTogglePerformed;

        adjustSizeUpAction.action.Enable();
        adjustSizeUpAction.action.performed += AdjustSizeUpInput;

        adjustSizeDownAction.action.Enable();
        adjustSizeDownAction.action.performed += AdjustSizeDownInput;

        reloadBatteryAction.action.Enable();
        reloadBatteryAction.action.performed += OnReloadBatteryPerformed;

        if (batteryInventory != null)
            batteryInventory.OnBatteryCountChanged += HandleBatteryCountChanged;
    }

    private void OnDisable()
    {
        if (isReloadAnimating)
            RestoreReloadPose();

        toggleAction.action.performed -= OnTogglePerformed;
        toggleAction.action.Disable();

        reloadBatteryAction.action.performed -= OnReloadBatteryPerformed;
        reloadBatteryAction.action.Disable();

        adjustSizeUpAction.action.performed -= AdjustSizeUpInput;
        adjustSizeUpAction.action.Disable();

        adjustSizeDownAction.action.performed -= AdjustSizeDownInput;
        adjustSizeDownAction.action.Disable();
        if (batteryInventory != null)
            batteryInventory.OnBatteryCountChanged -= HandleBatteryCountChanged;
    }

    private void HandleBatteryCountChanged(int count) => CheckCanReloadChanged();

    private void Start()
    {
        ApplyLightSize();
        UpdateLightState();
        lastCanReload = CanReload;
    }

    public void DisableFlashlight()
    {
        if (currentBattery <= 0f)
            return;

        isOn = false;
        ActiveAddictionalLight();
        AudioManager.Instance.PlaySFX(flashlightOffClip);
        UpdateLightState();
    }

    public void EnabledFlashlight()
    {
        if (currentBattery <= 0f)
            return;

        isOn = true;
        DisableAddictionalLight();
        AudioManager.Instance.PlaySFX(flashlightOnClip);
        UpdateLightState();
    }

    private void OnTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (ChaseMinigameController.Instance != null && ChaseMinigameController.Instance.IsActive)
            return;

        if (currentBattery <= 0f || isReloading)
            return;

        isOn = !isOn;

        if (isOn)
        {
            AudioManager.Instance.PlaySFX(flashlightOffClip);
        }
        else
        {
            AudioManager.Instance.PlaySFX(flashlightOnClip);
        }

        UpdateLightState();
    }

    private void Update()
    {
        if (isOn)
            DrainBattery(Time.deltaTime);
    }

    private void AdjustSizeUpInput(InputAction.CallbackContext ctx)
    {
        if (ChaseMinigameController.Instance != null && ChaseMinigameController.Instance.IsActive)
            return;

        lightSize += lightSizeStep;

        if (lightSize > 1f)
            lightSize = 1f;

        ApplyLightSize();
    }

    private void AdjustSizeDownInput(InputAction.CallbackContext ctx)
    {
        if (ChaseMinigameController.Instance != null && ChaseMinigameController.Instance.IsActive)
            return;

        lightSize -= lightSizeStep;

        if (lightSize < 0f)
            lightSize = 0f;

        ApplyLightSize();
    }

    private void ApplyLightSize()
    {
        flashlightLight.range = Mathf.Lerp(minRange, maxRange, lightSize);
        flashlightLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, lightSize);

        flashlightLight.innerSpotAngle =
            Mathf.Lerp(minInnerSpotAngle, maxInnerSpotAngle, lightSize);

        flashlightLight.spotAngle =
            Mathf.Lerp(minOuterSpotAngle, maxOuterSpotAngle, lightSize);
    }

    private void OnReloadBatteryPerformed(InputAction.CallbackContext ctx)
    {
        if (ChaseMinigameController.Instance != null && ChaseMinigameController.Instance.IsActive)
            return;

        if (!CanReload)
            return;

        AudioManager.Instance.PlaySFX(reloadBatteryClip);

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        CheckCanReloadChanged();

        bool wasOn = isOn;
        if (isOn)
        {
            isOn = false;
            UpdateLightState();
        }

        yield return StartCoroutine(AnimateReload());

        if (batteryInventory.ConsumeBattery())
            RefillBattery(1f);

        isOn = wasOn;
        UpdateLightState();

        DisableAddictionalLight();
        isReloading = false;
        CheckCanReloadChanged();
    }

    private IEnumerator AnimateReload()
    {
        if (flashlightModel == null)
        {
            yield return new WaitForSeconds(reloadDuration);
            yield break;
        }

        reloadBasePosition = flashlightModel.localPosition;
        reloadBaseRotation = flashlightModel.localRotation;
        isReloadAnimating = true;

        float elapsed = 0f;

        while (elapsed < reloadDuration)
        {
            elapsed += Time.deltaTime;

            float weight = reloadMotionCurve.Evaluate(elapsed / reloadDuration);

            flashlightModel.localPosition = reloadBasePosition + reloadPositionOffset * weight;
            flashlightModel.localRotation = reloadBaseRotation * Quaternion.Euler(reloadRotationOffset * weight);

            yield return null;
        }

        RestoreReloadPose();
    }

    private void RestoreReloadPose()
    {
        isReloadAnimating = false;

        if (flashlightModel == null)
            return;

        flashlightModel.localPosition = reloadBasePosition;
        flashlightModel.localRotation = reloadBaseRotation;
    }

    private void DrainBattery(float deltaTime)
    {
        float secondsOfLifeAtCurrentSize = Mathf.Lerp(batteryLifeAtMinSizeSeconds, batteryLifeAtMaxSizeSeconds, lightSize);
        float drainPerSecond = 1f / secondsOfLifeAtCurrentSize;

        currentBattery -= drainPerSecond * deltaTime;

        if (currentBattery <= 0f)
        {
            currentBattery = 0f;
            isOn = false;

            AudioManager.Instance.PlaySFX(batteryIsOverClip);

            UpdateLightState();
            ActiveAddictionalLight();
        }

        CheckCanReloadChanged();
    }

    private void ActiveAddictionalLight()
    {
        StartCoroutine(BatteryAddictionalLight(1f));
    }

    private void DisableAddictionalLight()
    {
        StartCoroutine(BatteryAddictionalLight(0f));
    }

    private IEnumerator BatteryAddictionalLight(float targetIntensity)
    {
        float startIntensity = addictionallight.intensity;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            addictionallight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }

        addictionallight.intensity = targetIntensity;
    }

    private void UpdateLightState()
    {
        flashlightLight.enabled = isOn && currentBattery > 0f;
    }

    public void RefillBattery(float amount01)
    {
        currentBattery = Mathf.Clamp01(currentBattery + amount01);
        CheckCanReloadChanged();
    }

#if UNITY_EDITOR
    [ContextMenu("Test/Play Reload Animation")]
    private void TestPlayReloadAnimation()
    {
        if (Application.isPlaying && !isReloadAnimating)
            StartCoroutine(AnimateReload());
    }
#endif
}
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-1000)]
public class FearCameraEffect : MonoBehaviour
{
    public static FearCameraEffect Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float volumePriority = 100f;

    [Header("Wartości bazowe (takie jak w głównym Volume sceny)")]
    [SerializeField] private float lensDistortionBase = 0f;
    [SerializeField] private float filmGrainBase = 0f;
    [SerializeField] private float vignetteIntensityBase = 0.5f;
    [SerializeField] private float vignetteSmoothnessBase = 0.2f;
    [SerializeField] private float chromaticAberrationIntensityBase = 0f;
    [SerializeField] private float bloomIntensityBase = 0.35f;

    public bool IsPlaying => preset != null;
    public FearEffectPreset CurrentPreset => preset;

    private Transform cameraTransform;

    private Volume fearVolume;
    private VolumeProfile fearProfile;
    private LensDistortion lensDistortion;
    private FilmGrain filmGrain;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private Bloom bloom;

    private FearEffectPreset preset;
    private float intensity;
    private float elapsed;
    private float noiseSeed;
    private float rollSign;

    private bool stopping;
    private float stopDuration;
    private float stopElapsed;

    private bool offsetApplied;
    private Quaternion baseRotation;
    private Quaternion appliedRotation;
    private Vector3 basePosition;
    private Vector3 appliedPosition;
    private float baseFov;
    private float appliedFov;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[FearCameraEffect] Na scenie jest już inna instancja - ten komponent zostaje wyłączony.", this);
            enabled = false;
            Destroy(this);
            return;
        }

        Instance = this;

        cameraTransform = targetCamera.transform;
        CreateVolume();
    }

    private void OnDisable()
    {
        RestoreCamera();
        Finish();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (fearProfile != null)
            Destroy(fearProfile);
    }

    // Nadpisywane są tylko intensity/smoothness (overrideState ustawia UpdateVolume), więc reszta
    // parametrów tych efektów (kolor winiety, typ ziarna itd.) zostaje z głównego Volume.
    private void CreateVolume()
    {
        fearProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        lensDistortion = fearProfile.Add<LensDistortion>(false);
        filmGrain = fearProfile.Add<FilmGrain>(false);
        vignette = fearProfile.Add<Vignette>(false);
        chromaticAberration = fearProfile.Add<ChromaticAberration>(false);
        bloom = fearProfile.Add<Bloom>(false);
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.8f;
        bloom.filter.overrideState = true;
        bloom.filter.value = BloomFilterMode.Dual;

        GameObject volumeObject = new GameObject("FearEffectVolume");
        volumeObject.transform.SetParent(transform, false);

        fearVolume = volumeObject.AddComponent<Volume>();
        fearVolume.isGlobal = true;

        fearVolume.priority = volumePriority;
        fearVolume.weight = 1f;
        fearVolume.profile = fearProfile;
        fearVolume.enabled = false;
    }

    [SerializeField] private FearEffectPreset fearEffect;

    [ContextMenu("Play")]
    public void Play()
    {
        Play(fearEffect);
    }

    /// <summary>Intensity skaluje cały efekt (1 = wartości z presetu). Trwający efekt przerywa tylko preset o priorytecie równym lub wyższym.</summary>
   
    public void Play(FearEffectPreset newPreset, float newIntensity = 1f)
    {
        if (newPreset == null || newIntensity <= 0f || !isActiveAndEnabled)
            return;

        if (preset != null && newPreset.priority < preset.priority)
            return;

        preset = newPreset;
        intensity = newIntensity;
        elapsed = 0f;
        stopping = false;
        noiseSeed = Random.value * 100f;
        rollSign = Random.value < 0.5f ? -1f : 1f;

        // Wartości startowe ustawiamy od razu, żeby pierwsza klatka nie pokazała pozostałości po poprzednim efekcie.
        UpdateVolume(0f, intensity);
        fearVolume.enabled = true;
    }

    public void Stop(float fadeOutDuration = 0.25f)
    {
        if (preset == null)
            return;

        stopping = true;
        stopDuration = Mathf.Max(0.01f, fadeOutDuration);
        stopElapsed = 0f;
    }

    private void Update()
    {
        RestoreCamera();
    }

    private void LateUpdate()
    {
        if (preset == null)
            return;

        float deltaTime = Time.deltaTime;
        elapsed += deltaTime;

        float fade = 1f;
        if (stopping)
        {
            stopElapsed += deltaTime;
            fade = 1f - Mathf.Clamp01(stopElapsed / stopDuration);
        }

        float normalizedTime = Mathf.Clamp01(elapsed / preset.duration);
        if (normalizedTime >= 1f || fade <= 0f)
        {
            Finish();
            return;
        }

        float strength = intensity * fade;

        UpdateVolume(normalizedTime, strength);
        ApplyCameraOffsets(normalizedTime, strength);
    }

    private void UpdateVolume(float normalizedTime, float strength)
    {
        Animate(lensDistortion.intensity, preset.lensDistortionIntensity, lensDistortionBase, normalizedTime, strength);
        Animate(filmGrain.intensity, preset.filmGrainIntensity, filmGrainBase, normalizedTime, strength);
        Animate(vignette.intensity, preset.vignetteIntensity, vignetteIntensityBase, normalizedTime, strength);
        Animate(vignette.smoothness, preset.vignetteSmoothness, vignetteSmoothnessBase, normalizedTime, strength);  
        Animate(chromaticAberration.intensity, preset.chromaticAberrationIntensity, chromaticAberrationIntensityBase, normalizedTime, strength);
        Animate(bloom.intensity, preset.bloomIntensity, bloomIntensityBase, normalizedTime, strength);
    }

    private static void Animate(VolumeParameter<float> parameter, VolumeParameterAnimation animation, float baseValue, float normalizedTime, float strength)
    {
        parameter.overrideState = animation.enabled;

        if (animation.enabled)
            parameter.value = animation.Evaluate(baseValue, normalizedTime, strength);
    }

    private void ApplyCameraOffsets(float normalizedTime, float strength)
    {
        float cameraStrength = Mathf.Max(0f, preset.cameraEnvelope.Evaluate(normalizedTime)) * strength;
        if (cameraStrength <= 0f)
            return;

        float noiseTime = elapsed * preset.shakeFrequency;

        Vector3 positionOffset = new Vector3(Noise(noiseTime, 0), Noise(noiseTime, 1), Noise(noiseTime, 2))
            * (preset.positionShake * cameraStrength);

        Vector3 shakeEuler = new Vector3(Noise(noiseTime, 3), Noise(noiseTime, 4), Noise(noiseTime, 5))
            * (preset.rotationShake * cameraStrength);

        Vector3 eulerOffset = shakeEuler + new Vector3(
            -preset.flinchPitch * cameraStrength,
            0f,
            preset.rollKick * rollSign * cameraStrength);

        baseRotation = cameraTransform.localRotation;
        basePosition = cameraTransform.localPosition;
        baseFov = targetCamera.fieldOfView;

        appliedRotation = baseRotation * Quaternion.Euler(eulerOffset);
        appliedPosition = basePosition + positionOffset;
        appliedFov = Mathf.Clamp(baseFov + preset.fovKick * cameraStrength, 1f, 170f);

        cameraTransform.localRotation = appliedRotation;
        cameraTransform.localPosition = appliedPosition;
        targetCamera.fieldOfView = appliedFov;

        offsetApplied = true;
    }

    // Wartość bazową przywracamy tylko wtedy, gdy nikt nie zdążył jej nadpisać - inaczej zostawiamy to, co ustawił inny skrypt.
    private void RestoreCamera()
    {
        if (!offsetApplied || cameraTransform == null || targetCamera == null)
            return;

        offsetApplied = false;

        if (Quaternion.Angle(cameraTransform.localRotation, appliedRotation) < 0.01f)
            cameraTransform.localRotation = baseRotation;

        if ((cameraTransform.localPosition - appliedPosition).sqrMagnitude < 1e-8f)
            cameraTransform.localPosition = basePosition;

        if (Mathf.Abs(targetCamera.fieldOfView - appliedFov) < 0.001f)
            targetCamera.fieldOfView = baseFov;
    }

    private void Finish()
    {
        preset = null;
        stopping = false;

        if (fearVolume != null)
            fearVolume.enabled = false;
    }

    private float Noise(float time, int axis)
    {
        return Mathf.PerlinNoise(time, noiseSeed + axis * 17.31f) * 2f - 1f;
    }
}
using System.Collections.Generic;
using UnityEngine;

public class FireEffect : MonoBehaviour
{
    private static readonly List<FireEffect> burningWithLight = new List<FireEffect>();
    private static int lastBudgetFrame = -1;
    private static float nextBudgetTime;
    private static AudioListener listener;

    public static int MaxActiveLights { get; set; } = 4;

    [Header("Płomienie")]
    [SerializeField] private float growDuration = 2.5f;
    [SerializeField, Range(0.05f, 1f)] private float startScale = 0.2f;

    [Header("Światło")]
    [SerializeField] private Light fireLight;
    [SerializeField] private float lightIntensity = 2.5f;
    [SerializeField, Range(0f, 1f)] private float flickerAmount = 0.35f;
    [SerializeField] private float flickerSpeed = 7f;
    [SerializeField] private float lightFadeSpeed = 2f;

    [Header("Dźwięk")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource igniteSource;
    [SerializeField] private AudioClip loopClip;
    [SerializeField] private AudioClip igniteClip;
    [SerializeField, Range(0f, 1f)] private float loopVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float igniteVolume = 0.8f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    private ParticleSystem[] systems;
    private ParticleSystem rootSystem;
    private Vector3 originalScale;
    private Vector3 baseScale;
    private float burnTime;
    private float growWeight;
    private float lightWeight;
    private bool lightAllowed;
    private float noiseSeed;
    private bool burning;
    private bool initialized;
    private bool audioOutOfRange;
    private bool silenced;
    private float silenceWeight = 1f;

    public bool IsBurning => burning;

    public void SilenceAudio()
    {
        silenced = true;
    }

    private void Awake()
    {
        Initialize();

        if (!burning)
            StopInstant();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        systems = GetComponentsInChildren<ParticleSystem>(true);
        rootSystem = GetComponent<ParticleSystem>();
        originalScale = transform.localScale;
        baseScale = originalScale;
        noiseSeed = Random.Range(0f, 100f);

        if (fireLight != null)
        {
            fireLight.enabled = false;
            fireLight.intensity = 0f;
        }
    }

    private void OnEnable()
    {
        if (burning && fireLight != null && !burningWithLight.Contains(this))
            burningWithLight.Add(this);
    }

    private void OnDisable()
    {
        burningWithLight.Remove(this);
    }

    public void Ignite(float sizeMultiplier = 1f)
    {
        Initialize();

        if (burning)
            return;

        burning = true;
        baseScale = originalScale * sizeMultiplier;
        burnTime = 0f;
        growWeight = 0f;
        transform.localScale = baseScale * startScale;

        if (rootSystem != null)
            rootSystem.Play(true);
        else
            foreach (ParticleSystem system in systems)
                system.Play(false);

        if (fireLight != null && !burningWithLight.Contains(this))
            burningWithLight.Add(this);

        StartAudio();
    }

    public void StopInstant()
    {
        Initialize();

        burning = false;
        growWeight = 0f;
        lightWeight = 0f;
        burningWithLight.Remove(this);

        foreach (ParticleSystem system in systems)
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (audioSource != null)
            audioSource.Stop();

        if (fireLight != null)
            fireLight.enabled = false;
    }

    private void StartAudio()
    {
        if (audioSource == null)
            return;

        audioSource.spatialBlend = 1f;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.volume = 0f;

        if (AudioManager.Instance != null && igniteClip != null && igniteSource != null)
            AudioManager.Instance.PlaySFX3D(igniteSource, igniteClip, igniteVolume);

        if (loopClip == null)
            return;

        audioSource.clip = loopClip;
        audioSource.time = Random.Range(0f, loopClip.length * 0.95f);
        audioSource.Play();
        audioOutOfRange = false;
    }

    private void Update()
    {
        if (!burning)
            return;

        float dt = Time.deltaTime;
        burnTime += dt;
        growWeight = Mathf.Clamp01(burnTime / Mathf.Max(growDuration, 0.01f));
        float grow = 1f - (1f - growWeight) * (1f - growWeight);

        if (growWeight < 1f)
            transform.localScale = baseScale * Mathf.Lerp(startScale, 1f, grow);
        else if (transform.localScale != baseScale)
            transform.localScale = baseScale;

        UpdateAudio(grow);
        UpdateLight(grow, dt);
    }

    private void UpdateAudio(float grow)
    {
        if (audioSource == null)
            return;

        AudioManager manager = AudioManager.Instance;
        float sfxVolume = manager != null ? manager.SFXVolume : 1f;

        silenceWeight = Mathf.MoveTowards(silenceWeight, silenced ? 0f : 1f, Time.unscaledDeltaTime / 1.5f);
        audioSource.volume = loopVolume * grow * sfxVolume * silenceWeight;
        audioSource.mute = manager != null && manager.IsGameplayAudioPaused;

        if (audioSource.clip == null)
            return;

        bool outOfRange = IsOutOfHearingRange();
        if (outOfRange == audioOutOfRange)
            return;

        audioOutOfRange = outOfRange;

        if (outOfRange)
            audioSource.Pause();
        else
            audioSource.UnPause();
    }

    private bool IsOutOfHearingRange()
    {
        if (listener == null || !listener.isActiveAndEnabled)
            listener = FindFirstObjectByType<AudioListener>();

        if (listener == null)
            return false;

        float range = audioSource.maxDistance * 1.05f;
        return (listener.transform.position - transform.position).sqrMagnitude > range * range;
    }

    private void UpdateLight(float grow, float dt)
    {
        if (fireLight == null)
            return;

        RefreshLightBudget();

        lightWeight = Mathf.MoveTowards(lightWeight, lightAllowed ? 1f : 0f, dt * lightFadeSpeed);

        if (lightWeight <= 0.001f)
        {
            if (fireLight.enabled)
                fireLight.enabled = false;

            return;
        }

        float time = Time.time * flickerSpeed;
        float flicker = Mathf.PerlinNoise(noiseSeed, time) * 0.7f + Mathf.PerlinNoise(noiseSeed + 31.7f, time * 2.3f) * 0.3f;
        fireLight.intensity = lightIntensity * grow * lightWeight * (1f - flickerAmount + flickerAmount * 2f * flicker);
        fireLight.enabled = true;
    }

    private static void RefreshLightBudget()
    {
        if (lastBudgetFrame == Time.frameCount || Time.unscaledTime < nextBudgetTime)
            return;

        lastBudgetFrame = Time.frameCount;
        nextBudgetTime = Time.unscaledTime + 0.25f;

        Camera camera = Camera.main;
        Vector3 listener = camera != null ? camera.transform.position : Vector3.zero;

        burningWithLight.RemoveAll(fire => fire == null);
        burningWithLight.Sort((a, b) =>
            (a.transform.position - listener).sqrMagnitude.CompareTo((b.transform.position - listener).sqrMagnitude));

        for (int i = 0; i < burningWithLight.Count; i++)
            burningWithLight[i].lightAllowed = i < MaxActiveLights;
    }
}

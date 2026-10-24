using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource breathingSource;
    [SerializeField] private AudioSource heartbeatSource;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float jumpscareVolumeBoost = 0.1f;

    [Header("Breathing")]
    [SerializeField] private AudioLowPassFilter breathingLowPassFilter;
    [SerializeField] private Vector2 breathingPitchRange = new Vector2(1f, 1.4f);
    [SerializeField] private Vector2 breathingVolumeRange = new Vector2(0.5f, 1f);
    [SerializeField] private Vector2 breathingLowPassRange = new Vector2(22000f, 900f);
    [SerializeField] private float breathingIntensitySmoothDuration = 0.5f;

    [Header("Heartbeat")]
    [SerializeField] private Vector2 heartbeatPitchRange = new Vector2(1f, 1.5f);
    [SerializeField] private Vector2 heartbeatVolumeRange = new Vector2(0.4f, 1f);
    [SerializeField] private float heartbeatIntensitySmoothDuration = 0.5f;

    private Coroutine musicFadeRoutine;
    private Coroutine breathingPitchRoutine;
    private Coroutine heartbeatIntensityRoutine;
    private bool usingSourceA = true;
    private bool gameplayAudioPaused;

    public float SFXVolume => sfxVolume;
    public bool IsGameplayAudioPaused => gameplayAudioPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSourceA != null)
            musicSourceA.loop = true;

        if (musicSourceB != null)
        {
            musicSourceB.loop = true;
            musicSourceB.volume = 0f;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = 1f;
            sfxSource.mute = false;
        }

        if (uiSource != null)
        {
            uiSource.volume = 1f;
            uiSource.mute = false;
        }
        if (breathingSource != null)
        {
            breathingSource.volume = 1f;
            breathingSource.mute = false;
        }

        if (heartbeatSource != null)
        {
            heartbeatSource.volume = 1f;
            heartbeatSource.mute = false;
        }
    }


    public void PlayMusic(AudioClip clip, float fadeDuration = 1.5f)
    {
        if (clip == null)
            return;

        AudioSource current = usingSourceA ? musicSourceA : musicSourceB;
        if (current != null && current.clip == clip && current.isPlaying)
            return;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(CrossfadeMusic(clip, fadeDuration));
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeOutMusic(fadeDuration));
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        AudioSource from = usingSourceA ? musicSourceA : musicSourceB;
        AudioSource to = usingSourceA ? musicSourceB : musicSourceA;

        if (to == null)
        {
            if (from != null)
            {
                from.clip = newClip;
                from.loop = true;
                from.volume = musicVolume;
                from.Play();
            }
            yield break;
        }

        to.clip = newClip;
        to.loop = true;
        to.volume = 0f;
        to.Play();

        float fromStartVolume = from != null ? from.volume : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (from != null)
                from.volume = Mathf.Lerp(fromStartVolume, 0f, t);

            to.volume = Mathf.Lerp(0f, musicVolume, t);

            yield return null;
        }

        if (from != null)
        {
            from.Stop();
            from.volume = 0f;
        }

        to.volume = musicVolume;
        usingSourceA = !usingSourceA;
        musicFadeRoutine = null;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        AudioSource current = usingSourceA ? musicSourceA : musicSourceB;
        if (current == null)
            yield break;

        float startVolume = current.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            current.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        current.Stop();
        current.volume = 0f;
        musicFadeRoutine = null;
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public float JumpscareVolume => sfxVolume <= 0f ? 0f : sfxVolume + jumpscareVolumeBoost;

    public void PlayJumpscareSFX(AudioClip clip)
    {
        if (clip == null || JumpscareVolume <= 0f)
            return;

        sfxSource.PlayOneShot(clip, JumpscareVolume);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position)
    {
        AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
    }

    public void PlaySFX3D(AudioSource emitter, AudioClip clip, float volumeScale = 1f)
    {
        if (emitter == null || clip == null || !IsWithinEmitterRange(emitter))
            return;

        PrepareEmitter3D(emitter);
        emitter.PlayOneShot(clip, volumeScale);
    }

    private void PrepareEmitter3D(AudioSource emitter)
    {
        emitter.spatialBlend = 1f;
        emitter.volume = sfxVolume;
        emitter.mute = gameplayAudioPaused;

        if (emitter.outputAudioMixerGroup == null && sfxSource != null)
            emitter.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
    }

    private static bool IsWithinEmitterRange(AudioSource emitter)
    {
        Camera listenerCamera = Camera.main;
        if (listenerCamera == null)
            return true;

        float maxDistance = emitter.maxDistance;
        return (emitter.transform.position - listenerCamera.transform.position).sqrMagnitude <= maxDistance * maxDistance;
    }


    public void PlayUISound(AudioClip clip)
    {
        uiSource.PlayOneShot(clip, uiVolume);
    }

    public void SetGameplayAudioPaused(bool paused)
    {
        gameplayAudioPaused = paused;

        if (sfxSource != null)
            sfxSource.mute = paused;

        if (breathingSource != null)
            breathingSource.mute = paused;
    }

    public void StartMonsterRoar(AudioSource emitter, AudioClip[] roarClips)
    {
        if (emitter == null || roarClips == null || roarClips.Length == 0)
            return;

        PrepareEmitter3D(emitter);
        emitter.clip = roarClips[Random.Range(0, roarClips.Length)];
        emitter.loop = false;
        emitter.Play();
    }

    public void StopMonsterRoar(AudioSource emitter)
    {
        if (emitter != null)
            emitter.Stop();
    }

    public void StartBreathing(AudioClip breathingClip)
    {
        breathingSource.clip = breathingClip;
        breathingSource.loop = true;
        breathingSource.pitch = breathingPitchRange.x;
        breathingSource.volume = breathingVolumeRange.x;
        breathingSource.Play();
    }

    public void StopBreathing()
    {
        if (breathingSource != null)
            breathingSource.Stop();
    }

    public void SetBreathingIntensity(float fearPercent01)
    {
        fearPercent01 = Mathf.Clamp01(fearPercent01);

        float targetPitch = Mathf.Lerp(breathingPitchRange.x, breathingPitchRange.y, fearPercent01);
        float targetVolume = Mathf.Lerp(breathingVolumeRange.x, breathingVolumeRange.y, fearPercent01);
        float targetCutoff = Mathf.Lerp(breathingLowPassRange.x, breathingLowPassRange.y, fearPercent01);

        if (breathingPitchRoutine != null)
            StopCoroutine(breathingPitchRoutine);

        breathingPitchRoutine = StartCoroutine(SmoothBreathingIntensity(targetPitch, targetVolume, targetCutoff));
    }

    private IEnumerator SmoothBreathingIntensity(float targetPitch, float targetVolume, float targetCutoff)
    {
        float startPitch = breathingSource.pitch;
        float startVolume = breathingSource.volume;
        float startCutoff = breathingLowPassFilter != null ? breathingLowPassFilter.cutoffFrequency : targetCutoff;

        float elapsed = 0f;

        while (elapsed < breathingIntensitySmoothDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / breathingIntensitySmoothDuration;

            breathingSource.pitch = Mathf.Lerp(startPitch, targetPitch, t);
            breathingSource.volume = Mathf.Lerp(startVolume, targetVolume, t);

            if (breathingLowPassFilter != null)
                breathingLowPassFilter.cutoffFrequency = Mathf.Lerp(startCutoff, targetCutoff, t);

            yield return null;
        }

        breathingSource.pitch = targetPitch;
        breathingSource.volume = targetVolume;

        if (breathingLowPassFilter != null)
            breathingLowPassFilter.cutoffFrequency = targetCutoff;

        breathingPitchRoutine = null;
    }

    public void StartHeartbeat(AudioClip heartbeatClip)
    {
        if (heartbeatSource == null || heartbeatClip == null)
            return;

        if (heartbeatSource.isPlaying && heartbeatSource.clip == heartbeatClip)
            return;

        heartbeatSource.clip = heartbeatClip;
        heartbeatSource.loop = true;
        heartbeatSource.pitch = heartbeatPitchRange.x;
        heartbeatSource.volume = heartbeatVolumeRange.x;
        heartbeatSource.Play();
    }

    public bool IsHeartbeatPlaying()
    {
        return heartbeatSource != null && heartbeatSource.isPlaying;
    }
    public void StopHeartbeat()
    {
        if (heartbeatSource != null)
            heartbeatSource.Stop();
    }

    public void SetHeartbeatIntensity(float intensity01)
    {
        intensity01 = Mathf.Clamp01(intensity01);

        float targetPitch = Mathf.Lerp(heartbeatPitchRange.x, heartbeatPitchRange.y, intensity01);
        float targetVolume = Mathf.Lerp(heartbeatVolumeRange.x, heartbeatVolumeRange.y, intensity01);

        if (heartbeatIntensityRoutine != null)
            StopCoroutine(heartbeatIntensityRoutine);

        heartbeatIntensityRoutine = StartCoroutine(SmoothHeartbeatIntensity(targetPitch, targetVolume));
    }

    private IEnumerator SmoothHeartbeatIntensity(float targetPitch, float targetVolume)
    {
        float startPitch = heartbeatSource.pitch;
        float startVolume = heartbeatSource.volume;
        float elapsed = 0f;

        while (elapsed < heartbeatIntensitySmoothDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / heartbeatIntensitySmoothDuration;

            heartbeatSource.pitch = Mathf.Lerp(startPitch, targetPitch, t);
            heartbeatSource.volume = Mathf.Lerp(startVolume, targetVolume, t);

            yield return null;
        }

        heartbeatSource.pitch = targetPitch;
        heartbeatSource.volume = targetVolume;
        heartbeatIntensityRoutine = null;
    }

    public void SetMusicVolume(float volume01)
    {
        musicVolume = Mathf.Clamp01(volume01);
        AudioSource active = usingSourceA ? musicSourceA : musicSourceB;
        if (active != null)
            active.volume = musicVolume;
    }

    public void SetSFXVolume(float volume01) => sfxVolume = Mathf.Clamp01(volume01);
    public void SetUIVolume(float volume01) => uiVolume = Mathf.Clamp01(volume01);
}
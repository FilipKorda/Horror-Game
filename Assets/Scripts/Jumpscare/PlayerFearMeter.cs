using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerFearMeter : MonoBehaviour
{
    [SerializeField] private int maxFearPoints = 5;
    [SerializeField] private int currentFearPoints = 0;

    [SerializeField] private AudioClip addFearClip;
    [SerializeField] private AudioClip reduceFearClip;
    [SerializeField] private AudioClip breathingClip;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private AudioClip chockingClip;
    [SerializeField] private int heartbeatStartThreshold = 3;
    [SerializeField] private PauseController pauseController;
    [SerializeField] private PlayerHider playerHider;
    [SerializeField] private GameObject dieResultPanel;
    [SerializeField] private FPSController playerController;
    [SerializeField] private CharacterController characterController;
    private bool isDead;
    private bool deathSequenceStarted;
    private bool heartbeatActive;

    public int MaxFearPoints => maxFearPoints;
    public int CurrentFearPoints => currentFearPoints;
    public float FearPercent01 => maxFearPoints > 0 ? (float)currentFearPoints / maxFearPoints : 0f;
    public int TimesCaught { get; private set; }
    public int MedicinesUsed { get; private set; }

    public event System.Action<int> OnFearChanged;

    [SerializeField] private GameObject promptPanel;
    [SerializeField] private GameObject[] allUiObjects;

    [Header("Death Animation")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private PlayerDeathFall deathFall;
    [SerializeField] private float panelDelayAfterFall = 2f;

    [Header("Death Vignette")]
    [SerializeField] private Volume deathVolume;
    [SerializeField] private float vignetteDuration = 2f;

    private void Start()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        AudioManager.Instance.StartBreathing(breathingClip);
    }

    private void AddFearClickSound()
    {
        AudioManager.Instance.PlaySFX(addFearClip);
    }

    private void ReduceFearClickSound()
    {
        AudioManager.Instance.PlaySFX(reduceFearClip);
    }

    [ContextMenu("Add Fear Test")]
    public void AddFearTest()
    {
        AddFear(1);
    }

    [ContextMenu("Reduce Fear Test")]
    public void ReduceFearTest()
    {
        ReduceFear(1);
    }

    public void AddFear(int points = 1)
    {
        if (isDead)
            return;

        AddFearClickSound();

        TimesCaught++;
        currentFearPoints = Mathf.Min(currentFearPoints + points, maxFearPoints);
        OnFearChanged?.Invoke(currentFearPoints);
        AudioManager.Instance.SetBreathingIntensity(FearPercent01);
        UpdateHeartbeat();

        if (currentFearPoints >= maxFearPoints && !deathSequenceStarted)
        {
            deathSequenceStarted = true;
            promptPanel.SetActive(false);
            StartCoroutine(WaitUntilPlayerLeavesWardrobe());
        }

    }

    private IEnumerator WaitUntilPlayerLeavesWardrobe()
    {
        while (true)
        {
            WardrobeController[] wardrobes = FindObjectsByType<WardrobeController>(FindObjectsSortMode.None);

            bool playerIsInWardrobe = false;

            foreach (WardrobeController wardrobe in wardrobes)
            {
                if (wardrobe.IsOccupied)
                {
                    playerIsInWardrobe = true;
                    break;
                }
            }

            if (!playerIsInWardrobe)
                break;

            yield return null;
        }

        yield return StartCoroutine(DieDealy());
    }

    public void ReduceFear(int points = 1)
    {
        if (isDead)
            return;

        ReduceFearClickSound();

        MedicinesUsed++;
        currentFearPoints = Mathf.Max(currentFearPoints - points, 0);
        OnFearChanged?.Invoke(currentFearPoints);
        AudioManager.Instance.SetBreathingIntensity(FearPercent01);
        UpdateHeartbeat();
    }

    private void UpdateHeartbeat()
    {
        bool shouldBeActive = currentFearPoints >= heartbeatStartThreshold;

        if (shouldBeActive && !heartbeatActive)
        {
            AudioManager.Instance.StartHeartbeat(heartbeatClip);
            heartbeatActive = true;
        }
        else if (!shouldBeActive && heartbeatActive)
        {
            AudioManager.Instance.StopHeartbeat();
            heartbeatActive = false;
        }

        if (shouldBeActive)
        {
            float intensity01 = Mathf.InverseLerp(heartbeatStartThreshold, maxFearPoints, currentFearPoints);
            AudioManager.Instance.SetHeartbeatIntensity(intensity01);
        }
    }

    private IEnumerator DeathVignetteRoutine()
    {
        if (deathVolume == null || deathVolume.profile == null)
            yield break;

        if (!deathVolume.profile.TryGet(out Vignette vignette))
            yield break;

        float startIntensity = vignette.intensity.value;
        float startSmoothness = vignette.smoothness.value;

        float elapsed = 0f;

        while (elapsed < vignetteDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / vignetteDuration);
            t = easingCurve.Evaluate(t);

            vignette.intensity.value = Mathf.Lerp(startIntensity, 1f, t);
            vignette.smoothness.value = Mathf.Lerp(startSmoothness, 1f, t);

            yield return null;
        }

        vignette.intensity.value = 1f;
        vignette.smoothness.value = 1f;
    }

    private IEnumerator DieDealy()
    {
        yield return new WaitForSeconds(5f);
        Die();
    }

    private void Die()
    {
        isDead = true;

        foreach (GameObject uiObject in allUiObjects)
        {
            if (uiObject != null)
                uiObject.SetActive(false);
        }

        playerController.SetMovementLocked(true);
        playerController.SetLookLocked(true);

        playerHider.SetLookLocked(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        pauseController.CanPause = false;

        if (characterController != null)
            characterController.enabled = false;

        StartCoroutine(DeathSequence());
        StartCoroutine(DeathVignetteRoutine());
    }

    private IEnumerator DeathSequence()
    {
        AudioManager.Instance.PlaySFX(chockingClip);

        if (deathFall == null)
            deathFall = GetComponent<PlayerDeathFall>();

        if (deathFall != null)
            yield return deathFall.Play();

        yield return new WaitForSeconds(panelDelayAfterFall);
        SetupDie();
    }

    private void SetupDie()
    {
        AudioManager.Instance.StopBreathing();

        if (playerController != null)
        {
            playerController.SetMovementLocked(true);
            playerController.SetLookLocked(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        pauseController.CanPause = false;

        if (dieResultPanel != null)
            dieResultPanel.SetActive(true);
    }

}
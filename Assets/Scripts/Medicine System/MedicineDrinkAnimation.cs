using System;
using System.Collections;
using UnityEngine;

public class MedicineDrinkAnimation : MonoBehaviour
{
    [Header("Elementy butelki")]
    [SerializeField] private Transform cap;
    [SerializeField] private Transform fluid;

    [Header("Pozycje szyjki (przestrzeń kamery)")]
    [SerializeField] private Vector3 hiddenNeckPosition = new Vector3(0.07f, -0.45f, 0.45f);
    [SerializeField] private Vector3 shownNeckPosition = new Vector3(0.08f, -0.06f, 0.45f);
    [SerializeField] private Vector3 mouthNeckPosition = new Vector3(0f, -0.13f, 0.33f);

    [Header("Obroty butelki (przestrzeń kamery)")]
    [SerializeField] private float bottleYaw = 90f;
    [SerializeField] private Vector3 shownTilt = new Vector3(-5f, 0f, 10f);
    [SerializeField] private Vector3 mouthTilt = new Vector3(-115f, 0f, 0f);
    [SerializeField] private float gulpExtraTilt = -20f;

    [Header("Czasy")]
    [SerializeField] private float raiseDuration = 0.35f;
    [SerializeField] private float openPopTime = 0.1f;
    [SerializeField] private float capFlyDuration = 0.3f;
    [SerializeField] private float toMouthDuration = 0.4f;
    [SerializeField] private float mouthBeforeFirstGulp = 0.15f;
    [SerializeField] private float[] gulpTimes = { 0.1f, 1.36f };
    [SerializeField] private float gulpDuration = 0.3f;
    [SerializeField] private float afterLastGulp = 0.1f;
    [SerializeField] private float lowerDuration = 0.35f;

    [Header("Zakrętka i płyn")]
    [SerializeField] private Vector3 capFlyOffset = new Vector3(15f, 70f, -10f);
    [SerializeField] private Vector3 capFlySpin = new Vector3(0f, 0f, 240f);
    [SerializeField, Range(0f, 1f)] private float fluidLeft = 0.35f;
    [SerializeField] private float gulpBob = 0.006f;
    [SerializeField] private float gulpPulseTilt = -6f;

    private Vector3 capLocalPosition;
    private Quaternion capLocalRotation;
    private Vector3 fluidLocalScale;
    private Vector3 neckLocalOffset;
    private bool initialized;

    private void Awake()
    {
        Initialize();
        Hide();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        if (cap != null)
        {
            capLocalPosition = cap.localPosition;
            capLocalRotation = cap.localRotation;
        }

        if (fluid != null)
            fluidLocalScale = fluid.localScale;

        neckLocalOffset = Vector3.zero;

        if (cap != null && cap.TryGetComponent(out Renderer capRenderer))
            neckLocalOffset = Vector3.Scale(transform.localScale, transform.InverseTransformPoint(capRenderer.bounds.center));

        foreach (Renderer bottleRenderer in GetComponentsInChildren<Renderer>(true))
            bottleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void Hide()
    {
        Initialize();
        ResetBottle();
        gameObject.SetActive(false);
    }

    public IEnumerator Play(Action onOpen, Action onDrink)
    {
        Initialize();
        ResetBottle();
        gameObject.SetActive(true);

        Quaternion shown = Pose(shownTilt);
        Quaternion mouth = Pose(mouthTilt);
        Quaternion gulp = Pose(mouthTilt + new Vector3(gulpExtraTilt, 0f, 0f));

        yield return Animate(raiseDuration, t =>
        {
            float e = EaseOutCubic(t);
            SetPose(Vector3.LerpUnclamped(hiddenNeckPosition, shownNeckPosition, e), shown);
        });

        onOpen?.Invoke();

        yield return Animate(openPopTime, t => SetPose(shownNeckPosition, shown));

        Coroutine capRoutine = StartCoroutine(PopCap());

        yield return Animate(capFlyDuration, t =>
        {
            float jolt = Mathf.Sin(t * Mathf.PI) * (1f - t) * 0.012f;
            SetPose(shownNeckPosition + Vector3.up * jolt, shown);
        });

        int gulpCount = gulpTimes != null ? gulpTimes.Length : 0;
        float firstGulp = gulpCount > 0 ? gulpTimes[0] : 0f;
        float lastGulp = gulpCount > 0 ? gulpTimes[gulpCount - 1] : 0f;
        float drinkSoundAt = Mathf.Clamp(toMouthDuration + mouthBeforeFirstGulp - firstGulp, 0f, toMouthDuration);
        bool drinkStarted = false;
        float drinkStartTime = 0f;

        yield return Animate(toMouthDuration, t =>
        {
            if (!drinkStarted && t * toMouthDuration >= drinkSoundAt)
            {
                drinkStarted = true;
                drinkStartTime = Time.time;
                onDrink?.Invoke();
            }

            float e = EaseInOutCubic(t);
            SetPose(Vector3.LerpUnclamped(shownNeckPosition, mouthNeckPosition, e), Quaternion.SlerpUnclamped(shown, mouth, e));
        });

        if (!drinkStarted)
        {
            drinkStartTime = Time.time;
            onDrink?.Invoke();
        }

        float drinkEnd = lastGulp + gulpDuration * 0.5f + afterLastGulp;
        float safeGulpDuration = Mathf.Max(0.01f, gulpDuration);

        while (Time.time - drinkStartTime < drinkEnd)
        {
            float now = Time.time - drinkStartTime;
            float drunk = 0f;
            float pulse = 0f;

            for (int i = 0; i < gulpCount; i++)
            {
                float p = Mathf.Clamp01((now - gulpTimes[i]) / safeGulpDuration + 0.5f);
                drunk += Mathf.SmoothStep(0f, 1f, p);

                if (p > 0f && p < 1f)
                    pulse = Mathf.Max(pulse, Mathf.Sin(p * Mathf.PI));
            }

            float share = gulpCount > 0 ? drunk / gulpCount : 1f;
            float idle = Mathf.Sin(now * 3f) * 0.002f;
            Vector3 tilt = mouthTilt + new Vector3(gulpExtraTilt * share + gulpPulseTilt * pulse, 0f, 0f);

            SetPose(mouthNeckPosition + Vector3.up * (idle + gulpBob * pulse), Pose(tilt));
            SetFluid(Mathf.Lerp(1f, fluidLeft, share));
            yield return null;
        }

        SetFluid(fluidLeft);

        yield return Animate(lowerDuration, t =>
        {
            float e = EaseInCubic(t);
            SetPose(Vector3.LerpUnclamped(mouthNeckPosition, hiddenNeckPosition, e), Quaternion.SlerpUnclamped(gulp, shown, EaseOutCubic(t)));
        });

        if (capRoutine != null)
            StopCoroutine(capRoutine);

        Hide();
    }

    private IEnumerator PopCap()
    {
        if (cap == null)
            yield break;

        Quaternion spin = Quaternion.Euler(capFlySpin);

        yield return Animate(capFlyDuration, t =>
        {
            float arc = Mathf.Sin(t * Mathf.PI * 0.5f);
            Vector3 offset = new Vector3(capFlyOffset.x * t, capFlyOffset.y * arc - capFlyOffset.y * 0.6f * t * t, capFlyOffset.z * t);
            cap.localPosition = capLocalPosition + offset;
            cap.localRotation = Quaternion.SlerpUnclamped(Quaternion.identity, spin, t) * capLocalRotation;
        });

        cap.gameObject.SetActive(false);
    }

    private Quaternion Pose(Vector3 tilt)
    {
        return Quaternion.Euler(tilt) * Quaternion.Euler(0f, bottleYaw, 0f);
    }

    private void SetPose(Vector3 neckPosition, Quaternion rotation)
    {
        transform.localRotation = rotation;
        transform.localPosition = neckPosition - rotation * neckLocalOffset;
    }

    private void SetFluid(float amount)
    {
        if (fluid == null)
            return;

        Vector3 scale = fluidLocalScale;
        scale.z *= Mathf.Max(0.01f, amount);
        fluid.localScale = scale;
    }

    private void ResetBottle()
    {
        if (cap != null)
        {
            cap.gameObject.SetActive(true);
            cap.localPosition = capLocalPosition;
            cap.localRotation = capLocalRotation;
        }

        SetFluid(1f);
        SetPose(hiddenNeckPosition, Pose(shownTilt));
    }

    private static IEnumerator Animate(float duration, Action<float> step)
    {
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(duration, 0.0001f));
            step(t);
            yield return null;
        }
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}

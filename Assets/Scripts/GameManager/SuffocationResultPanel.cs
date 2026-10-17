using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SuffocationResultPanel : ResultPanel
{
    [Header("Uduszenie")]
    [SerializeField] private Image lungsImage;
    [SerializeField] private Sprite[] lungStages;
    [SerializeField] private CanvasGroup oxygenMeterGroup;
    [SerializeField] private Image oxygenFill;
    [SerializeField] private RawImage[] smokeLayers;
    [SerializeField] private FireSpread fireSpread;

    [Header("Uduszenie - dźwięk")]
    [SerializeField] private AudioClip chokingClip;
    [SerializeField] private float[] gaspTimes = { 0.38f, 0.68f, 0.96f, 1.56f, 2.1f, 2.53f };
    [SerializeField] private AudioClip finalClip;

    [Header("Uduszenie - czas")]
    [SerializeField] private float darkenDuration = 1.1f;
    [SerializeField] private float chokeDuration = 3.1f;
    [SerializeField] private float silenceHold = 1.3f;
    [SerializeField, Range(0f, 1f)] private float startOxygen = 0.22f;

    [Header("Uduszenie - ruch")]
    [SerializeField] private Vector2 introLungsPosition = new Vector2(0f, -400f);
    [SerializeField] private float introLungsScale = 1.6f;
    [SerializeField] private float gaspPunch = 0.12f;
    [SerializeField] private float gaspShake = 8f;
    [SerializeField] private float finalShake = 24f;
    [SerializeField] private float titleShake = 12f;

    [Header("Uduszenie - dym")]
    [SerializeField, Range(0f, 1f)] private float smokeAlpha = 0.55f;
    [SerializeField, Range(0f, 1f)] private float smokeAlphaAfterReveal = 0.3f;
    [SerializeField] private float smokeRiseSpeed = 22f;
    [SerializeField] private Color smokeColor = new Color(0.16f, 0.15f, 0.15f, 1f);

    [SerializeField]
    private Gradient oxygenColor = new Gradient
    {
        colorKeys = new[]
        {
            new GradientColorKey(new Color(0.85f, 0.15f, 0.12f), 0f),
            new GradientColorKey(new Color(0.95f, 0.72f, 0.35f), 0.3f),
            new GradientColorKey(new Color(0.78f, 0.92f, 1f), 0.6f),
            new GradientColorKey(new Color(0.9f, 0.97f, 1f), 1f)
        },
        alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
    };

    private Vector2[] smokeBasePositions;
    private float smokeTime;
    private float smokeLevel;
    private float smokeBurst;
    private float lungsScale = 1f;
    private float punch;
    private float shake;
    private float oxygenTarget;
    private float oxygenShown;
    private float flash;

    protected override void Prepare()
    {
        base.Prepare();

        punch = 0f;
        shake = 0f;
        flash = 0f;
        smokeLevel = 0f;
        smokeBurst = 0f;
        smokeTime = 0f;
        lungsScale = introLungsScale;
        oxygenTarget = startOxygen;
        oxygenShown = startOxygen;

        if (fireSpread == null)
            fireSpread = FindFirstObjectByType<FireSpread>();

        if (chokingClip != null)
            chokingClip.LoadAudioData();

        if (lungsImage != null && lungStages.Length > 0)
            lungsImage.sprite = lungStages[0];

        SetGroup(oxygenMeterGroup, 0f);
        UpdateOxygenFill();

        if (smokeBasePositions == null || smokeBasePositions.Length != smokeLayers.Length)
        {
            smokeBasePositions = new Vector2[smokeLayers.Length];
            for (int i = 0; i < smokeLayers.Length; i++)
                smokeBasePositions[i] = smokeLayers[i] != null ? smokeLayers[i].rectTransform.anchoredPosition : Vector2.zero;
        }

        UpdateSmoke(0f);

        if (logoGroup != null)
            ((RectTransform)logoGroup.transform).anchoredPosition = introLungsPosition;
    }

    protected override void Update()
    {
        base.Update();

        float dt = Time.unscaledDeltaTime;
        punch *= Mathf.Exp(-dt * 6f);
        shake *= Mathf.Exp(-dt * 5f);
        flash *= Mathf.Exp(-dt * 4f);
        smokeBurst *= Mathf.Exp(-dt * 2.5f);
        oxygenShown = Mathf.Lerp(oxygenShown, oxygenTarget, 1f - Mathf.Exp(-dt * 10f));

        if (logoGroup != null)
            logoGroup.transform.localScale = Vector3.one * lungsScale * (1f + punch);

        if (content != null && TryGetBase(content, out Vector2 contentBase))
            content.anchoredPosition = shake > 0.05f ? contentBase + Random.insideUnitCircle * shake : contentBase;

        UpdateOxygenFill();
        UpdateSmoke(dt);
    }

    private void UpdateOxygenFill()
    {
        if (oxygenFill == null)
            return;

        oxygenFill.fillAmount = oxygenShown;
        oxygenFill.color = Color.Lerp(oxygenColor.Evaluate(oxygenShown), Color.white, flash);
    }

    private void UpdateSmoke(float dt)
    {
        smokeTime += dt;

        for (int i = 0; i < smokeLayers.Length; i++)
        {
            RawImage layer = smokeLayers[i];
            if (layer == null)
                continue;

            float phase = i * 1.7f;
            float speed = smokeRiseSpeed * (0.7f + 0.3f * i);
            RectTransform rect = layer.rectTransform;
            float height = rect.rect.height;
            float rise = height > 0f ? Mathf.Repeat(smokeTime * speed + phase * 90f, height * 0.5f) : 0f;
            rect.anchoredPosition = smokeBasePositions[i] + new Vector2(Mathf.Sin(smokeTime * 0.3f + phase) * 40f, rise);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(smokeTime * 0.15f + phase) * 6f);

            Color color = smokeColor;
            color.a = Mathf.Clamp01(smokeLevel + smokeBurst) * (0.8f + 0.2f * Mathf.Sin(smokeTime * 0.5f + phase));
            layer.color = color;
        }
    }

    protected override IEnumerator Intro()
    {
        yield return Tween(darkenDuration, t =>
        {
            SetAlpha(background, t);
            SetGroup(logoGroup, t);
            SetGroup(oxygenMeterGroup, t);
            smokeLevel = smokeAlpha * t;
        });

        LoadTexts();
        PlayClip(chokingClip);

        int gasp = 0;
        float duration = chokingClip != null ? Mathf.Max(chokeDuration, chokingClip.length) : chokeDuration;
        for (float elapsed = 0f; elapsed < duration;)
        {
            elapsed += Delta;

            while (gasp < gaspTimes.Length && elapsed >= gaspTimes[gasp])
            {
                Gasp(gasp);
                gasp++;
            }

            yield return null;
        }

        while (gasp < gaspTimes.Length)
        {
            Gasp(gasp);
            gasp++;
        }

        yield return Wait(silenceHold);
        yield return Tween(0.5f, t => SetGroup(oxygenMeterGroup, 1f - t));
    }

    private void Gasp(int index)
    {
        bool last = index == gaspTimes.Length - 1;
        int count = Mathf.Max(1, gaspTimes.Length);

        oxygenTarget = last ? 0f : startOxygen * (1f - (index + 1f) / count);
        punch = last ? gaspPunch * 2.2f : gaspPunch;
        shake = Mathf.Max(shake, last ? finalShake : gaspShake);
        smokeBurst += last ? 0.35f : 0.12f;
        flash = last ? 1f : 0.45f;

        if (lungStages.Length > 0)
        {
            int stage = last ? lungStages.Length - 1 : Mathf.Min(index + 1, lungStages.Length - 1);
            lungsImage.sprite = lungStages[stage];
        }

        if (last)
            PlayClip(finalClip);
    }

    protected override IEnumerator RevealLogoAndPanel()
    {
        RectTransform lungsRect = (RectTransform)logoGroup.transform;
        RectTransform panelRect = (RectTransform)panelGroup.transform;
        TryGetBase(lungsRect, out Vector2 lungsBase);
        float smokeStart = smokeLevel;

        yield return Tween(0.7f, t =>
        {
            float e = EaseOutCubic(t);
            lungsRect.anchoredPosition = Vector2.LerpUnclamped(introLungsPosition, lungsBase, e);
            lungsScale = Mathf.LerpUnclamped(introLungsScale, 1f, e);
            smokeLevel = Mathf.Lerp(smokeStart, smokeAlphaAfterReveal, e);
        });

        yield return Tween(0.5f, t =>
        {
            float e = EaseOutCubic(t);
            SetGroup(panelGroup, e);
            panelRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.94f, 1f, EaseOutBack(t));
        });
    }

    protected override void TitleEffect(float progress)
    {
        if (!TryGetBase(titleText.rectTransform, out Vector2 titleBase))
            return;

        float strength = (1f - progress) * (1f - progress) * titleShake;
        titleText.rectTransform.anchoredPosition = titleBase + new Vector2(Mathf.Sin(progress * 60f), Mathf.Cos(progress * 47f)) * strength;
    }

    protected override float GetStatValue(int index)
    {
        switch (index)
        {
            case 0:
                return EndTime;
            case 1:
                return fearMeter != null ? fearMeter.TimesCaught : 0;
            default:
                return fireSpread != null ? fireSpread.IgnitedCount : 0;
        }
    }
}

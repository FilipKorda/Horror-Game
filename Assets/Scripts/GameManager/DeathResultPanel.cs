using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeathResultPanel : ResultPanel
{
    [Header("Zawał")]
    [SerializeField] private Image brainImage;
    [SerializeField] private Sprite[] brainStages;
    [SerializeField] private Image heartMonitor;
    [SerializeField] private CanvasGroup heartMonitorGroup;
    [SerializeField] private float[] beatPositions = { 0.1f, 0.27f, 0.41f, 0.52f, 0.6f };
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private float heartbeatLead = 0.04f;
    [SerializeField] private AudioClip burstClip;

    [Header("Zawał - czas")]
    [SerializeField] private float darkenDuration = 0.9f;
    [SerializeField] private float monitorDuration = 4.2f;
    [SerializeField] private float flatlineHold = 1.2f;

    [Header("Zawał - ruch")]
    [SerializeField] private Vector2 introBrainPosition = new Vector2(0f, -400f);
    [SerializeField] private float introBrainScale = 1.6f;
    [SerializeField] private float beatPunch = 0.16f;
    [SerializeField] private float burstShake = 26f;
    [SerializeField] private float titleShake = 16f;

    private float brainScale = 1f;
    private float punch;
    private float shake;

    protected override void Prepare()
    {
        base.Prepare();

        punch = 0f;
        shake = 0f;
        brainScale = introBrainScale;

        if (heartMonitor != null)
            heartMonitor.fillAmount = 0f;

        SetGroup(heartMonitorGroup, 0f);

        if (heartbeatClip != null)
            heartbeatClip.LoadAudioData();

        if (brainImage != null && brainStages.Length > 0)
            brainImage.sprite = brainStages[0];

        if (logoGroup != null)
            ((RectTransform)logoGroup.transform).anchoredPosition = introBrainPosition;
    }

    protected override void Update()
    {
        base.Update();

        float dt = Time.unscaledDeltaTime;
        punch *= Mathf.Exp(-dt * 7f);
        shake *= Mathf.Exp(-dt * 5f);

        if (logoGroup != null)
            logoGroup.transform.localScale = Vector3.one * brainScale * (1f + punch);

        if (content != null && TryGetBase(content, out Vector2 contentBase))
            content.anchoredPosition = shake > 0.05f ? contentBase + Random.insideUnitCircle * shake : contentBase;
    }

    protected override IEnumerator Intro()
    {
        SetGroup(heartMonitorGroup, 1f);

        yield return Tween(darkenDuration, t =>
        {
            SetAlpha(background, t);
            SetGroup(logoGroup, t);
        });

        LoadTexts();

        int beat = 0;
        int sound = 0;
        float soundLead = heartbeatLead / Mathf.Max(monitorDuration, 0.01f);
        for (float elapsed = 0f; elapsed < monitorDuration;)
        {
            elapsed += Delta;
            float progress = Mathf.Clamp01(elapsed / monitorDuration);
            heartMonitor.fillAmount = progress;

            while (sound < beatPositions.Length && progress >= beatPositions[sound] - soundLead)
            {
                PlayClip(heartbeatClip);
                sound++;
            }

            while (beat < beatPositions.Length && progress >= beatPositions[beat])
            {
                Beat(beat);
                beat++;
            }

            yield return null;
        }

        yield return Wait(flatlineHold);
        yield return Tween(0.5f, t => SetGroup(heartMonitorGroup, 1f - t));
    }

    private void Beat(int index)
    {
        bool last = index == beatPositions.Length - 1;

        punch = last ? beatPunch * 2.4f : beatPunch;

        if (brainStages.Length > 0)
            brainImage.sprite = brainStages[last ? brainStages.Length - 1 : Mathf.Min(index + 1, brainStages.Length - 1)];

        if (last)
        {
            PlayClip(burstClip);
            shake = burstShake;
        }
    }

    protected override IEnumerator RevealLogoAndPanel()
    {
        RectTransform brainRect = (RectTransform)logoGroup.transform;
        RectTransform panelRect = (RectTransform)panelGroup.transform;
        TryGetBase(brainRect, out Vector2 brainBase);

        yield return Tween(0.7f, t =>
        {
            float e = EaseOutCubic(t);
            brainRect.anchoredPosition = Vector2.LerpUnclamped(introBrainPosition, brainBase, e);
            brainScale = Mathf.LerpUnclamped(introBrainScale, 1f, e);
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
        titleText.rectTransform.anchoredPosition = titleBase + Random.insideUnitCircle * strength;
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
                return fearMeter != null ? fearMeter.FearPercent01 * 100f : 100f;
        }
    }

    protected override string FormatStatValue(int index, float value)
    {
        if (index == 2)
            return Mathf.RoundToInt(value) + "%";

        return base.FormatStatValue(index, value);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UI;

public abstract class ResultPanel : MonoBehaviour
{
    [Serializable]
    protected class StatView
    {
        public CanvasGroup group;
        public TMP_Text label;
        public TMP_Text value;
        public LocalizedString labelString;
    }

    [Header("Ekran")]
    [SerializeField] protected Image background;
    [SerializeField] protected RectTransform content;
    [SerializeField] private float contentDesignHeight = 1060f;

    [Header("Elementy")]
    [SerializeField] protected CanvasGroup logoGroup;
    [SerializeField] protected CanvasGroup panelGroup;
    [SerializeField] protected TMP_Text titleText;
    [SerializeField] private RectTransform dividerLeft;
    [SerializeField] private RectTransform dividerRight;
    [SerializeField] private CanvasGroup taglineGroup;
    [SerializeField] private TMP_Text taglineText;
    [SerializeField] private CanvasGroup storyGroup;
    [SerializeField] private TMP_Text storyText;
    [SerializeField] protected StatView[] stats = new StatView[3];
    [SerializeField] private GameObject buttonHolder;
    [SerializeField] private CanvasGroup buttonsGroup;

    [Header("Lokalizacja")]
    [SerializeField] private LocalizedString titleString;
    [SerializeField] private LocalizedString taglineString;
    [SerializeField] private LocalizedString storyString;

    [Header("Statystyki")]
    [SerializeField] protected PlayerFearMeter fearMeter;

    [Header("Dźwięk")]
    [SerializeField] protected AudioClip revealClip;
    [SerializeField] private AudioClip titleClip;
    [SerializeField] private AudioClip typeClip;
    [SerializeField] private AudioClip statClip;
    [SerializeField] private AudioClip buttonsClip;
    [SerializeField, Min(1)] private int typeSoundEvery = 2;
    [SerializeField] private float musicFadeDuration = 2.5f;

    [Header("Czas")]
    [SerializeField] private float typingInterval = 0.022f;
    [SerializeField] private float punctuationPause = 0.22f;
    [SerializeField] private float typingFadeCharacters = 5f;
    [SerializeField] private float skipSpeed = 8f;

    private readonly Dictionary<RectTransform, Vector2> basePositions = new Dictionary<RectTransform, Vector2>();
    private bool allowSkip;
    private bool skipping;
    private bool finished;

    protected float EndTime { get; private set; }
    protected bool Finished => finished;
    protected float Delta => Time.unscaledDeltaTime * (skipping ? skipSpeed : 1f);

    protected abstract IEnumerator Intro();
    protected abstract float GetStatValue(int index);

    private void OnEnable()
    {
        EndTime = Time.timeSinceLevelLoad;

        StopAllCoroutines();
        Prepare();
        StopWorld();
        StartCoroutine(Sequence());
    }

    protected virtual void Update()
    {
        FitContent();

        if (allowSkip && !finished && !skipping && SkipPressed())
            skipping = true;
    }

    protected virtual void Prepare()
    {
        allowSkip = false;
        skipping = false;
        finished = false;

        CacheBase(content);
        CacheBase(logoGroup);
        CacheBase(taglineGroup);
        CacheBase(storyGroup);
        CacheBase(buttonsGroup);
        CacheBase(dividerLeft);
        CacheBase(dividerRight);
        CacheBase(titleText);
        foreach (StatView stat in stats)
            CacheBase(stat.group);

        SetAlpha(background, 0f);
        SetGroup(logoGroup, 0f);
        SetGroup(panelGroup, 0f);
        SetGroup(taglineGroup, 0f);
        SetGroup(storyGroup, 0f);
        foreach (StatView stat in stats)
            SetGroup(stat.group, 0f);

        if (titleText != null)
            titleText.alpha = 0f;

        SetDividers(0f);

        if (storyText != null)
            storyText.text = string.Empty;

        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        if (buttonHolder != null)
            buttonHolder.SetActive(false);

        FitContent();
    }

    private void StopWorld()
    {
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
            spawner.ActiveEnemySpawner(false);

        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);

        foreach (FireEffect fire in FindObjectsByType<FireEffect>(FindObjectsSortMode.None))
            fire.SilenceAudio();

        PlayerOxygen oxygen = FindFirstObjectByType<PlayerOxygen>();
        if (oxygen != null)
            oxygen.Stop();

        InteractionSystem interaction = FindFirstObjectByType<InteractionSystem>();
        if (interaction != null)
            interaction.enabled = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic(musicFadeDuration);
            AudioManager.Instance.StopBreathing();
            AudioManager.Instance.StopHeartbeat();
        }
    }

    protected void LoadTexts()
    {
        titleText.text = titleString.GetLocalizedString();
        taglineText.text = taglineString.GetLocalizedString();

        foreach (StatView stat in stats)
        {
            stat.label.text = stat.labelString.GetLocalizedString();
            stat.value.text = string.Empty;
        }

        storyText.text = storyString.GetLocalizedString();
        storyText.ForceMeshUpdate();
        ApplyTypingAlpha(0f);
    }

    private IEnumerator Sequence()
    {
        yield return Intro();

        allowSkip = true;

        PlayClip(revealClip);
        yield return RevealLogoAndPanel();

        PlayClip(titleClip);
        yield return Tween(0.9f, t =>
        {
            float e = EaseOutCubic(t);
            titleText.alpha = Mathf.Clamp01(t * 2f);
            titleText.characterSpacing = Mathf.Lerp(40f, 0f, e);
            titleText.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1.2f, 1f, EaseOutBack(t));
            SetDividers(e);
            TitleEffect(t);
        });
        TitleEffect(1f);

        yield return Tween(0.5f, t =>
        {
            float e = EaseOutCubic(t);
            SetGroup(taglineGroup, e);
            Rise(taglineGroup, 14f, e);
        });

        yield return Tween(0.45f, t =>
        {
            float e = EaseOutCubic(t);
            SetGroup(storyGroup, e);
            Rise(storyGroup, 20f, e);
        });

        yield return TypeStory();

        yield return Wait(0.25f);

        for (int i = 0; i < stats.Length; i++)
        {
            StartCoroutine(ShowStat(i));
            yield return Wait(0.22f);
        }

        yield return Wait(0.9f);

        buttonHolder.SetActive(true);
        PlayClip(buttonsClip);
        yield return Tween(0.5f, t =>
        {
            float e = EaseOutCubic(t);
            buttonsGroup.alpha = e;
            Rise(buttonsGroup, 24f, e);
        });

        buttonsGroup.interactable = true;
        buttonsGroup.blocksRaycasts = true;
        finished = true;
        skipping = false;
    }

    protected virtual IEnumerator RevealLogoAndPanel()
    {
        RectTransform panelRect = (RectTransform)panelGroup.transform;
        RectTransform logoRect = (RectTransform)logoGroup.transform;

        yield return Tween(0.6f, t =>
        {
            float e = EaseOutCubic(t);
            SetGroup(logoGroup, e);
            Rise(logoGroup, 20f, e);
            logoRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, EaseOutBack(t));
            SetGroup(panelGroup, e);
            panelRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.94f, 1f, EaseOutBack(t));
        });
    }

    protected virtual void TitleEffect(float progress)
    {
    }

    protected virtual string FormatStatValue(int index, float value)
    {
        if (index != 0)
            return Mathf.RoundToInt(value).ToString();

        int seconds = Mathf.FloorToInt(value);
        if (seconds >= 3600)
            return $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}";

        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private IEnumerator TypeStory()
    {
        TMP_TextInfo info = storyText.textInfo;
        int total = info.characterCount;
        float progress = 0f;
        float pause = 0f;
        int lastIndex = -1;

        while (progress < total + typingFadeCharacters)
        {
            if (skipping)
                progress += Time.unscaledDeltaTime * 400f;
            else if (pause > 0f)
                pause -= Time.unscaledDeltaTime;
            else
                progress += Time.unscaledDeltaTime / typingInterval;

            int index = Mathf.Min(Mathf.FloorToInt(progress), total - 1);
            while (lastIndex < index)
            {
                lastIndex++;
                char character = info.characterInfo[lastIndex].character;

                if (!skipping && !char.IsWhiteSpace(character) && lastIndex % typeSoundEvery == 0)
                    PlayClip(typeClip);

                if (!skipping && (character == '.' || character == '!' || character == '?' || character == '…'))
                    pause = punctuationPause;
            }

            ApplyTypingAlpha(progress);
            yield return null;
        }

        ApplyTypingAlpha(float.MaxValue);
    }

    private void ApplyTypingAlpha(float progress)
    {
        TMP_TextInfo info = storyText.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible)
                continue;

            float reveal = Mathf.Clamp01((progress - i) / Mathf.Max(typingFadeCharacters, 0.01f));
            byte alpha = (byte)(character.color.a * reveal);
            Color32[] colors = info.meshInfo[character.materialReferenceIndex].colors32;
            int vertex = character.vertexIndex;

            for (int v = 0; v < 4; v++)
                colors[vertex + v].a = alpha;
        }

        storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private IEnumerator ShowStat(int index)
    {
        StatView stat = stats[index];
        float target = GetStatValue(index);
        RectTransform rect = (RectTransform)stat.group.transform;

        PlayClip(statClip);
        yield return Tween(0.45f, t =>
        {
            float e = EaseOutCubic(t);
            SetGroup(stat.group, e);
            Rise(stat.group, 20f, e);
            rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, EaseOutBack(t));
            stat.value.text = FormatStatValue(index, 0f);
        });

        yield return Tween(0.8f, t => stat.value.text = FormatStatValue(index, target * EaseOutCubic(t)));
        stat.value.text = FormatStatValue(index, target);
    }

    private void SetDividers(float progress)
    {
        SetDivider(dividerLeft, progress);
        SetDivider(dividerRight, progress);
    }

    private void SetDivider(RectTransform divider, float progress)
    {
        if (divider == null || !TryGetBase(divider, out Vector2 basePosition))
            return;

        divider.anchoredPosition = new Vector2(basePosition.x * Mathf.Lerp(0.6f, 1f, progress), basePosition.y);

        if (divider.TryGetComponent(out Graphic graphic))
            SetAlpha(graphic, progress);
    }

    private void FitContent()
    {
        if (content == null || !(content.parent is RectTransform parent))
            return;

        float scale = Mathf.Min(1f, parent.rect.height / contentDesignHeight);
        content.localScale = Vector3.one * scale;
    }

    private static bool SkipPressed()
    {
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame ||
                                         Keyboard.current.enterKey.wasPressedThisFrame ||
                                         Keyboard.current.escapeKey.wasPressedThisFrame))
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                                           Gamepad.current.startButton.wasPressedThisFrame);
    }

    protected IEnumerator Tween(float duration, Action<float> step)
    {
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Delta / Mathf.Max(duration, 0.0001f));
            step(t);
            yield return null;
        }
    }

    protected IEnumerator Wait(float duration)
    {
        for (float t = 0f; t < duration; t += Delta)
            yield return null;
    }

    protected void CacheBase(Component target)
    {
        if (target == null)
            return;

        RectTransform rect = (RectTransform)target.transform;
        if (!basePositions.ContainsKey(rect))
            basePositions[rect] = rect.anchoredPosition;
    }

    protected bool TryGetBase(RectTransform rect, out Vector2 basePosition)
    {
        return basePositions.TryGetValue(rect, out basePosition);
    }

    protected void Rise(CanvasGroup group, float distance, float progress)
    {
        if (group == null)
            return;

        RectTransform rect = (RectTransform)group.transform;
        if (TryGetBase(rect, out Vector2 basePosition))
            rect.anchoredPosition = basePosition + Vector2.down * distance * (1f - progress);
    }

    protected void PlayClip(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(clip);
    }

    protected static void SetGroup(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }

    protected static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    protected static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    protected static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}

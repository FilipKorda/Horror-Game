using UnityEngine;
using UnityEngine.UI;

public class OxygenUI : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField] private PlayerOxygen oxygen;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform iconRect;

    [Header("Wygląd")]
    [SerializeField]
    private Gradient fillColor = new Gradient
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
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.6f;

    [Header("Mało tlenu")]
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.25f;
    [SerializeField, Min(0f)] private float pulseFrequency = 2.5f;
    [SerializeField, Min(0f)] private float pulseScale = 0.08f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 3f;

    private Vector2 barBasePosition;
    private float visibility;

    private void Awake()
    {
        if (barRoot != null)
            barBasePosition = barRoot.anchoredPosition;

        if (group != null)
            group.alpha = 0f;
    }

    private void Start()
    {
        if (oxygen == null)
            oxygen = FindFirstObjectByType<PlayerOxygen>();
    }

    private void Update()
    {
        bool visible = oxygen != null && oxygen.IsActive && !oxygen.IsStopped;
        visibility = Mathf.MoveTowards(visibility, visible ? 1f : 0f, Time.unscaledDeltaTime / fadeDuration);

        if (group != null)
            group.alpha = visibility;

        if (oxygen == null || visibility <= 0f)
            return;

        float level = oxygen.Oxygen01;
        float danger = lowThreshold > 0f ? Mathf.Clamp01(1f - level / lowThreshold) : 0f;
        float pulse = danger > 0f ? (Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * 0.5f + 0.5f) * danger : 0f;

        if (fillImage != null)
        {
            fillImage.fillAmount = level;
            fillImage.color = Color.Lerp(fillColor.Evaluate(level), Color.white, pulse * 0.35f);
        }

        if (iconRect != null)
            iconRect.localScale = Vector3.one * (1f + pulseScale * pulse);

        if (barRoot != null)
        {
            Vector2 shake = danger > 0f
                ? new Vector2(Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * (2f * shakeAmplitude * danger)
                : Vector2.zero;

            barRoot.anchoredPosition = barBasePosition + shake;
        }
    }
}

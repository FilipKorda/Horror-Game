using UnityEngine;
using UnityEngine.UI;

public class FearMeterUI : MonoBehaviour
{
    [SerializeField] private PlayerFearMeter fearMeter;

    [Header("Mózg")]
    [SerializeField] private Image brainImage;
    [Tooltip("Sprite'y kolejno od spokoju do rozpadu. Indeks jest mapowany na poziom strachu, więc działa dla dowolnego MaxFearPoints.")]
    [SerializeField] private Sprite[] brainStages;
    [SerializeField, Min(1f)] private float increasePopScale = 1.25f;
    [SerializeField, Min(1f)] private float decreasePopScale = 1.1f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.35f;
    [Tooltip("Wstrząs w pikselach UI, tylko przy wzroście strachu.")]
    [SerializeField, Min(0f)] private float shakeAmplitude = 7f;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.5f;
    [SerializeField, Min(0f)] private float shakeFrequency = 28f;

    [Header("Pulsowanie mózgu")]
    [Tooltip("Poziom strachu (0-1), od którego mózg zaczyna pulsować. Amplituda rośnie do maksimum przy pełnym strachu.")]
    [SerializeField, Range(0f, 1f)] private float throbStartFear = 0.6f;
    [SerializeField, Min(0f)] private float throbAmplitude = 0.04f;
    [SerializeField, Min(0f)] private float throbFrequency = 1.6f;

    [Header("Płyn")]
    [Tooltip("Kwadratowy RawImage bez tekstury, rozciągnięty na cały iluminator, z materiałem UI/FearLiquid.")]
    [SerializeField] private RawImage liquid;
    [Tooltip("Kolor płynu w zależności od poziomu strachu (0 = brak, 1 = max).")]
    [SerializeField]
    private Gradient fearColor = new Gradient
    {
        colorKeys = new[]
        {
            new GradientColorKey(new Color(0.42f, 0.18f, 0.78f), 0f),
            new GradientColorKey(new Color(0.80f, 0.12f, 0.42f), 0.55f),
            new GradientColorKey(new Color(1f, 0.10f, 0.07f), 1f)
        },
        alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
    };
    [SerializeField, Min(0.01f)] private float levelSmoothTime = 0.5f;
    [SerializeField, Min(0f)] private float flowSpeed = 1.2f;
    [Tooltip("Mnożnik prędkości płynięcia przy pełnym strachu.")]
    [SerializeField, Min(1f)] private float maxFearFlowMultiplier = 2f;
    [Tooltip("Jak długo po zmianie strachu powierzchnia płynu jest mocniej rozchlapana.")]
    [SerializeField, Min(0.01f)] private float agitationDuration = 1.2f;

    private const float TwoPi = Mathf.PI * 2f;

    private static readonly int LevelId = Shader.PropertyToID("_Level");
    private static readonly int AgitationId = Shader.PropertyToID("_Agitation");
    private static readonly int PhaseId = Shader.PropertyToID("_Phase");
    private static readonly int LiquidColorId = Shader.PropertyToID("_LiquidColor");

    private Material material;
    private RectTransform brainRect;
    private Vector2 brainBasePosition;
    private bool brainAtRest = true;

    private int lastPoints;
    private float targetLevel;
    private float level;
    private float levelVelocity;
    private float agitation;
    private float phase;
    private float popFrom = 1f;
    private float popElapsed;
    private float shakeElapsed;

    private void Awake()
    {
        if (fearMeter == null || brainImage == null || liquid == null || brainStages == null || brainStages.Length == 0)
        {
            Debug.LogError($"[{nameof(FearMeterUI)}] Brak PlayerFearMeter, brainImage, liquid albo brainStages.", this);
            enabled = false;
            return;
        }

        // CanvasRenderer nie ma MaterialPropertyBlock, więc potrzebna jest własna instancja materiału.
        material = new Material(liquid.material);
        liquid.material = material;

        if (!material.HasProperty(LevelId))
            Debug.LogError($"[{nameof(FearMeterUI)}] Materiał RawImage nie używa shadera UI/FearLiquid.", this);

        brainRect = brainImage.rectTransform;
        brainBasePosition = brainRect.anchoredPosition;
        popElapsed = popDuration;
        shakeElapsed = shakeDuration;
    }

    private void OnEnable()
    {
        if (material == null)
            return;

        fearMeter.OnFearChanged += HandleFearChanged;
        Snap(fearMeter.CurrentFearPoints);
    }

    private void OnDisable()
    {
        if (material == null)
            return;

        fearMeter.OnFearChanged -= HandleFearChanged;
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }

    private void HandleFearChanged(int points)
    {
        if (points == lastPoints)
            return;

        bool increased = points > lastPoints;
        lastPoints = points;
        targetLevel = fearMeter.FearPercent01;
        agitation = 1f;

        UpdateBrainSprite();
        popFrom = increased ? increasePopScale : decreasePopScale;
        popElapsed = 0f;

        if (increased)
            shakeElapsed = 0f;
    }

    private void Snap(int points)
    {
        lastPoints = points;
        targetLevel = fearMeter.FearPercent01;
        level = targetLevel;
        levelVelocity = 0f;
        agitation = 0f;
        popElapsed = popDuration;
        shakeElapsed = shakeDuration;

        UpdateBrainSprite();
    }

    private void UpdateBrainSprite()
    {
        int index = Mathf.RoundToInt(fearMeter.FearPercent01 * (brainStages.Length - 1));
        brainImage.sprite = brainStages[index];
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;

        UpdateLiquid(delta);
        UpdateBrain(delta);
    }

    private void UpdateLiquid(float delta)
    {
        level = Mathf.SmoothDamp(level, targetLevel, ref levelVelocity, levelSmoothTime, Mathf.Infinity, delta);
        agitation = Mathf.MoveTowards(agitation, 0f, delta / agitationDuration);
        phase = Mathf.Repeat(phase + delta * flowSpeed * Mathf.Lerp(1f, maxFearFlowMultiplier, level), TwoPi);

        material.SetFloat(LevelId, level);
        material.SetFloat(AgitationId, agitation);
        material.SetFloat(PhaseId, phase);
        material.SetColor(LiquidColorId, fearColor.Evaluate(level));
    }

    private void UpdateBrain(float delta)
    {
        popElapsed = Mathf.Min(popElapsed + delta, popDuration);
        shakeElapsed = Mathf.Min(shakeElapsed + delta, shakeDuration);

        bool throbbing = throbAmplitude > 0f && level > throbStartFear;
        bool animating = popElapsed < popDuration || shakeElapsed < shakeDuration || throbbing;

        // Zapis do RectTransform przebatchowuje canvas, więc w spoczynku go pomijamy.
        if (!animating && brainAtRest)
            return;

        brainAtRest = !animating;

        float t = popElapsed / popDuration;
        float scale = Mathf.Lerp(popFrom, 1f, 1f - (1f - t) * (1f - t));

        if (throbbing)
            scale *= 1f + Mathf.Sin(Time.unscaledTime * throbFrequency * TwoPi) * throbAmplitude * Mathf.InverseLerp(throbStartFear, 1f, level);

        Vector2 offset = Vector2.zero;
        float shake = 1f - shakeElapsed / shakeDuration;

        if (shake > 0f)
        {
            float time = Time.unscaledTime * shakeFrequency;
            offset = new Vector2(Mathf.PerlinNoise(time, 0.17f) - 0.5f, Mathf.PerlinNoise(0.63f, time) - 0.5f) * (2f * shakeAmplitude * shake * shake);
        }

        brainRect.localScale = Vector3.one * scale;
        brainRect.anchoredPosition = brainBasePosition + offset;
    }
}
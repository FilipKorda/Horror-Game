using UnityEngine;
using UnityEngine.UI;

public class MedicineSlotUI : MonoBehaviour
{
    [Tooltip("RawImage z maską plusa i materiałem UI/MedicineLiquid.")]
    [SerializeField] private RawImage liquid;

    [Header("Ciecz")]
    [SerializeField, Min(0.01f)] private float fillDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float drainDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float sloshDuration = 0.9f;
    [SerializeField, Min(0f)] private float waveSpeed = 3f;

    [Header("Boop")]
    [SerializeField] private float pickupPopScale = 1.3f;
    [SerializeField, Range(0.1f, 1f)] private float usePopScale = 0.8f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.3f;

    private static readonly int LevelId = Shader.PropertyToID("_Level");
    private static readonly int SloshId = Shader.PropertyToID("_Slosh");
    private static readonly int PhaseId = Shader.PropertyToID("_Phase");

    private Material material;
    private bool isFilled;
    private float level;
    private float slosh;
    private float phase;
    private float popFrom = 0.3f;
    private float popElapsed;

    private void Awake()
    {
        // CanvasRenderer nie ma MaterialPropertyBlock, więc każdy slot potrzebuje własnej instancji materiału.
        material = new Material(liquid.material);
        liquid.material = material;

        if (!material.HasProperty(LevelId))
            Debug.LogError($"[{nameof(MedicineSlotUI)}] Materiał RawImage nie używa shadera UI/MedicineLiquid.", this);

        popElapsed = popDuration;
        Apply();
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }

    public void SetFilled(bool filled, bool animate)
    {
        if (isFilled == filled)
            return;

        isFilled = filled;

        if (!animate)
        {
            level = filled ? 1f : 0f;
            slosh = 0f;
            popElapsed = popDuration;
            Apply();
            return;
        }

        slosh = 1f;
        popFrom = filled ? pickupPopScale : usePopScale;
        popElapsed = 0f;
        enabled = true;
    }

    private void Update()
    {
        float target = isFilled ? 1f : 0f;
        float delta = Time.unscaledDeltaTime;

        level = Mathf.MoveTowards(level, target, delta / (isFilled ? fillDuration : drainDuration));
        slosh = Mathf.MoveTowards(slosh, 0f, delta / sloshDuration);
        popElapsed = Mathf.Min(popElapsed + delta, popDuration);
        phase += delta * waveSpeed;

        Apply();

        if (level == target && slosh == 0f && popElapsed >= popDuration)
            enabled = false;
    }

    private void Apply()
    {
        material.SetFloat(LevelId, Mathf.SmoothStep(0f, 1f, level));
        material.SetFloat(SloshId, slosh);
        material.SetFloat(PhaseId, phase);

        float t = popElapsed / popDuration;
        float easeOut = 1f - (1f - t) * (1f - t);
        transform.localScale = new Vector3(0.3f, 0.3f, 0.3f) * Mathf.Lerp(popFrom, 1f, easeOut);
    }
}
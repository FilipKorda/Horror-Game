using UnityEngine;
using UnityEngine.UI;

public class BatterySlotUI : MonoBehaviour
{
    [SerializeField] private Image charge;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.25f;
    [SerializeField, Min(1f)] private float popScale = 1.3f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.3f;

    private bool isCharged;
    private float alpha;
    private float popElapsed;

    private void Awake()
    {
        popElapsed = popDuration;
        Apply();
    }

    public void SetCharged(bool charged, bool animate)
    {
        if (isCharged == charged)
            return;

        isCharged = charged;

        if (!animate)
        {
            alpha = charged ? 1f : 0f;
            popElapsed = popDuration;
            Apply();
            return;
        }

        if (charged)
            popElapsed = 0f;

        enabled = true;
    }

    private void Update()
    {
        float target = isCharged ? 1f : 0f;
        float delta = Time.unscaledDeltaTime;

        alpha = Mathf.MoveTowards(alpha, target, delta / fadeDuration);
        popElapsed = Mathf.Min(popElapsed + delta, popDuration);

        Apply();

        if (alpha == target && popElapsed >= popDuration)
            enabled = false;
    }

    private void Apply()
    {
        Color color = charge.color;
        color.a = alpha;
        charge.color = color;

        float t = popElapsed / popDuration;
        float easeOut = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, easeOut);
    }
}
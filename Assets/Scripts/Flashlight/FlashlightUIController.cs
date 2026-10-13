using UnityEngine;
using UnityEngine.UI;

public class FlashlightUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlashlightController flashlight;

    [Header("Sliders")]
    [SerializeField] private Slider lightSizeSlider;
    [SerializeField] private Slider batteryLevelSlider;

    [Header("Rozmiar światła")]
    [SerializeField, Min(0.01f)] private float lightSizeSmoothTime = 0.08f;

    [Header("Poziom baterii")]
    [SerializeField] private Image batteryFill;
    [SerializeField] private Gradient batteryColor;
    [SerializeField, Min(0.01f)] private float fillSmoothTime = 0.2f;

    [Header("Niski poziom baterii")]
    [SerializeField, Range(0f, 1f)] private float lowBatteryThreshold = 0.2f;
    [SerializeField, Min(0f)] private float lowPulseFrequency = 1.5f;
    [SerializeField, Range(0f, 1f)] private float lowPulseMinAlpha = 0.35f;

    private float displayedLightSize;
    private float lightSizeVelocity;
    private float displayedBatteryLevel;
    private float batteryLevelVelocity;

    private void Awake()
    {
        if (batteryFill == null && batteryLevelSlider != null && batteryLevelSlider.fillRect != null)
            batteryFill = batteryLevelSlider.fillRect.GetComponent<Image>();
    }

    private void OnEnable()
    {
        SetupLightSizeSlider();

        if (flashlight == null)
            return;

        displayedLightSize = flashlight.LightSize01;
        lightSizeVelocity = 0f;
        displayedBatteryLevel = flashlight.BatteryLevel01;
        batteryLevelVelocity = 0f;
    }

    private void Update()
    {
        if (flashlight == null)
            return;

        UpdateLightSize();
        UpdateBatteryLevel();
    }

    private void SetupLightSizeSlider()
    {
        if (lightSizeSlider == null)
            return;

        lightSizeSlider.wholeNumbers = false;
        lightSizeSlider.minValue = 0f;
        lightSizeSlider.maxValue = 1f;
    }

    private void UpdateLightSize()
    {
        if (lightSizeSlider == null)
            return;

        displayedLightSize = Mathf.SmoothDamp(displayedLightSize, flashlight.LightSize01, ref lightSizeVelocity, lightSizeSmoothTime);
        lightSizeSlider.value = displayedLightSize;
    }

    private void UpdateBatteryLevel()
    {
        float level = flashlight.BatteryLevel01;

        displayedBatteryLevel = Mathf.SmoothDamp(displayedBatteryLevel, level, ref batteryLevelVelocity, fillSmoothTime);

        if (batteryLevelSlider != null)
            batteryLevelSlider.value = displayedBatteryLevel;

        if (batteryFill == null)
            return;

        Color color = batteryColor.Evaluate(displayedBatteryLevel);

        if (level > 0f && level <= lowBatteryThreshold && lowPulseFrequency > 0f)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * lowPulseFrequency * Mathf.PI * 2f) * 0.5f + 0.5f;
            color.a *= Mathf.Lerp(lowPulseMinAlpha, 1f, pulse);
        }

        batteryFill.color = color;
    }
}
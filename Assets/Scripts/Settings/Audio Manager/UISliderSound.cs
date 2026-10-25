
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class UISliderSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private float soundCooldown = 0.05f;

    private Slider slider;
    private float lastSoundTime;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        if (Time.unscaledTime - lastSoundTime < soundCooldown)
            return;

        if (clickClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISound(clickClip);
            lastSoundTime = Time.unscaledTime;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(hoverClip);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (hoverClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(hoverClip);
    }
}


using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Dropdown))]
public class UIDropdownSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler, ISubmitHandler
{
    [SerializeField] private AudioClip toggleOnClip;
    [SerializeField] private AudioClip toggleOffClip;
    [SerializeField] private AudioClip hoverClip;

    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(PlayToggleSound);
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(PlayToggleSound);
    }

    private void PlayToggleSound(int value)
    {
        AudioClip clip = value > 0 ? toggleOnClip : toggleOffClip;
        AudioManager.Instance.PlayUISound(clip);
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

    public void OnSubmit(BaseEventData eventData)
    {
        if (toggleOnClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(toggleOnClip);
    }
}

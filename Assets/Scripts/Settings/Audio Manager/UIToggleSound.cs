
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class UIToggleSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler, ISubmitHandler
{
    [SerializeField] private AudioClip toggleOnClip;
    [SerializeField] private AudioClip toggleOffClip;
    [SerializeField] private AudioClip hoverClip;

    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(PlayToggleSound());
    }

    private IEnumerator PlayToggleSound()
    {
        yield return null;

        if (AudioManager.Instance == null)
            yield break;

        AudioClip clip = toggle.isOn || toggleOffClip == null
            ? toggleOnClip
            : toggleOffClip;

        if (clip != null)
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


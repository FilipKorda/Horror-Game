using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler, ISubmitHandler
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(clickClip);
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
        if (clickClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(clickClip);
    }
}
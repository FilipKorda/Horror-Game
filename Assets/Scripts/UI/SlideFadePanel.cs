using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class SlideFadePanel : MonoBehaviour
{
    [Tooltip("O ile jednostek canvasa panel przesuwa się w lewo przy chowaniu.")]
    [SerializeField] private float slideDistance = 300f;
    [SerializeField, Min(0.01f)] private float duration = 0.4f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Vector2 shownPosition;

    private void Awake()
    {
        rect = (RectTransform)transform;
        canvasGroup = GetComponent<CanvasGroup>();
        shownPosition = rect.anchoredPosition;
    }

    public void HideInstant()
    {
        rect.anchoredPosition = shownPosition + Vector2.left * slideDistance;
        canvasGroup.alpha = 0f;
    }

    public IEnumerator Hide()
    {
        return Animate(shownPosition + Vector2.left * slideDistance, 0f);
    }

    public IEnumerator Show()
    {
        return Animate(shownPosition, 1f);
    }

    private IEnumerator Animate(Vector2 targetPosition, float targetAlpha)
    {
        Vector2 startPosition = rect.anchoredPosition;
        float startAlpha = canvasGroup.alpha;

        for (float progress = 0f; progress < 1f;)
        {
            progress = Mathf.Min(progress + Time.deltaTime / duration, 1f);
            float eased = curve.Evaluate(progress);

            rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
            canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, eased);

            yield return null;
        }
    }
}
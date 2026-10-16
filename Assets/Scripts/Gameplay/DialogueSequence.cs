using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class DialogueSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private LocalizedString[] dialogues;
    [SerializeField] private float dialogueInterval = 5f;
    [SerializeField] private float transitionDuration = 0.35f;

    private Coroutine dialogueCoroutine;
    private Coroutine canvasCoroutine;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Initialize()
    {
        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine = StartCoroutine(DialogueRoutine());
    }

    private IEnumerator DialogueRoutine()
    {
        if (dialogues == null || dialogues.Length == 0)
            yield break;

        for (int i = 0; i < dialogues.Length; i++)
        {
            var handle = dialogues[i].GetLocalizedStringAsync();

            yield return handle;

            dialogueText.text = handle.Result;

            yield return StartCoroutine(FadeCanvas(1f));

            if (i < dialogues.Length - 1)
                yield return new WaitForSeconds(dialogueInterval);
        }
    }

    public void Hide()
    {
        if (canvasCoroutine != null)
            StopCoroutine(canvasCoroutine);

        dialogueText.text = "";

        canvasCoroutine = StartCoroutine(FadeCanvas(0f));
    }

    public void Show()
    {
        if (canvasCoroutine != null)
            StopCoroutine(canvasCoroutine);

        canvasCoroutine = StartCoroutine(FadeCanvas(1f));
    }

    public IEnumerator HideForCameraTransition()
    {
        if (canvasCoroutine != null)
            StopCoroutine(canvasCoroutine);

        yield return StartCoroutine(FadeCanvas(0f));
    }

    public IEnumerator ShowAfterCameraTransition()
    {
        if (canvasCoroutine != null)
            StopCoroutine(canvasCoroutine);

        yield return new WaitForSeconds(transitionDuration);

        yield return StartCoroutine(FadeCanvas(1f));
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        bool visible = targetAlpha > 0f;

        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider progressBar; 
    [SerializeField] private CanvasGroup fadeCanvasGroup; 

    [Header("Timing")]
    [SerializeField] private float minLoadingDuration = 1f; 
    [SerializeField] private float progressBarSmoothSpeed = 5f; 
    [SerializeField] private float fadeOutDuration = 0.3f; 

    private void Start()
    {
        StartCoroutine(LoadTargetSceneRoutine(SceneLoader.Instance.TargetSceneName));
    }

    private IEnumerator LoadTargetSceneRoutine(string sceneName)
    {
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 1f; 

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float startTime = Time.time;
        float displayedProgress = 0f;
        bool activationTriggered = false;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, progressBarSmoothSpeed * Time.deltaTime);

            if (progressBar != null)
                progressBar.value = displayedProgress;

            if (!activationTriggered)
            {
                bool readyToActivate = operation.progress >= 0.9f;
                bool minTimeElapsed = Time.time - startTime >= minLoadingDuration;
                bool barReachedFull = displayedProgress >= 0.99f;

                if (readyToActivate && minTimeElapsed && barReachedFull)
                {
                    if (progressBar != null)
                        progressBar.value = 1f;

                    activationTriggered = true;
                    operation.allowSceneActivation = true; 
                }
            }

            yield return null;
        }

        if (fadeCanvasGroup != null)
            yield return Fade(1f, 0f, fadeOutDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MenuIntroFade : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private float startExposure = -10f;
    [SerializeField] private float endExposure = 0f;
    [SerializeField, Min(0.01f)] private float duration = 2f;

    private void Start()
    {
        if (volume == null || !volume.profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            Debug.LogError($"[{nameof(MenuIntroFade)}] Brak Volume albo Color Adjustments w jego profilu.", this);
            return;
        }

        StartCoroutine(FadeRoutine(colorAdjustments.postExposure));
    }

    private IEnumerator FadeRoutine(FloatParameter exposure)
    {
        exposure.Override(startExposure);

        for (float progress = 0f; progress < 1f;)
        {
            yield return null;

            progress += Time.unscaledDeltaTime / duration;
            exposure.value = Mathf.Lerp(startExposure, endExposure, progress);
        }
    }
}
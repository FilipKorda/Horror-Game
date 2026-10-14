using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EscapeResultPanel : ResultPanel
{
    [Header("Rozbłysk")]
    [SerializeField] private Image lightFlash;
    [SerializeField] private AudioClip escapeMusic;
    [SerializeField] private float flashInDuration = 0.8f;
    [SerializeField] private float flashHoldDuration = 0.45f;
    [SerializeField] private float flashOutDuration = 1.6f;

    protected override void Prepare()
    {
        base.Prepare();
        SetAlpha(lightFlash, 0f);
    }

    protected override IEnumerator Intro()
    {
        yield return Tween(flashInDuration, t => SetAlpha(lightFlash, t * t));

        SetAlpha(background, 1f);
        LoadTexts();

        yield return Wait(flashHoldDuration);

        if (escapeMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(escapeMusic, 3f);

        yield return Tween(flashOutDuration, t => SetAlpha(lightFlash, 1f - EaseOutCubic(t)));
    }

    protected override float GetStatValue(int index)
    {
        switch (index)
        {
            case 0:
                return EndTime;
            case 1:
                return fearMeter != null ? fearMeter.TimesCaught : 0;
            default:
                return fearMeter != null ? fearMeter.MedicinesUsed : 0;
        }
    }
}

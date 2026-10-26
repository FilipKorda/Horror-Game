using UnityEngine;

public static class FrameRateLimiter
{
    public const int TargetFrameRate = 60;

    private const int MaxVSyncInterval = 4;
    private const float IntervalRounding = 0.05f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Apply(QualitySettings.vSyncCount > 0);
    }

    public static void Apply(bool vSync)
    {
        QualitySettings.vSyncCount = vSync ? GetVSyncInterval() : 0;
        Application.targetFrameRate = TargetFrameRate;
    }

    private static int GetVSyncInterval()
    {
        double refreshRate = Screen.currentResolution.refreshRateRatio.value;

        if (refreshRate <= 0d)
            return 1;

        int interval = Mathf.FloorToInt((float)(refreshRate / TargetFrameRate) + IntervalRounding);
        return Mathf.Clamp(interval, 1, MaxVSyncInterval);
    }
}

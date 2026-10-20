using UnityEngine;

[CreateAssetMenu(fileName = "FearEffectPreset", menuName = "Horror/Fear Effect Preset")]
public class FearEffectPreset : ScriptableObject
{
    [Header("Czas")]
    [Min(0.05f)] public float duration = 2f;
    public int priority;

    [Header("Volume")]
    public VolumeParameterAnimation lensDistortionIntensity = new VolumeParameterAnimation
    {
        peak = 0.5f,
        curve = CreateLensSweepCurve()
    };

    public VolumeParameterAnimation filmGrainIntensity = new VolumeParameterAnimation
    {
        peak = 1f,
        curve = CreateEnvelope(0.05f, 0.45f, 0.7f)
    };

    public VolumeParameterAnimation vignetteIntensity = new VolumeParameterAnimation
    {
        peak = 0.75f,
        curve = CreateEnvelope(0.06f, 0.4f, 0.55f)
    };

    public VolumeParameterAnimation vignetteSmoothness = new VolumeParameterAnimation
    {
        peak = 0.5f,
        curve = CreateEnvelope(0.06f, 0.4f, 0.55f)
    };

    public VolumeParameterAnimation chromaticAberrationIntensity = new VolumeParameterAnimation
    {
        peak = 0.7f,
        curve = CreateEnvelope(0.06f, 0.4f, 0.55f)
    };
    public VolumeParameterAnimation bloomIntensity = new VolumeParameterAnimation
    {
        peak = 2f,
        curve = CreateEnvelope(0.06f, 0.4f, 0.55f)
    };

    [Header("Kamera")]
    public AnimationCurve cameraEnvelope = CreateEnvelope(0.04f, 0.35f, 0.25f);
    public float fovKick = -10f;
    public float flinchPitch = 4f;
    public float rollKick = 5f;
    public float positionShake = 0.04f;
    public float rotationShake = 2f;
    [Min(0.1f)] public float shakeFrequency = 22f;

    private static AnimationCurve CreateEnvelope(float peakTime, float sustainTime, float sustainValue)
    {
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(peakTime, 1f),
            new Keyframe(sustainTime, sustainValue),
            new Keyframe(1f, 0f));

        for (int i = 0; i < curve.length; i++)
            curve.SmoothTangents(i, 0f);

        return curve;
    }

    // Zerowe styczne w kluczach, żeby krzywa trafiała dokładnie w -1 i +1 bez przestrzału.
    private static AnimationCurve CreateLensSweepCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.08f, -1f, 0f, 0f),
            new Keyframe(0.2f, 1f, 0f, 0f),
            new Keyframe(0.55f, 0f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f));
    }
}

/// <summary>Wartość = baza + (peak - baza) * krzywa(t) * siła. Bazę podaje FearCameraEffect.</summary>
[System.Serializable]
public class VolumeParameterAnimation
{
    public bool enabled = true;
    public float peak;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public float Evaluate(float baseValue, float normalizedTime, float strength)
    {
        return Mathf.LerpUnclamped(baseValue, peak, curve.Evaluate(normalizedTime) * strength);
    }
}
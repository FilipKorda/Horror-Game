using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering.Universal;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Tooltip("Paleta koloru konturu interaktywnych obiektów. Pozycja 0 suwaka = kolor domyślny.")]
    [SerializeField] private Gradient outlineGradient;

    [System.Serializable]
    public class GraphicsSettings
    {
        public int quality;
        public bool fullscreen;
        public bool vSync;
        public int resolutionWidth;
        public int resolutionHeight;
    }

    [System.Serializable]
    public class SoundSettings
    {
        [Range(0f, 1f)]
        public float musicVolume;

        [Range(0f, 1f)]
        public float sfxVolume;

        [Range(0f, 1f)]
        public float uiVolume;
    }

    [System.Serializable]
    public class GameplaySettings
    {
        [Range(0f, 1f)]
        public float mouseSensitivity;

        public bool invertY;

        [Range(0f, 1f)]
        public float outlineColorPosition;

        [Range(MinFieldOfView, MaxFieldOfView)]
        public float fieldOfView;
    }

    [System.Serializable]
    public class LanguageOption
    {
        public string localeCode;
        public LocalizedString localizedName;
    }

    public GraphicsSettings Graphics { get; private set; }
    public SoundSettings Sounds { get; private set; }
    public GameplaySettings Gameplay { get; private set; }
    public LanguageOption Language { get; private set; }

    public Color OutlineColor => outlineGradient.Evaluate(Gameplay.outlineColorPosition);

    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SFXVolumeKey = "Settings.SFXVolume";
    private const string UIVolumeKey = "Settings.UIVolume";

    private const string QualityKey = "Settings.Quality";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string VSyncKey = "Settings.VSync";
    private const string ResolutionWidthKey = "Settings.ResolutionWidth";
    private const string ResolutionHeightKey = "Settings.ResolutionHeight";

    private const string SensitivityKey = "Settings.MouseSensitivity";
    private const string InvertYKey = "Settings.InvertY";
    private const string OutlineColorKey = "Settings.OutlineColor";
    private const string FieldOfViewKey = "Settings.FieldOfView";

    public const float MinFieldOfView = 60f;
    public const float MaxFieldOfView = 70f;
    public const float DefaultFieldOfView = 60f;

    private const string LanguageKey = "Settings.Language";
    private const string DefaultLocaleCode = "en";

    private static readonly Vector2Int[] CommonResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160),
        new Vector2Int(1280, 800),
        new Vector2Int(1440, 900),
        new Vector2Int(1680, 1050),
        new Vector2Int(1920, 1200),
        new Vector2Int(2560, 1600)
    };

    private const float WideAspect = 16f / 9f;
    private const float AspectTolerance = 0.03f;
    private const float MinNativeAspect = 1.55f;
    private const float MaxNativeAspect = 1.8f;

    private readonly Dictionary<UniversalRenderPipelineAsset, float> baseRenderScales = new Dictionary<UniversalRenderPipelineAsset, float>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyGraphics();
    }

    private void Start()
    {

        ApplySoundSettings();
        ApplyGameplaySettings();
        ApplyLanguage();
    }

    private void LoadSettings()
    {
        Graphics = new GraphicsSettings
        {
            quality = PlayerPrefs.GetInt(
                QualityKey,
                QualitySettings.GetQualityLevel()
            ),

            fullscreen = PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0
            ) == 1,

            vSync = PlayerPrefs.GetInt(
                VSyncKey,
                1
            ) == 1,

            resolutionWidth = PlayerPrefs.GetInt(
                ResolutionWidthKey,
                Screen.currentResolution.width
            ),

            resolutionHeight = PlayerPrefs.GetInt(
                ResolutionHeightKey,
                Screen.currentResolution.height
            )
        };

        Vector2Int resolution = GetClosestSupportedResolution(
            Graphics.resolutionWidth,
            Graphics.resolutionHeight
        );

        Graphics.resolutionWidth = resolution.x;
        Graphics.resolutionHeight = resolution.y;

        Sounds = new SoundSettings
        {
            musicVolume = PlayerPrefs.GetFloat(
                MusicVolumeKey,
                0.5f
            ),

            sfxVolume = PlayerPrefs.GetFloat(
                SFXVolumeKey,
                1f
            ),

            uiVolume = PlayerPrefs.GetFloat(
                UIVolumeKey,
                1f
            )
        };

        Gameplay = new GameplaySettings
        {
            mouseSensitivity = PlayerPrefs.GetFloat(
                SensitivityKey,
                0.5f
            ),

            invertY = PlayerPrefs.GetInt(
                InvertYKey,
                0
            ) == 1,

            outlineColorPosition = PlayerPrefs.GetFloat(
                OutlineColorKey,
                0f
            ),

            fieldOfView = Mathf.Clamp(
                PlayerPrefs.GetFloat(
                    FieldOfViewKey,
                    DefaultFieldOfView
                ),
                MinFieldOfView,
                MaxFieldOfView
            )
        };

        Language = new LanguageOption
        {
            localeCode = PlayerPrefs.GetString(
                LanguageKey,
                DefaultLocaleCode
            )
        };
    }

    public void ApplySoundSettings()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetMusicVolume(
            Sounds.musicVolume
        );

        AudioManager.Instance.SetSFXVolume(
            Sounds.sfxVolume
        );

        AudioManager.Instance.SetUIVolume(
            Sounds.uiVolume
        );
    }

    public void ApplyGameplaySettings()
    {
        InteractOutlineFeature.SetColor(OutlineColor);

        FlashlightLookController lookController = FindFirstObjectByType<FlashlightLookController>();
        if (lookController == null)
            return;

        lookController.SetMouseSensitivity(Gameplay.mouseSensitivity);
        lookController.SetInvertY(Gameplay.invertY);
        lookController.SetFieldOfView(Gameplay.fieldOfView);
    }

    private void ApplyGraphics()
    {
        QualitySettings.SetQualityLevel(
            Graphics.quality
        );

        FrameRateLimiter.Apply(Graphics.vSync);

        ApplyScreenMode();
    }

    private void ApplyScreenMode()
    {
        if (Graphics.fullscreen)
        {
            Vector2Int native = GetNativeResolution();

            Screen.SetResolution(
                native.x,
                native.y,
                FullScreenMode.FullScreenWindow
            );
        }
        else
        {
            Screen.SetResolution(
                Graphics.resolutionWidth,
                Graphics.resolutionHeight,
                FullScreenMode.Windowed
            );
        }

        ApplyRenderScale();
    }

    private void ApplyRenderScale()
    {
        if (Application.isEditor)
            return;

        if (!(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline))
            return;

        if (!baseRenderScales.TryGetValue(pipeline, out float baseScale))
        {
            baseScale = pipeline.renderScale;
            baseRenderScales.Add(pipeline, baseScale);
        }

        pipeline.renderScale = Mathf.Clamp(baseScale * GetResolutionScale(), 0.1f, 2f);
    }

    private float GetResolutionScale()
    {
        if (!Graphics.fullscreen)
            return 1f;

        Vector2Int native = GetNativeResolution();
        float pixelRatio = (float)Graphics.resolutionWidth * Graphics.resolutionHeight / ((float)native.x * native.y);

        return Mathf.Clamp(Mathf.Sqrt(pixelRatio), 0.1f, 1f);
    }

    public static List<Vector2Int> GetSupportedResolutions()
    {
        Vector2Int native = GetNativeResolution();
        float nativeAspect = (float)native.x / native.y;
        List<Vector2Int> result = new List<Vector2Int>();

        foreach (Vector2Int resolution in CommonResolutions)
        {
            if (resolution.x > native.x || resolution.y > native.y)
                continue;

            float aspect = (float)resolution.x / resolution.y;
            bool isWide = Mathf.Abs(aspect - WideAspect) < AspectTolerance;
            bool matchesNative = Mathf.Abs(aspect - nativeAspect) < AspectTolerance;

            if (isWide || matchesNative)
                result.Add(resolution);
        }

        bool nativeUsable = nativeAspect >= MinNativeAspect && nativeAspect <= MaxNativeAspect;

        if ((nativeUsable || result.Count == 0) && !result.Contains(native))
            result.Add(native);

        result.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return result;
    }

    public static Vector2Int GetClosestSupportedResolution(int width, int height)
    {
        List<Vector2Int> supported = GetSupportedResolutions();
        Vector2Int requested = new Vector2Int(width, height);

        if (supported.Contains(requested))
            return requested;

        Vector2Int native = GetNativeResolution();
        if (supported.Contains(native) && width >= native.x && height >= native.y)
            return native;

        Vector2Int best = supported[0];

        foreach (Vector2Int resolution in supported)
        {
            if (resolution.x <= width && resolution.y <= height)
                best = resolution;
        }

        return best;
    }

    private static Vector2Int GetNativeResolution()
    {
        if (!Application.isEditor && Display.main.systemWidth > 0 && Display.main.systemHeight > 0)
            return new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);

        int width = Screen.currentResolution.width;
        int height = Screen.currentResolution.height;

        foreach (Resolution resolution in Screen.resolutions)
        {
            if (resolution.width * resolution.height > width * height)
            {
                width = resolution.width;
                height = resolution.height;
            }
        }

        return new Vector2Int(width, height);
    }

    public void SetLanguage(string localeCode)
    {
        Language.localeCode = localeCode;

        PlayerPrefs.SetString(
            LanguageKey,
            localeCode
        );

        PlayerPrefs.Save();

        ApplyLanguage();
    }

    public void ApplyLanguage()
    {
        StartCoroutine(ApplyLanguageRoutine());
    }

    private IEnumerator ApplyLanguageRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        Locale targetLocale = null;

        foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == Language.localeCode)
            {
                targetLocale = locale;
                break;
            }
        }

        if (targetLocale != null)
            LocalizationSettings.SelectedLocale = targetLocale;
    }

    public void SetMusicVolume(float value)
    {
        Sounds.musicVolume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            MusicVolumeKey,
            Sounds.musicVolume
        );

        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(
                Sounds.musicVolume
            );
    }

    public void SetSFXVolume(float value)
    {
        Sounds.sfxVolume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            SFXVolumeKey,
            Sounds.sfxVolume
        );

        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(
                Sounds.sfxVolume
            );
    }

    public void SetUIVolume(float value)
    {
        Sounds.uiVolume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            UIVolumeKey,
            Sounds.uiVolume
        );

        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetUIVolume(
                Sounds.uiVolume
            );
    }

    public void SetQuality(int value)
    {
        Graphics.quality = Mathf.Clamp(
            value,
            0,
            QualitySettings.names.Length - 1
        );

        QualitySettings.SetQualityLevel(
            Graphics.quality
        );

        FrameRateLimiter.Apply(Graphics.vSync);
        ApplyRenderScale();

        PlayerPrefs.SetInt(
            QualityKey,
            Graphics.quality
        );

        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool value)
    {
        Graphics.fullscreen = value;

        ApplyScreenMode();

        PlayerPrefs.SetInt(
            FullscreenKey,
            value ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    public void SetVSync(bool value)
    {
        Graphics.vSync = value;

        FrameRateLimiter.Apply(value);

        PlayerPrefs.SetInt(
            VSyncKey,
            value ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    public void SetResolution(int width, int height)
    {
        Vector2Int resolution = GetClosestSupportedResolution(width, height);

        Graphics.resolutionWidth = resolution.x;
        Graphics.resolutionHeight = resolution.y;

        ApplyScreenMode();

        PlayerPrefs.SetInt(
            ResolutionWidthKey,
            resolution.x
        );

        PlayerPrefs.SetInt(
            ResolutionHeightKey,
            resolution.y
        );

        PlayerPrefs.Save();
    }

    public void SetMouseSensitivity(float value)
    {
        Gameplay.mouseSensitivity =
            Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            SensitivityKey,
            Gameplay.mouseSensitivity
        );

        PlayerPrefs.Save();

        ApplyGameplaySettings();
    }

    public void SetInvertY(bool value)
    {
        Gameplay.invertY = value;

        PlayerPrefs.SetInt(
            InvertYKey,
            value ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplyGameplaySettings();
    }

    public void SetOutlineColor(float value)
    {
        Gameplay.outlineColorPosition =
            Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            OutlineColorKey,
            Gameplay.outlineColorPosition
        );

        PlayerPrefs.Save();

        InteractOutlineFeature.SetColor(OutlineColor);
    }

    public void SetFieldOfView(float value)
    {
        Gameplay.fieldOfView = Mathf.Clamp(
            value,
            MinFieldOfView,
            MaxFieldOfView
        );

        PlayerPrefs.SetFloat(
            FieldOfViewKey,
            Gameplay.fieldOfView
        );

        PlayerPrefs.Save();

        ApplyGameplaySettings();
    }

    public void ResetAllSettings()
    {
        PlayerPrefs.DeleteKey(MusicVolumeKey);
        PlayerPrefs.DeleteKey(SFXVolumeKey);
        PlayerPrefs.DeleteKey(UIVolumeKey);

        PlayerPrefs.DeleteKey(QualityKey);
        PlayerPrefs.DeleteKey(FullscreenKey);
        PlayerPrefs.DeleteKey(VSyncKey);
        PlayerPrefs.DeleteKey(ResolutionWidthKey);
        PlayerPrefs.DeleteKey(ResolutionHeightKey);

        PlayerPrefs.DeleteKey(SensitivityKey);
        PlayerPrefs.DeleteKey(InvertYKey);
        PlayerPrefs.DeleteKey(OutlineColorKey);
        PlayerPrefs.DeleteKey(FieldOfViewKey);

        PlayerPrefs.DeleteKey(LanguageKey);

        PlayerPrefs.Save();

        LoadSettings();
        ApplyGraphics();
        ApplySoundSettings();
        ApplyGameplaySettings();
        ApplyLanguage();
    }
}
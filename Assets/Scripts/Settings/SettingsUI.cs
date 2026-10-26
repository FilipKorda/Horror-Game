using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using static SettingsManager;

public class SettingsUI : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private GameObject graphicsTab;
    [SerializeField] private GameObject soundsTab;
    [SerializeField] private GameObject gameplayTab;
    [SerializeField] private GameObject controlsTab;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private LocalizedString[] qualityNames;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;

    [Header("Sounds")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    [Header("Gameplay")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private Slider outlineColorSlider;
    [SerializeField] private Image outlineColorPreview;
    [SerializeField] private Slider fieldOfViewSlider;
    [SerializeField] private TextMeshProUGUI fieldOfViewValueText;

    [Header("Language")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private LanguageOption[] languageOptions;

    [Header("Button")]
    [SerializeField] private GameObject menuButtonSelected;
    [SerializeField] private GameObject graphicButtonSelected;
    [Header("Panels")]
    [SerializeField] private GameObject settigsPanel;

    public bool isSettingsOpen;
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private readonly List<Locale> locales = new List<Locale>();

    private void Start()
    {
        SetupResolutionDropdown();
        SetupQualityDropdown();
        StartCoroutine(SetupLanguageDropdownRoutine());

        LoadUI();

        isSettingsOpen = false;

        if (IsGamepad())
            StartCoroutine(SelectNextFrame(menuButtonSelected));
    }

    public void OnBackButtonPressed()
    {
        isSettingsOpen = false;
        settigsPanel.SetActive(false);
        if (IsGamepad())
            StartCoroutine(SelectNextFrame(menuButtonSelected));
    }

    private bool IsGamepad()
    {
        if (InputDeviceDetector.Instance == null)
            return false;

        return InputDeviceDetector.Instance.CurrentDevice != InputDeviceType.KeyboardMouse;
    }

    private IEnumerator SelectNextFrame(GameObject target)
    {
        yield return null;

        SelectGameObject(target);
    }

    private void SelectGameObject(GameObject target)
    {
        if (!IsGamepad())
            return;

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return;

        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(target);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        SetupQualityDropdown();
        StartCoroutine(
            SetupLanguageDropdownRoutine()
        );
    }

    private void SetupResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();
        resolutions.Clear();

        List<string> options =
            new List<string>();

        int selectedIndex = 0;

        foreach (Vector2Int resolution in SettingsManager.GetSupportedResolutions())
        {
            resolutions.Add(resolution);
            options.Add(resolution.x + " x " + resolution.y);

            if (
                resolution.x ==
                SettingsManager.Instance.Graphics.resolutionWidth &&
                resolution.y ==
                SettingsManager.Instance.Graphics.resolutionHeight
            )
            {
                selectedIndex = options.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = selectedIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void SetupQualityDropdown()
    {
        qualityDropdown.ClearOptions();

        List<string> options =
            new List<string>();

        for (int i = 0; i < qualityNames.Length; i++)
        {
            options.Add(
                qualityNames[i].GetLocalizedString()
            );
        }

        qualityDropdown.AddOptions(options);

        qualityDropdown.value =
            SettingsManager.Instance.Graphics.quality;

        qualityDropdown.RefreshShownValue();
    }

    private IEnumerator SetupLanguageDropdownRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        languageDropdown.ClearOptions();
        locales.Clear();

        List<string> options =
            new List<string>();

        int selectedIndex = 0;

        IList<Locale> availableLocales =
            LocalizationSettings.AvailableLocales.Locales;

        for (int i = 0; i < availableLocales.Count; i++)
        {
            Locale locale =
                availableLocales[i];

            locales.Add(locale);

            LanguageOption languageOption = null;

            for (int j = 0; j < languageOptions.Length; j++)
            {
                if (
                    languageOptions[j].localeCode ==
                    locale.Identifier.Code
                )
                {
                    languageOption =
                        languageOptions[j];

                    break;
                }
            }

            if (languageOption != null)
            {
                options.Add(
                    languageOption.localizedName.GetLocalizedString()
                );
            }
            else
            {
                options.Add(
                    locale.LocaleName
                );
            }

            if (
                locale.Identifier.Code ==
                SettingsManager.Instance.Language.localeCode
            )
            {
                selectedIndex = i;
            }
        }

        languageDropdown.AddOptions(options);

        languageDropdown.value =
            selectedIndex;

        languageDropdown.RefreshShownValue();
    }

    private void LoadUI()
    {
        SettingsManager settings =
            SettingsManager.Instance;

        musicSlider.value =
            settings.Sounds.musicVolume;

        sfxSlider.value =
            settings.Sounds.sfxVolume;

        uiSlider.value =
            settings.Sounds.uiVolume;

        sensitivitySlider.value =
            settings.Gameplay.mouseSensitivity;

        fullscreenToggle.isOn =
            settings.Graphics.fullscreen;

        vSyncToggle.isOn =
            settings.Graphics.vSync;

        invertYToggle.isOn =
            settings.Gameplay.invertY;

        outlineColorSlider.value =
            settings.Gameplay.outlineColorPosition;

        outlineColorPreview.color =
            settings.OutlineColor;

        // Zakres bierzemy z SettingsManager, żeby suwak i zapis nigdy się nie rozjechały.
        // Wartość czytamy wcześniej - zmiana zakresu może wywołać OnFieldOfViewChanged i ją nadpisać
        float fieldOfView =
            settings.Gameplay.fieldOfView;

        fieldOfViewSlider.minValue =
            SettingsManager.MinFieldOfView;

        fieldOfViewSlider.maxValue =
            SettingsManager.MaxFieldOfView;

        fieldOfViewSlider.value =
            fieldOfView;

        UpdateFieldOfViewText(
            fieldOfView
        );
    }

    private void UpdateFieldOfViewText(float value)
    {
        if (fieldOfViewValueText != null)
            fieldOfViewValueText.text =
                Mathf.RoundToInt(value).ToString();
    }

    public void OpenSettings()
    {
        isSettingsOpen = true;
        settigsPanel.SetActive(true);
        ShowGraphicsTab();
    }

    public void ShowGraphicsTab()
    {
        SetTab(graphicsTab);

        if (IsGamepad())
            StartCoroutine(SelectNextFrame(graphicButtonSelected));
    }

    public void ShowSoundsTab()
    {
        SetTab(soundsTab);
    }

    public void ShowGameplayTab()
    {
        SetTab(gameplayTab);
    }

    public void ShowControlsTab()
    {
        SetTab(controlsTab);
    }

    private void SetTab(GameObject activeTab)
    {
        graphicsTab.SetActive(false);
        soundsTab.SetActive(false);
        gameplayTab.SetActive(false);
        controlsTab.SetActive(false);

        activeTab.SetActive(true);
    }

    public void OnResolutionChanged(int index)
    {
        if (
            index < 0 ||
            index >= resolutions.Count
        )
            return;

        Vector2Int resolution =
            resolutions[index];

        SettingsManager.Instance.SetResolution(
            resolution.x,
            resolution.y
        );
    }

    public void OnQualityChanged(int index)
    {
        SettingsManager.Instance.SetQuality(
            index
        );
    }

    public void OnFullscreenChanged(bool value)
    {
        SettingsManager.Instance.SetFullscreen(
            value
        );
    }

    public void OnVSyncChanged(bool value)
    {
        SettingsManager.Instance.SetVSync(
            value
        );
    }

    public void OnMusicVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMusicVolume(
            value
        );
    }

    public void OnSFXVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSFXVolume(
            value
        );
    }

    public void OnUIVolumeChanged(float value)
    {
        SettingsManager.Instance.SetUIVolume(
            value
        );
    }

    public void OnSensitivityChanged(float value)
    {
        SettingsManager.Instance.SetMouseSensitivity(
            value
        );
    }

    public void OnInvertYChanged(bool value)
    {
        SettingsManager.Instance.SetInvertY(
            value
        );
    }

    public void OnOutlineColorChanged(float value)
    {
        SettingsManager.Instance.SetOutlineColor(
            value
        );

        outlineColorPreview.color =
            SettingsManager.Instance.OutlineColor;
    }

    public void OnFieldOfViewChanged(float value)
    {
        SettingsManager.Instance.SetFieldOfView(
            value
        );

        UpdateFieldOfViewText(
            SettingsManager.Instance.Gameplay.fieldOfView
        );
    }

    public void OnLanguageChanged(int index)
    {
        if (index < 0 || index >= locales.Count)
            return;

        SettingsManager.Instance.SetLanguage(locales[index].Identifier.Code);
    }

    public void ResetSettings()
    {
        SettingsManager.Instance.ResetAllSettings();

        SetupResolutionDropdown();
        SetupQualityDropdown();
        StartCoroutine(SetupLanguageDropdownRoutine());
        LoadUI();
    }
}
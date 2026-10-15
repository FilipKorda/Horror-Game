using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizedInputPrompt : MonoBehaviour
{
    [SerializeField] private LocalizedString promptString; 
    [SerializeField] private InputActionReference action;
    [SerializeField] private InputGlyphProvider glyphProvider;
    [SerializeField] private TMP_Text targetText;

    private void OnEnable()
    {
        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void HandleDeviceChanged(InputDeviceType device) => Refresh();
    private void HandleLocaleChanged(Locale locale) => Refresh();

    private void Refresh()
    {
        if (targetText == null || promptString == null || action == null || glyphProvider == null)
            return;

        InputDeviceType device = InputDeviceDetector.Instance != null
            ? InputDeviceDetector.Instance.CurrentDevice
            : InputDeviceType.KeyboardMouse;

        string glyphTag = glyphProvider.GetGlyphTag(action.action, device);

        promptString.Arguments = new object[] { glyphTag };
        targetText.text = promptString.GetLocalizedString();
    }
}
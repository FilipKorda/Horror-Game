using UnityEngine;
using UnityEngine.EventSystems;

public class UIGamepadNavigationHelper : MonoBehaviour
{
    [SerializeField] private SettingsUI settingsUI;
    [SerializeField] private GameObject firstSelected;
    [SerializeField] private GameObject settingsFirstSelected;

    private void OnEnable()
    {
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
            HandleDeviceChanged(InputDeviceDetector.Instance.CurrentDevice);
        }
    }

    private void OnDisable()
    {
        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
    }

    private GameObject GetCurrentFirstSelected()
    {
        if (settingsUI != null && settingsUI.isSettingsOpen)
            return settingsFirstSelected;

        return firstSelected;
    }

    private void Update()
    {
        if (EventSystem.current == null || InputDeviceDetector.Instance == null)
            return;

        bool isGamepad = InputDeviceDetector.Instance.CurrentDevice != InputDeviceType.KeyboardMouse;

        if (isGamepad && EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(GetCurrentFirstSelected());
    }

    private void HandleDeviceChanged(InputDeviceType device)
    {
        if (EventSystem.current == null)
            return;

        if (device == InputDeviceType.KeyboardMouse)
            EventSystem.current.SetSelectedGameObject(null);
        else
            EventSystem.current.SetSelectedGameObject(GetCurrentFirstSelected());
    }
}
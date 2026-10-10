using UnityEngine;
using UnityEngine.EventSystems;

public class UISelector : MonoBehaviour
{
    [SerializeField] private GameObject firstSelected;
    readonly bool isGamepad = InputDeviceDetector.Instance != null &&
                      (InputDeviceDetector.Instance.CurrentDevice == InputDeviceType.Xbox ||
                       InputDeviceDetector.Instance.CurrentDevice == InputDeviceType.PlayStation);

    private void OnEnable()
    {
        if (isGamepad)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }

    public void OnReturnFromSettingsToMainPanel()
    {
        if (isGamepad)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
}
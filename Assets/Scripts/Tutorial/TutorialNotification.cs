using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.UI;

public class TutorialNotification : MonoBehaviour
{
    [SerializeField] private GameObject notificationpanel;
    [SerializeField] private LocalizedString localizedString;
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    [SerializeField] private Button[] buttons;
    [SerializeField] private string targetSceneName;
    [SerializeField] private GameObject buttonSelected;
    [SerializeField] private GameObject menuButtonSelected;

    private void ToggleButtons(bool toggle)
    {
        foreach (Button button in buttons)
        {
            button.interactable = toggle;
            button.enabled = toggle;
            button.GetComponent<UIButtonSound>().enabled = toggle;
        }
    }

    public void OpenPanel()
    {
        ToggleButtons(false);
        notificationpanel.SetActive(true);
        textMeshProUGUI.text = localizedString.GetLocalizedString();
        if (IsGamepad())
            StartCoroutine(SelectNextFrame(buttonSelected));
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

    public void ExitPanel()
    {
        ToggleButtons(true);

        notificationpanel.SetActive(false);
        textMeshProUGUI.text = "";
        OnBackButtonPressed();
    }

    public void OnBackButtonPressed()
    {
        if (IsGamepad())
            StartCoroutine(SelectNextFrame(menuButtonSelected));
    }

    public void HandleClickPlayTutorial()
    {
        TutorialManager.Instance.SetTutorialEnabled(true);

        notificationpanel.SetActive(false);

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(targetSceneName);
    }

    public void HandleClickDontPlayTutorial()
    {
        TutorialManager.Instance.SetTutorialEnabled(false);

        notificationpanel.SetActive(false);

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(targetSceneName);
    }

}

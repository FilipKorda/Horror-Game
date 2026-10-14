using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.UI;

public class NotificationAcceptablePanel : MonoBehaviour
{
    [SerializeField] private GameObject notificationAcceptablePanel;
    [SerializeField] private LocalizedString localizedExitGame;
    [SerializeField] private LocalizedString localizedMainMenu;
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    [SerializeField] private string targetSceneName;
    [SerializeField] private Button[] buttons;
    [SerializeField] private GameObject notificationButton;
    [SerializeField] private GameObject mainMenuButton;

    private bool isExitGame = false;

    private void ToggleButtons(bool toggle)
    {
        foreach (Button button in buttons)
        {
            button.interactable = toggle;
            button.enabled = toggle;
            button.GetComponent<UIButtonSound>().enabled = toggle;
        }
    }

    public void HandleClickMainMenu()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(targetSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void DoAfterClick()
    {
        if (isExitGame)
        {
            ExitGame();
        }
        else
        {
            HandleClickMainMenu();
        }
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

    public void OpenExitGamePanel()
    {
        isExitGame = true;
        ToggleButtons(false);
        notificationAcceptablePanel.SetActive(true);
        textMeshProUGUI.text = localizedExitGame.GetLocalizedString();

        if (IsGamepad())
            StartCoroutine(SelectNextFrame(notificationButton));
    }

    public void OpenMainMenuPanel()
    {
        isExitGame = false;
        ToggleButtons(false);
        notificationAcceptablePanel.SetActive(true);
        textMeshProUGUI.text = localizedMainMenu.GetLocalizedString();

        if (IsGamepad())
            StartCoroutine(SelectNextFrame(notificationButton));
    }

    public void ExitNotificationPanel()
    {
        ToggleButtons(true);

        isExitGame = false;
        notificationAcceptablePanel.SetActive(false);
        textMeshProUGUI.text = "";

        if (IsGamepad())
            StartCoroutine(SelectNextFrame(mainMenuButton));
    }
}

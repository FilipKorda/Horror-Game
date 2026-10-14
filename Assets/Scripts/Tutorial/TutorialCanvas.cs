using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialCanvas : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSController fPSController;
    [SerializeField] private FlashlightLookController flashlightLookController;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private QuestManager questManager;

    [SerializeField] private GameObject questPanel;
    [SerializeField] private CanvasGroup questPanelGroup;
    [SerializeField] private GameObject fearPanel;
    [SerializeField] private CanvasGroup fearPanelGroup;
    [SerializeField] private GameObject batteryPanel;
    [SerializeField] private CanvasGroup batteryPanelGroup;
    [SerializeField] private GameObject medsPanel;
    [SerializeField] private CanvasGroup medsPanelGroup;
    [SerializeField] private GameObject flashlightPanel;
    [SerializeField] private CanvasGroup flashlightPanelGroup;
    [SerializeField] private CanvasGroup clickToShowNext;

    [Header("Quest")]
    [SerializeField] private CanvasGroup quest;
    [Header("Fear")]
    [SerializeField] private CanvasGroup fear;
    [Header("Battery")]
    [SerializeField] private CanvasGroup batteryImage;
    [SerializeField] private CanvasGroup batteryIconsHolder;
    [Header("Meds")]
    [SerializeField] private CanvasGroup medicineIconsHolder;
    [Header("Flashlight")]
    [SerializeField] private CanvasGroup flashlightSlider;

    [Header("Input Indicators")]
    [SerializeField] private CanvasGroup pickupIndicator;
    [SerializeField] private CanvasGroup reloadbatteryIndicator;
    [SerializeField] private CanvasGroup useMedsIndicator;
    [SerializeField] private CanvasGroup onOffFlashlightIndicator;
    [SerializeField] private CanvasGroup scrollIndicator;
    [SerializeField] private CanvasGroup slowWalkIndicator;
    [SerializeField] private CanvasGroup sprintIndicator;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference waitInputAction;


    private void OnEnable()
    {
        waitInputAction.action.Enable();
    }

    private void OnDisable()
    {
        waitInputAction.action.Disable();
    }

    public void Initialize()
    {
        bool isTutorialActive = TutorialManager.Instance != null &&
                                 TutorialManager.Instance.IsTutorialEnabled;

        if (isTutorialActive)
        {
            fPSController.SetLookLocked(true);
            fPSController.SetMovementLocked(true);
            flashlightLookController.SetLookLocked(true);
            enemySpawner.ActiveEnemySpawner(false);

            questPanel.SetActive(false);
            fearPanel.SetActive(false);
            batteryPanel.SetActive(false);
            medsPanel.SetActive(false);
            flashlightPanel.SetActive(false);

            clickToShowNext.alpha = 1;

            questPanelGroup.alpha = 0;
            fearPanelGroup.alpha = 0;
            batteryPanelGroup.alpha = 0;
            medsPanelGroup.alpha = 0;
            flashlightPanelGroup.alpha = 0;

            quest.alpha = 0;
            fear.alpha = 0;
            batteryImage.alpha = 0;
            batteryIconsHolder.alpha = 0;
            medicineIconsHolder.alpha = 0;
            flashlightSlider.alpha = 0;

            pickupIndicator.alpha = 0;
            reloadbatteryIndicator.alpha = 0;
            useMedsIndicator.alpha = 0;
            onOffFlashlightIndicator.alpha = 0;
            scrollIndicator.alpha = 0;
            slowWalkIndicator.alpha = 0;
            sprintIndicator.alpha = 0;

            StartTutorial();
        }
        else
        {
            fPSController.SetLookLocked(false);
            fPSController.SetMovementLocked(false);
            flashlightLookController.SetLookLocked(false);
            enemySpawner.ActiveEnemySpawner(true);

            questPanel.SetActive(false);
            fearPanel.SetActive(false);
            batteryPanel.SetActive(false);
            medsPanel.SetActive(false);
            flashlightPanel.SetActive(false);

            clickToShowNext.alpha = 0;

            questPanelGroup.alpha = 0;
            fearPanelGroup.alpha = 0;
            batteryPanelGroup.alpha = 0;
            medsPanelGroup.alpha = 0;
            flashlightPanelGroup.alpha = 0;

            quest.alpha = 1;
            fear.alpha = 1;
            batteryImage.alpha = 1;
            batteryIconsHolder.alpha = 1;
            medicineIconsHolder.alpha = 1;
            flashlightSlider.alpha = 1;

            pickupIndicator.alpha = 0;
            reloadbatteryIndicator.alpha = 0;
            useMedsIndicator.alpha = 0;
            onOffFlashlightIndicator.alpha = 0;
            scrollIndicator.alpha = 0;
            slowWalkIndicator.alpha = 0;
            sprintIndicator.alpha = 0;
        }
    }

    private void StartTutorial()
    {
        StartCoroutine(StartShowSequence());
    }

    private IEnumerator StartShowSequence()
    {
        yield return null;

        questPanel.SetActive(true);
        yield return StartCoroutine(FadeIn(2f, questPanelGroup, quest));

        yield return StartCoroutine(WaitForInput());

        fearPanel.SetActive(true);
        yield return StartCoroutine(FadeIn(2f, fearPanelGroup, fear));

        yield return StartCoroutine(WaitForInput());

        batteryPanel.SetActive(true);
        yield return StartCoroutine(FadeIn(2f, batteryPanelGroup, batteryImage, batteryIconsHolder));

        yield return StartCoroutine(WaitForInput());

        medsPanel.SetActive(true);
        yield return StartCoroutine(FadeIn(2f, medsPanelGroup, medicineIconsHolder));

        yield return StartCoroutine(WaitForInput());

        flashlightPanel.SetActive(true);
        yield return StartCoroutine(FadeIn(2f, flashlightPanelGroup, flashlightSlider));

        yield return StartCoroutine(WaitForInput());
        yield return StartCoroutine(FadeIn(1f, pickupIndicator));

        yield return StartCoroutine(WaitForInput());
        yield return StartCoroutine(FadeIn(1f, reloadbatteryIndicator));

        yield return StartCoroutine(WaitForInput());
        yield return StartCoroutine(FadeIn(1f, useMedsIndicator));

        yield return StartCoroutine(WaitForInput());
        yield return StartCoroutine(FadeIn(1f, onOffFlashlightIndicator));

        yield return StartCoroutine(WaitForInput());
        yield return StartCoroutine(FadeIn(1f, scrollIndicator));

        yield return StartCoroutine(WaitForInput());
        yield return StartCoroutine(FadeIn(1f, slowWalkIndicator));

        yield return StartCoroutine(WaitForInput());
        yield return StartCoroutine(FadeIn(1f, sprintIndicator));

        yield return StartCoroutine(WaitForInput());

        TutorialManager.Instance.SetTutorialEnabled(false);
        questManager.OnTutorialFinished();

        gameObject.SetActive(false);
        fPSController.SetLookLocked(false);
        fPSController.SetMovementLocked(false);
        flashlightLookController.SetLookLocked(false);
        enemySpawner.ActiveEnemySpawner(true);
    }

    private IEnumerator FadeIn(float duration, params CanvasGroup[] canvasGroups)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, time / duration);

            foreach (CanvasGroup canvasGroup in canvasGroups)
                canvasGroup.alpha = alpha;

            yield return null;
        }

        foreach (CanvasGroup canvasGroup in canvasGroups)
            canvasGroup.alpha = 1f;
    }

    private IEnumerator WaitForInput()
    {
        bool inputReceived = false;

        void OnPerformed(InputAction.CallbackContext context)
        {
            inputReceived = true;
        }

        waitInputAction.action.performed += OnPerformed;

        while (!inputReceived)
            yield return null;

        waitInputAction.action.performed -= OnPerformed;
    }
}
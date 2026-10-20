using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class QuestManager : MonoBehaviour
{
    [SerializeField] private ItemInventory inventory;
    [SerializeField] private GameObject questPanelGameObject;
    [SerializeField] private TMP_Text questText;

    [Header("Quest transition")]
    [SerializeField] private GameObject checkmark;
    [SerializeField] private AudioClip questCompletedClip;
    [SerializeField, Min(0f)] private float completedHoldTime = 1.2f;

    [Header("Player")]
    [SerializeField] private FPSController playerController;
    [SerializeField] private PauseController pauseController;

    [Header("Result")]
    [SerializeField] private GameObject resultPanel;

    [Header("Quest 1")]
    [SerializeField] private ItemData belt;
    [SerializeField] private ItemData clothes;
    [SerializeField] private ItemData hood;
    [SerializeField] private ItemData scarf;

    [Header("Quest 2")]
    [SerializeField] private ItemData lighter;
    [SerializeField] private ItemData lighterFuel;

    [Header("Wygląd zadań")]
    [SerializeField] private string doneColor = "#2E7D32";
    [SerializeField] private string carriedColor = "#A85F00";
    [SerializeField] private string todoColor = "#5A5A5A";
    [SerializeField] private string hintColor = "#1A1A1A";
    [SerializeField, Min(1)] private int requiredFuel = 3;
    [SerializeField] private float pulseScale = 1.05f;
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.35f;
    [SerializeField] private PlayerOxygen playerOxygen;

    [SerializeField] private ItemSlot[] quest1Slots;
    [SerializeField] private PedestalBurner pedestalBurner;
    [SerializeField] private LastQuest lastQuest;
    [SerializeField] private SwitchLights switchLights;

    [SerializeField] private AudioClip blackoutLights;
    [SerializeField] private AudioClip fire;
    [SerializeField] private LocalizedString runFromHome;
    [SerializeField] private LocalizedString findOnThisFloor;
    [SerializeField] private LocalizedString beltString;
    [SerializeField] private LocalizedString clothesString;
    [SerializeField] private LocalizedString hoodString;
    [SerializeField] private LocalizedString scarfString;
    [SerializeField] private LocalizedString placeOnPedestal;
    [SerializeField] private LocalizedString goUp;
    [SerializeField] private LocalizedString lighterString;
    [SerializeField] private LocalizedString lighterFluidString;
    [SerializeField] private LocalizedString gobacktopedestal;
    [SerializeField] private LocalizedString tutorialQuestDescription;

    [Header("Teksty trackera")]
    [SerializeField] private LocalizedString questHeader = new LocalizedString("Gameplay", "QuestHeader");
    [SerializeField] private LocalizedString quest1Title = new LocalizedString("Gameplay", "Quest1Title");
    [SerializeField] private LocalizedString quest2Title = new LocalizedString("Gameplay", "Quest2Title");
    [SerializeField] private LocalizedString quest3Title = new LocalizedString("Gameplay", "Quest3Title");
    [SerializeField] private LocalizedString onPedestalCount = new LocalizedString("Gameplay", "QuestOnPedestal");
    [SerializeField] private LocalizedString carryingCount = new LocalizedString("Gameplay", "QuestCarrying");
    [SerializeField] private LocalizedString hintCarry = new LocalizedString("Gameplay", "QuestHintCarry");
    [SerializeField] private LocalizedString hintSearch = new LocalizedString("Gameplay", "QuestHintSearch");
    [SerializeField] private LocalizedString hintGoUp = new LocalizedString("Gameplay", "QuestHintGoUp");
    [SerializeField] private LocalizedString hintFindLighter = new LocalizedString("Gameplay", "QuestHintFindLighter");
    [SerializeField] private LocalizedString hintFindFuel = new LocalizedString("Gameplay", "QuestHintFindFuel");
    [SerializeField] private LocalizedString hintBurn = new LocalizedString("Gameplay", "QuestHintBurn");
    [SerializeField] private LocalizedString hintEscape = new LocalizedString("Gameplay", "QuestHintEscape");
    [SerializeField] private LocalizedString hintNoOxygen = new LocalizedString("Gameplay", "QuestHintNoOxygen");

    private int currentQuest = 1;

    private bool pedestalCompleted;
    private bool pedestalBurned;
    private bool escaped;

    private SlideFadePanel questPanel;
    private Coroutine transitionRoutine;
    private string pendingText;
    private bool isTextLocked;
    private bool isQuestPanelRevealed;
    private Coroutine pulseRoutine;

    private void Start()
    {
        inventory.OnInventoryChanged += UpdateQuestUI;

        foreach (ItemSlot slot in quest1Slots)
            slot.OnItemPlaced += OnQuest1ItemPlaced;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        pedestalCompleted = false;
        pedestalBurned = false;
        escaped = false;

        if (questPanelGameObject != null)
            questPanel = questPanelGameObject.GetComponent<SlideFadePanel>();

        if (checkmark != null)
            checkmark.SetActive(false);

        if (playerOxygen == null)
            playerOxygen = FindFirstObjectByType<PlayerOxygen>(FindObjectsInactive.Include);

        if (playerOxygen != null)
            playerOxygen.OnActivated += UpdateQuestUI;

        if (TutorialManager.Instance.IsTutorialEnabled)
        {
            isTextLocked = true;
            questText.text = tutorialQuestDescription.GetLocalizedString();
        }
        else if (questPanel != null)
        {
            questPanel.HideInstant();
        }

        UpdateQuestUI();
    }

    private void OnDestroy()
    {
        inventory.OnInventoryChanged -= UpdateQuestUI;

        foreach (ItemSlot slot in quest1Slots)
            slot.OnItemPlaced -= OnQuest1ItemPlaced;

        if (playerOxygen != null)
            playerOxygen.OnActivated -= UpdateQuestUI;
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
        UpdateQuestUI();
    }

    private void OnQuest1ItemPlaced(ItemSlot slot)
    {
        UpdateQuestUI();

        if (GetPlacedItemsCount() >= 4)
            CompletePedestal();
    }

    private int GetPlacedItemsCount()
    {
        int count = 0;

        foreach (ItemSlot slot in quest1Slots)
        {
            if (slot.IsFilled)
                count++;
        }

        return count;
    }

    public void CompletePedestal()
    {
        pedestalCompleted = true;

        if (pedestalCompleted && currentQuest == 1)
        {
            ChangeQuest(2);
        }
    }

    public void BurnPedestal()
    {
        pedestalBurned = true;

        if (pedestalBurned && currentQuest == 2)
        {
            ChangeQuest(3);
        }

        AudioManager.Instance.PlaySFX(blackoutLights);
        AudioManager.Instance.PlaySFXAtPosition(fire, pedestalBurner.transform.position);

        switchLights.SwitchToDarkLightmap();
    }

    public void Escape()
    {
        if (currentQuest != 3 || escaped)
            return;

        escaped = true;
        UpdateQuestUI();
    }

    private void UpdateQuestUI()
    {
        switch (currentQuest)
        {
            case 1:
                UpdateQuest1();
                break;

            case 2:
                UpdateQuest2();
                pedestalBurner.Unlock();
                break;

            case 3:
                UpdateQuest3();
                lastQuest.CompleteLastQuest();
                break;
        }
    }


    public void OnTutorialFinished()
    {
        StartTransition(false);
    }

    // Wywołaj przy skipie i przy naturalnym końcu cutsceny - drugie wywołanie jest ignorowane.
    public void OnCutsceneFinished()
    {
        if (isQuestPanelRevealed)
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialEnabled)
            return;

        isQuestPanelRevealed = true;

        if (questPanel != null)
            StartCoroutine(questPanel.Show());
    }

    private void ChangeQuest(int newQuest)
    {
        currentQuest = newQuest;
        isTextLocked = true;

        UpdateQuestUI();

        StartTransition(true);
    }

    private void StartTransition(bool questCompleted)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(QuestTransitionRoutine(questCompleted));
    }

    private IEnumerator QuestTransitionRoutine(bool questCompleted)
    {
        if (questCompleted)
        {
            if (checkmark != null)
                checkmark.SetActive(true);

            if (questCompletedClip != null)
                AudioManager.Instance.PlaySFX(questCompletedClip);

            yield return new WaitForSeconds(completedHoldTime);
        }

        if (questPanel != null)
            yield return questPanel.Hide();

        questText.text = pendingText;

        if (checkmark != null)
            checkmark.SetActive(false);

        isTextLocked = false;

        if (questPanel != null)
            yield return questPanel.Show();

        transitionRoutine = null;
    }

    // Podczas przejścia (i tutorialu) tekst czeka w pendingText, a na ekranie zostaje stary
    // aż do momentu, gdy panel jest schowany.
    private void SetQuestText(string text)
    {
        pendingText = text;

        if (isTextLocked || questText.text == text)
            return;

        bool changed = !string.IsNullOrEmpty(questText.text);
        questText.text = text;

        if (changed && questPanelGameObject != null && questPanelGameObject.activeInHierarchy)
        {
            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);

            pulseRoutine = StartCoroutine(PulseRoutine());
        }
    }

    private IEnumerator PulseRoutine()
    {
        Transform panel = questPanelGameObject.transform;

        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / pulseDuration);
            panel.localScale = Vector3.one * Mathf.Lerp(1f, pulseScale, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        panel.localScale = Vector3.one;
        pulseRoutine = null;
    }

    private void UpdateQuest3()
    {
        if (!escaped)
        {
            bool noOxygen = playerOxygen != null && playerOxygen.IsActive;

            SetQuestText(
                Header(3) +
                Title(quest3Title.GetLocalizedString()) +
                Hint(noOxygen ? hintNoOxygen.GetLocalizedString() : hintEscape.GetLocalizedString(), noOxygen, false));
            return;
        }

        if (playerController != null)
        {
            playerController.SetMovementLocked(true);
            playerController.SetLookLocked(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        pauseController.CanPause = false;

        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    private void UpdateQuest1()
    {
        ItemData[] items = { belt, clothes, hood, scarf };
        string[] names =
        {
            beltString.GetLocalizedString(),
            clothesString.GetLocalizedString(),
            hoodString.GetLocalizedString(),
            scarfString.GetLocalizedString()
        };

        int placed = GetPlacedItemsCount();
        int carried = 0;
        int missing = 0;
        var text = new System.Text.StringBuilder();

        text.Append(Header(1));
        text.Append(Title(quest1Title.GetLocalizedString()));

        for (int i = 0; i < items.Length; i++)
        {
            ItemState state = GetItemState(items[i]);

            if (state == ItemState.Carried)
                carried++;
            else if (state == ItemState.Missing)
                missing++;

            if (i % 2 == 1)
                text.Append("<pos=52%>");

            text.Append(ItemRow(names[i].Trim(), state));

            if (i % 2 == 1 || i == items.Length - 1)
                text.Append('\n');
        }

        text.Append("<size=85%><color=" + todoColor + ">");
        text.Append(string.Format(onPedestalCount.GetLocalizedString(), placed, items.Length));
        if (carried > 0)
            text.Append("  ·  <color=" + carriedColor + ">" + string.Format(carryingCount.GetLocalizedString(), carried) + "</color>");
        text.Append("</color></size>\n");

        if (carried > 0)
            text.Append(Hint(string.Format(hintCarry.GetLocalizedString(), carried), false));
        else if (missing > 0)
            text.Append(Hint(string.Format(hintSearch.GetLocalizedString(), missing), false));

        SetQuestText(text.ToString().TrimEnd('\n'));
    }

    private enum ItemState
    {
        Missing,
        Carried,
        Done
    }

    private ItemState GetItemState(ItemData item)
    {
        foreach (ItemSlot slot in quest1Slots)
        {
            if (slot.IsFilled && slot.requiredItem == item)
                return ItemState.Done;
        }

        return inventory.HasItem(item) ? ItemState.Carried : ItemState.Missing;
    }

    private void UpdateQuest2()
    {
        bool hasLighter = inventory.HasItem(lighter);
        int fuelCount = Mathf.Min(inventory.GetCount(lighterFuel), requiredFuel);
        bool hasFuel = fuelCount >= requiredFuel;

        var text = new System.Text.StringBuilder();
        text.Append(Header(2));
        text.Append(Title(quest2Title.GetLocalizedString()));
        text.Append(ItemRow(lighterString.GetLocalizedString().Trim(), hasLighter ? ItemState.Done : ItemState.Missing));
        text.Append('\n');
        text.Append(ItemRow(lighterFluidString.GetLocalizedString().Trim() + $"  {fuelCount}/{requiredFuel}", hasFuel ? ItemState.Done : fuelCount > 0 ? ItemState.Carried : ItemState.Missing));
        text.Append('\n');

        if (hasLighter && hasFuel)
            text.Append(Hint(hintBurn.GetLocalizedString(), true));
        else if (!hasLighter && fuelCount == 0)
            text.Append(Hint(hintGoUp.GetLocalizedString(), false));
        else if (!hasLighter)
            text.Append(Hint(hintFindLighter.GetLocalizedString(), false));
        else
            text.Append(Hint(string.Format(hintFindFuel.GetLocalizedString(), requiredFuel - fuelCount), false));

        SetQuestText(text.ToString().TrimEnd('\n'));
    }

    private string Header(int quest)
    {
        return "<size=72%><color=" + todoColor + "><b>" + string.Format(questHeader.GetLocalizedString(), quest, 3) + "</b></color></size>\n";
    }

    private static string Title(string title)
    {
        return "<size=112%><b>" + title + "</b></size>\n<size=40%> </size>\n";
    }

    private string ItemRow(string label, ItemState state)
    {
        switch (state)
        {
            case ItemState.Done:
                return "<color=" + doneColor + "><sprite name=\"quest_done\" tint=1> <s>" + label + "</s></color>";
            case ItemState.Carried:
                return "<color=" + carriedColor + "><sprite name=\"quest_carried\" tint=1> <b>" + label + "</b></color>";
            default:
                return "<color=" + todoColor + "><sprite name=\"quest_todo\" tint=1> " + label + "</color>";
        }
    }

    private string Hint(string hint, bool urgent, bool spaced = true)
    {
        string color = urgent ? carriedColor : hintColor;
        return (spaced ? "<size=40%> </size>\n" : string.Empty) + "<size=90%><color=" + color + "><sprite name=\"quest_arrow\" tint=1><indent=1.4em>" + (urgent ? "<b>" + hint + "</b>" : hint) + "</indent></color></size>";
    }
}

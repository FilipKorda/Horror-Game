using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class ChaseMinigameController : MonoBehaviour
{
    public static ChaseMinigameController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private TextMeshProUGUI textKeyIcon;
    [SerializeField] private RectTransform timerRing;
    [SerializeField] private RectTransform targetRing;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private AudioClip successClickClip;

    [Header("Wygląd")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Image timerRingImage;
    [Tooltip("Poświata strefy trafienia - zapala się, gdy pierścień jest w oknie, oraz przy trafieniu / pomyłce.")]
    [SerializeField] private Image hitZoneGlow;
    [Tooltip("Rozbłysk zostający w miejscu trafienia. Musi leżeć poza Minigame Panel, bo panel od razu przeskakuje.")]
    [SerializeField] private Image hitBurst;
    [SerializeField] private Image vignette;
    [SerializeField] private RectTransform keyPlate;
    [SerializeField] private RectTransform progressContainer;
    [SerializeField] private Image progressPipTemplate;
    [SerializeField] private float progressPipSpacing = 26f;

    [Header("Kolory")]
    [SerializeField] private Color ringColor = new Color(0.93f, 0.90f, 0.82f, 1f);
    [SerializeField] private Color ringInWindowColor = Color.white;
    [SerializeField] private Color ringLateColor = new Color(0.80f, 0.12f, 0.10f, 1f);
    [SerializeField] private Color successColor = new Color(0.80f, 1f, 0.78f, 1f);
    [SerializeField] private Color failColor = new Color(0.90f, 0.10f, 0.08f, 1f);
    [SerializeField] private Color pipPendingColor = new Color(0.22f, 0.22f, 0.23f, 1f);
    [SerializeField] private Color pipDoneColor = new Color(0.93f, 0.90f, 0.82f, 1f);
    [SerializeField, Range(0f, 1f)] private float vignetteAlpha = 0.85f;

    [Header("Input")]
    [SerializeField] private InputActionReference keyPressAction;
    [SerializeField] private InputActionReference[] keyHintActions = new InputActionReference[8];
    [SerializeField] private InputGlyphProvider glyphProvider;

    [Header("Localization")]
    [SerializeField] private LocalizedString keyPromptString;

    [Header("Blokada inputu gracza na czas minigry")]
    [SerializeField] private FlashlightController flashlightController;
    [SerializeField] private InteractionSystem interactionSystem;

    [Header("Blokowane na czas minigry")]
    [SerializeField] private InputActionReference[] actionsToSuppressDuringMinigame;

    [Header("Sekwencja")]
    [SerializeField] private int sequenceLength = 4;

    [Header("Timing")]
    [SerializeField] private float promptDuration = 1.5f;
    [SerializeField] private float ringStartScale = 3f;

    [Header("Pozycja minigry")]
    [SerializeField] private RectTransform minigamePanelRect;
    [SerializeField] private RectTransform minigameMovementArea;
    [SerializeField] private float minPanelMoveDistance = 150f;
    [SerializeField] private float maxPanelMoveDistance = 500f;
    [SerializeField] private int maxPositionAttempts = 50;

    private Action onWin;
    private Action onLose;

    private int[] sequence;
    private int sequenceIndex;
    private float promptStartTime;
    private bool minigameActive;

    private bool flashlightWasOn;
    private bool deviceDetectorSubscribed;
    private Vector2 panelStartPosition;
    private Vector2 keyIconBasePosition;

    private Coroutine outroCoroutine;
    private Coroutine burstCoroutine;

    private readonly List<InputAction> suppressedActions = new List<InputAction>();
    private readonly List<Image> progressPips = new List<Image>();

    public bool IsActive => minigameActive;

    private const int LogicalKeyCount = 8;
    private const int KeyboardBindingOffset = 0;
    private const int GamepadBindingOffset = 8;

    private const float PanelFadeInTime = 0.15f;
    private const float RingFadeInPortion = 0.2f;
    private const float KeyPopTime = 0.14f;
    private const float KeyPopScale = 1.3f;
    private const float BurstTime = 0.3f;
    private const float OutroTime = 0.4f;
    private const float FailShakeTime = 0.25f;
    private const float FailShakeStrength = 14f;
    private const float PulsesPerSecond = 1.6f;
    private const float InWindowGlowAlpha = 0.28f;

    [Header("Okno trafienia / Gizmos")]
    [SerializeField] private bool showTargetGizmo = true;
    [Tooltip("Rozmiar timerRing względem targetRing na ekranie (1 = timerRing idealnie na targetRing).")]
    [SerializeField] private float targetRingMinScale = 0.9f;
    [SerializeField] private float targetRingMaxScale = 1.1f;

    private void Awake()
    {
        Instance = this;

        if (minigamePanelRect != null)
            panelStartPosition = minigamePanelRect.anchoredPosition;

        if (textKeyIcon != null)
            keyIconBasePosition = textKeyIcon.rectTransform.anchoredPosition;

        HideVisuals();
    }

    private void Start()
    {
        // InputDeviceDetector może powstać dopiero po naszym OnEnable
        SubscribeDeviceDetector();
    }

    private void OnEnable()
    {
        if (keyPressAction != null)
        {
            keyPressAction.action.Enable();
            keyPressAction.action.performed += OnKeyPressPerformed;
        }

        SubscribeDeviceDetector();

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        if (keyPressAction != null)
        {
            keyPressAction.action.performed -= OnKeyPressPerformed;
            keyPressAction.action.Disable();
        }

        if (deviceDetectorSubscribed && InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;

        deviceDetectorSubscribed = false;

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (minigameActive)
        {
            minigameActive = false;
            SetPlayerInputLocked(false);

            if (crosshair != null)
                crosshair.SetActive(true);
        }

        outroCoroutine = null;
        burstCoroutine = null;

        HideVisuals();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void SubscribeDeviceDetector()
    {
        if (deviceDetectorSubscribed || InputDeviceDetector.Instance == null)
            return;

        InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
        deviceDetectorSubscribed = true;
    }

    private void Update()
    {
        if (!minigameActive)
            return;

        AnimateRing();
        AnimatePulse();
        CheckTimeout();
    }

    public void StartMinigame(Action winCallback, Action loseCallback)
    {
        bool wasActive = minigameActive;

        onWin = winCallback;
        onLose = loseCallback;

        sequence = GenerateSequence(Mathf.Max(1, sequenceLength));
        sequenceIndex = 0;
        minigameActive = true;

        if (!wasActive)
            SetPlayerInputLocked(true);

        if (outroCoroutine != null)
        {
            StopCoroutine(outroCoroutine);
            outroCoroutine = null;
        }

        if (minigamePanelRect != null)
            minigamePanelRect.anchoredPosition = panelStartPosition;

        if (minigamePanel != null)
            minigamePanel.SetActive(true);

        if (panelGroup != null)
            panelGroup.alpha = 0f;

        if (vignette != null)
        {
            vignette.gameObject.SetActive(true);
            SetAlpha(vignette, 0f);
        }

        if (hitZoneGlow != null)
            SetAlpha(hitZoneGlow, 0f);

        if (crosshair != null)
            crosshair.SetActive(false);

        BuildProgressPips();
        StartPrompt();
    }

    private void SetPlayerInputLocked(bool locked)
    {
        if (flashlightController != null)
        {
            if (locked)
            {
                flashlightWasOn = flashlightController.IsOn;

                if (flashlightWasOn)
                    flashlightController.DisableFlashlight();
            }

            flashlightController.enabled = !locked;

            if (!locked && flashlightWasOn)
                flashlightController.EnabledFlashlight();
        }

        if (locked)
        {
            suppressedActions.Clear();

            if (actionsToSuppressDuringMinigame == null)
                return;

            foreach (InputActionReference actionReference in actionsToSuppressDuringMinigame)
            {
                if (actionReference == null)
                    continue;

                InputAction action = actionReference.action;

                if (action == null || !action.enabled)
                    continue;

                action.Disable();
                suppressedActions.Add(action);
            }
        }
        else
        {
            foreach (InputAction action in suppressedActions)
                action.Enable();

            suppressedActions.Clear();
        }
    }

    private int[] GenerateSequence(int length)
    {
        int[] result = new int[length];

        for (int i = 0; i < length; i++)
            result[i] = UnityEngine.Random.Range(0, LogicalKeyCount);

        return result;
    }

    private void StartPrompt()
    {
        promptStartTime = Time.time;

        RefreshKeyHint();
        RefreshProgressPips();

        if (timerRing != null)
            timerRing.localScale = Vector3.one * ringStartScale;

        if (timerRingImage != null)
            timerRingImage.color = WithAlpha(ringColor, 0f);
    }

    private void RefreshKeyHint()
    {
        if (textKeyIcon == null ||
            glyphProvider == null ||
            keyPromptString == null ||
            sequence == null ||
            sequenceIndex >= sequence.Length)
            return;

        int logicalIndex = sequence[sequenceIndex];

        if (logicalIndex < 0 ||
            logicalIndex >= keyHintActions.Length ||
            keyHintActions[logicalIndex] == null)
            return;

        InputDeviceType deviceType =
            InputDeviceDetector.Instance != null
                ? InputDeviceDetector.Instance.CurrentDevice
                : InputDeviceType.KeyboardMouse;

        string glyphTag = glyphProvider.GetGlyphTag(
            keyHintActions[logicalIndex].action,
            deviceType
        );

        keyPromptString.Arguments = new object[] { glyphTag };
        textKeyIcon.text = keyPromptString.GetLocalizedString();

        CenterKeyIcon();
    }

    private void CenterKeyIcon()
    {
        textKeyIcon.ForceMeshUpdate();

        TMP_TextInfo textInfo = textKeyIcon.textInfo;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        bool anyVisible = false;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            min = Vector2.Min(min, character.bottomLeft);
            max = Vector2.Max(max, character.topRight);
            anyVisible = true;
        }

        if (!anyVisible)
            return;

        textKeyIcon.rectTransform.anchoredPosition = keyIconBasePosition - (min + max) * 0.5f;
    }

    private void HandleDeviceChanged(InputDeviceType device)
    {
        if (minigameActive)
            RefreshKeyHint();
    }

    private void HandleLocaleChanged(Locale locale)
    {
        if (minigameActive)
            RefreshKeyHint();
    }

    private void BuildProgressPips()
    {
        if (progressContainer == null || progressPipTemplate == null)
            return;

        progressPipTemplate.gameObject.SetActive(false);

        while (progressPips.Count < sequence.Length)
        {
            Image pip = Instantiate(progressPipTemplate, progressContainer);
            progressPips.Add(pip);
        }

        for (int i = 0; i < progressPips.Count; i++)
        {
            bool used = i < sequence.Length;
            progressPips[i].gameObject.SetActive(used);

            if (!used)
                continue;

            float x = (i - (sequence.Length - 1) * 0.5f) * progressPipSpacing;
            progressPips[i].rectTransform.anchoredPosition = new Vector2(x, 0f);
        }
    }

    private void RefreshProgressPips()
    {
        for (int i = 0; i < progressPips.Count; i++)
        {
            progressPips[i].color = i < sequenceIndex ? pipDoneColor : pipPendingColor;
            progressPips[i].rectTransform.localScale = Vector3.one;
        }
    }

    private void AnimateRing()
    {
        if (timerRing == null)
            return;

        float elapsed = Time.time - promptStartTime;
        float scale = GetRingScaleAtTime(elapsed);

        timerRing.localScale = Vector3.one * scale;

        float ratio = GetTimerRatio();
        bool inWindow = IsRatioInsideTarget(ratio);
        bool late = ratio >= 0f && ratio < targetRingMinScale;

        if (timerRingImage != null)
        {
            float fadeIn = Mathf.Clamp01(elapsed / (promptDuration * RingFadeInPortion));

            Color color = late ? ringLateColor : inWindow ? ringInWindowColor : ringColor;
            timerRingImage.color = WithAlpha(color, fadeIn);
        }

        if (hitZoneGlow != null)
        {
            float targetAlpha = inWindow ? InWindowGlowAlpha : 0f;
            Color glow = WithAlpha(ringInWindowColor, Mathf.MoveTowards(hitZoneGlow.color.a, targetAlpha, Time.deltaTime * 8f));
            hitZoneGlow.color = glow;
        }

        if (panelGroup != null && panelGroup.alpha < 1f)
            panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, 1f, Time.deltaTime / PanelFadeInTime);
    }

    private void AnimatePulse()
    {
        float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * PulsesPerSecond)), 6f);

        if (vignette != null)
        {
            float alpha = vignetteAlpha * (0.85f + 0.15f * beat);
            SetAlpha(vignette, Mathf.MoveTowards(vignette.color.a, alpha, Time.deltaTime * 4f));
        }

        if (keyPlate != null)
        {
            float pop = 1f - Mathf.Clamp01((Time.time - promptStartTime) / KeyPopTime);
            keyPlate.localScale = Vector3.one * (1f + (KeyPopScale - 1f) * pop + 0.03f * beat);
        }

        if (sequenceIndex < progressPips.Count)
            progressPips[sequenceIndex].rectTransform.localScale = Vector3.one * (1f + 0.25f * beat);
    }

    private void OnKeyPressPerformed(InputAction.CallbackContext ctx)
    {
        if (!minigameActive)
            return;

        if (Time.timeScale <= 0f)
            return;

        int bindingIndex = ctx.action.GetBindingIndexForControl(ctx.control);

        if (bindingIndex < 0)
            return;

        int logicalIndex = GetLogicalIndex(bindingIndex);

        if (logicalIndex < 0)
            return;

        if (logicalIndex != sequence[sequenceIndex])
        {
            Resolve(false);
            return;
        }

        if (!IsTimerInsideTarget())
        {
            Resolve(false);
            return;
        }

        if (AudioManager.Instance != null && successClickClip != null)
            AudioManager.Instance.PlaySFX(successClickClip);

        AdvanceSequence();
    }

    private int GetLogicalIndex(int bindingIndex)
    {
        if (bindingIndex >= KeyboardBindingOffset &&
            bindingIndex < KeyboardBindingOffset + LogicalKeyCount)
        {
            return bindingIndex - KeyboardBindingOffset;
        }

        if (bindingIndex >= GamepadBindingOffset &&
            bindingIndex < GamepadBindingOffset + LogicalKeyCount)
        {
            return bindingIndex - GamepadBindingOffset;
        }

        return -1;
    }

    private void CheckTimeout()
    {
        if (Time.time - promptStartTime >= promptDuration)
            Resolve(false);
    }

    private void AdvanceSequence()
    {
        sequenceIndex++;

        if (sequenceIndex >= sequence.Length)
        {
            Resolve(true);
            return;
        }

        PlayHitBurst(successColor);

        MoveMinigamePanel();
        StartPrompt();
    }

    private void PlayHitBurst(Color color)
    {
        if (hitBurst == null || targetRing == null)
            return;

        if (burstCoroutine != null)
            StopCoroutine(burstCoroutine);

        burstCoroutine = StartCoroutine(HitBurstRoutine(color));
    }

    private IEnumerator HitBurstRoutine(Color color)
    {
        RectTransform burstRect = hitBurst.rectTransform;

        burstRect.position = targetRing.position;
        hitBurst.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < BurstTime)
        {
            float t = elapsed / BurstTime;

            burstRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.7f, t);
            hitBurst.color = WithAlpha(color, 1f - t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        hitBurst.gameObject.SetActive(false);
        burstCoroutine = null;
    }

    private void MoveMinigamePanel()
    {
        if (minigamePanelRect == null || minigameMovementArea == null)
            return;

        Vector2 currentPosition = minigamePanelRect.anchoredPosition;

        Vector2 areaSize = minigameMovementArea.rect.size;
        Vector2 panelSize = minigamePanelRect.rect.size;

        Vector2 min = -areaSize * 0.5f + panelSize * 0.5f;
        Vector2 max = areaSize * 0.5f - panelSize * 0.5f;

        for (int i = 0; i < maxPositionAttempts; i++)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;

            if (direction == Vector2.zero)
                continue;

            float distance = UnityEngine.Random.Range(
                minPanelMoveDistance,
                maxPanelMoveDistance
            );

            Vector2 newPosition = currentPosition + direction * distance;

            if (newPosition.x < min.x || newPosition.x > max.x ||
                newPosition.y < min.y || newPosition.y > max.y)
                continue;

            minigamePanelRect.anchoredPosition = newPosition;
            return;
        }
    }

    private void Resolve(bool won)
    {
        minigameActive = false;

        if (crosshair != null)
            crosshair.SetActive(true);

        SetPlayerInputLocked(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopHeartbeat();

        // AudioManager.Instance.StopMonsterRoar(voiceSource);

        if (isActiveAndEnabled)
            outroCoroutine = StartCoroutine(OutroRoutine(won));
        else
            HideVisuals();

        if (won)
            onWin?.Invoke();
        else
            onLose?.Invoke();
    }

    private IEnumerator OutroRoutine(bool won)
    {
        Color color = won ? successColor : failColor;
        Vector2 basePosition = minigamePanelRect != null ? minigamePanelRect.anchoredPosition : Vector2.zero;
        float vignetteStart = vignette != null ? vignette.color.a : 0f;

        if (keyPlate != null)
            keyPlate.localScale = Vector3.one;

        if (won)
        {
            sequenceIndex = sequence.Length;
            RefreshProgressPips();
        }

        if (timerRingImage != null)
            timerRingImage.color = color;

        float elapsed = 0f;

        while (elapsed < OutroTime)
        {
            float t = elapsed / OutroTime;

            if (hitZoneGlow != null)
                hitZoneGlow.color = WithAlpha(color, 1f - t * 0.5f);

            if (panelGroup != null)
                panelGroup.alpha = 1f - Mathf.Clamp01((t - 0.35f) / 0.65f);

            if (vignette != null)
                SetAlpha(vignette, vignetteStart * (1f - t));

            if (won && timerRing != null)
                timerRing.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, t);

            if (!won && minigamePanelRect != null)
            {
                float shake = 1f - Mathf.Clamp01(elapsed / FailShakeTime);
                minigamePanelRect.anchoredPosition =
                    basePosition + UnityEngine.Random.insideUnitCircle * (FailShakeStrength * shake);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        outroCoroutine = null;
        HideVisuals();
    }

    private void HideVisuals()
    {
        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (vignette != null)
            vignette.gameObject.SetActive(false);

        if (hitBurst != null)
            hitBurst.gameObject.SetActive(false);

        if (minigamePanelRect != null)
            minigamePanelRect.anchoredPosition = panelStartPosition;
    }

    private bool IsTimerInsideTarget()
    {
        return IsRatioInsideTarget(GetTimerRatio());
    }

    private bool IsRatioInsideTarget(float ratio)
    {
        return ratio >= targetRingMinScale &&
               ratio <= targetRingMaxScale;
    }

    private float GetTimerRatio()
    {
        if (timerRing == null || targetRing == null)
            return -1f;

        float targetSize = GetScreenSize(targetRing);

        if (targetSize <= 0f)
            return -1f;

        return GetScreenSize(timerRing) / targetSize;
    }

    private static float GetScreenSize(RectTransform ring)
    {
        return ring.rect.width * ring.lossyScale.x;
    }

    private float GetRingScaleAtTime(float elapsed)
    {
        float t = Mathf.Clamp01(elapsed / promptDuration);
        return Mathf.Lerp(ringStartScale, 0f, t);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        graphic.color = WithAlpha(graphic.color, alpha);
    }

    private void OnDrawGizmos()
    {
        if (!showTargetGizmo || targetRing == null)
            return;

        Gizmos.color = Color.red;

        DrawRingGizmo(targetRing, targetRingMinScale);
        DrawRingGizmo(targetRing, targetRingMaxScale);

        if (timerRing == null)
            return;

        Gizmos.color = IsTimerInsideTarget()
            ? Color.blue
            : Color.magenta;

        DrawRingGizmo(timerRing, 1f);
    }

    private static void DrawRingGizmo(RectTransform ring, float sizeMultiplier)
    {
        Vector3 center = ring.TransformPoint(ring.rect.center);

        Gizmos.DrawWireSphere(center, GetScreenSize(ring) * 0.5f * sizeMultiplier);
    }
}

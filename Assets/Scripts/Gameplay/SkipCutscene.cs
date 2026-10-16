using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkipCutscene : MonoBehaviour
{
    [SerializeField] private CameraSequence cameraSequence;
    [SerializeField] private InputActionReference skipAction;
    [SerializeField] private Image skipImage;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float resetDuration = 0.25f;


    
    private float holdTime;
    private bool isHolding;
    private bool isSkipped;

    private void Awake()
    {
        skipImage.fillAmount = 0f;
    }

    private void OnEnable()
    {
        skipAction.action.Enable();
        skipAction.action.started += OnSkipStarted;
        skipAction.action.canceled += OnSkipCanceled;
    }

    private void OnDisable()
    {
        skipAction.action.started -= OnSkipStarted;
        skipAction.action.canceled -= OnSkipCanceled;
        skipAction.action.Disable();
    }

    private void Update()
    {
        if (isSkipped)
            return;

        if (isHolding)
        {
            holdTime += Time.deltaTime;

            skipImage.fillAmount = Mathf.Clamp01(holdTime / holdDuration);

            if (holdTime >= holdDuration)
            {
                isHolding = false;
                isSkipped = true;

                skipImage.fillAmount = 1f;
                cameraSequence.Skip();
            }

            return;
        }

        if (skipImage.fillAmount > 0f)
        {
            skipImage.fillAmount = Mathf.MoveTowards(
                skipImage.fillAmount,
                0f,
                Time.deltaTime / resetDuration
            );
        }
    }

    private void OnSkipStarted(InputAction.CallbackContext context)
    {
        if (isSkipped)
            return;

        isHolding = true;
        holdTime = skipImage.fillAmount * holdDuration;
    }

    private void OnSkipCanceled(InputAction.CallbackContext context)
    {
        isHolding = false;
    }
}
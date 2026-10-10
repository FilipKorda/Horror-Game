using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private PlayerHider playerHider;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference togglePauseAction;

    [SerializeField] private AudioClip pauseClip;
    [SerializeField] private AudioClip unpauseClip;

    public bool IsPaused { get; private set; }
    public bool CanPause = false;

    private void Start()
    {
        CanPause = true;
    }

    private void OnEnable()
    {
        togglePauseAction.action.Enable();
        togglePauseAction.action.performed += OnTogglePausePerformed;
    }

    private void OnDisable()
    {
        togglePauseAction.action.performed -= OnTogglePausePerformed;
        togglePauseAction.action.Disable();
    }

    private void OnTogglePausePerformed(InputAction.CallbackContext ctx)
    {
        if (CanPause)
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    private void PlayPauseClickSound()
    {
        AudioManager.Instance.PlayUISound(pauseClip);
    }

    private void PlayUnpauseClickSound()
    {
        AudioManager.Instance.PlayUISound(unpauseClip);
    }

    public void Pause()
    {
        if (IsPaused || !CanPause)
            return;

        IsPaused = true;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        if (playerHider != null)
        {
            playerHider.SetMovementLocked(true);
            playerHider.SetLookLocked(true);
        }

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayPauseClickSound();
        AudioManager.Instance.SetGameplayAudioPaused(true);
    }

    public void Resume()
    {
        if (!IsPaused || !CanPause)
            return;

        IsPaused = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (playerHider != null)
        {
            playerHider.SetMovementLocked(false);
            playerHider.SetLookLocked(false);
        }

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayUnpauseClickSound();
        AudioManager.Instance.SetGameplayAudioPaused(false);
    }
}
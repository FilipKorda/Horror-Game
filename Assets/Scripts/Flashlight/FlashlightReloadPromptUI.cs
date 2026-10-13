using UnityEngine;


public class FlashlightReloadPromptUI : MonoBehaviour
{
    [SerializeField] private FlashlightController flashlightController;
    [SerializeField] private GameObject promptPanel;

    private void OnEnable()
    {
        if (flashlightController == null)
            return;

        flashlightController.OnCanReloadChanged += HandleCanReloadChanged;
        HandleCanReloadChanged(flashlightController.CanReload); 
    }

    private void OnDisable()
    {
        if (flashlightController != null)
            flashlightController.OnCanReloadChanged -= HandleCanReloadChanged;
    }

    private void HandleCanReloadChanged(bool canReload)
    {
        if (promptPanel != null)
            promptPanel.SetActive(canReload);
    }
}
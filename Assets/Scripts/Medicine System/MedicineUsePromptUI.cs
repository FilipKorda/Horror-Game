using UnityEngine;


public class MedicineUsePromptUI : MonoBehaviour
{
    [SerializeField] private MedicineController medicineController;
    [SerializeField] private GameObject promptPanel;

    private void OnEnable()
    {
        if (medicineController == null)
            return;

        medicineController.OnCanUseChanged += HandleCanUseChanged;
        HandleCanUseChanged(medicineController.CanUseMedicine);
    }

    private void OnDisable()
    {
        if (medicineController != null)
            medicineController.OnCanUseChanged -= HandleCanUseChanged;
    }

    private void HandleCanUseChanged(bool canUse)
    {
        if (medicineController.fearMeter.CurrentFearPoints >= medicineController.fearMeter.MaxFearPoints || medicineController.fearMeter.CurrentFearPoints == 0)
            return;

        if (promptPanel != null)
            promptPanel.SetActive(canUse);
    }
}
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class MedicineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MedicineInventory medicineInventory;
    [SerializeField] private AudioClip openPotion;
    [SerializeField] private AudioClip medicineUse;
    [SerializeField] private MedicineDrinkAnimation drinkAnimation;
    public PlayerFearMeter fearMeter;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference useMedicineAction;

    [Header("Użycie")]
    [SerializeField] private float useDuration = 2f;
    [SerializeField] private int fearReductionPoints = 1;

    private bool isUsing;

    public bool IsUsing => isUsing;

    public bool CanUseMedicine => !isUsing
        && medicineInventory != null
        && medicineInventory.CurrentMedicine > 0;

    public event Action<bool> OnCanUseChanged;
    private bool lastCanUse;

    private void OnEnable()
    {
        useMedicineAction.action.Enable();
        useMedicineAction.action.performed += OnUseMedicinePerformed;

        if (medicineInventory != null)
            medicineInventory.OnMedicineCountChanged += HandleMedicineCountChanged;
    }

    private void OnDisable()
    {
        useMedicineAction.action.performed -= OnUseMedicinePerformed;
        useMedicineAction.action.Disable();

        if (medicineInventory != null)
            medicineInventory.OnMedicineCountChanged -= HandleMedicineCountChanged;
    }

    private void Start()
    {
        lastCanUse = CanUseMedicine;
    }

    private void HandleMedicineCountChanged(int count) => CheckCanUseChanged();

    private void CheckCanUseChanged()
    {
        bool current = CanUseMedicine;
        if (current == lastCanUse)
            return;

        lastCanUse = current;
        OnCanUseChanged?.Invoke(current);
    }

    private void OnUseMedicinePerformed(InputAction.CallbackContext ctx)
    {
        if (!CanUseMedicine || fearMeter.CurrentFearPoints >= fearMeter.MaxFearPoints || fearMeter.CurrentFearPoints == 0)
            return;

        StartCoroutine(UseMedicineRoutine());
    }

    private IEnumerator UseMedicineRoutine()
    {
        isUsing = true;
        CheckCanUseChanged();

        if (drinkAnimation != null)
        {
            yield return drinkAnimation.Play(PlayOpenSound, PlayDrinkSound);
        }
        else
        {
            PlayOpenSound();

            yield return new WaitForSeconds(0.1f);

            PlayDrinkSound();

            yield return new WaitForSeconds(useDuration);
        }

        if (medicineInventory.ConsumeMedicine())
            fearMeter.ReduceFear(fearReductionPoints);

        isUsing = false;
        CheckCanUseChanged();
    }

    private void PlayOpenSound()
    {
        AudioManager.Instance.PlaySFX(openPotion);
    }

    private void PlayDrinkSound()
    {
        AudioManager.Instance.PlaySFX(medicineUse);
    }
}
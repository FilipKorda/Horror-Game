using UnityEngine;

public class MedicineUIController : MonoBehaviour
{
    [SerializeField] private MedicineInventory medicineInventory;
    [SerializeField] private MedicineSlotUI slotPrefab;
    [Tooltip("Rodzic slotów (z Horizontal Layout Group). Jeśli puste, używany jest ten obiekt.")]
    [SerializeField] private Transform slotsRoot;

    private MedicineSlotUI[] slots;

    private void Awake()
    {
        if (medicineInventory == null || slotPrefab == null)
        {
            Debug.LogError($"[{nameof(MedicineUIController)}] Brak MedicineInventory albo slotPrefab.", this);
            return;
        }

        Transform root = slotsRoot != null ? slotsRoot : transform;
        slots = new MedicineSlotUI[medicineInventory.MaxMedicine];

        for (int i = 0; i < slots.Length; i++)
            slots[i] = Instantiate(slotPrefab, root);
    }

    private void OnEnable()
    {
        if (slots == null)
            return;

        medicineInventory.OnMedicineCountChanged += HandleMedicineCountChanged;
        UpdateSlots(medicineInventory.CurrentMedicine, false);
    }

    private void OnDisable()
    {
        if (slots == null)
            return;

        medicineInventory.OnMedicineCountChanged -= HandleMedicineCountChanged;
    }

    private void HandleMedicineCountChanged(int owned)
    {
        UpdateSlots(owned, true);
    }

    private void UpdateSlots(int owned, bool animate)
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetFilled(i < owned, animate);
    }
}
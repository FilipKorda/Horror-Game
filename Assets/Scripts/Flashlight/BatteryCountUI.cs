using UnityEngine;

public class BatteryCountUI : MonoBehaviour
{
    [SerializeField] private BatteryInventory batteryInventory;
    [SerializeField] private BatterySlotUI slotPrefab;
    [Tooltip("Rodzic slotów (z Horizontal Layout Group). Jeśli puste, używany jest ten obiekt.")]
    [SerializeField] private Transform slotsRoot;

    private BatterySlotUI[] slots;

    private void Awake()
    {
        if (batteryInventory == null || slotPrefab == null)
        {
            Debug.LogError($"[{nameof(BatteryCountUI)}] Brak BatteryInventory albo slotPrefab.", this);
            return;
        }

        Transform root = slotsRoot != null ? slotsRoot : transform;
        slots = new BatterySlotUI[batteryInventory.MaxBatteries];

        for (int i = 0; i < slots.Length; i++)
            slots[i] = Instantiate(slotPrefab, root);
    }

    private void OnEnable()
    {
        if (slots == null)
            return;

        batteryInventory.OnBatteryCountChanged += HandleBatteryCountChanged;
        UpdateSlots(batteryInventory.CurrentBatteries, false);
    }

    private void OnDisable()
    {
        if (slots == null)
            return;

        batteryInventory.OnBatteryCountChanged -= HandleBatteryCountChanged;
    }

    private void HandleBatteryCountChanged(int owned)
    {
        UpdateSlots(owned, true);
    }

    private void UpdateSlots(int owned, bool animate)
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetCharged(i < owned, animate);
    }
}
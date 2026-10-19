using System;
using UnityEngine;

public class MedicineInventory : MonoBehaviour
{
    [SerializeField] private int maxMedicine = 3;
    [SerializeField] private int currentMedicine = 0;

    public int MaxMedicine => maxMedicine;
    public int CurrentMedicine => currentMedicine;

    public event Action<int> OnMedicineCountChanged;

    public void AddMedicine()
    {
        currentMedicine = Mathf.Min(currentMedicine + 1, maxMedicine);
        OnMedicineCountChanged?.Invoke(currentMedicine);
    }

    public bool ConsumeMedicine()
    {
        if (currentMedicine <= 0)
            return false;

        currentMedicine--;
        OnMedicineCountChanged?.Invoke(currentMedicine);
        return true;
    }
}
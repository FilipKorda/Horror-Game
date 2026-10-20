using System;
using UnityEngine;

public class BatteryInventory : MonoBehaviour
{
    [SerializeField] private int maxBatteries = 4;
    [SerializeField] private int currentBatteries = 0;

    public int MaxBatteries => maxBatteries;
    public int CurrentBatteries => currentBatteries;

    public event Action<int> OnBatteryCountChanged;

    public void AddBattery()
    {
        currentBatteries = Mathf.Min(currentBatteries + 1, maxBatteries);
        OnBatteryCountChanged?.Invoke(currentBatteries);
    }

    public bool ConsumeBattery()
    {
        if (currentBatteries <= 0)
            return false;

        currentBatteries--;
        OnBatteryCountChanged?.Invoke(currentBatteries);
        return true;
    }
}
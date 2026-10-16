using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    public event Action OnInventoryChanged;

    private readonly Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        items.TryGetValue(item, out int current);
        items[item] = current + amount;

        OnInventoryChanged?.Invoke();
    }

    public int GetCount(ItemData item)
    {
        return item != null && items.TryGetValue(item, out int count) ? count : 0;
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetCount(item) >= amount;
    }

    public bool TryRemoveItem(ItemData item, int amount = 1)
    {
        if (!HasItem(item, amount))
            return false;

        items[item] -= amount;

        return true;
    }
}
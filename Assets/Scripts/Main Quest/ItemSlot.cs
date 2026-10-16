using System;
using UnityEngine;

public class ItemSlot : MonoBehaviour, IInteractable
{
    [Header("Item")]
    public ItemData requiredItem;

    [Header("Wizualizacja po włożeniu")]
    [SerializeField] private GameObject placedItemVisual;

    public event Action<ItemSlot> OnItemPlaced;

    public bool IsFilled { get; private set; }

    public InteractionType Type => InteractionType.Use;

    private void Awake()
    {
        if (placedItemVisual != null)
            placedItemVisual.SetActive(false);
    }

    public bool Interact(GameObject interactor)
    {
        if (IsFilled || requiredItem == null)
            return false;

        ItemInventory inventory = interactor.GetComponentInChildren<ItemInventory>();
        if (inventory == null)
            return false;

        if (!inventory.TryRemoveItem(requiredItem, 1))
            return false;

        IsFilled = true;

        if (placedItemVisual != null)
            placedItemVisual.SetActive(true);

        OnItemPlaced?.Invoke(this);
        return true;
    }

    public bool CanPlace(ItemInventory inventory)
    {
        return !IsFilled && requiredItem != null && inventory != null && inventory.HasItem(requiredItem);
    }
}
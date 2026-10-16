using UnityEngine;


[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    [Header("Pickup")]
    [SerializeField] private bool destroyOnPickup = true;

    public InteractionType Type => InteractionType.Pickup;

    public bool Interact(GameObject interactor)
    {
        if (itemData == null)
            return false;

        ItemInventory inventory = interactor.GetComponentInChildren<ItemInventory>();
        if (inventory == null)
            return false;

        inventory.AddItem(itemData, amount);

        if (destroyOnPickup)
            Destroy(gameObject);

        return true;
    }
}
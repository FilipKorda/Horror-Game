using UnityEngine;


[RequireComponent(typeof(Collider))]
public class MedicinePickup : MonoBehaviour, IInteractable
{
    [SerializeField] private bool destroyOnPickup = true;

    public InteractionType Type => InteractionType.Pickup;

    public bool Interact(GameObject interactor)
    {
        MedicineInventory inventory = interactor.GetComponentInChildren<MedicineInventory>();
        if (inventory == null)
            return false;

        if (inventory.CurrentMedicine >= inventory.MaxMedicine)
            return false; 

        inventory.AddMedicine();

        if (destroyOnPickup)
            Destroy(gameObject);

        return true;
    }
}
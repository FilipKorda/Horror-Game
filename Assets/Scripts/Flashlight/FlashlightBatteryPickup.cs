using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FlashlightBatteryPickup : MonoBehaviour, IInteractable
{
    [Header("Pickup")]
    [SerializeField] private bool destroyOnPickup = true;

    public string InteractionPrompt => "Podnieś baterię";
    public InteractionType Type => InteractionType.Pickup;

    public bool Interact(GameObject interactor)
    {
        BatteryInventory inventory = interactor.GetComponentInChildren<BatteryInventory>();
        if (inventory == null)
            return false;

        if (inventory.CurrentBatteries >= inventory.MaxBatteries)
            return false; 

        inventory.AddBattery();

        if (destroyOnPickup)
            Destroy(gameObject);

        return true;
    }
}
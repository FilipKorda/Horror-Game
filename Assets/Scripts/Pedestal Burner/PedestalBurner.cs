using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PedestalBurner : MonoBehaviour, IInteractable
{
    [Header("Inventory")]
    [SerializeField] private ItemData lighter;
    [SerializeField] private ItemData lighterFuel;

    [Header("Fire")]
    [SerializeField] private FireEffect fire;
    [SerializeField] private FireSpread houseFire;

    [SerializeField] private QuestManager questManager;

    private Collider pedestalCollider;
    private bool burned;


    public InteractionType Type => InteractionType.Fire;

    private void Awake()
    {
        pedestalCollider = GetComponent<Collider>();

        pedestalCollider.enabled = false;

        if (fire != null)
            fire.StopInstant();
    }

    public void Unlock()
    {
        if (burned)
            return;

        pedestalCollider.enabled = true;
    }

    public bool Interact(GameObject interactor)
    {
        if (burned)
            return false;

        ItemInventory inventory = interactor.GetComponentInChildren<ItemInventory>();

        if (inventory == null)
            return false;

        if (!inventory.HasItem(lighter))
            return false;

        if (inventory.GetCount(lighterFuel) < 3)
            return false;

        inventory.TryRemoveItem(lighter, 1);
        inventory.TryRemoveItem(lighterFuel, 3);

        burned = true;
        pedestalCollider.enabled = false;

        if (fire != null)
            fire.Ignite();

        if (houseFire != null)
            houseFire.StartSpreading(transform.position);

        if (questManager != null)
            questManager.BurnPedestal();

        return true;
    }
}
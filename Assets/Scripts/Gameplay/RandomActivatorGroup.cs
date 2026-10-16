using UnityEngine;

public class RandomActivatorGroup : MonoBehaviour
{
    [SerializeField] private MoveObjectActivator[] activators;
    [SerializeField] private GameObject[] entrances;
    [SerializeField, Range(0f, 1f)] private float activationChance = 0.5f;
    private bool hasTriggered;


    private void Start()
    {
        foreach (var entrance in entrances)
        {
            entrance.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    public void Trigger()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;

        if (Random.value < activationChance)
        {
            activators[Random.Range(0, activators.Length)].ActiveTriggerInBox();
            Debug.Log($"[RandomActivatorGroup] Activating random activator in group: {name}", this);
        }
           

        foreach (var entrance in entrances)
        {
            Destroy(entrance);
        }
    }
}
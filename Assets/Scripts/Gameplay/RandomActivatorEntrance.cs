using UnityEngine;

public class RandomActivatorEntrance : MonoBehaviour
{
    [SerializeField] private RandomActivatorGroup group;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            group.Trigger();
            //Debug.Log($"[RandomActivatorEntrance] Player entered trigger: {name}, activating group: {group.name}", this);
        }

    }
}
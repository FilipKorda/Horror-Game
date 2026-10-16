using UnityEngine;

public class FloorZone : MonoBehaviour
{
    [SerializeField] private int floorIndex;
    [SerializeField] private EnemySpawner enemySpawner;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        enemySpawner.SetPlayerFloor(floorIndex);
    }
}
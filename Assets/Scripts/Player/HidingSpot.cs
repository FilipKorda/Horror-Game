using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [SerializeField] private Transform enemyInteractionPoint;

    public Transform EnemyInteractionPoint => enemyInteractionPoint;
}
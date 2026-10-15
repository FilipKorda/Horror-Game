using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyAI enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnAreaCenter;

    [Header("Spawn Area")]
    [SerializeField] private float spawnAreaRadius = 40f;
    [SerializeField] private float minDistanceFromPlayer = 20f;
    [SerializeField] private float maxDistanceFromPlayer = 35f;
    [SerializeField] private int maxSpawnAttempts = 10;

    [Header("Presence")]
    [SerializeField] private float minSpawnDelay = 3f;
    [SerializeField] private float maxSpawnDelay = 10f;

    [Header("Floor Areas")]
    [SerializeField] private string groundFloorArea = "GroundFloor";
    [SerializeField] private string firstFloorArea = "FirstFloor";

    private int groundFloorMask;
    private int firstFloorMask;
    private int currentPlayerFloor;

    private EnemyAI activeEnemy;

    private void Awake()
    {
        int groundFloorAreaIndex = NavMesh.GetAreaFromName(groundFloorArea);
        int firstFloorAreaIndex = NavMesh.GetAreaFromName(firstFloorArea);

        if (groundFloorAreaIndex == -1)
            Debug.LogError($"Nie znaleziono NavMesh Area: {groundFloorArea}", this);
        else
            groundFloorMask = 1 << groundFloorAreaIndex;

        if (firstFloorAreaIndex == -1)
            Debug.LogError($"Nie znaleziono NavMesh Area: {firstFloorArea}", this);
        else
            firstFloorMask = 1 << firstFloorAreaIndex;
    }

    public void ActiveEnemySpawner(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    private void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    public void SetPlayerFloor(int floor)
    {
        currentPlayerFloor = floor;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (activeEnemy == null)
            {
                yield return new WaitForSeconds(
                    Random.Range(minSpawnDelay, maxSpawnDelay)
                );

                TrySpawnEnemy();
            }
            else
            {
                yield return null;
            }
        }
    }

    private void TrySpawnEnemy()
    {
        if (enemyPrefab == null)
            return;

        if (!TryGetSpawnPoint(out Vector3 point, out int areaMask))
            return;

        EnemyAI enemy = Instantiate(enemyPrefab, point, Quaternion.identity);
        enemy.SetNavMeshAreaMask(areaMask);

        enemy.OnDespawnRequested += HandleEnemyDespawnRequested;
        activeEnemy = enemy;
    }

    private void HandleEnemyDespawnRequested(EnemyAI enemy)
    {
        enemy.OnDespawnRequested -= HandleEnemyDespawnRequested;

        activeEnemy = null;

        Destroy(enemy.gameObject);
    }

    private bool TryGetSpawnPoint(out Vector3 point, out int areaMask)
    {
        point = Vector3.zero;
        areaMask = GetCurrentFloorAreaMask();

        if (areaMask == 0 || player == null)
            return false;

        Vector3 center = spawnAreaCenter != null
            ? spawnAreaCenter.position
            : transform.position;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Punkt losowany w pierścieniu wokół gracza, a nie w całej strefie spawnu - dzięki temu
            // wróg zawsze pojawia się w zakresie min-max, zamiast odrzucać większość losowań.
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

            Vector3 candidate = player.position + new Vector3(direction.x, 0f, direction.y) * distance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, areaMask))
                continue;

            // SamplePosition może przesunąć punkt, więc zakres sprawdzamy jeszcze raz na wyniku.
            float distanceToPlayer = Vector3.Distance(hit.position, player.position);
            if (distanceToPlayer < minDistanceFromPlayer || distanceToPlayer > maxDistanceFromPlayer)
                continue;

            if (Vector3.Distance(hit.position, center) > spawnAreaRadius)
                continue;

            point = hit.position;
            return true;
        }

        return false;
    }

    private int GetCurrentFloorAreaMask()
    {
        switch (currentPlayerFloor)
        {
            case 0:
                return groundFloorMask;

            case 1:
                return firstFloorMask;

            default:
                return groundFloorMask;
        }
    }

    private void OnValidate()
    {
        maxDistanceFromPlayer = Mathf.Max(maxDistanceFromPlayer, minDistanceFromPlayer);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = spawnAreaCenter != null
            ? spawnAreaCenter.position
            : transform.position;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, spawnAreaRadius);

        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                player.position,
                minDistanceFromPlayer
            );

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(
                player.position,
                maxDistanceFromPlayer
            );
        }
    }
}
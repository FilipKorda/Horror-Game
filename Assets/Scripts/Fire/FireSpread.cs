using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSpread : MonoBehaviour
{
    [Header("Prefaby")]
    [SerializeField] private FireEffect smallFirePrefab;
    [SerializeField] private FireEffect mediumFirePrefab;
    [SerializeField] private FireEffect largeFirePrefab;

    [Header("Rozprzestrzenianie")]
    [SerializeField] private float startDelay = 4f;
    [SerializeField] private float spreadSpeed = 0.35f;
    [SerializeField] private float randomDelay = 3f;
    [SerializeField] private float verticalDistanceWeight = 0.5f;

    [Header("Wydajność")]
    [SerializeField] private int maxActiveLights = 4;

    private readonly List<KeyValuePair<float, FireSpreadPoint>> schedule = new List<KeyValuePair<float, FireSpreadPoint>>();
    private Coroutine spreadRoutine;

    public bool IsSpreading => spreadRoutine != null;

    private void Awake()
    {
        FireEffect.MaxActiveLights = maxActiveLights;
    }

    public void StartSpreading(Vector3 origin)
    {
        if (spreadRoutine != null)
            return;

        FireEffect.MaxActiveLights = maxActiveLights;

        schedule.Clear();
        foreach (FireSpreadPoint point in GetComponentsInChildren<FireSpreadPoint>())
        {
            Vector3 offset = point.transform.position - origin;
            offset.y *= verticalDistanceWeight;

            float delay = point.DelayOverride >= 0f
                ? point.DelayOverride
                : startDelay + offset.magnitude / Mathf.Max(spreadSpeed, 0.01f) + Random.Range(0f, randomDelay);

            schedule.Add(new KeyValuePair<float, FireSpreadPoint>(delay, point));
        }

        schedule.Sort((a, b) => a.Key.CompareTo(b.Key));
        spreadRoutine = StartCoroutine(SpreadRoutine());
    }

    private IEnumerator SpreadRoutine()
    {
        float elapsed = 0f;

        foreach (KeyValuePair<float, FireSpreadPoint> entry in schedule)
        {
            while (elapsed < entry.Key)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            IgnitePoint(entry.Value);
        }
    }

    private void IgnitePoint(FireSpreadPoint point)
    {
        if (point == null || point.Fire != null)
            return;

        FireEffect prefab = GetPrefab(point.Size);
        if (prefab == null)
            return;

        FireEffect fire = Instantiate(prefab, point.transform.position, point.transform.rotation, point.transform);
        point.Fire = fire;
        fire.Ignite(point.Scale);
    }

    private FireEffect GetPrefab(FireSpreadPoint.FireSize size)
    {
        switch (size)
        {
            case FireSpreadPoint.FireSize.Small:
                return smallFirePrefab;
            case FireSpreadPoint.FireSize.Large:
                return largeFirePrefab;
            default:
                return mediumFirePrefab;
        }
    }
}

using UnityEngine;

public class MenuCameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform center;
    [SerializeField, Min(0.1f)] private float radius = 10f;
    [SerializeField] private float height = 2f;
    [Tooltip("Prędkość liniowa kamery po okręgu [m/s].")]
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private bool clockwise = true;
    [SerializeField, Range(0f, 360f)] private float startAngle;

    private float angle;

    private float Direction => clockwise ? 1f : -1f;

    private void Start()
    {
        if (center == null)
        {
            Debug.LogError($"[{nameof(MenuCameraOrbit)}] Brak przypisanego Center.", this);
            enabled = false;
            return;
        }

        angle = startAngle * Mathf.Deg2Rad;
        Apply();
    }

    // unscaledDeltaTime, bo po powrocie z pauzy timeScale może zostać 0 i kamera by stanęła
    private void Update()
    {
        float step = Direction * speed / radius * Time.unscaledDeltaTime;
        angle = Mathf.Repeat(angle + step, Mathf.PI * 2f);
        Apply();
    }

    private void Apply()
    {
        float sin = Mathf.Sin(angle);
        float cos = Mathf.Cos(angle);

        transform.position = center.position + new Vector3(sin * radius, height, cos * radius);

        transform.rotation = Quaternion.LookRotation(new Vector3(cos, 0f, -sin) * Direction);
    }

    private void OnDrawGizmosSelected()
    {
        if (center == null)
            return;

        const int segments = 64;
        Vector3 origin = center.position + Vector3.up * height;
        Vector3 previous = origin + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 next = origin + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
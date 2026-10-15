using UnityEngine;

public class FireSpreadPoint : MonoBehaviour
{
    public enum FireSize
    {
        Small,
        Medium,
        Large
    }

    [SerializeField] private FireSize size = FireSize.Medium;
    [SerializeField] private float delayOverride = -1f;
    [SerializeField] private float scale = 1f;

    public FireSize Size => size;
    public float DelayOverride => delayOverride;
    public float Scale => scale;
    public FireEffect Fire { get; set; }

    private void OnDrawGizmos()
    {
        float radius;
        Color color;

        switch (size)
        {
            case FireSize.Small:
                radius = 0.25f;
                color = new Color(1f, 0.85f, 0.2f, 0.9f);
                break;
            case FireSize.Large:
                radius = 0.7f;
                color = new Color(1f, 0.15f, 0.05f, 0.9f);
                break;
            default:
                radius = 0.45f;
                color = new Color(1f, 0.5f, 0.1f, 0.9f);
                break;
        }

        radius *= scale;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * radius, radius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * radius * 3f);
    }
}

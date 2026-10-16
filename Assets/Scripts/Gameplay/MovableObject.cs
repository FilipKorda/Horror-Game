using UnityEngine;

public class MovableObject : MonoBehaviour
{
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private float minImpactSpeed = 0.5f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

    public bool HasHit { get; private set; }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.collider.gameObject;

        if ((hitLayers.value & (1 << other.layer)) == 0)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minImpactSpeed)
            return;

        HasHit = true;

        Vector3 point = collision.GetContact(0).point;
     /*   Debug.Log($"[MovableObject] {name} w coś trafił: {other.name} [{LayerMask.LayerToName(other.layer)}] " +
                  $"({impactSpeed:F1} m/s, pozycja {point.ToString("F2")})", this);*/

        AudioManager.Instance.PlaySFX3D(audioSource, hitSound, soundVolume);
    }
}
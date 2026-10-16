using System.Collections;
using UnityEngine;

public class PlayerDeathFall : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform handPoint;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Atak serca")]
    [SerializeField] private float seizureDuration = 1.1f;
    [SerializeField] private float seizurePitch = 18f;
    [SerializeField] private float seizureTremor = 0.012f;
    [SerializeField] private float stumbleBack = 0.16f;
    [SerializeField] private float hunch = 0.12f;

    [Header("Kolana")]
    [SerializeField] private float buckleDuration = 0.42f;
    [SerializeField] private float kneelEyeHeight = 0.95f;
    [SerializeField] private float kneelPitch = 30f;
    [SerializeField] private float kneeDip = 0.05f;
    [SerializeField] private float wobbleDuration = 0.8f;
    [SerializeField] private float wobbleLean = 7f;

    [Header("Upadek")]
    [SerializeField] private float headRadius = 0.15f;
    [SerializeField] private float headRestHeight = 0.2f;
    [SerializeField, Range(0f, 1f)] private float forwardBias = 0.3f;
    [SerializeField] private float toppleGravity = 16f;
    [SerializeField] private float impactBounce = 0.22f;
    [SerializeField] private float settleDuration = 0.9f;
    [SerializeField] private float restPitch = 6f;

    [Header("Ręka")]
    [SerializeField] private Vector3 handDropOffset = new Vector3(0.05f, -0.4f, -0.15f);
    [SerializeField] private Vector3 handDropRotation = new Vector3(45f, 0f, 20f);

    [Header("Wstrząsy")]
    [SerializeField] private float shakePosition = 0.035f;
    [SerializeField] private float shakeRotation = 3.5f;
    [SerializeField] private float shakeDecay = 6f;

    [Header("Dźwięk")]
    [SerializeField] private AudioClip kneeImpactClip;
    [SerializeField] private AudioClip bodyImpactClip;

    private float shake;
    private float seed;
    private float elapsedTotal;

    private Vector3 forward;
    private Vector3 right;
    private float startPitch;
    private Vector3 handBasePosition;
    private Quaternion handBaseRotation;

    public IEnumerator Play()
    {
        if (cameraTransform == null)
            yield break;

        seed = Random.Range(0f, 100f);
        shake = 0f;
        elapsedTotal = 0f;

        forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        forward.Normalize();
        right = Vector3.Cross(Vector3.up, forward);

        startPitch = Vector3.SignedAngle(forward, cameraTransform.forward, right);

        if (handPoint != null)
        {
            handBasePosition = handPoint.localPosition;
            handBaseRotation = handPoint.localRotation;
        }

        Vector3 standEye = cameraTransform.position;
        float floorY = FindFloor(standEye);
        Vector3 fallDirection = ChooseFallDirection(standEye, floorY, out float freeDistance, out float sideSign);

        shake = 0.6f;
        Vector3 stumbleEye = standEye;
        float hunchedPitch = seizurePitch;

        for (float t = 0f; t < 1f;)
        {
            t = Step(t, seizureDuration);
            float s = Mathf.SmoothStep(0f, 1f, t);

            Vector3 sway = right * Mathf.Sin(t * Mathf.PI * 1.6f) * 0.05f * sideSign;
            Vector3 tremor = Noise3(18f) * seizureTremor * (1f - t * 0.5f);
            stumbleEye = standEye - forward * stumbleBack * s - Vector3.up * hunch * s;

            float jolt = Mathf.Clamp01(t * 4f);
            float pitch = Mathf.Lerp(startPitch, seizurePitch, 1f - (1f - jolt) * (1f - jolt)) + Mathf.Sin(t * 22f) * 3f * (1f - t);
            float roll = Mathf.Sin(t * Mathf.PI * 1.3f) * 5f * sideSign;

            Apply(stumbleEye + sway + tremor, pitch, roll, Quaternion.identity);
            yield return null;
        }

        Vector3 pivot = new Vector3(stumbleEye.x, floorY, stumbleEye.z) + forward * 0.15f;
        Vector3 kneelEye = pivot + Vector3.up * kneelEyeHeight;

        for (float t = 0f; t < 1f;)
        {
            t = Step(t, buckleDuration);
            float fall = t * t;

            Vector3 position = Vector3.Lerp(stumbleEye, kneelEye, fall);
            float pitch = Mathf.Lerp(hunchedPitch, kneelPitch, fall);
            float roll = Mathf.Lerp(5f * sideSign * Mathf.Sin(Mathf.PI * 1.3f), -2f * sideSign, t);

            DropHand(Mathf.SmoothStep(0f, 1f, t));
            Apply(position, pitch, roll, Quaternion.identity);
            yield return null;
        }

        Play(kneeImpactClip);
        shake = 1f;

        Vector3 axis = Vector3.Cross(Vector3.up, fallDirection).normalized;
        float leanTarget = wobbleLean;

        for (float t = 0f; t < 1f;)
        {
            t = Step(t, wobbleDuration);

            float dip = -kneeDip * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.22f));
            float lean = leanTarget * Mathf.SmoothStep(0f, 1f, t) + Mathf.Sin(t * Mathf.PI * 3f) * 2.5f * (1f - t);
            Quaternion tilt = Quaternion.AngleAxis(lean, axis);
            Vector3 head = pivot + tilt * (Vector3.up * kneelEyeHeight) + Vector3.up * dip;
            float pitch = kneelPitch + 8f * t;

            Apply(head, pitch, -2f * sideSign, tilt);
            yield return null;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopBreathing();

        float floorLimit = Mathf.Acos(Mathf.Clamp01(headRestHeight / kneelEyeHeight));
        float wallLimit = Mathf.Asin(Mathf.Clamp01((freeDistance - headRadius) / kneelEyeHeight));
        float maxAngle = Mathf.Max(Mathf.Min(floorLimit, wallLimit), leanTarget * Mathf.Deg2Rad + 0.1f);

        float angle = leanTarget * Mathf.Deg2Rad;
        float velocity = 0.35f;
        bool impacted = false;
        float sinceImpact = 0f;
        float droopStart = kneelPitch + 8f;

        while (!impacted || sinceImpact < settleDuration)
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            elapsedTotal += dt;

            velocity += toppleGravity * Mathf.Sin(angle) * dt;
            velocity *= 1f - 0.4f * dt;
            angle += velocity * dt;

            if (angle >= maxAngle)
            {
                angle = maxAngle;

                if (!impacted)
                {
                    impacted = true;
                    Play(bodyImpactClip);
                    shake = 1.4f;
                }

                velocity = -velocity * impactBounce;
            }

            if (impacted)
                sinceImpact += dt;

            float progress = Mathf.Clamp01(angle / maxAngle);
            Quaternion tilt = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);
            Vector3 head = pivot + tilt * (Vector3.up * kneelEyeHeight);
            float pitch = Mathf.Lerp(droopStart, restPitch, progress * progress);

            Apply(head, pitch, -2f * sideSign * (1f - progress), tilt);
            yield return null;
        }
    }

    private float Step(float t, float duration)
    {
        float dt = Time.deltaTime;
        elapsedTotal += dt;
        return Mathf.Min(1f, t + dt / Mathf.Max(duration, 0.0001f));
    }

    private void Apply(Vector3 position, float pitch, float roll, Quaternion tilt)
    {
        shake *= Mathf.Exp(-shakeDecay * Time.deltaTime);

        Quaternion look = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(pitch, 0f, roll);
        Vector3 shakeOffset = Noise3(30f) * shakePosition * shake;
        Vector3 shakeAngles = Noise3(26f) * shakeRotation * shake;

        Vector3 finalPosition = position + shakeOffset;
        finalPosition.y = Mathf.Max(finalPosition.y, FloorAt(finalPosition) + headRadius * 0.8f);

        cameraTransform.SetPositionAndRotation(finalPosition, tilt * look * Quaternion.Euler(shakeAngles));
    }

    private void DropHand(float weight)
    {
        if (handPoint == null)
            return;

        handPoint.localPosition = handBasePosition + handDropOffset * weight;
        handPoint.localRotation = handBaseRotation * Quaternion.Euler(handDropRotation * weight);
    }

    private Vector3 ChooseFallDirection(Vector3 eye, float floorY, out float freeDistance, out float sideSign)
    {
        Vector3 origin = new Vector3(eye.x, floorY + kneelEyeHeight * 0.6f, eye.z);
        float preferred = Random.value < 0.5f ? 1f : -1f;

        Vector3[] candidates =
        {
            (right * preferred + forward * forwardBias).normalized,
            (-right * preferred + forward * forwardBias).normalized,
            forward,
            -forward
        };

        Vector3 best = candidates[0];
        float bestFree = -1f;

        for (int i = 0; i < candidates.Length; i++)
        {
            float free = FreeDistance(origin, candidates[i]);
            float score = free - i * 0.05f;

            if (free >= kneelEyeHeight)
            {
                best = candidates[i];
                bestFree = free;
                break;
            }

            if (score > bestFree)
            {
                best = candidates[i];
                bestFree = free;
            }
        }

        freeDistance = bestFree;
        sideSign = Vector3.Dot(best, right) >= 0f ? 1f : -1f;
        return best;
    }

    private float FreeDistance(Vector3 origin, Vector3 direction)
    {
        float maxDistance = kneelEyeHeight + headRadius;

        float nearest = maxDistance + 1f;

        foreach (RaycastHit hit in Physics.SphereCastAll(origin, headRadius, direction, maxDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (IsSelf(hit.collider))
                continue;

            nearest = Mathf.Min(nearest, hit.distance > 0f ? hit.distance + headRadius : headRadius);
        }

        return nearest;
    }

    private float FindFloor(Vector3 eye)
    {
        if (TryRaycastDown(transform.position, 3f, out float floorY))
            return floorY;

        return eye.y - 1.5f;
    }

    private float FloorAt(Vector3 position)
    {
        if (TryRaycastDown(position + Vector3.up * 0.5f, 1.5f, out float floorY))
            return floorY;

        return float.MinValue;
    }

    private bool TryRaycastDown(Vector3 origin, float distance, out float floorY)
    {
        floorY = float.MinValue;
        float nearest = float.MaxValue;

        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (IsSelf(hit.collider) || hit.distance >= nearest)
                continue;

            nearest = hit.distance;
            floorY = hit.point.y;
        }

        return nearest < float.MaxValue;
    }

    private bool IsSelf(Collider other)
    {
        return other.transform == transform || other.transform.IsChildOf(transform);
    }

    private Vector3 Noise3(float frequency)
    {
        float time = elapsedTotal * frequency;
        return new Vector3(
            Mathf.PerlinNoise(seed, time) - 0.5f,
            Mathf.PerlinNoise(seed + 17.3f, time) - 0.5f,
            Mathf.PerlinNoise(seed + 41.9f, time) - 0.5f) * 2f;
    }

    private static void Play(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip);
    }
}

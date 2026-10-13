using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyFootsteps : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Animator animator;

    [Header("Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Volume & Pitch")]
    [SerializeField] private float walkVolume = 0.6f;
    [SerializeField] private float runVolume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Step Protection")]
    [SerializeField] private float minimumStepInterval = 0.18f;
    [SerializeField, Range(0.5f, 1f)] private float minimumClipWeight = 0.75f;

    [Header("3D Audio")]
    [SerializeField] private float minAudibleDistance = 2f;
    [SerializeField] private float maxAudibleDistance = 15f;
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    private float lastStepTime = -Mathf.Infinity;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (animator == null)
            animator = GetComponentInParent<Animator>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.rolloffMode = rolloffMode;
        audioSource.minDistance = minAudibleDistance;
        audioSource.maxDistance = maxAudibleDistance;
    }

    public void PlayFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent == null || animator == null || AudioManager.Instance == null)
            return;

        if (footstepClips == null || footstepClips.Length == 0)
            return;

        if (Time.time - lastStepTime < minimumStepInterval)
            return;

        AnimatorClipInfo clipInfo = animationEvent.animatorClipInfo;

        if (clipInfo.clip == null || clipInfo.weight < minimumClipWeight)
            return;

        float speed = animator.GetFloat(SpeedHash);

        if (speed < 0.5f)
            return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        AudioManager.Instance.PlaySFX3D(audioSource, clip, speed >= 1.5f ? runVolume : walkVolume);

        lastStepTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minAudibleDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxAudibleDistance);
    }
}
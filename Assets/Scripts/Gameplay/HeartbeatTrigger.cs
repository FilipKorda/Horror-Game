using UnityEngine;

public class HeartbeatTrigger : MonoBehaviour
{
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private string playerTag = "Player";

    private bool startedHeartbeat;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (AudioManager.Instance == null)
            return;

        if (AudioManager.Instance.IsHeartbeatPlaying())
            return;

        AudioManager.Instance.StartHeartbeat(heartbeatClip);
        startedHeartbeat = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (!startedHeartbeat)
            return;

        startedHeartbeat = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopHeartbeat();
    }
}
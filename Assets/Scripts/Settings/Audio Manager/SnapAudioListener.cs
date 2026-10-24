using UnityEngine;

public class SnapAudioListener : MonoBehaviour
{
    public Transform player;
    public void SnapThisAudioListenerToPlayer()
    {
        Debug.Log("Snapping Audio Listener to Player After Spawn");
        transform.SetParent(player.transform);
    }
}

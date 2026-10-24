using UnityEngine;


public class SceneMusicTrigger : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float fadeDuration = 1.5f;

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(musicClip, fadeDuration);
    }
}
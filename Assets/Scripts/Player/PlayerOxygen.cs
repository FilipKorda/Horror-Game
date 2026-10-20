using UnityEngine;

public class PlayerOxygen : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField] private FireSpread fireSpread;
    [SerializeField] private PlayerFearMeter fearMeter;

    [Header("Tlen")]
    [SerializeField, Min(1)] private int burningPointsToStart = 3;
    [SerializeField, Min(0.1f)] private float duration = 60f;

    private float oxygen = 1f;
    private bool stopped;

    public bool IsActive { get; private set; }
    public bool IsStopped => stopped || (fearMeter != null && fearMeter.IsDead);
    public float Oxygen01 => oxygen;
    public float SecondsLeft => oxygen * duration;

    public event System.Action OnActivated;

    private void Start()
    {
        if (fearMeter == null)
            fearMeter = GetComponent<PlayerFearMeter>();

        if (fireSpread == null)
            fireSpread = FindFirstObjectByType<FireSpread>();
    }

    private void Update()
    {
        if (stopped || fearMeter == null || fearMeter.IsDead)
            return;

        if (!IsActive)
        {
            if (fireSpread == null || fireSpread.IgnitedCount < burningPointsToStart)
                return;

            IsActive = true;
            OnActivated?.Invoke();
        }

        oxygen = Mathf.Max(0f, oxygen - Time.deltaTime / duration);

        if (oxygen <= 0f)
        {
            stopped = true;
            fearMeter.DieFromSuffocation();
        }
    }

    public void Stop()
    {
        stopped = true;
    }
}

using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string TutorialKey = "TutorialEnabled";

    public bool IsTutorialEnabled { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsTutorialEnabled = PlayerPrefs.GetInt(TutorialKey, 1) == 1;
    }

    public void SetTutorialEnabled(bool enabled)
    {
        IsTutorialEnabled = enabled;

        PlayerPrefs.SetInt(TutorialKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
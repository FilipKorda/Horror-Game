using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private string loadingSceneName = "Loading";

    public string TargetSceneName { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string targetSceneName)
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1f;
        }
        TargetSceneName = targetSceneName;
        SceneManager.LoadScene(loadingSceneName);
    }
}
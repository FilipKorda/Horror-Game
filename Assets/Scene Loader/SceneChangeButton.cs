using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneChangeButton : MonoBehaviour
{
    [SerializeField] private string targetSceneName;

    public void HandleClick()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(targetSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
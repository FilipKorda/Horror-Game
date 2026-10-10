
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderWindow : EditorWindow
{
    private int selectedSceneIndex;

    [MenuItem("Tools/Scene Loader")]
    private static void OpenWindow()
    {
        GetWindow<SceneLoaderWindow>("Scene Loader");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField("Scene Loader", EditorStyles.boldLabel);

        GUILayout.Space(5);

        string[] scenes = GetBuildScenes();

        if (scenes.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "Brak scen w Build Settings.",
                MessageType.Warning
            );

            return;
        }

        selectedSceneIndex = EditorGUILayout.Popup(
            "Scene",
            selectedSceneIndex,
            scenes
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Load Scene", GUILayout.Height(35)))
        {
            LoadSelectedScene();
        }
    }

    private string[] GetBuildScenes()
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

        string[] sceneNames = new string[buildScenes.Length];

        for (int i = 0; i < buildScenes.Length; i++)
        {
            string path = buildScenes[i].path;
            sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        return sceneNames;
    }

    private void LoadSelectedScene()
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

        if (selectedSceneIndex < 0 || selectedSceneIndex >= buildScenes.Length)
            return;

        string scenePath = buildScenes[selectedSceneIndex].path;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}


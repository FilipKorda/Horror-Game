using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StaticSwitcher))]
public class StaticSwitcherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StaticSwitcher switcher = (StaticSwitcher)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Disable Static"))
        {
            Undo.RecordObjects(switcher.targets.ToArray(), "Disable Static");

            switcher.DisableStatic();

            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Restore Static"))
        {
            Undo.RecordObjects(switcher.targets.ToArray(), "Restore Static");

            switcher.RestoreStatic();

            SceneView.RepaintAll();
        }
    }
}
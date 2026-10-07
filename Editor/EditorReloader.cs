#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class EditorReloader : EditorWindow
{
    [MenuItem("Tools/Restart Unity Editor")]
    static void RestartEditor()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        EditorApplication.OpenProject(projectPath);
    }
}
#endif
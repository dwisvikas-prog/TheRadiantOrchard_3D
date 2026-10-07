#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Simple Play helper — always resets onboarding so progression works from step 1.
public class EmptyIslandWindow : EditorWindow
{
    const string ScenePath = "Assets/CoC_BlankGround.unity";

    [MenuItem("Radiant Orchard/Empty Island", priority = 1)]
    [MenuItem("Tools/Empty Island", priority = 1)]
    public static void Open()
    {
        var win = GetWindow<EmptyIslandWindow>("Empty Island");
        win.minSize = new Vector2(280, 180);
        win.Show();
    }

    void OnGUI()
    {
        GUILayout.Space(8);
        GUILayout.Label("Empty Island", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Rebuild Scene", GUILayout.Height(32)))
                CreateCoCBlankScene.Create();
        }

        GUILayout.Space(6);
        if (GUILayout.Button("Reset Onboarding Progress", GUILayout.Height(28)))
        {
            RadiantOrchard.EmptyIslandProgress.ClearAll();
            EditorUtility.DisplayDialog("Empty Island",
                "All prefs cleared. Next Play = step 1.", "OK");
        }

        EditorGUILayout.HelpBox(
            "PLAY always starts from step 1 (editor).\n" +
            "1 Plant Wish Tree → 2 Wish → 3 Visitor → 4 Heal\n" +
            "F6 = skip current step",
            MessageType.Info);

        GUILayout.Space(6);
        if (EditorApplication.isPlaying)
        {
            if (GUILayout.Button("STOP", GUILayout.Height(36)))
                EditorApplication.isPlaying = false;
        }
        else if (GUILayout.Button("▶ PLAY (fresh start)", GUILayout.Height(40)))
        {
            RadiantOrchard.EmptyIslandProgress.ClearAll();
            if (EditorSceneManager.GetActiveScene().path != ScenePath && System.IO.File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            EditorApplication.isPlaying = true;
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

// Thin wrapper — real work is MainSceneSquareGround.Apply() → CoCVillageLayout.
public static class BuildCoCVillageZones
{
    const string MainScenePath = "Assets/The Main RadiantOrchard_3d.unity";

    [MenuItem("Tools/BUILD CoC VILLAGE ZONES", priority = 2)]
    [MenuItem("Radiant Orchard/BUILD CoC VILLAGE ZONES", priority = 2)]
    public static void RunFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        MainSceneSquareGround.Apply();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("CoC Village", "Clean spacious layout applied.\nOld trees/island removed.\n\nPress Play.", "OK");
    }
}
#endif

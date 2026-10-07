#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

// Super-visible menu entries so the square-ground fix is easy to find.
public static class MakeMainGroundSquareMenu
{
    const string MainScenePath = "Assets/The Main RadiantOrchard_3d.unity";

    [MenuItem("Tools/MAKE MAIN GROUND SQUARE", priority = 1)]
    [MenuItem("Tools/BUILD CoC BASE GROUND", priority = 0)]
    [MenuItem("Radiant Orchard/BUILD CoC BASE GROUND", priority = 0)]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        MainSceneSquareGround.Apply();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "CoC Base Ground",
            "Clash-style base ready:\n• Green grass pad 110x110\n• Brown cliff under it\n\nPress Play.",
            "OK");

        Debug.Log("[MakeMainGroundSquareMenu] CoC_BaseGround saved on Main scene.");
    }
}
#endif

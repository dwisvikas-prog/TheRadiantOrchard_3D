#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

public static class WireLevelManager
{
    [MenuItem("Tools/Radiant Orchard/Wire Level Manager - Phase 6")]
    private static void Wire()
    {
        var managers = GameObject.Find("GameManagers");
        if (managers == null)
        {
            Debug.LogError("WireLevelManager: GameManagers not found — run 'Wire Interaction Demo - Phase 4' first.");
            return;
        }

        var levelManager = managers.GetComponent<LevelManager>();
        if (levelManager == null)
            levelManager = Undo.AddComponent<LevelManager>(managers);

        var guids = AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/GameData/Levels" });
        var levels = new LevelDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            levels[i] = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));

        var so = new SerializedObject(levelManager);
        var prop = so.FindProperty("allLevels");
        prop.arraySize = levels.Length;
        for (int i = 0; i < levels.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Radiant Orchard: LevelManager wired with {levels.Length} level(s). Save the scene (Ctrl+S).");
    }
}
#endif

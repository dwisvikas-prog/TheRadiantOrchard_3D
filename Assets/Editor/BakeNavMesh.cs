#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Re-bakes the NavMesh for the open scene.
//
// Roaming NPCs (WanderingNPC) and the Stickman spawn positioning all read the
// baked NavMesh — NavMesh.SamplePosition, NavMesh.CalculateTriangulation and
// every NavMeshAgent path. When the island is rebuilt or decorated after the
// last bake, the baked surface no longer matches the geometry: agents sit
// off-mesh and simply stand still ("stickmen getting stuck"), and new spawn
// points can sample onto a surface that no longer exists. Re-running this after
// any terrain/layout change fixes both.
//
// The legacy editor bake API is reached by reflection on purpose: it lives in
// different assemblies/flavours across AI Navigation versions, and an editor
// script that fails to compile blocks every other tool in the project.
public static class BakeNavMesh
{
    [MenuItem("Tools/Radiant Orchard/Bake NavMesh")]
    private static void Bake()
    {
        bool built = TryCallStatic("UnityEditor.AI.NavMeshBuilder, UnityEditor", "BuildNavMesh");

        if (!built)
        {
            Debug.LogError(
                "Radiant Orchard: couldn't call the editor NavMesh bake API in this Unity version. " +
                "Bake manually instead: Window ▸ AI ▸ Navigation ▸ Bake — or add a NavMeshSurface " +
                "component (AI Navigation package) to the island root and press Bake there.");
            return;
        }

        // MarkSceneDirty is Editor-only and must not be called during Play mode
        if (!UnityEditor.EditorApplication.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Radiant Orchard: NavMesh re-baked from the current scene geometry. " +
                  "Save the scene (Ctrl+S) to keep it.");
    }

    private static bool TryCallStatic(string typeName, string methodName)
    {
        var type = System.Type.GetType(typeName);
        var method = type?.GetMethod(methodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (method == null) return false;

        method.Invoke(null, null);
        return true;
    }
}
#endif

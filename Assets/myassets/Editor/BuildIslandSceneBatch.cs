using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Command-line entry point so the island layout can be generated without
// clicking through the Editor UI: run via
// Unity.exe -batchmode -quit -projectPath <project> -executeMethod BuildIslandSceneBatch.Run
public static class BuildIslandSceneBatch
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/newmvp.unity");

        var go = GameObject.Find("IslandGenerator");
        if (go == null)
        {
            go = new GameObject("IslandGenerator");
        }

        var generator = go.GetComponent<IslandSceneGenerator>();
        if (generator == null)
        {
            generator = go.AddComponent<IslandSceneGenerator>();
        }

        generator.GenerateIslandScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("BuildIslandSceneBatch: island scene generated and saved.");
    }
}

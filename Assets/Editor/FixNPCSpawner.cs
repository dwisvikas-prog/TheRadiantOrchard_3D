#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

// Tools → Radiant Orchard → Fix NPC Spawner
// Wires NPCSpawner with:
//   - Stickman prefab
//   - 3 L1 FruitData (Strawberry, Pineapple, Watermelon)
//   - 5 spawn points placed around the island edge
// Run this ONCE then Ctrl+S → Play.
public static class FixNPCSpawner
{
    [MenuItem("Tools/Radiant Orchard/Fix NPC Spawner")]
    static void Fix()
    {
        Undo.SetCurrentGroupName("Fix NPC Spawner");
        int g = Undo.GetCurrentGroup();

        // ── Find NPCSpawner ───────────────────────────────────────────────
        var spawner = Object.FindAnyObjectByType<NPCSpawner>();
        if (spawner == null)
        {
            // Create on GameManagers
            var managers = GameObject.Find("GameManagers");
            if (managers == null)
            {
                managers = new GameObject("GameManagers");
                Undo.RegisterCreatedObjectUndo(managers, "GameManagers");
            }
            spawner = Undo.AddComponent<NPCSpawner>(managers);
            Debug.Log("Created NPCSpawner on GameManagers");
        }

        var so = new SerializedObject(spawner);

        // ── 1. Stickman prefab ────────────────────────────────────────────
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/GameData/Prefabs/Stickman.prefab");
        so.FindProperty("stickmanPrefab").objectReferenceValue = prefab;
        Debug.Log(prefab ? "✓ stickmanPrefab = Stickman.prefab" : "✗ Stickman.prefab not found");

        // ── 2. Fruit pool — Level 1 fruits (all DoubleTap) ───────────────
        string[] l1Fruits = { "Strawberry", "Pineapple", "Watermelon" };
        var fruitPoolProp = so.FindProperty("fruitPool");
        fruitPoolProp.arraySize = l1Fruits.Length;
        for (int i = 0; i < l1Fruits.Length; i++)
        {
            var fd = AssetDatabase.LoadAssetAtPath<FruitData>(
                $"Assets/GameData/FruitData/FruitData_{l1Fruits[i]}.asset");
            fruitPoolProp.GetArrayElementAtIndex(i).objectReferenceValue = fd;
            Debug.Log(fd ? $"✓ fruitPool[{i}] = {l1Fruits[i]}" : $"✗ FruitData_{l1Fruits[i]} not found");
        }

        // ── 3. Spawn points — 5 around island edge at radius 28 ──────────
        // Delete old spawn point root if exists
        var oldRoot = GameObject.Find("SpawnPoints");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

        var spawnRoot = new GameObject("SpawnPoints");
        Undo.RegisterCreatedObjectUndo(spawnRoot, "SpawnPoints");

        float radius = 28f;
        float[] angles = { 0f, 72f, 144f, 216f, 288f };
        var spawnTransforms = new Transform[angles.Length];

        for (int i = 0; i < angles.Length; i++)
        {
            float rad = angles[i] * Mathf.Deg2Rad;
            var sp = new GameObject("SpawnPoint_" + i);
            Undo.RegisterCreatedObjectUndo(sp, "SpawnPoint");
            sp.transform.SetParent(spawnRoot.transform, false);
            sp.transform.position = new Vector3(
                Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
            // Face inward toward island centre
            sp.transform.rotation = Quaternion.LookRotation(
                -sp.transform.position.normalized, Vector3.up);
            spawnTransforms[i] = sp.transform;
        }

        var spawnPointsProp = so.FindProperty("spawnPoints");
        spawnPointsProp.arraySize = spawnTransforms.Length;
        for (int i = 0; i < spawnTransforms.Length; i++)
            spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];

        Debug.Log($"✓ {spawnTransforms.Length} spawn points created at radius {radius}");

        // ── 4. Spawn interval — fast for testing ─────────────────────────
        so.FindProperty("spawnInterval").floatValue  = 8f;
        so.FindProperty("maxActiveStickmen").intValue = 3;
        so.FindProperty("fruitScale").floatValue      = 1.5f;

        // Also set fruitOffset so fruit appears next to stickman
        so.FindProperty("fruitOffset").vector3Value = new Vector3(1.5f, 0.8f, 0f);

        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("✅ NPCSpawner fixed! Ctrl+S → Bake NavMesh → Play");
    }
}
#endif

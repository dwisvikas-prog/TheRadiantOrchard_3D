#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

// Dense ground cover (grass/bushes/flowers/mushrooms/moss boulders) and
// scattered pine + broad-leafy trees across the island's top surface,
// avoiding the 9 orchard zones so nothing overlaps what's already placed.
// Reuses real vegetation prefabs already in the project (Ultimate Nature
// Starter + Polytope Studio + Blinktool's mossy rock pool) rather than more
// procedural primitives — this is decorative ground cover, not something
// that needs guaranteed-safe materials the way earlier structural pieces did.
public static class GenerateJungleVegetation
{
    // angleDeg, radius — must mirror GenerateOrchardZones.Zones so ground
    // cover doesn't spawn on top of the fenced fruit plots.
    private static readonly Vector2[] OrchardZoneCenters =
    {
        new Vector2(345f, 20f), new Vector2(320f, 23f), new Vector2(295f, 24f),
        new Vector2(265f, 22f), new Vector2(235f, 22f), new Vector2(40f, 23f),
        new Vector2(70f, 22f), new Vector2(100f, 22f), new Vector2(125f, 22f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Jungle Vegetation")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Jungle Vegetation");
        int undoGroup = Undo.GetCurrentGroup();

        var root = FindOrCreateGroup("JungleVegetation");
        ClearChildren(root);

        var grassPool = LoadPool(
            "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Vegetation/Grass/Prefabs/UNS_Grass.prefab",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab");
        var bushPool = LoadPool(
            "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Vegetation/Bushes/Prefabs/UNS_Bush.prefab",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab");
        var flowerPool = LoadPool(
            "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Vegetation/Flowers/Prefabs/UNS_Flower.prefab",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Flowers/PT_Poppy_02.prefab");
        var mushroomPool = LoadPool(
            "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Vegetation/Mushrooms/Prefabs/UNS_Mushroom_Patch.prefab",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Mushrooms/PT_Caesars_Mushroom_01.prefab");
        var mossyRockPool = LoadPoolFromFolder("Assets/Blinktool/Low poly rocks/Prefabs/Mossy");
        var pinePool = LoadPool("Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab");
        var leafyTreePool = LoadPool(
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_plums.prefab");

        Random.InitState(91827);

        ScatterGround(root, grassPool, "Grass", 60, 0.9f);
        ScatterGround(root, bushPool, "Bush", 28, 1.1f);
        ScatterGround(root, flowerPool, "Flower", 24, 0.9f);
        ScatterGround(root, mushroomPool, "Mushroom", 12, 0.9f);
        ScatterAlongPathEdges(root, mossyRockPool, "PathStone", 6);
        ScatterTrees(root, pinePool, "PineTree", 10);
        ScatterTrees(root, leafyTreePool, "LeafyTree", 8);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: dense jungle vegetation scattered across the island. Save the scene (Ctrl+S).");
    }

    private static void ScatterGround(Transform parent, GameObject[] pool, string label, int count, float baseScale)
    {
        if (pool == null || pool.Length == 0) return;

        int placed = 0, attempts = 0;
        while (placed < count && attempts < count * 6)
        {
            attempts++;
            float radius = Random.Range(11f, 38f);
            float angle = Random.Range(0f, 360f);
            if (TooCloseToZone(angle, radius, 4f)) continue;

            var prefab = pool[Random.Range(0, pool.Length)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Create " + label);
            instance.transform.localPosition = PointOnCircle(angle, radius);
            instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.localScale = Vector3.one * baseScale * Random.Range(0.85f, 1.25f);
            StripCollider(instance);
            placed++;
        }
    }

    // A handful of small stones right along the path rings' edges — not
    // ground-covering boulders, just natural trail-side accents.
    private static readonly float[] PathRingRadii = { 14f, 24f, 34f };

    private static void ScatterAlongPathEdges(Transform parent, GameObject[] pool, string label, int count)
    {
        if (pool == null || pool.Length == 0) return;

        int placed = 0, attempts = 0;
        while (placed < count && attempts < count * 8)
        {
            attempts++;
            float ringRadius = PathRingRadii[Random.Range(0, PathRingRadii.Length)];
            float radius = ringRadius + Random.Range(-1.4f, 1.4f);
            float angle = Random.Range(0f, 360f);
            if (TooCloseToZone(angle, radius, 3f)) continue;

            var prefab = pool[Random.Range(0, pool.Length)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Create " + label);
            instance.transform.localPosition = PointOnCircle(angle, radius);
            instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.localScale = Vector3.one * Random.Range(0.35f, 0.55f);
            StripCollider(instance);
            placed++;
        }
    }

    private static void ScatterTrees(Transform parent, GameObject[] pool, string label, int count)
    {
        if (pool == null || pool.Length == 0) return;

        var placedPositions = new List<Vector3>();
        int placed = 0, attempts = 0;
        while (placed < count && attempts < count * 10)
        {
            attempts++;
            float radius = Random.Range(12f, 36f);
            float angle = Random.Range(0f, 360f);
            if (TooCloseToZone(angle, radius, 5f)) continue;

            var pos = PointOnCircle(angle, radius);
            bool tooCloseToOtherTree = false;
            foreach (var p in placedPositions)
            {
                if (Vector3.Distance(p, pos) < 6f) { tooCloseToOtherTree = true; break; }
            }
            if (tooCloseToOtherTree) continue;

            var prefab = pool[Random.Range(0, pool.Length)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Create " + label);
            instance.transform.localPosition = pos;
            instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.localScale = Vector3.one * Random.Range(0.85f, 1.2f);
            placedPositions.Add(pos);
            placed++;
        }
    }

    private static bool TooCloseToZone(float angleDeg, float radius, float minDist)
    {
        var pos = PointOnCircle(angleDeg, radius);
        foreach (var zone in OrchardZoneCenters)
        {
            var zonePos = PointOnCircle(zone.x, zone.y);
            if (Vector3.Distance(pos, zonePos) < minDist + 3.5f) return true;
        }
        return false;
    }

    private static Vector3 PointOnCircle(float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
    }

    private static void StripCollider(GameObject go)
    {
        var collider = go.GetComponentInChildren<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
    }

    private static GameObject[] LoadPool(params string[] paths)
    {
        var list = new List<GameObject>();
        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) list.Add(prefab);
        }
        return list.ToArray();
    }

    private static GameObject[] LoadPoolFromFolder(string folder)
    {
        var list = new List<GameObject>();
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        foreach (var guid in guids)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null) list.Add(prefab);
        }
        return list.ToArray();
    }

    private static Transform FindOrCreateGroup(string groupName)
    {
        var go = GameObject.Find(groupName);
        if (go == null)
        {
            go = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + groupName);
        }
        return go.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }
}
#endif

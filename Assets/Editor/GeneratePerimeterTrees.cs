#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a dense ring of pine trees around the island perimeter to match
// the reference image — tall dark conifers lining the outer edge on all sides.
// GenerateJungleVegetation only scatters trees randomly in the interior;
// this tool places them specifically at the outer rim (radius 28-38).
//
// Run via: Tools → Radiant Orchard → Generate Perimeter Pine Ring
public static class GeneratePerimeterTrees
{
    // Orchard zone angles (from GenerateOrchardZones) — keep trees clear of
    // the zone centre so they don't block the fruit patch signboards.
    private static readonly (float angle, float clearance)[] ZoneClearances =
    {
        (345f, 12f), (320f, 11f), (295f, 11f), (265f, 11f), (235f, 11f),
        (40f,  11f), (70f,  11f), (100f, 11f), (125f, 11f),
        // Dock side — leave a visual gap so the dock+boat are not buried.
        (270f, 18f),
        // Waterfall — leave gap so the cascade is visible.
        (180f, 14f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Perimeter Pine Ring")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Perimeter Pine Ring");
        int undoGroup = Undo.GetCurrentGroup();

        var root = FindOrCreateGroup("PerimeterTrees");
        ClearChildren(root);

        // Try to load real Polytope pine prefabs — fall back to procedural.
        var pool = new List<GameObject>();
        pool.AddRange(LoadPool(
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green_cut.prefab",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_01_green.prefab",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_02_green.prefab"
        ));

        // Outer ring — dense, at island edge (radius 30-37)
        PlaceRing(root, pool, count: 40, minRadius: 30f, maxRadius: 37f,
                  minScale: 0.9f, maxScale: 1.4f, label: "OuterPine");

        // Inner ring — sparser, gives depth (radius 22-30)
        PlaceRing(root, pool, count: 24, minRadius: 22f, maxRadius: 29f,
                  minScale: 0.7f, maxScale: 1.1f, label: "InnerPine");

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: perimeter pine ring generated (40 outer + 24 inner). Save the scene (Ctrl+S).");
    }

    private static void PlaceRing(Transform root, List<GameObject> pool,
        int count, float minRadius, float maxRadius,
        float minScale, float maxScale, string label)
    {
        var placed = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            // Evenly distribute around the circle with slight jitter
            float angle  = i * (360f / count) + Random.Range(-6f, 6f);
            float radius = Random.Range(minRadius, maxRadius);

            // Skip positions too close to a fruit zone or dock/waterfall
            if (TooCloseToZone(angle, radius)) continue;

            Vector3 pos = PointOnCircle(angle, radius);

            // Don't stack trees on top of each other
            bool overlap = false;
            foreach (var p in placed)
                if (Vector3.Distance(p, pos) < 4.5f) { overlap = true; break; }
            if (overlap) continue;

            placed.Add(pos);
            float scale = Random.Range(minScale, maxScale);

            if (pool.Count > 0)
            {
                var prefab   = pool[Random.Range(0, pool.Count)];
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                Undo.RegisterCreatedObjectUndo(instance, "Create " + label);
                instance.name = label + "_" + i;
                instance.transform.localPosition = pos;
                instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                instance.transform.localScale    = Vector3.one * scale;
                // Strip colliders — decorative only, keeps mobile perf clean.
                foreach (var col in instance.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);
            }
            else
            {
                // Procedural 5-tier pine (same as IslandSceneGenerator fallback)
                var go = BuildFallbackPine(label + "_" + i, scale);
                Undo.RegisterCreatedObjectUndo(go, "Create " + label);
                go.transform.SetParent(root, false);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }
    }

    private static bool TooCloseToZone(float angleDeg, float radius)
    {
        var pos = PointOnCircle(angleDeg, radius);
        foreach (var (zAngle, clearance) in ZoneClearances)
        {
            // Zone radii are 20-24 from GenerateOrchardZones — use 22 as average
            var zonePos = PointOnCircle(zAngle, 22f);
            if (Vector3.Distance(pos, zonePos) < clearance) return true;
        }
        return false;
    }

    // ── Procedural fallback pine (5-tier dark conifer) ────────────────────
    private static GameObject BuildFallbackPine(string name, float scale)
    {
        var root    = new GameObject(name);
        var trunk   = new Color(0.28f, 0.18f, 0.10f);
        var dark    = new Color(0.10f, 0.28f, 0.12f);
        var tip     = new Color(0.14f, 0.34f, 0.14f);

        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.7f*scale, 0),
            new Vector3(0.22f*scale, 0.7f*scale, 0.22f*scale), trunk);

        float[] ty = { 1.1f, 2.2f, 3.2f, 4.1f, 5.0f };
        float[] sx = { 2.2f, 1.75f, 1.35f, 0.95f, 0.6f };
        float[] sy = { 1.2f, 1.1f, 1.0f, 0.9f, 0.85f };
        for (int t = 0; t < 5; t++)
        {
            Color c = t < 3 ? dark : tip;
            AddPart(root, PrimitiveType.Sphere,
                new Vector3(0, ty[t]*scale, 0),
                new Vector3(sx[t]*scale, sy[t]*scale, sx[t]*scale), c);
        }
        AddPart(root, PrimitiveType.Cylinder,
            new Vector3(0, 5.9f*scale, 0),
            new Vector3(0.08f*scale, 0.45f*scale, 0.08f*scale), tip);
        return root;
    }

    private static void AddPart(GameObject parent, PrimitiveType type,
        Vector3 pos, Vector3 scale, Color color)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var part   = GameObject.CreatePrimitive(type);
        part.transform.SetParent(parent.transform, false);
        part.transform.localPosition = pos;
        part.transform.localScale    = scale;
        var mat = new Material(Shader.Find(isURP
            ? "Universal Render Pipeline/Lit" : "Standard")) { color = color };
        part.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    private static Vector3 PointOnCircle(float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
    }

    private static List<GameObject> LoadPool(params string[] paths)
    {
        var list = new List<GameObject>();
        foreach (var p in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab != null) list.Add(prefab);
        }
        return list;
    }

    private static Transform FindOrCreateGroup(string groupName)
    {
        var go = GameObject.Find(groupName);
        if (go == null) { go = new GameObject(groupName); Undo.RegisterCreatedObjectUndo(go, "Create " + groupName); }
        return go.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }
}
#endif

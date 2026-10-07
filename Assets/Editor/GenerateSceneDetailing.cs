#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// One-shot dressing pass for the island scene: second waterfall's rocky
// outcrop, dock + rowboat, distant islets, path-side flowers, cliff moss/
// vines, and the Tree of Life glow at WisdomTree's base. Reuses existing
// project assets everywhere one exists (rocks, flowers, water material);
// dock/boat/vine have no matching asset anywhere in the project (audited
// via file search first) so those are built procedurally in the same
// wood/primitive style GenerateOrchardZones.cs already established.
public static class GenerateSceneDetailing
{
    const float WaterY = -3.5f;
    const float IslandEdgeRadius = 37.5f;

    [MenuItem("Tools/Radiant Orchard/Generate Scene Detailing Pass")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Scene Detailing Pass");
        int undoGroup = Undo.GetCurrentGroup();
        Random.InitState(12345);

        var detailGroup = FindOrCreateGroup("SceneDetailing");

        DressSecondWaterfall(detailGroup);
        BuildDockAndBoat(detailGroup);
        BuildDistantIslets(detailGroup);
        ScatterPathFlowers(detailGroup);
        DressCliffMossAndVines(detailGroup);
        BuildTreeOfLifeGlow();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: scene detailing pass complete. Save the scene (Ctrl+S).");
    }

    // ---------- 1. Second waterfall's rocky outcrop ----------
    static void DressSecondWaterfall(Transform group)
    {
        var waterfalls = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name == "Waterfall").ToList();
        var right = waterfalls.OrderByDescending(t => t.position.x).FirstOrDefault();
        if (right == null) { Debug.LogWarning("No 'Waterfall' objects found."); return; }

        Undo.RegisterCompleteObjectUndo(right, "Resize Right Waterfall");
        // Make it visibly smaller than the main (bottom) waterfall.
        right.localScale = new Vector3(right.localScale.x * 0.7f, right.localScale.y * 0.75f, right.localScale.z);

        var outcrop = new GameObject("Waterfall_RockyOutcrop");
        Undo.RegisterCreatedObjectUndo(outcrop, "Create Rocky Outcrop");
        outcrop.transform.SetParent(group, false);
        outcrop.transform.position = right.position + new Vector3(0f, right.localScale.y * 0.5f + 0.5f, 0f);

        int[] sandstoneIdx = { 4, 11, 22, 33, 9 };
        for (int i = 0; i < sandstoneIdx.Length; i++)
        {
            var prefab = LoadRock("Sandstone", sandstoneIdx[i]);
            if (prefab == null) continue;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, outcrop.transform);
            Undo.RegisterCreatedObjectUndo(inst, "Create Outcrop Rock");
            float a = i * (360f / sandstoneIdx.Length);
            float r = 1.4f;
            inst.transform.localPosition = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * r, Random.Range(-0.3f, 0.6f), Mathf.Cos(a * Mathf.Deg2Rad) * r * 0.4f - 1.5f);
            inst.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
            float s = Random.Range(1.1f, 1.9f);
            inst.transform.localScale *= s;
        }
    }

    // ---------- 2. Dock + rowboat ----------
    static void BuildDockAndBoat(Transform group)
    {
        var woodDark = new Color(0.36f, 0.24f, 0.14f);
        var woodLight = new Color(0.55f, 0.4f, 0.24f);
        var boatBrown = new Color(0.42f, 0.24f, 0.13f);

        // Left side = -X (world -X is camera-left when facing +Z).
        Vector3 anchor = new Vector3(-IslandEdgeRadius, WaterY - 0.1f, 2f);
        Vector3 outDir = Vector3.left;

        var dock = new GameObject("Dock");
        Undo.RegisterCreatedObjectUndo(dock, "Create Dock");
        dock.transform.SetParent(group, false);
        dock.transform.position = anchor;
        dock.transform.rotation = Quaternion.LookRotation(outDir, Vector3.up);

        const int plankCount = 7;
        const float plankLen = 1.3f;
        const float deckWidth = 2.2f;

        for (int i = 0; i < plankCount; i++)
        {
            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(plank, "Create Dock Plank");
            plank.name = "Plank_" + i;
            plank.transform.SetParent(dock.transform, false);
            plank.transform.localPosition = new Vector3(0f, 0.05f, i * plankLen + plankLen * 0.5f);
            plank.transform.localScale = new Vector3(deckWidth, 0.15f, plankLen * 0.95f);
            ApplyColor(plank, i % 2 == 0 ? woodLight : woodDark, glossy: false);

            if (i % 2 == 0)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Undo.RegisterCreatedObjectUndo(post, "Create Dock Post");
                    post.name = "SupportPost";
                    post.transform.SetParent(dock.transform, false);
                    post.transform.localPosition = new Vector3(side * deckWidth * 0.45f, -0.9f, i * plankLen + plankLen * 0.5f);
                    post.transform.localScale = new Vector3(0.18f, 1.9f, 0.18f);
                    ApplyColor(post, woodDark, glossy: false);
                }
            }
        }

        // Two tie posts at the far end.
        Transform tiePost = null;
        foreach (float side in new[] { -1f, 1f })
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(post, "Create Tie Post");
            post.name = "TiePost";
            post.transform.SetParent(dock.transform, false);
            post.transform.localPosition = new Vector3(side * deckWidth * 0.45f, 0.55f, plankCount * plankLen);
            post.transform.localScale = new Vector3(0.16f, 0.5f, 0.16f);
            ApplyColor(post, woodDark, glossy: false);
            tiePost = post.transform;
        }

        // Rowboat, floating beside the end of the dock.
        var boat = new GameObject("Rowboat");
        Undo.RegisterCreatedObjectUndo(boat, "Create Rowboat");
        boat.transform.SetParent(group, false);
        boat.transform.position = dock.transform.TransformPoint(new Vector3(deckWidth * 0.5f + 1.1f, -0.55f, plankCount * plankLen - 0.5f));
        boat.transform.rotation = dock.transform.rotation * Quaternion.Euler(0f, 8f, 0f);

        var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(hull, "Create Boat Hull");
        hull.name = "Hull";
        hull.transform.SetParent(boat.transform, false);
        hull.transform.localScale = new Vector3(1.0f, 0.45f, 2.4f);
        ApplyColor(hull, boatBrown, glossy: true);

        foreach (float dir in new[] { -1f, 1f })
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(cap, "Create Boat End Cap");
            cap.name = dir < 0 ? "Stern" : "Bow";
            cap.transform.SetParent(boat.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0f, dir * 1.35f);
            cap.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            cap.transform.localScale = new Vector3(1.0f, 0.45f, 0.7f);
            ApplyColor(cap, boatBrown, glossy: true);
        }

        if (tiePost != null)
        {
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(rope, "Create Tie Rope");
            rope.name = "TieRope";
            rope.transform.SetParent(group, false);
            Vector3 a = tiePost.position + Vector3.up * 0.3f;
            Vector3 b = boat.transform.position + Vector3.up * 0.15f;
            rope.transform.position = (a + b) * 0.5f;
            rope.transform.up = (b - a).normalized;
            rope.transform.localScale = new Vector3(0.03f, Vector3.Distance(a, b) * 0.5f, 0.03f);
            ApplyColor(rope, new Color(0.6f, 0.55f, 0.4f), glossy: false);
        }
    }

    // ---------- 3. Distant rocky islets ----------
    static void BuildDistantIslets(Transform group)
    {
        var specs = new (float angle, float radius)[]
        {
            (20f, 50f),
            (60f, 63f),
            (130f, 55f),
        };

        var islets = FindOrCreateGroup("DistantIslets", group);

        for (int i = 0; i < specs.Length; i++)
        {
            var (angle, radius) = specs[i];
            var rad = angle * Mathf.Deg2Rad;
            var center = new Vector3(Mathf.Sin(rad) * radius, WaterY - 1.2f, Mathf.Cos(rad) * radius);

            var islet = new GameObject("Islet_" + i);
            Undo.RegisterCreatedObjectUndo(islet, "Create Islet");
            islet.transform.SetParent(islets, false);
            islet.transform.position = center;

            int rockCount = Random.Range(3, 5);
            for (int j = 0; j < rockCount; j++)
            {
                bool mossy = j % 2 == 0;
                int idx = Random.Range(1, 40);
                var prefab = LoadRock(mossy ? "Mossy" : "Sandstone", idx);
                if (prefab == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, islet.transform);
                Undo.RegisterCreatedObjectUndo(inst, "Create Islet Rock");
                float a = j * (360f / rockCount) + Random.Range(-15f, 15f);
                float r = Random.Range(0.5f, 2.2f);
                inst.transform.localPosition = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * r, Random.Range(-0.2f, 0.9f), Mathf.Cos(a * Mathf.Deg2Rad) * r);
                inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                inst.transform.localScale *= Random.Range(1.5f, 3.2f);
            }
        }
    }

    // ---------- 4. Flowers along the dirt paths ----------
    static void ScatterPathFlowers(Transform group)
    {
        var poppyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Flowers/PT_Poppy_02.prefab");
        var baseMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Polytope Studio/Lowpoly_Environments/Sources/Materials/PT_Poppy_mat.mat");
        if (poppyPrefab == null || baseMat == null)
        {
            Debug.LogWarning("Poppy flower prefab/material not found — skipping flower scatter.");
            return;
        }

        var pink = new Material(baseMat) { name = "Flower_Pink" };
        pink.SetFloat("_CUSTOMFLOWERSCOLOR", 1f);
        pink.SetColor("_FLOWERSCOLOR", new Color(1f, 0.55f, 0.75f));

        var white = new Material(baseMat) { name = "Flower_White" };
        white.SetFloat("_CUSTOMFLOWERSCOLOR", 1f);
        white.SetColor("_FLOWERSCOLOR", new Color(0.97f, 0.97f, 0.9f));

        var yellow = new Material(baseMat) { name = "Flower_Yellow" };
        yellow.SetFloat("_CUSTOMFLOWERSCOLOR", 1f);
        yellow.SetColor("_FLOWERSCOLOR", new Color(1f, 0.85f, 0.2f));

        var palette = new[] { pink, white, yellow };

        var pathways = GameObject.Find("Pathways");
        if (pathways == null) { Debug.LogWarning("Pathways group not found — skipping flower scatter."); return; }

        var flowersGroup = FindOrCreateGroup("PathFlowers", group);

        var byPrefix = pathways.transform.Cast<Transform>()
            .GroupBy(t => System.Text.RegularExpressions.Regex.Replace(t.name, @"_\d+$", ""))
            .ToList();

        int placed = 0;
        int colorCursor = 0;
        foreach (var strand in byPrefix)
        {
            var points = strand.OrderBy(t =>
            {
                int.TryParse(t.name.Substring(t.name.LastIndexOf('_') + 1), out var n);
                return n;
            }).ToList();

            for (int i = 0; i < points.Count; i += 4) // stride: sparse, natural spacing
            {
                if (Random.value < 0.25f) continue; // occasional gap, not a solid hedge

                var p = points[i].position;
                Vector3 tangent = (i + 1 < points.Count ? points[i + 1].position : p) -
                                   (i - 1 >= 0 ? points[i - 1].position : p);
                if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.forward;
                Vector3 normal = Vector3.Cross(tangent.normalized, Vector3.up);

                foreach (float side in new[] { -1f, 1f })
                {
                    if (Random.value < 0.2f) continue;
                    var offset = normal * side * Random.Range(1.0f, 1.6f);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(poppyPrefab, flowersGroup);
                    Undo.RegisterCreatedObjectUndo(inst, "Create Path Flower");
                    inst.transform.position = p + offset + Vector3.up * 0.02f;
                    inst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    inst.transform.localScale *= Random.Range(0.7f, 1.1f);

                    var mat = palette[colorCursor % palette.Length];
                    colorCursor++;
                    foreach (var r in inst.GetComponentsInChildren<Renderer>())
                        r.sharedMaterial = mat;

                    placed++;
                }
            }
        }
        Debug.Log("Scattered " + placed + " flowers along paths.");
    }

    // ---------- 5. Cliff-base moss + hanging vines ----------
    static void DressCliffMossAndVines(Transform group)
    {
        var cliffGroup = FindOrCreateGroup("CliffOvergrowth", group);
        const float radius = 36.5f;
        const int count = 16;
        var vineColor = new Color(0.16f, 0.32f, 0.12f);

        for (int i = 0; i < count; i++)
        {
            float a = i * (360f / count) + Random.Range(-4f, 4f);
            var rad = a * Mathf.Deg2Rad;
            Vector3 rim = new Vector3(Mathf.Sin(rad) * radius, -2.4f, Mathf.Cos(rad) * radius);
            Vector3 outward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            if (i % 2 == 0)
            {
                int idx = Random.Range(1, 58);
                var prefab = LoadRock("Mossy", idx);
                if (prefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cliffGroup);
                    Undo.RegisterCreatedObjectUndo(inst, "Create Cliff Moss Rock");
                    inst.transform.position = rim + outward * 0.6f + Vector3.down * Random.Range(0.3f, 1.4f);
                    inst.transform.rotation = Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                    inst.transform.localScale *= Random.Range(1.3f, 2.4f);
                }
            }
            else
            {
                var vine = new GameObject("HangingVine_" + i);
                Undo.RegisterCreatedObjectUndo(vine, "Create Hanging Vine");
                vine.transform.SetParent(cliffGroup, false);
                vine.transform.position = rim + outward * 0.3f;

                int segs = Random.Range(3, 6);
                float segLen = Random.Range(0.6f, 1.0f);
                Vector3 cursor = Vector3.zero;
                for (int s = 0; s < segs; s++)
                {
                    var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Undo.RegisterCreatedObjectUndo(seg, "Create Vine Segment");
                    seg.name = "Segment_" + s;
                    seg.transform.SetParent(vine.transform, false);
                    float sway = Mathf.Sin(s * 1.3f + i) * 0.15f;
                    cursor += new Vector3(sway, -segLen, 0f);
                    seg.transform.localPosition = cursor;
                    seg.transform.localScale = new Vector3(Mathf.Lerp(0.12f, 0.05f, s / (float)segs), segLen * 1.05f, 0.08f);
                    ApplyColor(seg, vineColor, glossy: false);
                    Object.DestroyImmediate(seg.GetComponent<Collider>());
                }
            }
        }
    }

    // ---------- 6. Tree of Life glow ----------
    static void BuildTreeOfLifeGlow()
    {
        var wisdomTree = GameObject.Find("WisdomTree");
        if (wisdomTree == null) { Debug.LogWarning("WisdomTree not found — skipping glow."); return; }

        var glowColor = new Color(0.25f, 0.85f, 0.9f);

        var glowRoot = new GameObject("TreeOfLifeGlow");
        Undo.RegisterCreatedObjectUndo(glowRoot, "Create Tree Glow Root");
        glowRoot.transform.SetParent(wisdomTree.transform, false);
        glowRoot.transform.localPosition = new Vector3(0f, 0.3f, 0f);

        var light = glowRoot.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.intensity = 2.2f;
        light.range = 5.5f;
        light.shadows = LightShadows.None;

        var psGO = new GameObject("GlowMotes");
        Undo.RegisterCreatedObjectUndo(psGO, "Create Glow Particles");
        psGO.transform.SetParent(glowRoot.transform, false);

        var ps = psGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 3.5f;
        main.startSpeed = 0.3f;
        main.startSize = 0.25f;
        main.startColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.6f);
        main.maxParticles = 30;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 6f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.8f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(glowColor, 0f), new GradientColorKey(glowColor, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.3f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = grad;

        var renderer = psGO.GetComponent<ParticleSystemRenderer>();
        var circleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GameData/UI/circle.png");
        if (circleTex != null)
        {
            var shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            var mat = new Material(shader);
            if (mat.HasProperty("_MainTex")) mat.mainTexture = circleTex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", circleTex);
            renderer.sharedMaterial = mat;
        }
    }

    // ---------- helpers ----------
    static GameObject LoadRock(string category, int index) =>
        AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Blinktool/Low poly rocks/Prefabs/{category}/Rocks_{category}_{index}.prefab");

    static void ApplyColor(GameObject go, Color color, bool glossy)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        var mat = new Material(shader) { color = color };
        if (glossy)
        {
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.75f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.75f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
        }
        renderer.sharedMaterial = mat;
    }

    static Transform FindOrCreateGroup(string groupName, Transform parent = null)
    {
        Transform existing = parent != null ? parent.Find(groupName) : GameObject.Find(groupName)?.transform;
        if (existing != null) return existing;
        var go = new GameObject(groupName);
        Undo.RegisterCreatedObjectUndo(go, "Create " + groupName);
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }
}
#endif

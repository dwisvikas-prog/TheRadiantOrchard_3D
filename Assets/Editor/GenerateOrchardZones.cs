#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

// Places the 9 fruit zones at the compass positions read off the reference
// mockup (clockwise from the top of that image: Watermelon nearest the
// tree, then Pineapple/Strawberry upper-left, Lemon/Grapes lower-left, a
// gap at the bottom for the pond/dock, then Apple/Peach/Banana/Cherry down
// the right side) — hand-matched angles, not evenly spaced. No fruit or
// signpost models exist in the project (audit found none), so each zone is
// a fenced clearing (reusing a Polytope fence prefab already owned) with a
// procedural wooden sign showing the fruit name and a cluster of
// fruit-colored spheres standing in for the crop.
//
// Decorative only — the actual harvest mechanic is FruitHarvester, spawned
// per-Stickman by NPCSpawner and driven by touch gesture (see FruitHarvester.cs).
// These zones used to also carry a FruitZoneInteractable (tap-to-grow-stage)
// component, but that was a second, disconnected harvest/reward path with its
// own rules; it's been removed so there's one harvesting mechanic, not two.
public static class GenerateOrchardZones
{
    private struct ZoneSpec
    {
        public FruitType type;
        public float angleDeg; // 0 = "north" / top of the reference image, clockwise
        public float radius;
    }

    private static readonly ZoneSpec[] Zones =
    {
        new ZoneSpec { type = FruitType.Watermelon, angleDeg = 345f, radius = 20f },
        new ZoneSpec { type = FruitType.Pineapple, angleDeg = 320f, radius = 23f },
        new ZoneSpec { type = FruitType.Strawberry, angleDeg = 295f, radius = 24f },
        new ZoneSpec { type = FruitType.Lemon, angleDeg = 265f, radius = 22f },
        new ZoneSpec { type = FruitType.Grapes, angleDeg = 235f, radius = 22f },
        new ZoneSpec { type = FruitType.Apple, angleDeg = 40f, radius = 23f },
        new ZoneSpec { type = FruitType.Peach, angleDeg = 70f, radius = 22f },
        new ZoneSpec { type = FruitType.Banana, angleDeg = 100f, radius = 22f },
        new ZoneSpec { type = FruitType.Cherry, angleDeg = 125f, radius = 22f },
    };

    [MenuItem("Tools/Radiant Orchard/Generate Orchard Fruit Zones")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Orchard Fruit Zones");
        int undoGroup = Undo.GetCurrentGroup();

        var fruitsGroup = FindOrCreateGroup("Fruits");
        ClearChildren(fruitsGroup);

        // Built-in RP is active again (RevertToBuiltIn.cs) so the real fence
        // prefab's own material renders correctly — use it instead of the
        // procedural post-and-rail fallback.
        var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab");

        foreach (var zone in Zones)
        {
            var def = AssetDatabase.LoadAssetAtPath<FruitZoneDefinition>($"Assets/GameData/Fruits/Fruit_{zone.type}.asset");
            if (def == null) continue;

            var angleRad = zone.angleDeg * Mathf.Deg2Rad;
            var center = new Vector3(Mathf.Sin(angleRad) * zone.radius, 0f, Mathf.Cos(angleRad) * zone.radius);

            var zoneRoot = new GameObject("Orchard_" + zone.type);
            Undo.RegisterCreatedObjectUndo(zoneRoot, "Create Orchard Zone");
            zoneRoot.transform.SetParent(fruitsGroup, false);
            zoneRoot.transform.localPosition = center;
            zoneRoot.transform.localRotation = Quaternion.LookRotation(-center.normalized, Vector3.up);

            BuildFenceRing(zoneRoot.transform, fencePrefab, 3.2f);
            BuildSign(zoneRoot.transform, zone.type.ToString());
            BuildFruitCluster(zoneRoot.transform, zone.type, def.fruitColor);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: 9 orchard fruit zones placed to match the reference layout. Save the scene (Ctrl+S).");
    }

    private static void BuildFenceRing(Transform parent, GameObject fencePrefab, float radius)
    {
        if (fencePrefab != null)
        {
            const int prefabSegments = 6;
            for (int i = 0; i < prefabSegments; i++)
            {
                float a = i * (360f / prefabSegments);
                var p = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * radius, 0f, Mathf.Cos(a * Mathf.Deg2Rad) * radius);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, parent);
                Undo.RegisterCreatedObjectUndo(instance, "Create Fence Segment");
                instance.transform.localPosition = p;
                instance.transform.localRotation = Quaternion.Euler(0f, a + 90f, 0f);
            }
            return;
        }

        // Fallback: procedural post-and-rail fence if the prefab is missing.
        var fenceColor = new Color(0.42f, 0.3f, 0.18f);
        const int segments = 10;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * (360f / segments);
            var pos = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0f, Mathf.Cos(angle * Mathf.Deg2Rad) * radius);

            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(post, "Create Fence Post");
            post.name = "FencePost_" + i;
            post.transform.SetParent(parent, false);
            post.transform.localPosition = pos + new Vector3(0f, 0.4f, 0f);
            post.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            post.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
            ApplyColor(post, fenceColor);
            Object.DestroyImmediate(post.GetComponent<Collider>());

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(rail, "Create Fence Rail");
            rail.name = "FenceRail_" + i;
            rail.transform.SetParent(parent, false);
            float railLength = 2f * Mathf.PI * radius / segments * 1.1f;
            rail.transform.localPosition = pos + new Vector3(0f, 0.55f, 0f);
            rail.transform.localRotation = Quaternion.Euler(0f, angle + 90f, 0f);
            rail.transform.localScale = new Vector3(railLength, 0.1f, 0.06f);
            ApplyColor(rail, fenceColor);
            Object.DestroyImmediate(rail.GetComponent<Collider>());
        }
    }

    private static void BuildSign(Transform parent, string label)
    {
        var woodDark = new Color(0.42f, 0.3f, 0.18f);
        var woodBoard = new Color(0.55f, 0.38f, 0.22f);

        // Two uprights + a crossbar, with the board hung below the crossbar
        // on two thin "rope" struts — reads as an actual hanging signboard
        // instead of a sign nailed straight to a single post.
        foreach (float side in new[] { -0.7f, 0.7f })
        {
            var upright = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(upright, "Create Sign Upright");
            upright.name = "SignUpright";
            upright.transform.SetParent(parent, false);
            upright.transform.localPosition = new Vector3(side, 0.75f, 3.6f);
            upright.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
            ApplyColor(upright, woodDark, glossy: false);
            Object.DestroyImmediate(upright.GetComponent<Collider>());
        }

        var crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(crossbar, "Create Sign Crossbar");
        crossbar.name = "SignCrossbar";
        crossbar.transform.SetParent(parent, false);
        crossbar.transform.localPosition = new Vector3(0f, 1.5f, 3.6f);
        crossbar.transform.localScale = new Vector3(1.7f, 0.1f, 0.1f);
        ApplyColor(crossbar, woodDark, glossy: false);
        Object.DestroyImmediate(crossbar.GetComponent<Collider>());

        foreach (float side in new[] { -0.55f, 0.55f })
        {
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(rope, "Create Sign Rope");
            rope.name = "HangRope";
            rope.transform.SetParent(parent, false);
            rope.transform.localPosition = new Vector3(side, 1.25f, 3.6f);
            rope.transform.localScale = new Vector3(0.035f, 0.35f, 0.035f);
            ApplyColor(rope, new Color(0.6f, 0.55f, 0.4f), glossy: false);
            Object.DestroyImmediate(rope.GetComponent<Collider>());
        }

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(board, "Create Sign Board");
        board.name = "SignBoard";
        board.transform.SetParent(parent, false);
        board.transform.localPosition = new Vector3(0f, 1.0f, 3.6f);
        board.transform.localScale = new Vector3(1.6f, 0.4f, 0.06f);
        board.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-3f, 3f)); // slight hang tilt
        ApplyColor(board, woodBoard, glossy: false);
        Object.DestroyImmediate(board.GetComponent<Collider>());

        var textGO = new GameObject("SignText");
        textGO.transform.SetParent(board.transform, false);
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        textGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = label;
        textMesh.characterSize = 0.25f;
        textMesh.fontSize = 48;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
    }

    // Real matching fruit meshes don't exist for 8 of the 9 types (audited —
    // only PT_Fruit_Tree_01_apples matches). So each zone gets a real
    // Polytope bush/tree prefab as the host plant (a real "bush, tree, or
    // vine" per the brief) plus oversized glossy primitive fruit shapes
    // standing in for the crop itself.
    private static readonly string ShrubPath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    private static readonly string TreeHostPath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab";
    private static readonly string AppleTreePath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_apples.prefab";

    private static void BuildFruitCluster(Transform parent, FruitType type, Color color)
    {
        bool isBushFruit = type == FruitType.Strawberry || type == FruitType.Pineapple ||
                            type == FruitType.Watermelon || type == FruitType.Grapes;

        string hostPath = type == FruitType.Apple ? AppleTreePath : (isBushFruit ? ShrubPath : TreeHostPath);
        var hostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hostPath);
        if (hostPrefab != null)
        {
            var host = (GameObject)PrefabUtility.InstantiatePrefab(hostPrefab, parent);
            Undo.RegisterCreatedObjectUndo(host, "Create Fruit Host Plant");
            host.transform.localPosition = Vector3.zero;
            // FIX: scale up host plants so zones look lush — old 1.2x was
            // too small; reference shows plants almost filling their fenced area.
            host.transform.localScale *= isBushFruit ? 2.0f : 1.5f;
        }

        // Apple already carries real apples — light cluster on top.
        // All others: more fruits (9 vs 7) in a wider, taller arrangement
        // so zones look full and colourful like the reference image.
        int   count           = type == FruitType.Apple ? 5 : 9;
        // FIX: raise height and widen radius so fruits sit ON TOP of the
        // plant visually instead of clipping through its base.
        float hostFruitHeight = isBushFruit ? 1.2f : 2.8f;
        float clusterRadius   = isBushFruit ? 1.4f : 1.8f;

        for (int i = 0; i < count; i++)
        {
            float angle  = i * (360f / count) * Mathf.Deg2Rad;
            float yWave  = hostFruitHeight + Mathf.Sin(i * 1.7f) * 0.45f;
            var   offset = new Vector3(Mathf.Cos(angle) * clusterRadius, yWave, Mathf.Sin(angle) * clusterRadius);
            BuildFruitShape(parent, type, offset, i, color);
        }
    }

    private static void BuildFruitShape(Transform parent, FruitType type, Vector3 localPos, int index, Color color)
    {
        switch (type)
        {
            case FruitType.Grapes:
            case FruitType.Cherry:
            {
                // Small paired/bunched spheres instead of one big fruit.
                int sub = type == FruitType.Grapes ? 6 : 2;
                float subRadius = type == FruitType.Grapes ? 0.11f : 0.14f;
                for (int s = 0; s < sub; s++)
                {
                    var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Undo.RegisterCreatedObjectUndo(f, "Create Fruit");
                    f.name = type + "_" + index + "_" + s;
                    f.transform.SetParent(parent, false);
                    var jitter = new Vector3(Mathf.Sin(s * 2.1f) * 0.13f, -s * subRadius * 1.4f, Mathf.Cos(s * 2.1f) * 0.13f);
                    f.transform.localPosition = localPos + jitter;
                    f.transform.localScale = Vector3.one * subRadius * 2f;
                    ApplyColor(f, color, glossy: true);
                    Object.DestroyImmediate(f.GetComponent<Collider>());
                }
                break;
            }
            case FruitType.Banana:
            {
                // A small fan of elongated capsules approximating a hand of bananas.
                for (int s = 0; s < 4; s++)
                {
                    var f = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Undo.RegisterCreatedObjectUndo(f, "Create Fruit");
                    f.name = "Banana_" + index + "_" + s;
                    f.transform.SetParent(parent, false);
                    float fan = (s - 1.5f) * 18f;
                    f.transform.localPosition = localPos + new Vector3(0f, -0.25f, 0f);
                    f.transform.localRotation = Quaternion.Euler(80f, 0f, fan);
                    f.transform.localScale = new Vector3(0.16f, 0.42f, 0.16f);
                    ApplyColor(f, color, glossy: true);
                    Object.DestroyImmediate(f.GetComponent<Collider>());
                }
                break;
            }
            case FruitType.Pineapple:
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(body, "Create Fruit");
                body.name = "Pineapple_" + index;
                body.transform.SetParent(parent, false);
                body.transform.localPosition = localPos;
                body.transform.localScale = new Vector3(0.32f, 0.42f, 0.32f);
                ApplyColor(body, color, glossy: true);
                Object.DestroyImmediate(body.GetComponent<Collider>());

                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(crown, "Create Fruit Crown");
                crown.name = "PineappleCrown_" + index;
                crown.transform.SetParent(parent, false);
                crown.transform.localPosition = localPos + Vector3.up * 0.55f;
                crown.transform.localScale = new Vector3(0.16f, 0.32f, 0.16f);
                ApplyColor(crown, new Color(0.2f, 0.45f, 0.18f), glossy: true);
                Object.DestroyImmediate(crown.GetComponent<Collider>());
                break;
            }
            case FruitType.Strawberry:
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(body, "Create Fruit");
                body.name = "Strawberry_" + index;
                body.transform.SetParent(parent, false);
                body.transform.localPosition = localPos;
                body.transform.localScale = new Vector3(0.32f, 0.4f, 0.32f);
                ApplyColor(body, color, glossy: true);
                Object.DestroyImmediate(body.GetComponent<Collider>());

                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(cap, "Create Fruit Cap");
                cap.name = "StrawberryCap_" + index;
                cap.transform.SetParent(parent, false);
                cap.transform.localPosition = localPos + Vector3.up * 0.2f;
                cap.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f);
                ApplyColor(cap, new Color(0.2f, 0.45f, 0.18f), glossy: true);
                Object.DestroyImmediate(cap.GetComponent<Collider>());
                break;
            }
            case FruitType.Lemon:
            {
                var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(f, "Create Fruit");
                f.name = "Lemon_" + index;
                f.transform.SetParent(parent, false);
                f.transform.localPosition = localPos;
                f.transform.localScale = new Vector3(0.34f, 0.26f, 0.34f);
                ApplyColor(f, color, glossy: true);
                Object.DestroyImmediate(f.GetComponent<Collider>());
                break;
            }
            case FruitType.Watermelon:
            {
                // FIX: larger scale (0.65→0.95) + force dark green colour
                // regardless of what FruitZoneDefinition.fruitColor says —
                // watermelons are green in every reference, not pink.
                var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(f, "Create Fruit");
                f.name = "Watermelon_" + index;
                f.transform.SetParent(parent, false);
                f.transform.localPosition = localPos;
                // Slightly squashed sphere = rounder melon silhouette
                f.transform.localScale = new Vector3(0.95f, 0.82f, 0.95f);
                // Always dark green — ignore whatever color the zone passes in.
                ApplyColor(f, new Color(0.18f, 0.48f, 0.14f), glossy: true);
                Object.DestroyImmediate(f.GetComponent<Collider>());

                // Dark stripe band around equator (single flat disc)
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(stripe, "Melon Stripe Band");
                stripe.name = "WatermelonStripe_" + index;
                stripe.transform.SetParent(parent, false);
                stripe.transform.localPosition = localPos;
                stripe.transform.localScale = new Vector3(1.0f, 0.04f, 1.0f);
                ApplyColor(stripe, new Color(0.10f, 0.28f, 0.08f), glossy: false);
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
                break;
            }
            default: // Apple, Peach
            {
                var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(f, "Create Fruit");
                f.name = type + "_" + index;
                f.transform.SetParent(parent, false);
                f.transform.localPosition = localPos;
                f.transform.localScale = Vector3.one * 0.36f;
                ApplyColor(f, color, glossy: true);
                Object.DestroyImmediate(f.GetComponent<Collider>());
                break;
            }
        }
    }

    private static void ApplyColor(GameObject go, Color color, bool glossy = false)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        var mat = new Material(shader) { color = color };
        if (glossy)
        {
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.8f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
        }
        renderer.sharedMaterial = mat;
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

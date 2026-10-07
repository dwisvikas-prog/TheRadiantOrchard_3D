#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Pineapple Patch matching the reference image style:
//   • 6 pineapple plants growing from ground (rosette of spiky leaves + fruit)
//     using real PolyOne SM_Fruits_Pineapple_1 prefabs
//   • Wooden post-and-rail fence (PT_Modular_Fence_Wood_01)
//   • Hanging wooden sign labelled "Pineapple"
//   • Lush surrounding greenery: PT_Generic_Shrub_01_green + PT_Grass_02
//
// Tools → Radiant Orchard → Generate Pineapple Patch
public static class GeneratePineapplePatch
{
    const float AngleDeg = 320f;
    const float Radius   = 23f;

    const string FencePrefab     = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    const string ShrubPrefab     = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    const string GrassPrefab     = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
    const string PineapplePrefab = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Pineapple_1.prefab";

    static readonly Vector3[] PlantSlots =
    {
        new Vector3(-1.2f, 0f,  0.8f),
        new Vector3( 0.0f, 0f,  1.0f),
        new Vector3( 1.2f, 0f,  0.6f),
        new Vector3(-0.7f, 0f, -0.5f),
        new Vector3( 0.8f, 0f, -0.4f),
        new Vector3( 0.1f, 0f, -1.2f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Pineapple Patch")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Pineapple Patch");
        int g = Undo.GetCurrentGroup();

        float rad = AngleDeg * Mathf.Deg2Rad;
        Vector3 center = new Vector3(Mathf.Sin(rad) * Radius, 0f, Mathf.Cos(rad) * Radius);

        var fruitsGO = GameObject.Find("Fruits");
        Transform fruitsParent = fruitsGO != null ? fruitsGO.transform : null;

        var old = GameObject.Find("Orchard_Pineapple");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Orchard_Pineapple");
        Undo.RegisterCreatedObjectUndo(root, "Create Pineapple Patch");
        if (fruitsParent != null) root.transform.SetParent(fruitsParent, false);
        root.transform.localPosition = center;
        root.transform.localRotation = Quaternion.LookRotation(-center.normalized, Vector3.up);

        BuildFenceRing(root.transform, 2.8f);
        BuildSign(root.transform, "Pineapple", 3.0f);
        BuildPlantBed(root.transform);
        BuildGroundVines(root.transform);
        BuildSurroundingGreenery(root.transform, 3.6f);

        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Pineapple Patch generated.");
    }

    static void BuildFenceRing(Transform root, float halfSize)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FencePrefab);
        float step = halfSize;

        (Vector3 pos, float yRot)[] segments =
        {
            (new Vector3(-step * 0.5f, 0f,  halfSize), 0f),
            (new Vector3( step * 0.5f, 0f,  halfSize), 0f),
            (new Vector3( halfSize, 0f,  step * 0.5f), 90f),
            (new Vector3( halfSize, 0f, -step * 0.5f), 90f),
            (new Vector3( step * 0.5f, 0f, -halfSize), 180f),
            (new Vector3(-step * 0.5f, 0f, -halfSize), 180f),
            (new Vector3(-halfSize, 0f, -step * 0.5f), 270f),
            (new Vector3(-halfSize, 0f,  step * 0.5f), 270f),
        };

        foreach (var (pos, yRot) in segments)
        {
            if (prefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                Undo.RegisterCreatedObjectUndo(inst, "Fence Segment");
                inst.transform.localPosition = pos;
                inst.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
            }
            else
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(rail, "Fence Rail");
                rail.name = "FenceRail";
                rail.transform.SetParent(root, false);
                rail.transform.localPosition = pos + Vector3.up * 0.45f;
                rail.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
                rail.transform.localScale    = new Vector3(step, 0.08f, 0.08f);
                ApplyMat(rail, new Color(0.42f, 0.3f, 0.15f));
                Object.DestroyImmediate(rail.GetComponent<Collider>());
            }
        }
    }

    static void BuildSign(Transform root, string label, float signZ)
    {
        Color woodDark  = new Color(0.32f, 0.22f, 0.10f);
        Color woodBoard = new Color(0.50f, 0.34f, 0.16f);
        Color ropeCol   = new Color(0.60f, 0.52f, 0.36f);

        foreach (float sx in new[] { -0.65f, 0.65f })
        {
            var up = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(up, "Sign Upright");
            up.name = "SignUpright";
            up.transform.SetParent(root, false);
            up.transform.localPosition = new Vector3(sx, 0.7f, signZ);
            up.transform.localScale    = new Vector3(0.11f, 1.4f, 0.11f);
            ApplyMat(up, woodDark);
            Object.DestroyImmediate(up.GetComponent<Collider>());
        }

        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(bar, "Crossbar");
        bar.name = "SignCrossbar";
        bar.transform.SetParent(root, false);
        bar.transform.localPosition = new Vector3(0f, 1.42f, signZ);
        bar.transform.localScale    = new Vector3(1.55f, 0.11f, 0.11f);
        ApplyMat(bar, woodDark);
        Object.DestroyImmediate(bar.GetComponent<Collider>());

        foreach (float sx in new[] { -0.50f, 0.50f })
        {
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(rope, "Rope");
            rope.name = "SignRope";
            rope.transform.SetParent(root, false);
            rope.transform.localPosition = new Vector3(sx, 1.18f, signZ);
            rope.transform.localScale    = new Vector3(0.04f, 0.32f, 0.04f);
            ApplyMat(rope, ropeCol);
            Object.DestroyImmediate(rope.GetComponent<Collider>());
        }

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(board, "Sign Board");
        board.name = "SignBoard";
        board.transform.SetParent(root, false);
        board.transform.localPosition = new Vector3(0f, 0.95f, signZ);
        board.transform.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
        board.transform.localScale    = new Vector3(1.52f, 0.38f, 0.07f);
        ApplyMat(board, woodBoard);
        Object.DestroyImmediate(board.GetComponent<Collider>());

        var textGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(textGO, "Label");
        textGO.transform.SetParent(board.transform, false);
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.58f);
        textGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var tm = textGO.AddComponent<TextMesh>();
        tm.text          = label;
        tm.characterSize = 0.20f;
        tm.fontSize      = 52;
        tm.fontStyle     = FontStyle.Bold;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = Color.white;
    }

    static void BuildPlantBed(Transform root)
    {
        var pineapplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PineapplePrefab);

        for (int i = 0; i < PlantSlots.Length; i++)
            BuildPineapplePlant(root, PlantSlots[i], i, pineapplePrefab);
    }

    static void BuildPineapplePlant(Transform root, Vector3 pos, int idx, GameObject prefab)
    {
        float sz = Random.Range(0.85f, 1.1f);

        if (prefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            Undo.RegisterCreatedObjectUndo(inst, "Pineapple Plant");
            inst.name = "Pineapple_" + idx;
            inst.transform.localPosition = pos;
            inst.transform.localScale    = Vector3.one * sz * 1.6f;
            inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        else
        {
            // --- Procedural: spiky leaf rosette from ground ---
            Color leafGreen = new Color(0.20f, 0.50f, 0.14f);
            int leafCount = 8;
            for (int l = 0; l < leafCount; l++)
            {
                float ang   = l * (360f / leafCount) * Mathf.Deg2Rad;
                float droop = -30f;
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(leaf, "Pineapple Leaf");
                leaf.name = "PineLeaf_" + l;
                leaf.transform.SetParent(root, false);
                Vector3 leafDir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                leaf.transform.localPosition = pos + Vector3.up * 0.08f + leafDir * 0.15f;
                leaf.transform.localRotation = Quaternion.LookRotation(leafDir, Vector3.up) * Quaternion.Euler(droop, 0f, 0f);
                leaf.transform.localScale    = new Vector3(0.08f, 0.04f, sz * 0.8f);
                ApplyMat(leaf, leafGreen, 0.1f);
                Object.DestroyImmediate(leaf.GetComponent<Collider>());
            }

            // Fruit body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(body, "Pineapple Body");
            body.name = "PineBody_" + idx;
            body.transform.SetParent(root, false);
            body.transform.localPosition = pos + Vector3.up * (sz * 0.4f);
            body.transform.localScale    = new Vector3(sz * 0.28f, sz * 0.42f, sz * 0.28f);
            ApplyMat(body, new Color(0.90f, 0.72f, 0.10f), 0.4f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Fruit crown
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(crown, "Pineapple Crown");
            crown.name = "PineCrown_" + idx;
            crown.transform.SetParent(root, false);
            crown.transform.localPosition = pos + Vector3.up * (sz * 0.90f);
            crown.transform.localScale    = new Vector3(sz * 0.18f, sz * 0.35f, sz * 0.18f);
            ApplyMat(crown, new Color(0.18f, 0.45f, 0.12f), 0.1f);
            Object.DestroyImmediate(crown.GetComponent<Collider>());
        }
    }

    // Short vine tendrils between plants
    static void BuildGroundVines(Transform root)
    {
        Color vineGreen = new Color(0.20f, 0.48f, 0.12f);
        for (int v = 0; v < 8; v++)
        {
            var vine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(vine, "Vine");
            vine.name = "Vine_" + v;
            vine.transform.SetParent(root, false);
            vine.transform.localPosition = new Vector3(Random.Range(-1.5f, 1.5f), 0.02f, Random.Range(-1.2f, 1.2f));
            vine.transform.localScale    = new Vector3(0.07f, 0.035f, Random.Range(0.4f, 0.9f));
            vine.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ApplyMat(vine, vineGreen, 0.1f);
            Object.DestroyImmediate(vine.GetComponent<Collider>());
        }
    }

    static void BuildSurroundingGreenery(Transform root, float ringRadius)
    {
        var shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShrubPrefab);
        var grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefab);

        for (int b = 0; b < 14; b++)
        {
            float angle = b * (360f / 14f) * Mathf.Deg2Rad;
            float r     = ringRadius + (b % 2 == 0 ? 0f : 0.5f) + Random.Range(-0.2f, 0.2f);
            Vector3 pos = new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);

            if (shrubPrefab != null)
            {
                var sh = (GameObject)PrefabUtility.InstantiatePrefab(shrubPrefab, root);
                Undo.RegisterCreatedObjectUndo(sh, "Shrub");
                sh.name = "Shrub_" + b;
                sh.transform.localPosition = pos;
                sh.transform.localScale    = Vector3.one * Random.Range(0.5f, 0.9f);
                sh.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(bush, "Bush");
                bush.name = "Bush_" + b;
                bush.transform.SetParent(root, false);
                bush.transform.localPosition = pos + Vector3.up * 0.2f;
                bush.transform.localScale    = Vector3.one * 0.35f;
                ApplyMat(bush, new Color(0.18f, 0.42f, 0.14f));
                Object.DestroyImmediate(bush.GetComponent<Collider>());
            }

            if (grassPrefab != null && b % 2 == 0)
            {
                var gr = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, root);
                Undo.RegisterCreatedObjectUndo(gr, "Grass");
                gr.name = "Grass_" + b;
                gr.transform.localPosition = pos + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
                gr.transform.localScale    = Vector3.one * Random.Range(0.6f, 1.1f);
            }
        }
    }

    static void ApplyMat(GameObject go, Color color, float smoothness = 0.25f)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var mat    = new Material(shader) { color = color };
        if (!isURP) mat.SetFloat("_Glossiness", smoothness);
        else        mat.SetFloat("_Smoothness",  smoothness);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }
}
#endif

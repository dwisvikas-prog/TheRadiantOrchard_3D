#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Grapes Patch matching the reference image style:
//   • 4 wooden trellis frames with real PolyOne SM_Fruits_Grape_P2 bunches
//   • Vine ropes across the trellis with dangling grape clusters
//   • Wooden post-and-rail fence (PT_Modular_Fence_Wood_01)
//   • Hanging wooden sign labelled "Grapes"
//   • Lush surrounding greenery: PT_Generic_Shrub_01_green + PT_Grass_02
//
// Tools → Radiant Orchard → Generate Grapes Patch
public static class GenerateGrapesPatch
{
    const float AngleDeg = 235f;
    const float Radius   = 22f;

    const string FencePrefab = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    const string ShrubPrefab = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    const string GrassPrefab = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
    const string GrapePrefab = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Grape_P2.prefab";

    // Trellis positions — 2 rows of 2 frames
    static readonly Vector3[] TrellisSlots =
    {
        new Vector3(-1.1f, 0f,  0.5f),
        new Vector3( 1.1f, 0f,  0.5f),
        new Vector3(-1.1f, 0f, -0.8f),
        new Vector3( 1.1f, 0f, -0.8f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Grapes Patch")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Grapes Patch");
        int g = Undo.GetCurrentGroup();

        float rad = AngleDeg * Mathf.Deg2Rad;
        Vector3 center = new Vector3(Mathf.Sin(rad) * Radius, 0f, Mathf.Cos(rad) * Radius);

        var fruitsGO = GameObject.Find("Fruits");
        Transform fruitsParent = fruitsGO != null ? fruitsGO.transform : null;

        var old = GameObject.Find("Orchard_Grapes");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Orchard_Grapes");
        Undo.RegisterCreatedObjectUndo(root, "Create Grapes Patch");
        if (fruitsParent != null) root.transform.SetParent(fruitsParent, false);
        root.transform.localPosition = center;
        root.transform.localRotation = Quaternion.LookRotation(-center.normalized, Vector3.up);

        BuildFenceRing(root.transform, 2.8f);
        BuildSign(root.transform, "Grapes", 3.0f);
        BuildTrellisBed(root.transform);
        BuildSurroundingGreenery(root.transform, 3.6f);

        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Grapes Patch generated.");
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
                Undo.RegisterCreatedObjectUndo(rail, "FenceRail");
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
            Undo.RegisterCreatedObjectUndo(up, "SignUpright");
            up.name = "SignUpright"; up.transform.SetParent(root, false);
            up.transform.localPosition = new Vector3(sx, 0.7f, signZ);
            up.transform.localScale    = new Vector3(0.11f, 1.4f, 0.11f);
            ApplyMat(up, woodDark); Object.DestroyImmediate(up.GetComponent<Collider>());
        }

        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(bar, "Crossbar");
        bar.name = "SignCrossbar"; bar.transform.SetParent(root, false);
        bar.transform.localPosition = new Vector3(0f, 1.42f, signZ);
        bar.transform.localScale    = new Vector3(1.55f, 0.11f, 0.11f);
        ApplyMat(bar, woodDark); Object.DestroyImmediate(bar.GetComponent<Collider>());

        foreach (float sx in new[] { -0.50f, 0.50f })
        {
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(rope, "Rope");
            rope.name = "SignRope"; rope.transform.SetParent(root, false);
            rope.transform.localPosition = new Vector3(sx, 1.18f, signZ);
            rope.transform.localScale    = new Vector3(0.04f, 0.32f, 0.04f);
            ApplyMat(rope, ropeCol); Object.DestroyImmediate(rope.GetComponent<Collider>());
        }

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(board, "SignBoard");
        board.name = "SignBoard"; board.transform.SetParent(root, false);
        board.transform.localPosition = new Vector3(0f, 0.95f, signZ);
        board.transform.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
        board.transform.localScale    = new Vector3(1.52f, 0.38f, 0.07f);
        ApplyMat(board, woodBoard); Object.DestroyImmediate(board.GetComponent<Collider>());

        var textGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(textGO, "Label");
        textGO.transform.SetParent(board.transform, false);
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.58f);
        textGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var tm = textGO.AddComponent<TextMesh>();
        tm.text = label; tm.characterSize = 0.22f; tm.fontSize = 52;
        tm.fontStyle = FontStyle.Bold; tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center; tm.color = Color.white;
    }

    // =========================================================================
    // TRELLIS BED — wooden arch frames with vine ropes and grape bunches
    // =========================================================================
    static void BuildTrellisBed(Transform root)
    {
        Color woodCol = new Color(0.42f, 0.30f, 0.14f);
        Color vineCol = new Color(0.22f, 0.50f, 0.14f);
        var grapePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrapePrefab);

        for (int i = 0; i < TrellisSlots.Length; i++)
        {
            Vector3 pos = TrellisSlots[i];

            // Two vertical posts
            foreach (float sx in new[] { -0.35f, 0.35f })
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(post, "Trellis Post");
                post.name = "TrellisPost";
                post.transform.SetParent(root, false);
                post.transform.localPosition = pos + new Vector3(sx, 0.75f, 0f);
                post.transform.localScale    = new Vector3(0.07f, 0.75f, 0.07f);
                ApplyMat(post, woodCol, 0.1f);
                Object.DestroyImmediate(post.GetComponent<Collider>());
            }

            // Horizontal crossbar at top
            var crossbar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(crossbar, "Trellis Crossbar");
            crossbar.name = "TrellisCrossbar";
            crossbar.transform.SetParent(root, false);
            crossbar.transform.localPosition = pos + new Vector3(0f, 1.5f, 0f);
            crossbar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            crossbar.transform.localScale    = new Vector3(0.055f, 0.38f, 0.055f);
            ApplyMat(crossbar, woodCol, 0.1f);
            Object.DestroyImmediate(crossbar.GetComponent<Collider>());

            // Vine rope along crossbar
            var vine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(vine, "Vine Rope");
            vine.name = "VineRope";
            vine.transform.SetParent(root, false);
            vine.transform.localPosition = pos + new Vector3(0f, 1.55f, 0f);
            vine.transform.localScale    = new Vector3(0.06f, 0.03f, 0.75f);
            vine.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            ApplyMat(vine, vineCol, 0.1f);
            Object.DestroyImmediate(vine.GetComponent<Collider>());

            // Leaf clusters on vine
            for (int l = 0; l < 3; l++)
            {
                float lx = (l - 1) * 0.28f;
                var leafClump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(leafClump, "VineLeaf");
                leafClump.name = "VineLeaf_" + l;
                leafClump.transform.SetParent(root, false);
                leafClump.transform.localPosition = pos + new Vector3(lx, 1.55f, 0f);
                leafClump.transform.localScale    = new Vector3(0.22f, 0.15f, 0.22f);
                ApplyMat(leafClump, new Color(0.18f, 0.45f, 0.13f), 0.1f);
                Object.DestroyImmediate(leafClump.GetComponent<Collider>());
            }

            // Grape bunches hanging from vine — real prefab or procedural
            for (int b = 0; b < 2; b++)
            {
                float bx = (b == 0 ? -0.22f : 0.22f);
                Vector3 bunchPos = pos + new Vector3(bx, 1.15f, 0f);

                if (grapePrefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(grapePrefab, root);
                    Undo.RegisterCreatedObjectUndo(inst, "Grape Bunch");
                    inst.name = "GrapeBunch_" + i + "_" + b;
                    inst.transform.localPosition = bunchPos;
                    inst.transform.localScale    = Vector3.one * 1.2f;
                    inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
                else
                {
                    // Procedural grape bunch: pyramid of small spheres
                    Color purpleGrape = new Color(0.42f, 0.12f, 0.55f);
                    int rows = 3;
                    for (int row = 0; row < rows; row++)
                    {
                        int perRow = rows - row;
                        for (int gr = 0; gr < perRow; gr++)
                        {
                            var berry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            Undo.RegisterCreatedObjectUndo(berry, "Grape Berry");
                            berry.name = "GrapeBerry";
                            berry.transform.SetParent(root, false);
                            berry.transform.localPosition = bunchPos + new Vector3(
                                (gr - perRow * 0.5f + 0.5f) * 0.09f,
                                -row * 0.10f,
                                0f);
                            berry.transform.localScale = Vector3.one * 0.09f;
                            ApplyMat(berry, purpleGrape, 0.7f);
                            Object.DestroyImmediate(berry.GetComponent<Collider>());
                        }
                    }
                }
            }

            // Ground vine tendrils from base
            var groundVine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(groundVine, "GroundVine");
            groundVine.name = "GroundVine_" + i;
            groundVine.transform.SetParent(root, false);
            groundVine.transform.localPosition = pos + new Vector3(0f, 0.02f, 0f);
            groundVine.transform.localScale    = new Vector3(0.07f, 0.03f, Random.Range(0.5f, 0.9f));
            groundVine.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ApplyMat(groundVine, vineCol, 0.1f);
            Object.DestroyImmediate(groundVine.GetComponent<Collider>());
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
                bush.name = "Bush_" + b; bush.transform.SetParent(root, false);
                bush.transform.localPosition = pos + Vector3.up * 0.2f;
                bush.transform.localScale    = Vector3.one * 0.38f;
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

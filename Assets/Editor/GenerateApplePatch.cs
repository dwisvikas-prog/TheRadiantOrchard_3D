#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Apple Patch matching the reference image style:
//   • 3 apple trees (trunk + layered canopy + red apple clusters)
//   • Wooden post-and-rail fence enclosing a rectangular bed
//   • Hanging wooden sign labelled "Apple"
//   • Lush surrounding greenery (small bushes + grass tufts)
//   • Fallen apples on the ground
//
// Run via:  Tools → Radiant Orchard → Generate Apple Patch
public static class GenerateApplePatch
{
    [MenuItem("Tools/Radiant Orchard/Generate Apple Patch")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Apple Patch");
        int undoGroup = Undo.GetCurrentGroup();

        const float angleDeg = 40f;
        const float radius   = 23f;
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 patchCenter = new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);

        var fruitsGroup = GameObject.Find("Fruits");
        Transform parent = fruitsGroup != null ? fruitsGroup.transform : null;

        var existing = GameObject.Find("Orchard_Apple");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var root = new GameObject("Orchard_Apple");
        Undo.RegisterCreatedObjectUndo(root, "Create Apple Patch");
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.localPosition = patchCenter;
        root.transform.localRotation = Quaternion.LookRotation(-patchCenter.normalized, Vector3.up);

        BuildFence(root.transform);
        BuildSign(root.transform);
        BuildAppleBed(root.transform);
        BuildSurroundingGreenery(root.transform);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Apple Patch generated. Save the scene (Ctrl+S).");
    }

    // =========================================================================
    // FENCE
    // =========================================================================
    private static void BuildFence(Transform root)
    {
        const float hw = 3.0f;
        const float hd = 3.0f;

        Color woodDark  = new Color(0.32f, 0.22f, 0.10f);
        Color woodLight = new Color(0.52f, 0.36f, 0.18f);

        Vector3[] postPositions =
        {
            new Vector3(-hw, 0,  hd), new Vector3(0,  0,  hd), new Vector3(hw,  0,  hd),
            new Vector3( hw, 0,   0),
            new Vector3( hw, 0, -hd), new Vector3(0,  0, -hd), new Vector3(-hw, 0, -hd),
            new Vector3(-hw, 0,   0),
        };
        foreach (var pp in postPositions) CreatePost(root, pp, woodDark);

        float[] railHeights = { 0.35f, 0.65f };
        (Vector3 a, Vector3 b)[] sides =
        {
            (new Vector3(-hw, 0,  hd), new Vector3( hw, 0,  hd)),
            (new Vector3( hw, 0,  hd), new Vector3( hw, 0, -hd)),
            (new Vector3( hw, 0, -hd), new Vector3(-hw, 0, -hd)),
            (new Vector3(-hw, 0, -hd), new Vector3(-hw, 0,  hd)),
        };
        foreach (var (a, b) in sides)
        {
            foreach (float rh in railHeights)
            {
                Vector3 mid = (a + b) * 0.5f + Vector3.up * rh;
                float len   = Vector3.Distance(a, b);
                Vector3 dir = (b - a).normalized;
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(rail, "Fence Rail");
                rail.name = "Rail";
                rail.transform.SetParent(root, false);
                rail.transform.localPosition = mid;
                rail.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
                rail.transform.localScale    = new Vector3(0.07f, 0.07f, len * 0.97f);
                ApplyMat(rail, woodLight);
                Object.DestroyImmediate(rail.GetComponent<Collider>());
            }
        }
    }

    private static void CreatePost(Transform root, Vector3 localXZ, Color col)
    {
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(post, "Fence Post");
        post.name = "Post";
        post.transform.SetParent(root, false);
        post.transform.localPosition = localXZ + Vector3.up * 0.45f;
        post.transform.localScale    = new Vector3(0.14f, 0.9f, 0.14f);
        ApplyMat(post, col);
        Object.DestroyImmediate(post.GetComponent<Collider>());
    }

    // =========================================================================
    // SIGN
    // =========================================================================
    private static void BuildSign(Transform root)
    {
        Color woodDark  = new Color(0.32f, 0.22f, 0.10f);
        Color woodBoard = new Color(0.50f, 0.34f, 0.16f);
        Color ropeCol   = new Color(0.60f, 0.52f, 0.36f);

        float signZ = 3.2f;
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
        Undo.RegisterCreatedObjectUndo(bar, "Sign Crossbar");
        bar.name = "SignCrossbar";
        bar.transform.SetParent(root, false);
        bar.transform.localPosition = new Vector3(0f, 1.42f, signZ);
        bar.transform.localScale    = new Vector3(1.55f, 0.11f, 0.11f);
        ApplyMat(bar, woodDark);
        Object.DestroyImmediate(bar.GetComponent<Collider>());

        foreach (float sx in new[] { -0.50f, 0.50f })
        {
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(rope, "Sign Rope");
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
        Undo.RegisterCreatedObjectUndo(textGO, "Sign Label");
        textGO.transform.SetParent(board.transform, false);
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.58f);
        textGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var tm = textGO.AddComponent<TextMesh>();
        tm.text          = "Apple";
        tm.characterSize = 0.22f;
        tm.fontSize      = 52;
        tm.fontStyle     = FontStyle.Bold;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = Color.white;
    }

    // =========================================================================
    // APPLE BED  —  3 apple trees + fallen apples on ground
    // =========================================================================
    private static readonly Vector3[] TreeSlots =
    {
        new Vector3(-1.4f, 0f,  0.5f),
        new Vector3( 0.0f, 0f,  1.0f),
        new Vector3( 1.4f, 0f, -0.2f),
    };

    const string RealAppleTreePath = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_apples.prefab";
    const string RealAppleFruitPath = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Apple_R1.prefab";

    private static void BuildAppleBed(Transform root)
    {
        // Fallen apples on ground
        BuildFallenFruits(root);

        for (int i = 0; i < TreeSlots.Length; i++)
            BuildAppleTree(root, TreeSlots[i], i);
    }

    private static void BuildAppleTree(Transform root, Vector3 pos, int idx)
    {
        // --- Use real Polytope apple tree as host ---
        var realTree = AssetDatabase.LoadAssetAtPath<GameObject>(RealAppleTreePath);
        if (realTree != null)
        {
            var treeInst = (GameObject)PrefabUtility.InstantiatePrefab(realTree, root);
            Undo.RegisterCreatedObjectUndo(treeInst, "Apple Tree");
            treeInst.name = "AppleTree_" + idx;
            treeInst.transform.localPosition = pos;
            treeInst.transform.localScale    = Vector3.one * Random.Range(0.9f, 1.1f);
            treeInst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        else
        {
            // Fallback: procedural trunk + canopy
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(trunk, "Apple Trunk");
            trunk.name = "AppleTrunk_" + idx;
            trunk.transform.SetParent(root, false);
            trunk.transform.localPosition = pos + Vector3.up * 0.8f;
            trunk.transform.localScale    = new Vector3(0.22f, 0.8f, 0.22f);
            ApplyMat(trunk, new Color(0.38f, 0.26f, 0.12f));
            Object.DestroyImmediate(trunk.GetComponent<Collider>());

            Color canopyDark  = new Color(0.18f, 0.38f, 0.12f);
            Color canopyLight = new Color(0.26f, 0.52f, 0.18f);
            Vector3 canopyBase2 = pos + Vector3.up * 1.9f;
            float[] layerY2     = { 0f, 0.45f, 0.85f };
            float[] layerScale2 = { 1.10f, 0.90f, 0.60f };
            for (int l = 0; l < 3; l++)
            {
                var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(canopy, "Canopy Layer");
                canopy.name = "Canopy_" + l; canopy.transform.SetParent(root, false);
                canopy.transform.localPosition = canopyBase2 + Vector3.up * layerY2[l];
                float s = layerScale2[l];
                canopy.transform.localScale = new Vector3(s, s * 0.82f, s);
                ApplyMat(canopy, l % 2 == 0 ? canopyDark : canopyLight);
                Object.DestroyImmediate(canopy.GetComponent<Collider>());
            }
        }

        // --- Apple clusters scattered on canopy surface ---
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RealAppleFruitPath);
        Vector3 canopyBase = pos + Vector3.up * 2.1f;
        int appleCount = 6;
        for (int a = 0; a < appleCount; a++)
        {
            float ang = a * (360f / appleCount) * Mathf.Deg2Rad;
            float layerYVal = (a % 3) * 0.35f;
            Vector3 applePos = canopyBase + new Vector3(
                Mathf.Sin(ang) * 0.52f,
                layerYVal + 0.1f,
                Mathf.Cos(ang) * 0.52f);

            if (prefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                Undo.RegisterCreatedObjectUndo(inst, "Apple Prefab");
                inst.name = "Apple_" + idx + "_" + a;
                inst.transform.localPosition = applePos;
                inst.transform.localScale    = Vector3.one * 0.55f;
                inst.transform.localRotation = Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f);
            }
            else
            {
                var apple = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(apple, "Apple");
                apple.name = "Apple_" + idx + "_" + a;
                apple.transform.SetParent(root, false);
                apple.transform.localPosition = applePos;
                apple.transform.localScale    = Vector3.one * 0.18f;
                ApplyMat(apple, new Color(0.85f, 0.15f, 0.10f), 0.75f);
                Object.DestroyImmediate(apple.GetComponent<Collider>());

                // Small stem
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(stem, "Apple Stem");
                stem.name = "AppleStem";
                stem.transform.SetParent(root, false);
                stem.transform.localPosition = applePos + Vector3.up * 0.11f;
                stem.transform.localScale    = new Vector3(0.025f, 0.06f, 0.025f);
                ApplyMat(stem, new Color(0.28f, 0.18f, 0.08f));
                Object.DestroyImmediate(stem.GetComponent<Collider>());

                // Tiny leaf
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(leaf, "Apple Leaf");
                leaf.name = "AppleLeaf";
                leaf.transform.SetParent(root, false);
                leaf.transform.localPosition = applePos + new Vector3(0.06f, 0.15f, 0f);
                leaf.transform.localScale    = new Vector3(0.10f, 0.03f, 0.07f);
                ApplyMat(leaf, new Color(0.20f, 0.48f, 0.14f));
                Object.DestroyImmediate(leaf.GetComponent<Collider>());
            }
        }
    }

    private static void BuildFallenFruits(Transform root)
    {
        Color appleRed = new Color(0.85f, 0.15f, 0.10f);
        for (int i = 0; i < 5; i++)
        {
            float ang = i * (360f / 5f) * Mathf.Deg2Rad;
            float r   = 0.8f + (i % 2) * 0.4f;
            var fallen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(fallen, "Fallen Apple");
            fallen.name = "FallenApple_" + i;
            fallen.transform.SetParent(root, false);
            fallen.transform.localPosition = new Vector3(Mathf.Sin(ang) * r, 0.08f, Mathf.Cos(ang) * r);
            fallen.transform.localScale    = Vector3.one * 0.16f;
            ApplyMat(fallen, appleRed, 0.65f);
            Object.DestroyImmediate(fallen.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // SURROUNDING GREENERY
    // =========================================================================
    private static void BuildSurroundingGreenery(Transform root)
    {
        var shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab");

        Color bushDark  = new Color(0.18f, 0.42f, 0.14f);
        Color bushLight = new Color(0.28f, 0.55f, 0.20f);
        float[] rings   = { 3.8f, 4.4f };

        for (int b = 0; b < 12; b++)
        {
            float angle  = b * (360f / 12f) * Mathf.Deg2Rad;
            float r      = rings[b % 2] + Random.Range(-0.25f, 0.25f);
            Vector3 bpos = new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);
            float bscale = Random.Range(0.5f, 0.9f);

            if (shrubPrefab != null)
            {
                var sh = (GameObject)PrefabUtility.InstantiatePrefab(shrubPrefab, root);
                Undo.RegisterCreatedObjectUndo(sh, "Shrub");
                sh.name = "Shrub_" + b;
                sh.transform.localPosition = bpos;
                sh.transform.localScale    = Vector3.one * bscale;
                sh.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                var bushRoot = new GameObject("Bush_" + b);
                Undo.RegisterCreatedObjectUndo(bushRoot, "Bush");
                bushRoot.transform.SetParent(root, false);
                bushRoot.transform.localPosition = bpos;
                for (int l = 0; l < 3; l++)
                {
                    var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Undo.RegisterCreatedObjectUndo(s, "Bush Layer");
                    s.transform.SetParent(bushRoot.transform, false);
                    s.transform.localPosition = new Vector3(Random.Range(-0.1f, 0.1f), l * 0.18f * bscale, Random.Range(-0.1f, 0.1f));
                    float ls = bscale * Random.Range(0.28f, 0.44f);
                    s.transform.localScale = new Vector3(ls, ls * 0.85f, ls);
                    ApplyMat(s, l % 2 == 0 ? bushDark : bushLight);
                    Object.DestroyImmediate(s.GetComponent<Collider>());
                }
            }
        }

        // Grass tufts
        Color grassCol = new Color(0.22f, 0.50f, 0.15f);
        for (int g = 0; g < 20; g++)
        {
            float angle  = g * (360f / 20f) * Mathf.Deg2Rad + Random.Range(-0.1f, 0.1f);
            float r      = 3.2f + Random.Range(-0.15f, 0.3f);
            var tuft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(tuft, "Grass Tuft");
            tuft.name = "GrassTuft_" + g;
            tuft.transform.SetParent(root, false);
            tuft.transform.localPosition = new Vector3(Mathf.Sin(angle) * r, 0.04f, Mathf.Cos(angle) * r);
            tuft.transform.localScale    = new Vector3(Random.Range(0.25f, 0.38f), Random.Range(0.10f, 0.18f), Random.Range(0.20f, 0.32f));
            tuft.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ApplyMat(tuft, grassCol);
            Object.DestroyImmediate(tuft.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // HELPERS
    // =========================================================================
    private static void ApplyMat(GameObject go, Color color, float smoothness = 0.25f)
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

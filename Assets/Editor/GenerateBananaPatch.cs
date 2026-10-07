#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Banana Patch matching the reference image style:
//   • 3 banana palm trees (procedural trunk with lean + frond crown)
//     with real PolyOne banana prefabs hanging in clusters
//   • Wooden post-and-rail fence (PT_Modular_Fence_Wood_01)
//   • Hanging wooden sign labelled "Banana"
//   • Lush surrounding greenery: PT_Generic_Shrub_01_green + PT_Grass_02
//
// Tools → Radiant Orchard → Generate Banana Patch
public static class GenerateBananaPatch
{
    // zone position — matches GenerateOrchardZones angle 100°, radius 22
    const float AngleDeg = 100f;
    const float Radius   = 22f;

    const string FencePrefab  = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    const string ShrubPrefab  = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    const string GrassPrefab  = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
    const string BananaPrefab = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Banana_Y1.prefab";

    // Three palm positions inside the 6×6 bed
    static readonly Vector3[] PalmSlots =
    {
        new Vector3(-1.5f, 0f,  0.8f),
        new Vector3( 0.2f, 0f,  1.2f),
        new Vector3( 1.3f, 0f, -0.5f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Banana Patch")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Banana Patch");
        int g = Undo.GetCurrentGroup();

        float rad = AngleDeg * Mathf.Deg2Rad;
        Vector3 center = new Vector3(Mathf.Sin(rad) * Radius, 0f, Mathf.Cos(rad) * Radius);

        var fruitsGO = GameObject.Find("Fruits");
        Transform fruitsParent = fruitsGO != null ? fruitsGO.transform : null;

        var old = GameObject.Find("Orchard_Banana");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Orchard_Banana");
        Undo.RegisterCreatedObjectUndo(root, "Create Banana Patch");
        if (fruitsParent != null) root.transform.SetParent(fruitsParent, false);
        root.transform.localPosition = center;
        root.transform.localRotation = Quaternion.LookRotation(-center.normalized, Vector3.up);

        BuildFenceRing(root.transform, 3.8f);
        BuildSign(root.transform, "Banana", 4.0f);
        BuildPalmBed(root.transform);
        BuildSurroundingGreenery(root.transform, 4.5f);

        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Banana Patch generated.");
    }

    // =========================================================================
    // FENCE — real Polytope modular fence segments in a square ring
    // =========================================================================
    static void BuildFenceRing(Transform root, float halfSize)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FencePrefab);

        // 4 sides, each side split into 2 segments so spacing looks tight
        float step = halfSize;
        // Segment centres along each side
        (Vector3 pos, float yRot)[] segments =
        {
            // Front (Z+)
            (new Vector3(-step * 0.5f, 0f,  halfSize), 0f),
            (new Vector3( step * 0.5f, 0f,  halfSize), 0f),
            // Right (X+)
            (new Vector3( halfSize, 0f,  step * 0.5f), 90f),
            (new Vector3( halfSize, 0f, -step * 0.5f), 90f),
            // Back (Z-)
            (new Vector3( step * 0.5f, 0f, -halfSize), 180f),
            (new Vector3(-step * 0.5f, 0f, -halfSize), 180f),
            // Left (X-)
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
                // Fallback cube rail
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(rail, "Fence Rail");
                rail.name = "FenceRail";
                rail.transform.SetParent(root, false);
                rail.transform.localPosition = pos + Vector3.up * 0.5f;
                rail.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
                rail.transform.localScale    = new Vector3(step, 0.08f, 0.08f);
                ApplyMat(rail, new Color(0.42f, 0.3f, 0.15f));
                Object.DestroyImmediate(rail.GetComponent<Collider>());
            }
        }
    }

    // =========================================================================
    // SIGN — two uprights + crossbar + rope + hanging board + TextMesh
    // =========================================================================
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
        tm.characterSize = 0.22f;
        tm.fontSize      = 52;
        tm.fontStyle     = FontStyle.Bold;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = Color.white;
    }

    // =========================================================================
    // PALM BED
    // =========================================================================
    static void BuildPalmBed(Transform root)
    {
        var bananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BananaPrefab);

        for (int i = 0; i < PalmSlots.Length; i++)
            BuildPalmTree(root, PalmSlots[i], i, bananaPrefab);
    }

    static void BuildPalmTree(Transform root, Vector3 basePos, int idx, GameObject bananaPrefab)
    {
        // --- Trunk: stacked slightly offset cylinders for a natural lean ---
        Color trunkCol = new Color(0.45f, 0.32f, 0.14f);
        int segments   = 6;
        float segH     = 0.55f;
        float lean     = idx * 7f; // each tree leans a different direction

        for (int s = 0; s < segments; s++)
        {
            float t     = (float)s / (segments - 1);
            float width = Mathf.Lerp(0.20f, 0.10f, t); // taper toward crown
            float ang   = (lean + s * 5f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(ang) * s * 0.04f, s * segH + basePos.y + segH * 0.5f, Mathf.Cos(ang) * s * 0.04f);

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(seg, "Palm Segment");
            seg.name = "TrunkSeg_" + s;
            seg.transform.SetParent(root, false);
            seg.transform.localPosition = basePos + new Vector3(offset.x, (float)s * segH + segH * 0.5f, offset.z);
            seg.transform.localScale    = new Vector3(width, segH * 0.52f, width);
            ApplyMat(seg, trunkCol, 0.1f);
            Object.DestroyImmediate(seg.GetComponent<Collider>());
        }

        Vector3 crownPos = basePos + Vector3.up * (segments * segH);

        // --- Fronds: 7 elongated flat leaf shapes radiating from crown ---
        Color frondDark  = new Color(0.16f, 0.40f, 0.10f);
        Color frondLight = new Color(0.25f, 0.55f, 0.16f);
        int frondCount = 7;

        for (int f = 0; f < frondCount; f++)
        {
            float fAng = f * (360f / frondCount) * Mathf.Deg2Rad;
            float droop = -22f + (f % 2) * 8f;
            Vector3 frondDir = new Vector3(Mathf.Sin(fAng), 0f, Mathf.Cos(fAng));

            var frond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(frond, "Palm Frond");
            frond.name = "Frond_" + f;
            frond.transform.SetParent(root, false);
            frond.transform.localPosition = crownPos + frondDir * 0.4f;
            frond.transform.localRotation = Quaternion.LookRotation(frondDir, Vector3.up) *
                                            Quaternion.Euler(droop, 0f, 0f);
            frond.transform.localScale = new Vector3(0.18f, 0.06f, 1.3f);
            ApplyMat(frond, f % 2 == 0 ? frondDark : frondLight, 0.1f);
            Object.DestroyImmediate(frond.GetComponent<Collider>());
        }

        // --- Banana bunches (real prefab or procedural) ---
        int bunchCount = 2;
        for (int b = 0; b < bunchCount; b++)
        {
            float bAng = (b * 180f + idx * 60f) * Mathf.Deg2Rad;
            Vector3 bunchPos = crownPos + new Vector3(Mathf.Sin(bAng) * 0.35f, -0.3f, Mathf.Cos(bAng) * 0.35f);

            if (bananaPrefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(bananaPrefab, root);
                Undo.RegisterCreatedObjectUndo(inst, "Banana Bunch");
                inst.name = "BananaBunch_" + idx + "_" + b;
                inst.transform.localPosition = bunchPos;
                inst.transform.localScale    = Vector3.one * 1.3f;
                inst.transform.localRotation = Quaternion.Euler(0f, bAng * Mathf.Rad2Deg, 0f);
            }
            else
            {
                // Procedural: fan of 4 capsules
                for (int k = 0; k < 4; k++)
                {
                    var finger = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Undo.RegisterCreatedObjectUndo(finger, "Banana Finger");
                    finger.name = "BananaFinger_" + k;
                    finger.transform.SetParent(root, false);
                    float fan = (k - 1.5f) * 16f;
                    finger.transform.localPosition = bunchPos + new Vector3(0f, -0.1f * k, 0f);
                    finger.transform.localRotation = Quaternion.Euler(80f, bAng * Mathf.Rad2Deg, fan);
                    finger.transform.localScale    = new Vector3(0.09f, 0.28f, 0.09f);
                    ApplyMat(finger, new Color(0.95f, 0.85f, 0.10f), 0.5f);
                    Object.DestroyImmediate(finger.GetComponent<Collider>());
                }
            }
        }
    }

    // =========================================================================
    // SURROUNDING GREENERY — real shrubs + grass
    // =========================================================================
    static void BuildSurroundingGreenery(Transform root, float ringRadius)
    {
        var shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShrubPrefab);
        var grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefab);

        for (int b = 0; b < 14; b++)
        {
            float angle  = b * (360f / 14f) * Mathf.Deg2Rad;
            float r      = ringRadius + (b % 2 == 0 ? 0f : 0.5f) + Random.Range(-0.2f, 0.2f);
            Vector3 pos  = new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);

            if (shrubPrefab != null)
            {
                var sh = (GameObject)PrefabUtility.InstantiatePrefab(shrubPrefab, root);
                Undo.RegisterCreatedObjectUndo(sh, "Shrub");
                sh.name = "Shrub_" + b;
                sh.transform.localPosition = pos;
                sh.transform.localScale    = Vector3.one * Random.Range(0.5f, 1.0f);
                sh.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(bush, "Bush");
                bush.name = "Bush_" + b;
                bush.transform.SetParent(root, false);
                bush.transform.localPosition = pos + Vector3.up * 0.2f;
                bush.transform.localScale    = Vector3.one * Random.Range(0.3f, 0.55f);
                ApplyMat(bush, new Color(0.18f, 0.42f, 0.14f));
                Object.DestroyImmediate(bush.GetComponent<Collider>());
            }

            // Scatter grass tufts nearby
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

    // =========================================================================
    // HELPERS
    // =========================================================================
    static void ApplyMat(GameObject go, Color color, float smoothness = 0.2f)
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

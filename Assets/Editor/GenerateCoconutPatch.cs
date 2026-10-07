#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Coconut Patch matching the reference image style:
//   • 3 coconut palm trees (curving multi-segment trunk + large frond crown)
//     with real PolyOne SM_Fruits_Coconut_1 / Coconut_4 prefabs in the crown
//   • Wooden post-and-rail fence (PT_Modular_Fence_Wood_01)
//   • Hanging wooden sign labelled "Coconut"
//   • Lush surrounding greenery: PT_Generic_Shrub_01_green + PT_Grass_02
//
// Tools → Radiant Orchard → Generate Coconut Patch
public static class GenerateCoconutPatch
{
    const float AngleDeg = 210f; // spare position
    const float Radius   = 23f;

    const string FencePrefab    = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    const string ShrubPrefab    = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    const string GrassPrefab    = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
    const string CoconutPrefab1 = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Coconut_1.prefab";
    const string CoconutPrefab4 = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Coconut_4.prefab";

    static readonly Vector3[] PalmSlots =
    {
        new Vector3(-1.6f, 0f,  0.4f),
        new Vector3( 0.0f, 0f,  1.2f),
        new Vector3( 1.5f, 0f, -0.3f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Coconut Patch")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Coconut Patch");
        int g = Undo.GetCurrentGroup();

        float rad = AngleDeg * Mathf.Deg2Rad;
        Vector3 center = new Vector3(Mathf.Sin(rad) * Radius, 0f, Mathf.Cos(rad) * Radius);

        var fruitsGO = GameObject.Find("Fruits");
        Transform fruitsParent = fruitsGO != null ? fruitsGO.transform : null;

        var old = GameObject.Find("Orchard_Coconut");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Orchard_Coconut");
        Undo.RegisterCreatedObjectUndo(root, "Create Coconut Patch");
        if (fruitsParent != null) root.transform.SetParent(fruitsParent, false);
        root.transform.localPosition = center;
        root.transform.localRotation = Quaternion.LookRotation(-center.normalized, Vector3.up);

        BuildFenceRing(root.transform, 3.5f);
        BuildSign(root.transform, "Coconut", 3.7f);
        BuildPalmBed(root.transform);
        BuildSurroundingGreenery(root.transform, 4.2f);

        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Coconut Patch generated.");
    }

    static void BuildFenceRing(Transform root, float halfSize)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FencePrefab);
        float step = halfSize;

        (Vector3 pos, float yRot)[] segs =
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

        foreach (var (pos, yRot) in segs)
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
                rail.name = "FenceRail"; rail.transform.SetParent(root, false);
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
    // PALM BED — tall curving coconut palms
    // =========================================================================
    static void BuildPalmBed(Transform root)
    {
        var coco1 = AssetDatabase.LoadAssetAtPath<GameObject>(CoconutPrefab1);
        var coco4 = AssetDatabase.LoadAssetAtPath<GameObject>(CoconutPrefab4);

        for (int i = 0; i < PalmSlots.Length; i++)
            BuildCoconutPalm(root, PalmSlots[i], i, i % 2 == 0 ? coco1 : coco4);
    }

    static void BuildCoconutPalm(Transform root, Vector3 basePos, int idx, GameObject cocoPrefab)
    {
        Color trunkCol = new Color(0.50f, 0.36f, 0.18f);
        Color ringCol  = new Color(0.40f, 0.28f, 0.12f);
        int   segments = 8;
        float segH     = 0.55f;
        float lean     = idx * 40f; // lean direction varies per tree

        // Stacked curved trunk segments
        Vector3 prev = basePos;
        for (int s = 0; s < segments; s++)
        {
            float t     = (float)s / (segments - 1);
            float width = Mathf.Lerp(0.24f, 0.09f, t);
            float ang   = lean * Mathf.Deg2Rad;
            float curve = s * 0.035f; // accumulating lean

            Vector3 curPos = basePos + new Vector3(
                Mathf.Sin(ang) * curve,
                s * segH + segH * 0.5f,
                Mathf.Cos(ang) * curve);

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(seg, "Palm Seg");
            seg.name = "PalmSeg_" + s; seg.transform.SetParent(root, false);
            seg.transform.localPosition = curPos;

            // Tilt segment toward lean direction
            Vector3 dir = (curPos - prev).normalized;
            if (s > 0) seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            seg.transform.localScale = new Vector3(width, segH * 0.52f, width);
            ApplyMat(seg, trunkCol, 0.12f);
            Object.DestroyImmediate(seg.GetComponent<Collider>());

            // Ring scar at each segment junction
            if (s > 0)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(ring, "Ring");
                ring.name = "Ring_" + s; ring.transform.SetParent(root, false);
                ring.transform.localPosition = curPos - Vector3.up * (segH * 0.46f);
                ring.transform.localRotation = seg.transform.localRotation;
                ring.transform.localScale    = new Vector3(width * 1.12f, 0.04f, width * 1.12f);
                ApplyMat(ring, ringCol, 0.08f);
                Object.DestroyImmediate(ring.GetComponent<Collider>());
            }

            prev = curPos;
        }

        // Crown position
        float crownCurve = segments * 0.035f;
        float crownAng   = lean * Mathf.Deg2Rad;
        Vector3 crown = basePos + new Vector3(
            Mathf.Sin(crownAng) * crownCurve,
            segments * segH,
            Mathf.Cos(crownAng) * crownCurve);

        // Fronds — 8 large arching leaf blades
        Color frondDark  = new Color(0.16f, 0.42f, 0.10f);
        Color frondLight = new Color(0.25f, 0.56f, 0.17f);
        int frondCount = 8;

        for (int f = 0; f < frondCount; f++)
        {
            float fa    = f * (360f / frondCount) * Mathf.Deg2Rad;
            float droop = -28f + (f % 2) * 10f;
            Vector3 dir = new Vector3(Mathf.Sin(fa), 0f, Mathf.Cos(fa));

            var frond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(frond, "Frond");
            frond.name = "Frond_" + f; frond.transform.SetParent(root, false);
            frond.transform.localPosition = crown + dir * 0.5f;
            frond.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(droop, 0f, 0f);
            frond.transform.localScale    = new Vector3(0.20f, 0.07f, 1.6f);
            ApplyMat(frond, f % 2 == 0 ? frondDark : frondLight, 0.1f);
            Object.DestroyImmediate(frond.GetComponent<Collider>());
        }

        // Coconuts nestled at crown base
        int cocoCount = 3;
        for (int c = 0; c < cocoCount; c++)
        {
            float ca = c * (360f / cocoCount) * Mathf.Deg2Rad;
            Vector3 cocoPos = crown + new Vector3(Mathf.Sin(ca) * 0.28f, -0.15f, Mathf.Cos(ca) * 0.28f);

            if (cocoPrefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(cocoPrefab, root);
                Undo.RegisterCreatedObjectUndo(inst, "Coconut");
                inst.name = "Coconut_" + idx + "_" + c;
                inst.transform.localPosition = cocoPos;
                inst.transform.localScale    = Vector3.one * 0.65f;
                inst.transform.localRotation = Quaternion.Euler(0f, ca * Mathf.Rad2Deg, 0f);
            }
            else
            {
                var coco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(coco, "Coconut");
                coco.name = "Coconut"; coco.transform.SetParent(root, false);
                coco.transform.localPosition = cocoPos;
                coco.transform.localScale    = new Vector3(0.18f, 0.22f, 0.18f);
                ApplyMat(coco, new Color(0.40f, 0.28f, 0.12f), 0.3f);
                Object.DestroyImmediate(coco.GetComponent<Collider>());
            }
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
                sh.name = "Shrub_" + b; sh.transform.localPosition = pos;
                sh.transform.localScale = Vector3.one * Random.Range(0.5f, 0.9f);
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

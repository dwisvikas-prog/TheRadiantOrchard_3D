#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Strawberry Patch matching the reference image style:
//   • Low ground-cover bed with 8 strawberry plants
//     (rosette of flat leaves + red heart-shaped fruit, no prefab — procedural)
//   • Small shrub borders using PT_Generic_Shrub_01_green
//   • Wooden post-and-rail fence (PT_Modular_Fence_Wood_01)
//   • Hanging wooden sign labelled "Strawberry"
//   • PT_Grass_02 tufts around the fence perimeter
//
// Tools → Radiant Orchard → Generate Strawberry Patch
public static class GenerateStrawberryPatch
{
    const float AngleDeg = 295f;
    const float Radius   = 24f;

    const string FencePrefab = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    const string ShrubPrefab = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    const string GrassPrefab = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";

    static readonly Vector3[] PlantSlots =
    {
        new Vector3(-1.1f, 0f,  0.7f),
        new Vector3( 0.0f, 0f,  1.0f),
        new Vector3( 1.1f, 0f,  0.6f),
        new Vector3(-0.6f, 0f, -0.2f),
        new Vector3( 0.7f, 0f, -0.1f),
        new Vector3(-1.0f, 0f, -0.9f),
        new Vector3( 0.1f, 0f, -1.0f),
        new Vector3( 1.0f, 0f, -0.9f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Strawberry Patch")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Strawberry Patch");
        int g = Undo.GetCurrentGroup();

        float rad = AngleDeg * Mathf.Deg2Rad;
        Vector3 center = new Vector3(Mathf.Sin(rad) * Radius, 0f, Mathf.Cos(rad) * Radius);

        var fruitsGO = GameObject.Find("Fruits");
        Transform fruitsParent = fruitsGO != null ? fruitsGO.transform : null;

        var old = GameObject.Find("Orchard_Strawberry");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Orchard_Strawberry");
        Undo.RegisterCreatedObjectUndo(root, "Create Strawberry Patch");
        if (fruitsParent != null) root.transform.SetParent(fruitsParent, false);
        root.transform.localPosition = center;
        root.transform.localRotation = Quaternion.LookRotation(-center.normalized, Vector3.up);

        BuildFenceRing(root.transform, 2.5f);
        BuildSign(root.transform, "Strawberry", 2.7f);
        BuildPlantBed(root.transform);
        BuildGroundCover(root.transform);
        BuildSurroundingGreenery(root.transform, 3.2f);

        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Strawberry Patch generated.");
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
        tm.text = label; tm.characterSize = 0.18f; tm.fontSize = 48;
        tm.fontStyle = FontStyle.Bold; tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center; tm.color = Color.white;
    }

    // =========================================================================
    // PLANT BED — low rosette plants with strawberry fruits
    // =========================================================================
    static void BuildPlantBed(Transform root)
    {
        for (int i = 0; i < PlantSlots.Length; i++)
            BuildStrawberryPlant(root, PlantSlots[i], i);
    }

    static void BuildStrawberryPlant(Transform root, Vector3 pos, int idx)
    {
        float sz = Random.Range(0.80f, 1.05f);
        Color leafGreen = new Color(0.18f, 0.48f, 0.13f);
        Color leafLight = new Color(0.28f, 0.60f, 0.20f);

        // Leaf rosette: 5 flat ellipsoid leaves radiating from base
        int leafCount = 5;
        for (int l = 0; l < leafCount; l++)
        {
            float ang  = l * (360f / leafCount) * Mathf.Deg2Rad;
            float droop = -18f;
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(leaf, "Leaf");
            leaf.name = "StrawLeaf_" + l; leaf.transform.SetParent(root, false);
            Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            leaf.transform.localPosition = pos + Vector3.up * 0.05f + dir * 0.12f * sz;
            leaf.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(droop, 0f, 0f);
            leaf.transform.localScale    = new Vector3(0.12f * sz, 0.04f * sz, 0.26f * sz);
            ApplyMat(leaf, l % 2 == 0 ? leafGreen : leafLight, 0.1f);
            Object.DestroyImmediate(leaf.GetComponent<Collider>());
        }

        // 2-3 fruits per plant
        int fruitCount = 2 + (idx % 2);
        for (int f = 0; f < fruitCount; f++)
        {
            float fang = f * (360f / fruitCount) * Mathf.Deg2Rad + idx * 0.8f;
            float fr   = 0.10f * sz;
            Vector3 fp = pos + new Vector3(Mathf.Sin(fang) * fr, sz * 0.22f, Mathf.Cos(fang) * fr);

            // Heart-shaped strawberry: main body + indent dimple at top
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(body, "Strawberry Body");
            body.name = "StrawBody"; body.transform.SetParent(root, false);
            body.transform.localPosition = fp;
            body.transform.localScale    = new Vector3(0.13f * sz, 0.165f * sz, 0.12f * sz);
            ApplyMat(body, new Color(0.88f, 0.14f, 0.18f), 0.7f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Cap (green sepal)
            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(cap, "Strawberry Cap");
            cap.name = "StrawCap"; cap.transform.SetParent(root, false);
            cap.transform.localPosition = fp + Vector3.up * sz * 0.10f;
            cap.transform.localScale    = new Vector3(0.10f * sz, 0.04f * sz, 0.10f * sz);
            ApplyMat(cap, new Color(0.18f, 0.48f, 0.13f), 0.1f);
            Object.DestroyImmediate(cap.GetComponent<Collider>());
        }
    }

    // Runners (horizontal vine-like stolons between plants)
    static void BuildGroundCover(Transform root)
    {
        Color runnerCol = new Color(0.22f, 0.52f, 0.15f);
        for (int r = 0; r < 10; r++)
        {
            var runner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(runner, "Runner");
            runner.name = "Runner_" + r; runner.transform.SetParent(root, false);
            runner.transform.localPosition = new Vector3(Random.Range(-1.3f, 1.3f), 0.015f, Random.Range(-1.0f, 1.0f));
            runner.transform.localScale    = new Vector3(0.05f, 0.025f, Random.Range(0.35f, 0.70f));
            runner.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ApplyMat(runner, runnerCol, 0.1f);
            Object.DestroyImmediate(runner.GetComponent<Collider>());
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
                sh.transform.localScale = Vector3.one * Random.Range(0.5f, 0.85f);
                sh.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(bush, "Bush");
                bush.name = "Bush_" + b; bush.transform.SetParent(root, false);
                bush.transform.localPosition = pos + Vector3.up * 0.18f;
                bush.transform.localScale    = Vector3.one * 0.32f;
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

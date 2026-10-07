#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Dragonfruit Patch matching the reference image style:
//   • 5 dragonfruit cactus columns (stacked cylinder segments with ridges)
//     with real PolyOne SM_Fruits_Dragonfruit_R2 prefabs on top
//   • Wooden post-and-rail fence (PT_Modular_Fence_Wood_01)
//   • Hanging wooden sign labelled "Dragonfruit"
//   • Lush surrounding greenery: PT_Generic_Shrub_01_green + PT_Grass_02
//
// Tools → Radiant Orchard → Generate Dragonfruit Patch
public static class GenerateDragonfruitPatch
{
    const float AngleDeg = 195f;  // spare position on island
    const float Radius   = 22f;

    const string FencePrefab       = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    const string ShrubPrefab       = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    const string GrassPrefab       = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
    const string DragonfruitPrefab = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Dragonfruit_R2.prefab";

    static readonly Vector3[] PlantSlots =
    {
        new Vector3(-1.0f, 0f,  0.6f),
        new Vector3( 0.0f, 0f,  1.0f),
        new Vector3( 1.0f, 0f,  0.5f),
        new Vector3(-0.5f, 0f, -0.7f),
        new Vector3( 0.7f, 0f, -0.6f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Dragonfruit Patch")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Dragonfruit Patch");
        int g = Undo.GetCurrentGroup();

        float rad = AngleDeg * Mathf.Deg2Rad;
        Vector3 center = new Vector3(Mathf.Sin(rad) * Radius, 0f, Mathf.Cos(rad) * Radius);

        var fruitsGO = GameObject.Find("Fruits");
        Transform fruitsParent = fruitsGO != null ? fruitsGO.transform : null;

        var old = GameObject.Find("Orchard_Dragonfruit");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Orchard_Dragonfruit");
        Undo.RegisterCreatedObjectUndo(root, "Create Dragonfruit Patch");
        if (fruitsParent != null) root.transform.SetParent(fruitsParent, false);
        root.transform.localPosition = center;
        root.transform.localRotation = Quaternion.LookRotation(-center.normalized, Vector3.up);

        BuildFenceRing(root.transform, 2.8f);
        BuildSign(root.transform, "Dragonfruit", 3.0f);
        BuildPlantBed(root.transform);
        BuildSurroundingGreenery(root.transform, 3.6f);

        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Dragonfruit Patch generated.");
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
        tm.text = label; tm.characterSize = 0.16f; tm.fontSize = 46;
        tm.fontStyle = FontStyle.Bold; tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center; tm.color = Color.white;
    }

    // =========================================================================
    // PLANT BED — cactus columns with ridges + dragonfruit on top
    // =========================================================================
    static void BuildPlantBed(Transform root)
    {
        var dfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DragonfruitPrefab);

        for (int i = 0; i < PlantSlots.Length; i++)
            BuildCactusColumn(root, PlantSlots[i], i, dfPrefab);
    }

    static void BuildCactusColumn(Transform root, Vector3 pos, int idx, GameObject dfPrefab)
    {
        float sz      = Random.Range(0.88f, 1.10f);
        int columns   = 1 + (idx % 2); // 1 or 2 branches
        Color cactus  = new Color(0.22f, 0.52f, 0.18f);
        Color ribCol  = new Color(0.18f, 0.42f, 0.14f);

        for (int col = 0; col < columns; col++)
        {
            float colOff = columns == 2 ? (col == 0 ? -0.18f : 0.18f) : 0f;
            float colH   = columns == 2 && col == 1 ? 0.5f : 0f; // second branch shorter
            Vector3 cpos = pos + new Vector3(colOff, 0f, 0f);

            int segCount = 4;
            float segH   = 0.32f * sz;

            for (int s = 0; s < segCount; s++)
            {
                float width = Mathf.Lerp(0.22f, 0.14f, (float)s / (segCount - 1)) * sz;
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(seg, "Cactus Seg");
                seg.name = "CactusSeg_" + s; seg.transform.SetParent(root, false);
                seg.transform.localPosition = cpos + Vector3.up * ((s + colH) * segH + segH * 0.5f);
                seg.transform.localScale    = new Vector3(width, segH * 0.52f, width);
                ApplyMat(seg, cactus, 0.12f);
                Object.DestroyImmediate(seg.GetComponent<Collider>());

                // 4 ribs/ridges radiating outward around each segment
                for (int rib = 0; rib < 4; rib++)
                {
                    float ra = rib * 90f * Mathf.Deg2Rad;
                    var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Undo.RegisterCreatedObjectUndo(r, "Rib");
                    r.name = "Rib"; r.transform.SetParent(root, false);
                    r.transform.localPosition = cpos + Vector3.up * ((s + colH) * segH + segH * 0.5f) +
                                                new Vector3(Mathf.Sin(ra) * width * 0.9f, 0f, Mathf.Cos(ra) * width * 0.9f);
                    r.transform.localRotation = Quaternion.Euler(0f, rib * 90f, 0f);
                    r.transform.localScale    = new Vector3(0.025f, segH * 1.0f, 0.06f);
                    ApplyMat(r, ribCol, 0.1f);
                    Object.DestroyImmediate(r.GetComponent<Collider>());
                }
            }

            // Fruit on top
            Vector3 fruitPos = cpos + Vector3.up * ((segCount + colH) * segH);

            if (dfPrefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(dfPrefab, root);
                Undo.RegisterCreatedObjectUndo(inst, "Dragonfruit");
                inst.name = "Dragonfruit_" + idx + "_" + col;
                inst.transform.localPosition = fruitPos;
                inst.transform.localScale    = Vector3.one * sz * 1.2f;
                inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                // Procedural: bright pink oval + green scale tips
                var fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(fruit, "Dragonfruit");
                fruit.name = "Dragonfruit"; fruit.transform.SetParent(root, false);
                fruit.transform.localPosition = fruitPos;
                fruit.transform.localScale    = new Vector3(0.22f * sz, 0.28f * sz, 0.22f * sz);
                ApplyMat(fruit, new Color(0.92f, 0.18f, 0.45f), 0.6f);
                Object.DestroyImmediate(fruit.GetComponent<Collider>());

                // Scale tips around fruit
                for (int sc = 0; sc < 6; sc++)
                {
                    float sa = sc * 60f * Mathf.Deg2Rad;
                    float sh = (sc % 2 == 0 ? 0f : 0.1f) * sz;
                    var scale = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Undo.RegisterCreatedObjectUndo(scale, "Scale");
                    scale.name = "Scale"; scale.transform.SetParent(root, false);
                    scale.transform.localPosition = fruitPos + new Vector3(Mathf.Sin(sa) * 0.13f * sz, sh, Mathf.Cos(sa) * 0.13f * sz);
                    scale.transform.localRotation = Quaternion.Euler(-30f, sc * 60f, 0f);
                    scale.transform.localScale    = new Vector3(0.04f * sz, 0.08f * sz, 0.04f * sz);
                    ApplyMat(scale, new Color(0.20f, 0.50f, 0.14f), 0.1f);
                    Object.DestroyImmediate(scale.GetComponent<Collider>());
                }
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

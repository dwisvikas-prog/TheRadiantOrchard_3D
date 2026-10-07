#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Avocado Patch matching the reference image style:
//   • 3 avocado trees using PT_Fruit_Tree_01_green as host
//     with real PolyOne SM_Fruits_Avocado_1 / Avocado_2 prefabs as the crop
//   • Wooden post-and-rail fence (PT_Modular_Fence_Wood_01)
//   • Hanging wooden sign labelled "Avocado"
//   • Lush surrounding greenery: PT_Generic_Shrub_01_green + PT_Grass_02
//
// Tools → Radiant Orchard → Generate Avocado Patch
public static class GenerateAvocadoPatch
{
    const float AngleDeg = 160f;  // spare position on island
    const float Radius   = 22f;

    const string FencePrefab    = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    const string TreePrefab     = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab";
    const string ShrubPrefab    = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    const string GrassPrefab    = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
    const string AvocadoPrefab1 = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Avocado_1.prefab";
    const string AvocadoPrefab2 = "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Avocado_2.prefab";

    static readonly Vector3[] TreeSlots =
    {
        new Vector3(-1.4f, 0f,  0.5f),
        new Vector3( 0.1f, 0f,  1.1f),
        new Vector3( 1.4f, 0f, -0.3f),
    };

    [MenuItem("Tools/Radiant Orchard/Generate Avocado Patch")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Avocado Patch");
        int g = Undo.GetCurrentGroup();

        float rad = AngleDeg * Mathf.Deg2Rad;
        Vector3 center = new Vector3(Mathf.Sin(rad) * Radius, 0f, Mathf.Cos(rad) * Radius);

        var fruitsGO = GameObject.Find("Fruits");
        Transform fruitsParent = fruitsGO != null ? fruitsGO.transform : null;

        var old = GameObject.Find("Orchard_Avocado");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("Orchard_Avocado");
        Undo.RegisterCreatedObjectUndo(root, "Create Avocado Patch");
        if (fruitsParent != null) root.transform.SetParent(fruitsParent, false);
        root.transform.localPosition = center;
        root.transform.localRotation = Quaternion.LookRotation(-center.normalized, Vector3.up);

        BuildFenceRing(root.transform, 3.0f);
        BuildSign(root.transform, "Avocado", 3.2f);
        BuildTreeBed(root.transform);
        BuildSurroundingGreenery(root.transform, 3.8f);

        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Avocado Patch generated.");
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

    static void BuildTreeBed(Transform root)
    {
        var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefab);
        var avoPrefab1 = AssetDatabase.LoadAssetAtPath<GameObject>(AvocadoPrefab1);
        var avoPrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>(AvocadoPrefab2);

        for (int i = 0; i < TreeSlots.Length; i++)
            BuildAvocadoTree(root, TreeSlots[i], i, treePrefab, i % 2 == 0 ? avoPrefab1 : avoPrefab2);
    }

    static void BuildAvocadoTree(Transform root, Vector3 pos, int idx, GameObject treePrefab, GameObject avoPrefab)
    {
        if (treePrefab != null)
        {
            var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, root);
            Undo.RegisterCreatedObjectUndo(tree, "Avocado Tree");
            tree.name = "AvocadoTree_" + idx;
            tree.transform.localPosition = pos;
            tree.transform.localScale    = Vector3.one * Random.Range(0.9f, 1.1f);
            tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        else
        {
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(trunk, "Trunk");
            trunk.name = "Trunk"; trunk.transform.SetParent(root, false);
            trunk.transform.localPosition = pos + Vector3.up * 0.9f;
            trunk.transform.localScale    = new Vector3(0.22f, 0.9f, 0.22f);
            ApplyMat(trunk, new Color(0.35f, 0.22f, 0.10f), 0.1f);
            Object.DestroyImmediate(trunk.GetComponent<Collider>());

            Color c1 = new Color(0.18f, 0.40f, 0.12f);
            Color c2 = new Color(0.25f, 0.52f, 0.18f);
            Vector3 cb = pos + Vector3.up * 2.1f;
            for (int l = 0; l < 3; l++)
            {
                var can = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(can, "Canopy");
                can.name = "Canopy_" + l; can.transform.SetParent(root, false);
                can.transform.localPosition = cb + Vector3.up * l * 0.42f;
                float s = Mathf.Lerp(1.1f, 0.52f, (float)l / 2f);
                can.transform.localScale = new Vector3(s, s * 0.85f, s);
                ApplyMat(can, l % 2 == 0 ? c1 : c2, 0.1f);
                Object.DestroyImmediate(can.GetComponent<Collider>());
            }
        }

        // Avocado fruits hanging from canopy — teardrop (wide sphere top, pointy bottom)
        Vector3 canopyBase = pos + Vector3.up * 2.1f;
        int fruitCount = 5;
        for (int f = 0; f < fruitCount; f++)
        {
            float ang = f * (360f / fruitCount) * Mathf.Deg2Rad;
            float h   = (f % 3) * 0.30f;
            Vector3 fp = canopyBase + new Vector3(Mathf.Sin(ang) * 0.48f, h, Mathf.Cos(ang) * 0.48f);

            if (avoPrefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(avoPrefab, root);
                Undo.RegisterCreatedObjectUndo(inst, "Avocado");
                inst.name = "Avocado_" + idx + "_" + f;
                inst.transform.localPosition = fp;
                inst.transform.localScale    = Vector3.one * 0.55f;
                inst.transform.localRotation = Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f);
            }
            else
            {
                // Teardrop: upper sphere + lower cone approximation
                var avo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(avo, "Avocado");
                avo.name = "Avocado"; avo.transform.SetParent(root, false);
                avo.transform.localPosition = fp;
                avo.transform.localScale    = new Vector3(0.12f, 0.20f, 0.12f);
                ApplyMat(avo, new Color(0.15f, 0.30f, 0.12f), 0.45f); // dark avocado green
                Object.DestroyImmediate(avo.GetComponent<Collider>());
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

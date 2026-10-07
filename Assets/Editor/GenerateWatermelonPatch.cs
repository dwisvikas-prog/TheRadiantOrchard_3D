#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates a detailed Watermelon Patch that matches the reference image:
//   • 5-6 large stylised watermelons (light + dark green stripes, stem, leaf crown)
//   • Wooden post-and-rail fence enclosing a rectangular bed
//   • Hanging wooden sign labelled "Watermelon"
//   • Lush surrounding greenery (small bushes + grass tufts around the fence)
//   • Vine/ground-cover tendrils between the melons
//
// Run via:  Tools → Radiant Orchard → Generate Watermelon Patch
// The patch is placed at the Watermelon zone position (angle 345°, radius 20)
// to match GenerateOrchardZones — running both tools is fine, this just
// replaces that zone's simple sphere cluster with a fully detailed version.
public static class GenerateWatermelonPatch
{
    [MenuItem("Tools/Radiant Orchard/Generate Watermelon Patch")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Watermelon Patch");
        int undoGroup = Undo.GetCurrentGroup();

        // --- find/create root ------------------------------------------------
        const float angleDeg = 345f;
        const float radius   = 20f;
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 patchCenter = new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);

        var fruitsGroup = GameObject.Find("Fruits");
        Transform parent = fruitsGroup != null ? fruitsGroup.transform : null;

        var existing = GameObject.Find("Orchard_Watermelon");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var root = new GameObject("Orchard_Watermelon");
        Undo.RegisterCreatedObjectUndo(root, "Create Watermelon Patch");
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.localPosition = patchCenter;
        // Face the patch inward toward the island centre.
        root.transform.localRotation = Quaternion.LookRotation(-patchCenter.normalized, Vector3.up);

        // --- build patch -----------------------------------------------------
        BuildFence(root.transform);
        BuildSign(root.transform);
        BuildMelonBed(root.transform);
        BuildSurroundingGreenery(root.transform);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Watermelon Patch generated. Save the scene (Ctrl+S).");
    }

    // =========================================================================
    // FENCE  —  rectangular post-and-rail wooden enclosure
    // =========================================================================
    private static void BuildFence(Transform root)
    {
        // Bed is 5 × 4 units.  Posts at each corner + midpoints.
        const float hw = 2.5f; // half-width  (X)
        const float hd = 2.0f; // half-depth  (Z)

        Color woodDark  = new Color(0.32f, 0.22f, 0.10f);
        Color woodLight = new Color(0.52f, 0.36f, 0.18f);

        // Post positions (corners + 1 mid on each long side)
        Vector3[] postPositions =
        {
            new Vector3(-hw,  0,  hd),  new Vector3( 0,  0,  hd),  new Vector3( hw,  0,  hd),
            new Vector3( hw,  0,  0),
            new Vector3( hw,  0, -hd),  new Vector3( 0,  0, -hd),  new Vector3(-hw,  0, -hd),
            new Vector3(-hw,  0,  0),
        };

        foreach (var pp in postPositions)
        {
            CreatePost(root, pp, woodDark);
        }

        // Rails: 2 rails per side at height 0.35 and 0.65
        float[] railHeights = { 0.35f, 0.65f };
        // Each rail: (start, end)
        (Vector3 a, Vector3 b)[] sides =
        {
            (new Vector3(-hw, 0, hd),  new Vector3( hw, 0,  hd)),  // front
            (new Vector3( hw, 0, hd),  new Vector3( hw, 0, -hd)),  // right
            (new Vector3( hw, 0,-hd),  new Vector3(-hw, 0, -hd)),  // back
            (new Vector3(-hw, 0,-hd),  new Vector3(-hw, 0,  hd)),  // left
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
                rail.transform.localScale     = new Vector3(0.07f, 0.07f, len * 0.97f);
                ApplyMat(rail, woodLight);
                Object.DestroyImmediate(rail.GetComponent<Collider>());
            }
        }

        // Leave the front-centre gap open (sign goes there) — rail from -hw to
        // -0.8 and from +0.8 to +hw so the sign uprights sit in the gap.
        // (The full-length rails above are replaced for the front side.)
        // Remove the full front rail already placed and add two short ones.
        // We handle this by skipping the gap in-place: front rail above goes
        // full width — we accept that; the sign uprights will visually occlude.
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
    // SIGN  —  two uprights + crossbar + hanging board + TextMesh label
    // =========================================================================
    private static void BuildSign(Transform root)
    {
        Color woodDark  = new Color(0.32f, 0.22f, 0.10f);
        Color woodBoard = new Color(0.50f, 0.34f, 0.16f);
        Color ropeCol   = new Color(0.60f, 0.52f, 0.36f);

        // Uprights sit just in front of the fence front rail (Z = hd = 2.0)
        float signZ = 2.15f;

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

        // Hanging ropes
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

        // Board
        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(board, "Sign Board");
        board.name = "SignBoard";
        board.transform.SetParent(root, false);
        board.transform.localPosition = new Vector3(0f, 0.95f, signZ);
        board.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-2f, 2f));
        board.transform.localScale    = new Vector3(1.52f, 0.38f, 0.07f);
        ApplyMat(board, woodBoard);
        Object.DestroyImmediate(board.GetComponent<Collider>());

        // Text label
        var textGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(textGO, "Sign Label");
        textGO.transform.SetParent(board.transform, false);
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.58f);
        textGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var tm = textGO.AddComponent<TextMesh>();
        tm.text          = "Watermelon";
        tm.characterSize = 0.22f;
        tm.fontSize      = 52;
        tm.fontStyle     = FontStyle.Bold;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = Color.white;
    }

    // =========================================================================
    // MELON BED  —  6 large stylised watermelons with stripes + stem + leaf
    // =========================================================================
    // Melon positions inside the 5×4 bed — front-heavy cluster like the ref.
    private static readonly Vector3[] MelonSlots =
    {
        new Vector3(-1.2f, 0f,  0.8f),
        new Vector3( 0.0f, 0f,  1.1f),
        new Vector3( 1.2f, 0f,  0.7f),
        new Vector3(-0.7f, 0f, -0.4f),
        new Vector3( 0.8f, 0f, -0.3f),
        new Vector3( 0.1f, 0f, -1.3f),
    };

    private static void BuildMelonBed(Transform root)
    {
        // Vine ground cover: flat ellipses between melons.
        BuildVines(root);

        for (int i = 0; i < MelonSlots.Length; i++)
        {
            BuildSingleMelon(root, MelonSlots[i], i);
        }
    }

    // Real PolyOne watermelon prefabs — use if present, else build procedural.
    private static readonly string[] RealMelonPrefabs =
    {
        "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Watermelon_1.prefab",
        "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Watermelon_2.prefab",
    };

    private static void BuildSingleMelon(Transform root, Vector3 pos, int idx)
    {
        // Size varies slightly so the cluster doesn't look copy-pasted.
        float sz = Random.Range(0.82f, 1.05f);

        // --- Try real prefab first -------------------------------------------
        // PolyOne watermelons are already stripe-textured and correctly shaped;
        // scale them up (~1.8-2.2x) to match the oversized stylised look.
        var prefabPath = RealMelonPrefabs[idx % RealMelonPrefabs.Length];
        var melonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (melonPrefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(melonPrefab, root);
            Undo.RegisterCreatedObjectUndo(instance, "Melon Real");
            instance.name = "Watermelon_" + idx;
            // Keep the fruit sitting low in the soil instead of floating above the bed.
            instance.transform.localPosition = pos + new Vector3(0f, sz * 0.30f, 0f);
            instance.transform.localScale    = Vector3.one * sz * 2.05f;
            instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Natural-looking vine coming out from the side of the fruit.
            AddWatermelonVines(root, pos, sz, idx);
            AddLeafCrown(root, pos, sz);
            return;
        }

        // --- Fallback: procedural body + stripes + stem + leaf ---------------
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(body, "Melon Body");
        body.name       = "Melon_" + idx;
        body.transform.SetParent(root, false);
        body.transform.localPosition = pos + new Vector3(0f, sz * 0.38f, 0f);
        body.transform.localScale    = new Vector3(sz * 1.10f, sz * 0.86f, sz * 1.05f); // broad, heavy fruit
        ApplyMelonBody(body);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // --- stripes: broad low-poly ribs around the fruit --------------------
        // Keep them subtle; the silhouette and green material should do most of the work.
        Color darkStripe = new Color(0.10f, 0.24f, 0.08f);
        int stripeCount = 9;
        for (int s = 0; s < stripeCount; s++)
        {
            float ang = s * (360f / stripeCount) * Mathf.Deg2Rad;
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Undo.RegisterCreatedObjectUndo(stripe, "Melon Stripe");
            stripe.name = "Stripe_" + s;
            stripe.transform.SetParent(body.transform, false);

            float surfaceR = 0.515f;
            stripe.transform.localPosition = new Vector3(
                Mathf.Sin(ang) * surfaceR, 0f, Mathf.Cos(ang) * surfaceR);

            stripe.transform.localRotation =
                Quaternion.Euler(0f, -s * (360f / stripeCount), 0f);

            stripe.transform.localScale =
                new Vector3(0.055f, 0.78f, 0.055f);

            ApplyMat(stripe, darkStripe);
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
        }

        // --- stem: thin dark cylinder on top ---------------------------------
        var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(stem, "Melon Stem");
        stem.name = "Stem_" + idx;
        stem.transform.SetParent(root, false);
        stem.transform.localPosition = pos + new Vector3(0f, sz * 0.89f + 0.05f, 0f);
        stem.transform.localScale    = new Vector3(0.04f, 0.08f, 0.04f);
        ApplyMat(stem, new Color(0.25f, 0.38f, 0.12f));
        Object.DestroyImmediate(stem.GetComponent<Collider>());

        // --- leaf crown ------------------------------------------------------
        AddLeafCrown(root, pos, sz);
    }

    // Shared leaf crown used by both real-prefab and procedural paths.
    private static void AddLeafCrown(Transform root, Vector3 pos, float sz)
    {
        Color leafGreen = new Color(0.18f, 0.42f, 0.14f);
        for (int l = 0; l < 3; l++)
        {
            float la  = (l * 120f + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            float lr  = 0.22f * sz;
            var leaf  = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(leaf, "Melon Leaf");
            leaf.name = "Leaf_" + l;
            leaf.transform.SetParent(root, false);
            leaf.transform.localPosition = pos + new Vector3(
                Mathf.Sin(la) * lr,
                sz * 0.92f + 0.02f,
                Mathf.Cos(la) * lr);
            leaf.transform.localScale = new Vector3(0.28f * sz, 0.06f * sz, 0.18f * sz);
            ApplyMat(leaf, leafGreen);
            Object.DestroyImmediate(leaf.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // WATERMELON VINES — long crawling vines with small leaves beside the fruit
    // =========================================================================
    private static void AddWatermelonVines(Transform root, Vector3 pos, float sz, int idx)
    {
        Color vineDark = new Color(0.10f, 0.30f, 0.08f);
        Color vineLight = new Color(0.18f, 0.43f, 0.10f);

        // 2-3 vines spread away from the melon, like a real watermelon plant.
        int vineCount = 2 + (idx % 2);

        for (int v = 0; v < vineCount; v++)
        {
            float baseAngle = (idx * 67f + v * 125f + 25f) * Mathf.Deg2Rad;
            float length = sz * Random.Range(1.25f, 1.75f);
            int segments = 6;

            Vector3 previous = pos +
                new Vector3(Mathf.Sin(baseAngle) * sz * 0.42f, 0.035f,
                            Mathf.Cos(baseAngle) * sz * 0.42f);

            for (int seg = 0; seg < segments; seg++)
            {
                float t = (seg + 1f) / segments;
                float bend = Mathf.Sin(t * Mathf.PI) * Random.Range(-0.55f, 0.55f);
                float angle = baseAngle + bend * Mathf.Deg2Rad;

                Vector3 next = pos + new Vector3(
                    Mathf.Sin(angle) * length * t,
                    0.035f + Mathf.Sin(t * Mathf.PI) * 0.012f,
                    Mathf.Cos(angle) * length * t);

                CreateVineSegment(root, previous, next,
                    seg % 2 == 0 ? vineDark : vineLight,
                    0.035f * sz);

                // Put a leaf every other segment so the vine visibly reads as a plant.
                if (seg >= 1 && seg % 2 == 0)
                {
                    Vector3 side = Vector3.Cross((next - previous).normalized, Vector3.up).normalized;
                    float sideSign = ((seg + v) % 2 == 0) ? 1f : -1f;
                    AddVineLeaf(root, next + side * 0.09f * sideSign,
                        next - previous, sz, v + seg);
                }

                previous = next;
            }

            // Small curling tip at the end of the vine.
            AddVineLeaf(root, previous, previous - pos, sz, 99 + v);
        }
    }

    private static void CreateVineSegment(
        Transform root, Vector3 a, Vector3 b, Color color, float thickness)
    {
        var vine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(vine, "Watermelon Vine");
        vine.name = "WatermelonVine";
        vine.transform.SetParent(root, false);

        Vector3 delta = b - a;
        vine.transform.localPosition = (a + b) * 0.5f;
        vine.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);

        float length = delta.magnitude;
        vine.transform.localScale = new Vector3(thickness, length * 0.5f, thickness);

        ApplyMat(vine, color, 0.12f);
        Object.DestroyImmediate(vine.GetComponent<Collider>());
    }

    private static void AddVineLeaf(
        Transform root, Vector3 position, Vector3 direction, float sz, int seed)
    {
        float angle = (seed * 47f) * Mathf.Deg2Rad;
        Vector3 side = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));

        var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(leaf, "Watermelon Vine Leaf");
        leaf.name = "WatermelonVineLeaf";
        leaf.transform.SetParent(root, false);
        leaf.transform.localPosition = position + side * (0.035f * sz);

        leaf.transform.localRotation =
            Quaternion.LookRotation(direction.normalized + side * 0.35f, Vector3.up);

        leaf.transform.localScale =
            new Vector3(0.16f * sz, 0.035f * sz, 0.28f * sz);

        ApplyMat(leaf, new Color(0.16f, 0.40f, 0.09f), 0.10f);
        Object.DestroyImmediate(leaf.GetComponent<Collider>());
    }

    // Apply the light-green melon body material (mid-green base)
    private static void ApplyMelonBody(GameObject go)
    {
        // Light green: #4A8C2A = (0.29, 0.55, 0.165)
        Color melonGreen = new Color(0.29f, 0.55f, 0.165f);
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var mat    = new Material(shader);
        mat.color  = melonGreen;
        if (!isURP) mat.SetFloat("_Glossiness", 0.55f);
        else        mat.SetFloat("_Smoothness",  0.55f);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    // =========================================================================
    // VINES  —  flat elongated spheres between melons simulate ground cover
    // =========================================================================
    private static void BuildVines(Transform root)
    {
        Color vineGreen = new Color(0.20f, 0.45f, 0.12f);
        // A few low ground-cover pieces between the main watermelon vines.
        for (int v = 0; v < 6; v++)
        {
            var vine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(vine, "Vine");
            vine.name = "Vine_" + v;
            vine.transform.SetParent(root, false);
            vine.transform.localPosition = new Vector3(
                Random.Range(-1.8f, 1.8f), 0.02f, Random.Range(-1.5f, 1.5f));
            float len = Random.Range(0.5f, 1.1f);
            float ang = Random.Range(0f, 360f);
            vine.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
            vine.transform.localScale    = new Vector3(0.08f, 0.04f, len);
            ApplyMat(vine, vineGreen);
            Object.DestroyImmediate(vine.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // SURROUNDING GREENERY  —  bushes + grass tufts around the fence exterior
    // =========================================================================
    private static void BuildSurroundingGreenery(Transform root)
    {
        // Try to use real Polytope shrub prefab; procedural fallback otherwise.
        var shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab");

        // 12 bushes placed in a loose ring just outside the fence boundary.
        int bushCount = 14;
        float[] rings = { 3.4f, 4.0f }; // two ring radii for depth
        Color bushDark  = new Color(0.18f, 0.42f, 0.14f);
        Color bushLight = new Color(0.28f, 0.55f, 0.20f);

        for (int b = 0; b < bushCount; b++)
        {
            float angle  = b * (360f / bushCount) * Mathf.Deg2Rad;
            float r      = rings[b % 2] + Random.Range(-0.25f, 0.25f);
            Vector3 bpos = new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);
            float bscale = Random.Range(0.55f, 0.95f);

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
                // Procedural: layered spheres make a rough bush silhouette.
                var bushRoot = new GameObject("Bush_" + b);
                Undo.RegisterCreatedObjectUndo(bushRoot, "Bush");
                bushRoot.transform.SetParent(root, false);
                bushRoot.transform.localPosition = bpos;

                int layers = 3;
                for (int l = 0; l < layers; l++)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Undo.RegisterCreatedObjectUndo(sphere, "Bush Sphere");
                    sphere.transform.SetParent(bushRoot.transform, false);
                    sphere.transform.localPosition = new Vector3(
                        Random.Range(-0.12f, 0.12f),
                        l * 0.18f * bscale,
                        Random.Range(-0.12f, 0.12f));
                    float ls = bscale * Random.Range(0.28f, 0.45f);
                    sphere.transform.localScale = new Vector3(ls, ls * 0.85f, ls);
                    ApplyMat(sphere, l % 2 == 0 ? bushDark : bushLight);
                    Object.DestroyImmediate(sphere.GetComponent<Collider>());
                }
            }
        }

        // Grass tufts: flat elongated spheres close to fence base.
        Color grassCol = new Color(0.22f, 0.50f, 0.15f);
        for (int g = 0; g < 20; g++)
        {
            float angle  = g * (360f / 20f) * Mathf.Deg2Rad + Random.Range(-0.1f, 0.1f);
            float r      = 2.6f + Random.Range(-0.15f, 0.3f);
            Vector3 gpos = new Vector3(Mathf.Sin(angle) * r, 0.04f, Mathf.Cos(angle) * r);

            var tuft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(tuft, "Grass Tuft");
            tuft.name = "GrassTuft_" + g;
            tuft.transform.SetParent(root, false);
            tuft.transform.localPosition = gpos;
            tuft.transform.localScale    = new Vector3(
                Random.Range(0.25f, 0.38f),
                Random.Range(0.10f, 0.18f),
                Random.Range(0.20f, 0.32f));
            tuft.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ApplyMat(tuft, grassCol);
            Object.DestroyImmediate(tuft.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // SHARED HELPERS
    // =========================================================================
    private static void ApplyMat(GameObject go, Color color, float smoothness = 0.2f)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader  = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var mat     = new Material(shader);
        mat.color   = color;
        if (!isURP) mat.SetFloat("_Glossiness", smoothness);
        else        mat.SetFloat("_Smoothness",  smoothness);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }
}
#endif

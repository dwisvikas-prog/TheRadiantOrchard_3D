#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

// =============================================================================
//  GENERATE CENTRAL MAGIC TREE
//  Matches the reference image exactly:
//
//   [1] TRUNK   — 4 massive twisted cylinders merged at base, warm brown,
//                 slightly tapered, gnarled bark rings every ~0.4 units
//   [2] RUNES   — 8 blue glowing streak particles (LineRenderer fallback:
//                 thin cyan cube columns) crawling up the trunk
//   [3] CANOPY  — 3-layer fluffy sphere cluster: bottom dark green,
//                 mid yellow-green, top bright lime; total width ~12u
//   [4] PLATFORM— circular raised stone base (2 concentric cylinder rings
//                 + flat disc) + 4 stone steps leading outward
//   [5] FLOWERS — 24 tiny flowers around the base (4 colours: white, pink,
//                 yellow, purple) using PT_Poppy_02 where available
//   [6] FENCE   — PT_Modular_Fence_Wood_01 ring at radius 7
//   [7] BENCHES — 4 procedural wooden benches at cardinal points outside fence
//   [8] PATH    — 4 stone-slab radial paths from platform to fence using
//                 PT_River_Rock_Pile_02 + flat cube slabs
//   [9] SURROUND— 8 real PT_Fruit_Tree_01_green + PT_Pine_Tree_03_green alt,
//                 16 PT_Generic_Shrub_01_green, 24 PT_Grass_02 tufts
//
//  Run via:  Tools → Radiant Orchard → Generate Central Magic Tree
// =============================================================================
public static class GenerateCentralMagicTree
{
    // ---- real asset paths -----------------------------------------------
    const string FencePrefab  = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    const string FruitTree    = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab";
    const string PineTree     = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab";
    const string Shrub        = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";
    const string Grass        = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
    const string Flower       = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Flowers/PT_Poppy_02.prefab";
    const string RockPile     = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_River_Rock_Pile_02.prefab";
    const string SmallRock    = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab";
    const string OakTree      = "Assets/ALP_Assets/Big Oak Tree FREE/Prefabs/OakBigTree01_pr.prefab";

    // ---- sizes ----------------------------------------------------------
    const float PlatformRadius   = 4.0f;
    const float FlowerRingRadius = 4.8f;
    const float FenceRadius      = 7.5f;
    const float SurroundRadius   = 11.5f;

    [MenuItem("Tools/Radiant Orchard/Generate Central Magic Tree")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Central Magic Tree");
        int undoGroup = Undo.GetCurrentGroup();

        // Place at world origin (island centre)
        var old = GameObject.Find("CentralMagicTree");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("CentralMagicTree");
        Undo.RegisterCreatedObjectUndo(root, "Create Central Magic Tree");

        // ----- build layers -----------------------------------------------
        BuildStonePlatform(root.transform);
        BuildMagicTrunk(root.transform);
        BuildGlowingRunes(root.transform);
        BuildCanopy(root.transform);
        BuildFlowerRing(root.transform);
        BuildFenceRing(root.transform);
        BuildBenches(root.transform);
        BuildStonePaths(root.transform);
        BuildSurroundingTrees(root.transform);
        BuildSurroundingBushesAndGrass(root.transform);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Central Magic Tree generated. Save with Ctrl+S.");
    }

    // =========================================================================
    // [4]  STONE PLATFORM  — raised circular dais + 4 steps
    // =========================================================================
    static void BuildStonePlatform(Transform root)
    {
        // Colour palette: light warm grey + slightly darker rim
        Color stoneLight = new Color(0.72f, 0.70f, 0.65f);
        Color stoneDark  = new Color(0.55f, 0.52f, 0.48f);
        Color mossy      = new Color(0.32f, 0.48f, 0.22f);

        var platform = new GameObject("Platform");
        Undo.RegisterCreatedObjectUndo(platform, "Platform");
        platform.transform.SetParent(root, false);

        // Main disc
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(disc, "PlatDisc");
        disc.name = "PlatDisc";
        disc.transform.SetParent(platform.transform, false);
        disc.transform.localPosition = Vector3.up * 0.18f;
        disc.transform.localScale    = new Vector3(PlatformRadius * 2f, 0.18f, PlatformRadius * 2f);
        ApplyMat(disc, stoneLight, 0.05f);
        Object.DestroyImmediate(disc.GetComponent<Collider>());

        // Raised rim ring
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(rim, "PlatRim");
        rim.name = "PlatRim";
        rim.transform.SetParent(platform.transform, false);
        rim.transform.localPosition = Vector3.up * 0.35f;
        rim.transform.localScale    = new Vector3(PlatformRadius * 2f + 0.3f, 0.08f, PlatformRadius * 2f + 0.3f);
        ApplyMat(rim, stoneDark, 0.05f);
        Object.DestroyImmediate(rim.GetComponent<Collider>());

        // Mossy inner circle (ground under tree)
        var moss = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(moss, "Moss");
        moss.name = "Moss";
        moss.transform.SetParent(platform.transform, false);
        moss.transform.localPosition = Vector3.up * 0.37f;
        moss.transform.localScale    = new Vector3(PlatformRadius * 1.4f, 0.02f, PlatformRadius * 1.4f);
        ApplyMat(moss, mossy, 0.05f);
        Object.DestroyImmediate(moss.GetComponent<Collider>());

        // 4 stone steps radiating outward at N/S/E/W
        float[] stepAngles = { 0f, 90f, 180f, 270f };
        foreach (float a in stepAngles)
        {
            float rad = a * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            // 3 stone slabs per step (near → far, descending height)
            for (int s = 0; s < 3; s++)
            {
                float dist = PlatformRadius + s * 0.55f;
                float h    = 0.28f - s * 0.09f;
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(slab, "Step");
                slab.name = "Step_" + (int)a + "_" + s;
                slab.transform.SetParent(platform.transform, false);
                slab.transform.localPosition = dir * dist + Vector3.up * (h * 0.5f);
                slab.transform.localRotation = Quaternion.Euler(0f, a, 0f);
                slab.transform.localScale    = new Vector3(1.5f - s * 0.15f, h, 0.52f);
                ApplyMat(slab, s % 2 == 0 ? stoneLight : stoneDark, 0.05f);
                Object.DestroyImmediate(slab.GetComponent<Collider>());
            }
        }

        // Small rocks around platform edge for detail — real prefab or fallback
        var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallRock);
        for (int r = 0; r < 12; r++)
        {
            float angle  = r * (360f / 12f) * Mathf.Deg2Rad;
            Vector3 rpos = new Vector3(Mathf.Sin(angle), 0.36f, Mathf.Cos(angle)) * (PlatformRadius + 0.05f);
            if (rockPrefab != null)
            {
                var ri = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, platform.transform);
                Undo.RegisterCreatedObjectUndo(ri, "PlatRock");
                ri.name = "PlatRock_" + r;
                ri.transform.localPosition = rpos;
                ri.transform.localScale    = Vector3.one * Random.Range(0.12f, 0.22f);
                ri.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                var rb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(rb, "PlatRock");
                rb.name = "PlatRock_" + r; rb.transform.SetParent(platform.transform, false);
                rb.transform.localPosition = rpos;
                rb.transform.localScale    = new Vector3(0.16f, 0.12f, 0.16f);
                ApplyMat(rb, stoneDark, 0.05f);
                Object.DestroyImmediate(rb.GetComponent<Collider>());
            }
        }
    }

    // =========================================================================
    // [1]  MASSIVE TWISTED TRUNK
    // =========================================================================
    static void BuildMagicTrunk(Transform root)
    {
        // Bark colours — warm brown base with darker crevice lines
        Color barkBase  = new Color(0.55f, 0.36f, 0.16f);
        Color barkDark  = new Color(0.32f, 0.20f, 0.08f);
        Color barkLight = new Color(0.70f, 0.50f, 0.25f);

        var trunkRoot = new GameObject("Trunk");
        Undo.RegisterCreatedObjectUndo(trunkRoot, "Trunk");
        trunkRoot.transform.SetParent(root, false);
        trunkRoot.transform.localPosition = Vector3.up * 0.37f;

        // 4 main trunk columns, merged at base, fanning out slightly upward
        (Vector3 offset, float lean, float topScale)[] columns =
        {
            (new Vector3( 0.55f, 0f,  0.30f),  8f, 0.55f),
            (new Vector3(-0.55f, 0f,  0.25f), -7f, 0.52f),
            (new Vector3( 0.20f, 0f, -0.55f),  5f, 0.50f),
            (new Vector3(-0.15f, 0f, -0.30f), -4f, 0.48f),
        };

        foreach (var (offset, lean, topScale) in columns)
        {
            int segs = 8;
            float segH = 0.55f;
            for (int s = 0; s < segs; s++)
            {
                float t     = (float)s / (segs - 1);
                float width = Mathf.Lerp(0.88f, topScale, t);
                float leanR = lean * Mathf.Deg2Rad;
                Vector3 leanOff = new Vector3(Mathf.Sin(leanR) * s * 0.04f, 0f, Mathf.Cos(leanR * 0.5f) * s * 0.02f);

                var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(seg, "TrunkSeg");
                seg.name = "TrunkSeg_" + s;
                seg.transform.SetParent(trunkRoot.transform, false);
                seg.transform.localPosition = offset + leanOff + Vector3.up * (s * segH + segH * 0.5f);
                seg.transform.localScale    = new Vector3(width, segH * 0.52f, width);
                ApplyMat(seg, t < 0.3f ? barkBase : (t < 0.7f ? barkDark : barkLight), 0.08f);
                Object.DestroyImmediate(seg.GetComponent<Collider>());

                // Bark ridge ring every other segment
                if (s % 2 == 0)
                {
                    var ridge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Undo.RegisterCreatedObjectUndo(ridge, "BarkRidge");
                    ridge.name = "BarkRidge_" + s;
                    ridge.transform.SetParent(trunkRoot.transform, false);
                    ridge.transform.localPosition = offset + leanOff + Vector3.up * (s * segH + segH * 0.15f);
                    ridge.transform.localScale    = new Vector3(width + 0.06f, 0.055f, width + 0.06f);
                    ApplyMat(ridge, barkDark, 0.05f);
                    Object.DestroyImmediate(ridge.GetComponent<Collider>());
                }
            }
        }

        // Central merged core at base (thick short cylinder to fill the gap)
        var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(core, "TrunkCore");
        core.name = "TrunkCore";
        core.transform.SetParent(trunkRoot.transform, false);
        core.transform.localPosition = Vector3.up * 1.1f;
        core.transform.localScale    = new Vector3(1.8f, 1.1f, 1.8f);
        ApplyMat(core, barkBase, 0.08f);
        Object.DestroyImmediate(core.GetComponent<Collider>());

        // Root buttresses: 6 flat fins flaring out at base
        Color rootCol = new Color(0.45f, 0.30f, 0.12f);
        for (int rb = 0; rb < 6; rb++)
        {
            float ang = rb * 60f * Mathf.Deg2Rad;
            var buttress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(buttress, "Buttress");
            buttress.name = "Buttress_" + rb;
            buttress.transform.SetParent(trunkRoot.transform, false);
            Vector3 bdir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            buttress.transform.localPosition = bdir * 1.05f + Vector3.up * 0.45f;
            buttress.transform.localRotation = Quaternion.LookRotation(bdir, Vector3.up) * Quaternion.Euler(-20f, 0f, 0f);
            buttress.transform.localScale    = new Vector3(0.25f, 0.95f, 1.2f);
            ApplyMat(buttress, rootCol, 0.06f);
            Object.DestroyImmediate(buttress.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // [2]  GLOWING BLUE RUNES / MAGIC STREAKS
    // =========================================================================
    static void BuildGlowingRunes(Transform root)
    {
        // Bright cyan-blue = magic energy streaks running up the trunk
        Color runeBlue  = new Color(0.20f, 0.75f, 1.00f);
        Color runeWhite = new Color(0.80f, 0.95f, 1.00f);

        var runeRoot = new GameObject("MagicRunes");
        Undo.RegisterCreatedObjectUndo(runeRoot, "MagicRunes");
        runeRoot.transform.SetParent(root, false);
        runeRoot.transform.localPosition = Vector3.up * 0.37f;

        // 8 glowing streaks at different angles and heights
        (float angle, float startH, float endH, float radius)[] streaks =
        {
            ( 15f, 0.3f, 3.5f, 0.85f),
            ( 75f, 0.5f, 2.8f, 0.78f),
            (135f, 0.2f, 3.0f, 0.92f),
            (195f, 0.4f, 3.8f, 0.80f),
            (255f, 0.1f, 2.5f, 0.88f),
            (315f, 0.6f, 3.2f, 0.75f),
            ( 45f, 1.0f, 4.0f, 0.70f),
            (225f, 0.8f, 3.6f, 0.82f),
        };

        foreach (var (angle, startH, endH, radius) in streaks)
        {
            float rad  = angle * Mathf.Deg2Rad;
            Vector3 x  = new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
            float height = endH - startH;

            // Try LineRenderer first for best visual
            var streakGO = new GameObject("Rune_" + (int)angle);
            Undo.RegisterCreatedObjectUndo(streakGO, "Rune");
            streakGO.transform.SetParent(runeRoot.transform, false);
            streakGO.transform.localPosition = x + Vector3.up * startH;

            var lr = streakGO.AddComponent<LineRenderer>();
            lr.positionCount = 6;
            lr.startWidth    = 0.045f;
            lr.endWidth      = 0.015f;
            lr.useWorldSpace = false;

            bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Material lrMat = new Material(Shader.Find(isURP
                ? "Universal Render Pipeline/Unlit"
                : "Unlit/Color"));
            lrMat.color = runeBlue;
            // Enable emission glow
            lrMat.EnableKeyword("_EMISSION");
            if (lrMat.HasProperty("_EmissionColor"))
                lrMat.SetColor("_EmissionColor", runeBlue * 1.8f);
            lr.sharedMaterial = lrMat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Zig-zag rune path going upward
            for (int p = 0; p < 6; p++)
            {
                float t    = (float)p / 5f;
                float zigX = (p % 2 == 0 ? 0.08f : -0.08f);
                lr.SetPosition(p, new Vector3(zigX, t * height, 0f));
            }

            // Glow orb at top of each streak
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(orb, "RuneOrb");
            orb.name = "RuneOrb";
            orb.transform.SetParent(streakGO.transform, false);
            orb.transform.localPosition = Vector3.up * height;
            orb.transform.localScale    = Vector3.one * (0.10f + Random.Range(0f, 0.04f));
            ApplyUnlit(orb, runeWhite);
            Object.DestroyImmediate(orb.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // [3]  CANOPY — 3-layer fluffy clusters
    // =========================================================================
    static void BuildCanopy(Transform root)
    {
        // Colour layers matching reference: bottom=dark green, mid=lime, top=yellow-green
        Color darkGreen   = new Color(0.14f, 0.35f, 0.10f);
        Color midGreen    = new Color(0.26f, 0.58f, 0.12f);
        Color limeGreen   = new Color(0.42f, 0.75f, 0.16f);
        Color yellowGreen = new Color(0.60f, 0.85f, 0.20f);

        var canopyRoot = new GameObject("Canopy");
        Undo.RegisterCreatedObjectUndo(canopyRoot, "Canopy");
        canopyRoot.transform.SetParent(root, false);
        // Canopy base sits at top of merged trunk (~4.8u above platform)
        canopyRoot.transform.localPosition = Vector3.up * (0.37f + 4.8f);

        // Layer 0 — wide dark base (6 large spheres in a ring)
        SpawnCanopyRing(canopyRoot.transform, 6, 3.2f, -0.4f, 2.2f, 1.8f, darkGreen, midGreen);
        // Layer 1 — mid ring, slightly higher (8 spheres)
        SpawnCanopyRing(canopyRoot.transform, 8, 2.4f,  0.6f, 1.9f, 1.5f, midGreen, limeGreen);
        // Layer 2 — upper dense ring (10 spheres)
        SpawnCanopyRing(canopyRoot.transform, 10, 1.6f, 1.5f, 1.5f, 1.2f, limeGreen, midGreen);
        // Layer 3 — top dome cap (5 spheres)
        SpawnCanopyRing(canopyRoot.transform, 5, 0.9f,  2.3f, 1.2f, 1.0f, yellowGreen, limeGreen);
        // Center top sphere
        var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(top, "CanopyTop");
        top.name = "CanopyTop"; top.transform.SetParent(canopyRoot.transform, false);
        top.transform.localPosition = Vector3.up * 3.0f;
        top.transform.localScale    = Vector3.one * 1.5f;
        ApplyMat(top, yellowGreen, 0.08f);
        Object.DestroyImmediate(top.GetComponent<Collider>());
    }

    static void SpawnCanopyRing(Transform parent, int count, float radius, float height,
                                 float scaleA, float scaleB, Color colA, Color colB)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            float sz    = Random.Range(scaleB, scaleA);
            var sphere  = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(sphere, "CanopySphere");
            sphere.name = "Canopy_" + i;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = new Vector3(
                Mathf.Sin(angle) * radius,
                height + Random.Range(-0.15f, 0.15f),
                Mathf.Cos(angle) * radius);
            sphere.transform.localScale = new Vector3(sz, sz * 0.88f, sz);
            ApplyMat(sphere, i % 2 == 0 ? colA : colB, 0.08f);
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // [5]  FLOWER RING — 24 tiny flowers at platform edge
    // =========================================================================
    static void BuildFlowerRing(Transform root)
    {
        var flowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Flower);

        // 4 colours cycling: white, soft pink, yellow, light purple
        Color[] flowerCols =
        {
            new Color(0.98f, 0.98f, 0.98f), // white
            new Color(0.98f, 0.60f, 0.72f), // pink
            new Color(0.98f, 0.92f, 0.22f), // yellow
            new Color(0.72f, 0.52f, 0.90f), // lavender
        };

        var flowerRoot = new GameObject("FlowerRing");
        Undo.RegisterCreatedObjectUndo(flowerRoot, "FlowerRing");
        flowerRoot.transform.SetParent(root, false);

        int count = 24;
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            float r     = FlowerRingRadius + Random.Range(-0.3f, 0.3f);
            Vector3 pos = new Vector3(Mathf.Sin(angle) * r, 0.4f, Mathf.Cos(angle) * r);

            if (flowerPrefab != null)
            {
                var fl = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab, flowerRoot.transform);
                Undo.RegisterCreatedObjectUndo(fl, "Flower");
                fl.name = "Flower_" + i;
                fl.transform.localPosition = pos;
                fl.transform.localScale    = Vector3.one * Random.Range(0.55f, 0.85f);
                fl.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                // Procedural fallback: tiny sphere petals on a thin stem
                BuildProceduralFlower(flowerRoot.transform, pos, flowerCols[i % 4]);
            }

            // Always add leaf tufts (real grass or procedural)
            var grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Grass);
            if (grassPrefab != null && i % 3 == 0)
            {
                var gr = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, flowerRoot.transform);
                Undo.RegisterCreatedObjectUndo(gr, "GrassTuft");
                gr.name = "GrassTuft_" + i;
                gr.transform.localPosition = pos + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));
                gr.transform.localScale    = Vector3.one * Random.Range(0.4f, 0.7f);
            }
        }
    }

    static void BuildProceduralFlower(Transform parent, Vector3 pos, Color petalCol)
    {
        // Stem
        var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(stem, "FlowerStem");
        stem.name = "FlowerStem"; stem.transform.SetParent(parent, false);
        stem.transform.localPosition = pos + Vector3.up * 0.12f;
        stem.transform.localScale    = new Vector3(0.025f, 0.12f, 0.025f);
        ApplyMat(stem, new Color(0.28f, 0.55f, 0.18f), 0.1f);
        Object.DestroyImmediate(stem.GetComponent<Collider>());

        // Petal cluster
        var petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(petal, "FlowerPetal");
        petal.name = "FlowerPetal"; petal.transform.SetParent(parent, false);
        petal.transform.localPosition = pos + Vector3.up * 0.26f;
        petal.transform.localScale    = new Vector3(0.10f, 0.07f, 0.10f);
        ApplyMat(petal, petalCol, 0.4f);
        Object.DestroyImmediate(petal.GetComponent<Collider>());

        // Tiny yellow centre
        var centre = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(centre, "FlowerCentre");
        centre.name = "FlowerCentre"; centre.transform.SetParent(parent, false);
        centre.transform.localPosition = pos + Vector3.up * 0.30f;
        centre.transform.localScale    = Vector3.one * 0.04f;
        ApplyMat(centre, new Color(1.0f, 0.88f, 0.10f), 0.5f);
        Object.DestroyImmediate(centre.GetComponent<Collider>());
    }

    // =========================================================================
    // [6]  FENCE RING — PT_Modular_Fence_Wood_01 in a circle
    // =========================================================================
    static void BuildFenceRing(Transform root)
    {
        var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FencePrefab);

        var fenceRoot = new GameObject("FenceRing");
        Undo.RegisterCreatedObjectUndo(fenceRoot, "FenceRing");
        fenceRoot.transform.SetParent(root, false);

        int segments = 16; // ~16 fence pieces in a circle of radius 7.5
        for (int i = 0; i < segments; i++)
        {
            float angle = i * (360f / segments) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * FenceRadius, 0f, Mathf.Cos(angle) * FenceRadius);

            if (fencePrefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, fenceRoot.transform);
                Undo.RegisterCreatedObjectUndo(inst, "FenceSegment");
                inst.name = "Fence_" + i;
                inst.transform.localPosition = pos;
                // Face tangent to the circle
                inst.transform.localRotation = Quaternion.Euler(0f, i * (360f / segments) + 90f, 0f);
            }
            else
            {
                // Procedural post + rail fallback
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(post, "FencePost");
                post.name = "FencePost_" + i; post.transform.SetParent(fenceRoot.transform, false);
                post.transform.localPosition = pos + Vector3.up * 0.45f;
                post.transform.localScale    = new Vector3(0.13f, 0.9f, 0.13f);
                ApplyMat(post, new Color(0.42f, 0.30f, 0.14f));
                Object.DestroyImmediate(post.GetComponent<Collider>());
            }
        }
    }

    // =========================================================================
    // [7]  WOODEN BENCHES — 4 at cardinal points just outside the fence
    // =========================================================================
    static void BuildBenches(Transform root)
    {
        Color seatCol = new Color(0.58f, 0.40f, 0.18f);
        Color legCol  = new Color(0.38f, 0.25f, 0.10f);

        var benchRoot = new GameObject("Benches");
        Undo.RegisterCreatedObjectUndo(benchRoot, "Benches");
        benchRoot.transform.SetParent(root, false);

        float[] angles = { 0f, 90f, 180f, 270f };
        foreach (float a in angles)
        {
            float rad  = a * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            Vector3 pos = dir * (FenceRadius + 1.2f);
            Quaternion rot = Quaternion.Euler(0f, a + 180f, 0f); // face inward toward tree

            var bench = new GameObject("Bench_" + (int)a);
            Undo.RegisterCreatedObjectUndo(bench, "Bench");
            bench.transform.SetParent(benchRoot.transform, false);
            bench.transform.localPosition = pos;
            bench.transform.localRotation = rot;

            // Seat board
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(seat, "Seat");
            seat.name = "Seat"; seat.transform.SetParent(bench.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            seat.transform.localScale    = new Vector3(1.55f, 0.10f, 0.42f);
            ApplyMat(seat, seatCol, 0.2f);
            Object.DestroyImmediate(seat.GetComponent<Collider>());

            // Back board
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(back, "Back");
            back.name = "Back"; back.transform.SetParent(bench.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.70f, -0.18f);
            back.transform.localScale    = new Vector3(1.55f, 0.32f, 0.08f);
            ApplyMat(back, seatCol, 0.2f);
            Object.DestroyImmediate(back.GetComponent<Collider>());

            // 4 legs
            foreach (var lp in new[] {
                new Vector3(-0.62f, 0.21f, 0.14f),
                new Vector3( 0.62f, 0.21f, 0.14f),
                new Vector3(-0.62f, 0.21f,-0.14f),
                new Vector3( 0.62f, 0.21f,-0.14f),
            })
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(leg, "Leg");
                leg.name = "Leg"; leg.transform.SetParent(bench.transform, false);
                leg.transform.localPosition = lp;
                leg.transform.localScale    = new Vector3(0.10f, 0.42f, 0.10f);
                ApplyMat(leg, legCol, 0.1f);
                Object.DestroyImmediate(leg.GetComponent<Collider>());
            }

            // Cross brace between leg pairs
            var brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(brace, "Brace");
            brace.name = "Brace"; brace.transform.SetParent(bench.transform, false);
            brace.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            brace.transform.localScale    = new Vector3(1.35f, 0.06f, 0.06f);
            ApplyMat(brace, legCol, 0.1f);
            Object.DestroyImmediate(brace.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // [8]  STONE PATHS — 4 radial paths from platform to fence
    // =========================================================================
    static void BuildStonePaths(Transform root)
    {
        Color slabCol = new Color(0.68f, 0.65f, 0.60f);
        Color slabAlt = new Color(0.58f, 0.55f, 0.50f);

        var pathRoot = new GameObject("StonePaths");
        Undo.RegisterCreatedObjectUndo(pathRoot, "StonePaths");
        pathRoot.transform.SetParent(root, false);

        var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockPile);

        float[] pathAngles = { 45f, 135f, 225f, 315f }; // diagonal paths (N/S/E/W covered by steps)
        foreach (float a in pathAngles)
        {
            float aRad = a * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(aRad), 0f, Mathf.Cos(aRad));

            // 8 slabs per path, spaced 0.38u apart, from platform edge to fence
            float startDist = PlatformRadius + 0.6f;
            float endDist   = FenceRadius   - 0.4f;
            int   slabCount = 8;

            for (int s = 0; s < slabCount; s++)
            {
                float t    = (float)s / (slabCount - 1);
                float dist = Mathf.Lerp(startDist, endDist, t);
                Vector3 pos = dir * dist + Vector3.up * 0.03f;

                // Slight random offset for organic placement
                Vector3 tangent = new Vector3(-dir.z, 0f, dir.x);
                pos += tangent * Random.Range(-0.12f, 0.12f);
                pos += dir    * Random.Range(-0.05f, 0.05f);

                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(slab, "PathSlab");
                slab.name = "PathSlab_" + (int)a + "_" + s;
                slab.transform.SetParent(pathRoot.transform, false);
                slab.transform.localPosition = pos;
                slab.transform.localRotation = Quaternion.Euler(0f, a + Random.Range(-8f, 8f), 0f);
                float slabW = Random.Range(0.42f, 0.62f);
                slab.transform.localScale    = new Vector3(slabW, 0.07f, slabW * Random.Range(0.8f, 1.3f));
                ApplyMat(slab, s % 2 == 0 ? slabCol : slabAlt, 0.05f);
                Object.DestroyImmediate(slab.GetComponent<Collider>());
            }

            // Rock pile accent at path start
            if (rockPrefab != null)
            {
                var rk = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, pathRoot.transform);
                Undo.RegisterCreatedObjectUndo(rk, "PathRock");
                rk.name = "PathRock_" + (int)a;
                rk.transform.localPosition = dir * (PlatformRadius + 0.1f) + Vector3.up * 0.05f;
                rk.transform.localScale    = Vector3.one * 0.25f;
                rk.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }
    }

    // =========================================================================
    // [9a]  SURROUNDING TREES — alternating fruit trees and pine trees in ring
    // =========================================================================
    static void BuildSurroundingTrees(Transform root)
    {
        var fruitTree = AssetDatabase.LoadAssetAtPath<GameObject>(FruitTree);
        var pineTree  = AssetDatabase.LoadAssetAtPath<GameObject>(PineTree);
        var oakTree   = AssetDatabase.LoadAssetAtPath<GameObject>(OakTree);

        var treeRoot = new GameObject("SurroundingTrees");
        Undo.RegisterCreatedObjectUndo(treeRoot, "SurroundingTrees");
        treeRoot.transform.SetParent(root, false);

        int treeCount = 10;
        float innerR  = SurroundRadius;
        float outerR  = SurroundRadius + 2.5f;

        for (int i = 0; i < treeCount; i++)
        {
            float angle = i * (360f / treeCount) * Mathf.Deg2Rad + 0.32f; // slight phase offset
            float r     = (i % 2 == 0) ? innerR : outerR;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);

            GameObject prefab;
            float scale;
            if (i % 3 == 0 && oakTree != null)
            {
                prefab = oakTree;  scale = Random.Range(0.5f, 0.7f);
            }
            else if (i % 2 == 0 && fruitTree != null)
            {
                prefab = fruitTree; scale = Random.Range(0.9f, 1.3f);
            }
            else
            {
                prefab = pineTree; scale = Random.Range(0.8f, 1.2f);
            }

            if (prefab != null)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, treeRoot.transform);
                Undo.RegisterCreatedObjectUndo(tree, "SurroundTree");
                tree.name = "SurroundTree_" + i;
                tree.transform.localPosition = pos;
                tree.transform.localScale    = Vector3.one * scale;
                tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                BuildProceduralSmallTree(treeRoot.transform, pos, i);
            }
        }
    }

    static void BuildProceduralSmallTree(Transform parent, Vector3 pos, int idx)
    {
        Color tc1 = new Color(0.38f, 0.25f, 0.10f);
        Color cc1 = new Color(0.18f, 0.40f, 0.12f);
        Color cc2 = new Color(0.12f, 0.30f, 0.10f);

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(trunk, "SmallTrunk");
        trunk.name = "STrunk_" + idx; trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = pos + Vector3.up * 0.7f;
        trunk.transform.localScale    = new Vector3(0.18f, 0.7f, 0.18f);
        ApplyMat(trunk, tc1, 0.1f);
        Object.DestroyImmediate(trunk.GetComponent<Collider>());

        for (int l = 0; l < 3; l++)
        {
            var can = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(can, "SmallCanopy");
            can.name = "SCanopy_" + l; can.transform.SetParent(parent, false);
            float s = Mathf.Lerp(0.9f, 0.45f, (float)l / 2f);
            can.transform.localPosition = pos + Vector3.up * (1.5f + l * 0.35f);
            can.transform.localScale    = new Vector3(s, s * 0.82f, s);
            ApplyMat(can, l % 2 == 0 ? cc1 : cc2, 0.08f);
            Object.DestroyImmediate(can.GetComponent<Collider>());
        }
    }

    // =========================================================================
    // [9b]  SURROUNDING BUSHES + GRASS TUFTS
    // =========================================================================
    static void BuildSurroundingBushesAndGrass(Transform root)
    {
        var shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Shrub);
        var grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Grass);
        var flowerPref  = AssetDatabase.LoadAssetAtPath<GameObject>(Flower);

        var greenRoot = new GameObject("SurroundGreenery");
        Undo.RegisterCreatedObjectUndo(greenRoot, "SurroundGreenery");
        greenRoot.transform.SetParent(root, false);

        Color bushDark  = new Color(0.16f, 0.38f, 0.11f);
        Color bushLight = new Color(0.26f, 0.54f, 0.18f);

        // Dense shrub band between fence and surrounding trees
        int shrubCount = 24;
        for (int b = 0; b < shrubCount; b++)
        {
            float angle = b * (360f / shrubCount) * Mathf.Deg2Rad + Random.Range(-0.05f, 0.05f);
            float r     = FenceRadius + 1.8f + (b % 3) * 0.7f + Random.Range(-0.3f, 0.3f);
            Vector3 pos = new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);

            if (shrubPrefab != null)
            {
                var sh = (GameObject)PrefabUtility.InstantiatePrefab(shrubPrefab, greenRoot.transform);
                Undo.RegisterCreatedObjectUndo(sh, "Shrub");
                sh.name = "Shrub_" + b; sh.transform.localPosition = pos;
                sh.transform.localScale    = Vector3.one * Random.Range(0.5f, 1.0f);
                sh.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(bush, "Bush");
                bush.name = "Bush_" + b; bush.transform.SetParent(greenRoot.transform, false);
                bush.transform.localPosition = pos + Vector3.up * 0.22f;
                bush.transform.localScale    = Vector3.one * Random.Range(0.28f, 0.50f);
                ApplyMat(bush, b % 2 == 0 ? bushDark : bushLight, 0.08f);
                Object.DestroyImmediate(bush.GetComponent<Collider>());
            }
        }

        // Grass tufts — inner ring (between platform and fence)
        int grassCount = 32;
        for (int g = 0; g < grassCount; g++)
        {
            float angle = g * (360f / grassCount) * Mathf.Deg2Rad + Random.Range(-0.08f, 0.08f);
            float r     = PlatformRadius + 0.6f + (g % 4) * 0.5f + Random.Range(-0.2f, 0.2f);
            if (r >= FenceRadius - 0.5f) r = FenceRadius - 0.6f;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * r, 0.38f, Mathf.Cos(angle) * r);

            if (grassPrefab != null)
            {
                var gr = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, greenRoot.transform);
                Undo.RegisterCreatedObjectUndo(gr, "Grass");
                gr.name = "Grass_" + g; gr.transform.localPosition = pos;
                gr.transform.localScale    = Vector3.one * Random.Range(0.35f, 0.65f);
                gr.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                var tuft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(tuft, "GrassTuft");
                tuft.name = "GrassTuft_" + g; tuft.transform.SetParent(greenRoot.transform, false);
                tuft.transform.localPosition = pos;
                tuft.transform.localScale    = new Vector3(Random.Range(0.12f, 0.20f), 0.08f, Random.Range(0.10f, 0.18f));
                ApplyMat(tuft, new Color(0.26f, 0.56f, 0.18f), 0.08f);
                Object.DestroyImmediate(tuft.GetComponent<Collider>());
            }
        }

        // Scattered flowers inside the fence ring
        Color[] fCols = { new Color(0.98f, 0.98f, 0.98f), new Color(0.98f, 0.6f, 0.72f),
                          new Color(0.98f, 0.92f, 0.22f), new Color(0.72f, 0.52f, 0.90f) };
        for (int f = 0; f < 18; f++)
        {
            float angle = f * (360f / 18f) * Mathf.Deg2Rad + 0.4f;
            float r     = FlowerRingRadius + 0.5f + Random.Range(-0.3f, 0.4f);
            Vector3 pos = new Vector3(Mathf.Sin(angle) * r, 0.38f, Mathf.Cos(angle) * r);

            if (flowerPref != null)
            {
                var fl = (GameObject)PrefabUtility.InstantiatePrefab(flowerPref, greenRoot.transform);
                Undo.RegisterCreatedObjectUndo(fl, "InnerFlower");
                fl.name = "InnerFlower_" + f; fl.transform.localPosition = pos;
                fl.transform.localScale    = Vector3.one * Random.Range(0.45f, 0.7f);
                fl.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            else
            {
                BuildProceduralFlower(greenRoot.transform, pos, fCols[f % 4]);
            }
        }
    }

    // =========================================================================
    // SHARED HELPERS
    // =========================================================================
    static void ApplyMat(GameObject go, Color color, float smoothness = 0.15f)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var mat    = new Material(shader) { color = color };
        if (!isURP) mat.SetFloat("_Glossiness", smoothness);
        else        mat.SetFloat("_Smoothness",  smoothness);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static void ApplyUnlit(GameObject go, Color color)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.0f);
        }
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }
}
#endif

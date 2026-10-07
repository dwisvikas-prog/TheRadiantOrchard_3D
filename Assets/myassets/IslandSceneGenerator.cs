using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// Procedurally assembles the Radiant Orchard island layout (base, central shrine
/// area, paths, and the four prop zones). On Generate, it first auto-discovers real
/// prefabs/materials already in the project (Assets/Blinktool, Polytope Studio,
/// InnerverseInteractive, ALP_Assets, AssetsStore, stylized-mini-floating-island,
/// myassets) via AssetDatabase and only falls back to procedural primitives for
/// slots nothing was found for. Auto-discovery is editor-only (#if UNITY_EDITOR) so
/// the script still compiles into player builds untouched. Attach to an empty
/// GameObject and run via the component's context menu (gear icon / right-click
/// header -> "Generate Island Scene").
public class IslandSceneGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private bool clearExistingOnGenerate = true;
    [SerializeField] private bool autoDiscoverFromProject = true;

    [Header("Island Base")]
    [SerializeField] private GameObject islandBasePrefab;
    [SerializeField] private GameObject cliffWallPrefab;
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private Material waterMaterialOverride;
    [SerializeField] private float islandRadius = 40f;
    [SerializeField] private float islandTopHeight = 4f;
    [SerializeField] private float cliffDepth = 6f;
    [SerializeField] private float waterRadius = 65f;
    [SerializeField] private float waterLevelOffset = -3.5f;

    [Header("Central Area")]
    [SerializeField] private GameObject wisdomTreePrefab;
    [SerializeField] private GameObject stoneWellPrefab;
    [SerializeField] private GameObject pavingStonePrefab;
    [SerializeField] private float centralRingRadius = 9f;
    [SerializeField] private int pavingStoneCount = 24;

    [Header("Pathways")]
    [SerializeField] private GameObject pathSegmentPrefab;
    [SerializeField] private float[] pathRingRadii = { 14f, 24f, 34f };
    [SerializeField] private int pathSegmentsPerRing = 36;
    [SerializeField] private float pathWidth = 1.6f;
    [SerializeField] private float pathJitter = 0.6f;

    [Header("North Zone - Crystal Shrine")]
    [SerializeField] private GameObject crystalShrinePrefab;
    [SerializeField] private GameObject[] pineTreePrefabPool;
    [SerializeField] private GameObject cropPatchPrefab;
    // Reference: dense pine ring around island perimeter — 16 minimum.
    [SerializeField] private int pineTreeCount = 16;
    [SerializeField] private int cropRows = 3;
    [SerializeField] private int cropColumns = 4;

    [Header("West Zone - Pond")]
    [SerializeField] private GameObject pondPrefab;
    [SerializeField] private GameObject lilyPadPrefab;
    [SerializeField] private GameObject dockPrefab;
    [SerializeField] private GameObject[] fruitTreePrefabPool;
    [SerializeField] private int lilyPadCount = 10;
    [SerializeField] private int fruitTreeCount = 5;

    [Header("East Zone - Well & Rest Area")]
    [SerializeField] private GameObject eastWellPrefab;
    [SerializeField] private GameObject benchPrefab;
    [SerializeField] private GameObject lanternPrefab;
    [SerializeField] private int benchCount = 4;
    [SerializeField] private int lanternCount = 6;

    [Header("Periphery")]
    [SerializeField] private GameObject[] flowerBushPrefabPool;
    [SerializeField] private GameObject[] rockPrefabPool;
    [SerializeField] private GameObject[] fencePrefabPool;
    [SerializeField] private int flowerBushCount = 24;
    [SerializeField] private int rockCount = 18;
    [SerializeField] private int fenceSegmentCount = 20;

    [Header("Optional Characters")]
    [SerializeField] private GameObject stickmanPrefab;
    // Reference: 6-7 grey humanoids visible walking around the island.
    [SerializeField] private int stickmanCount = 7;

    private const string RootName = "IslandScene_Generated";

    private Dictionary<(Color color, bool transparent, bool emissive), Material> fallbackMaterialCache;
    private Transform root, islandGroup, centralGroup, pathGroup, northGroup, westGroup, eastGroup, peripheryGroup, characterGroup;
    private bool usingCompleteIslandModel;

    #region Public Entry Points

    [ContextMenu("Generate Island Scene")]
    public void GenerateIslandScene()
    {
        fallbackMaterialCache = new Dictionary<(Color, bool, bool), Material>();
        UnityEngine.Random.InitState(randomSeed);

#if UNITY_EDITOR
        if (autoDiscoverFromProject)
        {
            AutoDiscoverAssets();
        }
#endif

        if (clearExistingOnGenerate)
        {
            ClearGeneratedScene();
        }

        BuildRootHierarchy();

        GenerateIslandBase();
        GenerateWaterPlane();
        GenerateCentralArea();
        GeneratePathways();
        GenerateNorthZone();
        GenerateWestZone();
        GenerateEastZone();
        GeneratePeriphery();
        GenerateCharacters();

        Debug.Log("IslandSceneGenerator: layout generation complete.");
    }

    [ContextMenu("Clear Generated Scene")]
    public void ClearGeneratedScene()
    {
        var existing = transform.Find(RootName);
        if (existing != null)
        {
            SafeDestroy(existing.gameObject);
        }
    }

    #endregion

    #region Auto-Discovery (Editor Only)

#if UNITY_EDITOR
    // Known-good asset paths verified in this project. Exact hits are tried first;
    // pools use folder-scoped searches for categories with many valid variants
    // (rocks) so we're not hand-listing hundreds of paths.
    private void AutoDiscoverAssets()
    {
        if (islandBasePrefab == null)
        {
            islandBasePrefab = LoadPrefab("Assets/stylized-mini-floating-island/source/lowPolyFinal.fbx");
        }
        usingCompleteIslandModel = islandBasePrefab != null && islandBasePrefab.name == "lowPolyFinal";

        if (wisdomTreePrefab == null)
        {
            wisdomTreePrefab = LoadPrefab("Assets/ALP_Assets/Big Oak Tree FREE/Prefabs/OakBigTree01_pr.prefab");
        }

        var discoveredWell = LoadPrefab("Assets/AssetsStore/Weel/Assets/Prefabs/well.prefab");
        if (stoneWellPrefab == null) stoneWellPrefab = discoveredWell;
        if (eastWellPrefab == null) eastWellPrefab = discoveredWell;

        if (stickmanPrefab == null)
        {
            stickmanPrefab = LoadPrefab("Assets/myassets/girl.fbx");
        }

        if (waterMaterialOverride == null)
        {
            waterMaterialOverride = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Water/Materials/UNS_Water.mat");
        }

        if (IsEmpty(pineTreePrefabPool))
        {
            pineTreePrefabPool = LoadPool(
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab",
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green_cut.prefab");
        }

        if (IsEmpty(fruitTreePrefabPool))
        {
            fruitTreePrefabPool = LoadPool(
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_apples.prefab",
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_pears.prefab",
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_plums.prefab",
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab");
        }

        if (IsEmpty(flowerBushPrefabPool))
        {
            flowerBushPrefabPool = LoadPool(
                "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Vegetation/Bushes/Prefabs/UNS_Bush.prefab",
                "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Vegetation/Flowers/Prefabs/UNS_Flower.prefab",
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab");
        }

        if (IsEmpty(fencePrefabPool))
        {
            fencePrefabPool = LoadPool(
                "Assets/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/Fence.prefab",
                "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab",
                "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_02.prefab",
                "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_03.prefab");
        }

        if (IsEmpty(rockPrefabPool))
        {
            rockPrefabPool = LoadPoolFromFolders(
                "Assets/Blinktool/Low poly rocks/Prefabs",
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks");
        }

        Debug.Log("IslandSceneGenerator: auto-discovery complete — " +
            $"island={(islandBasePrefab != null)}, wisdomTree={(wisdomTreePrefab != null)}, well={(discoveredWell != null)}, " +
            $"stickman={(stickmanPrefab != null)}, waterMat={(waterMaterialOverride != null)}, " +
            $"pines={pineTreePrefabPool.Length}, fruitTrees={fruitTreePrefabPool.Length}, " +
            $"bushes={flowerBushPrefabPool.Length}, fences={fencePrefabPool.Length}, rocks={rockPrefabPool.Length}.");
    }

    private static bool IsEmpty(GameObject[] pool) => pool == null || pool.Length == 0;

    private static GameObject LoadPrefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    private static GameObject[] LoadPool(params string[] paths)
    {
        var list = new List<GameObject>();
        foreach (var path in paths)
        {
            var prefab = LoadPrefab(path);
            if (prefab != null) list.Add(prefab);
        }
        return list.ToArray();
    }

    private static GameObject[] LoadPoolFromFolders(params string[] folders)
    {
        var list = new List<GameObject>();
        var guids = AssetDatabase.FindAssets("t:Prefab", folders);
        foreach (var guid in guids)
        {
            var prefab = LoadPrefab(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null) list.Add(prefab);
        }
        return list.ToArray();
    }
#endif

    #endregion

    #region Hierarchy & Spawn Helpers

    private void BuildRootHierarchy()
    {
        var rootGO = new GameObject(RootName);
        rootGO.transform.SetParent(transform, false);
        root = rootGO.transform;

        islandGroup = CreateGroup("IslandBase");
        centralGroup = CreateGroup("CentralArea");
        pathGroup = CreateGroup("Pathways");
        northGroup = CreateGroup("NorthZone_CrystalShrine");
        westGroup = CreateGroup("WestZone_Pond");
        eastGroup = CreateGroup("EastZone_Well");
        peripheryGroup = CreateGroup("Periphery");
        characterGroup = CreateGroup("Characters");
    }

    private Transform CreateGroup(string groupName)
    {
        var go = new GameObject(groupName);
        go.transform.SetParent(root, false);
        return go.transform;
    }

    private GameObject Spawn(GameObject prefab, Func<GameObject> fallbackBuilder, Vector3 localPos, Quaternion localRot, Transform parent, string objName)
    {
        GameObject go = prefab != null ? Instantiate(prefab) : fallbackBuilder();
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.name = objName;
        return go;
    }

    private GameObject PickFromPool(GameObject[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        return pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    private Vector3 PointOnCircle(float radius, float angleDeg, float y = 0f)
    {
        var rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, y, Mathf.Sin(rad) * radius);
    }

    // Aligns a box's local +X (its "length" axis) to the tangent of PointOnCircle
    // at angleDeg, so path/fence segments lie flush along the circle instead of
    // pointing radially outward.
    private Quaternion TangentRotation(float angleDeg)
    {
        return Quaternion.Euler(0f, -angleDeg - 90f, 0f);
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    private void StripCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null) SafeDestroy(collider);
    }

    #endregion

    #region Fallback Materials

    private Material GetOrCreateFallbackMaterial(Color color, bool transparent, bool emissive)
    {
        var key = (color, transparent, emissive);
        if (fallbackMaterialCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // Graphics Settings has no pipeline asset assigned right now, so the
        // project actually renders on the Built-in RP even though URP is
        // installed — a URP shader here would render pink. Match whichever is
        // really active.
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var mat = new Material(shader);
        mat.color = color;
        mat.SetFloat(isURP ? "_Smoothness" : "_Glossiness", transparent ? 0.85f : 0.25f);

        if (transparent)
        {
            if (isURP)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                mat.SetFloat("_Mode", 3f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", color * 2.2f);
        }

        fallbackMaterialCache[key] = mat;
        return mat;
    }

    private void ApplyFallbackMaterial(GameObject go, Color color, bool transparent = false, bool emissive = false)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        renderer.sharedMaterial = GetOrCreateFallbackMaterial(color, transparent, emissive);
    }

    private GameObject NewEmpty(string objName) => new GameObject(objName);

    // Every decorative fallback primitive strips its auto-added collider (mobile
    // perf) — only the island base/cliff keep theirs, added separately.
    private GameObject AddPart(GameObject parent, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color,
        bool transparent = false, bool emissive = false, Quaternion? localRot = null)
    {
        var part = GameObject.CreatePrimitive(type);
        part.transform.SetParent(parent.transform, false);
        part.transform.localPosition = localPos;
        part.transform.localRotation = localRot ?? Quaternion.identity;
        part.transform.localScale = localScale;
        ApplyFallbackMaterial(part, color, transparent, emissive);
        StripCollider(part);
        return part;
    }

    #endregion

    #region Island Base & Water

    private void GenerateIslandBase()
    {
        var topColor = new Color(0.36f, 0.62f, 0.28f);
        var cliffColor = new Color(0.42f, 0.38f, 0.34f);

        Spawn(islandBasePrefab, () => BuildFallbackCylinder(islandRadius, islandTopHeight, topColor, true),
            Vector3.zero, Quaternion.identity, islandGroup, "IslandTop");

        if (usingCompleteIslandModel) return; // the model already includes cliffs/underside

        Spawn(cliffWallPrefab, () => BuildFallbackCylinder(islandRadius * 0.94f, cliffDepth, cliffColor, true),
            new Vector3(0f, -cliffDepth * 0.5f - islandTopHeight * 0.5f + 0.05f, 0f), Quaternion.identity,
            islandGroup, "CliffWalls");
    }

    private void GenerateWaterPlane()
    {
        var waterColor = new Color(0.05f, 0.35f, 0.55f, 0.75f);
        var water = Spawn(waterPrefab, () => BuildFallbackCylinder(waterRadius, 1f, waterColor, false, true),
            new Vector3(0f, waterLevelOffset, 0f), Quaternion.identity, islandGroup, "WaterPlane");
        ApplyWaterMaterialOverride(water);
    }

    private void ApplyWaterMaterialOverride(GameObject waterObject)
    {
        if (waterMaterialOverride == null) return;
        var renderer = waterObject.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = waterMaterialOverride;
    }

    private GameObject BuildFallbackCylinder(float radius, float height, Color color, bool addCollider, bool transparent = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        ApplyFallbackMaterial(go, color, transparent);
        if (!addCollider) StripCollider(go);
        return go;
    }

    #endregion

    #region Central Area

    private void GenerateCentralArea()
    {
        var wellOffset = PointOnCircle(5.5f, 225f);
        Spawn(stoneWellPrefab, BuildFallbackWell, wellOffset, Quaternion.identity, centralGroup, "CentralStoneWell");

        Spawn(wisdomTreePrefab,
            () => BuildFallbackTree(6f, 0.9f, 3.2f, new Color(0.32f, 0.2f, 0.12f), new Color(0.22f, 0.45f, 0.2f)),
            Vector3.zero, Quaternion.identity, centralGroup, "WisdomTree");

        for (var i = 0; i < pavingStoneCount; i++)
        {
            var angle = i * (360f / pavingStoneCount);
            var pos = PointOnCircle(centralRingRadius, angle);
            Spawn(pavingStonePrefab, () => BuildFallbackPavingStone(1.3f), pos,
                Quaternion.Euler(0f, angle, 0f), centralGroup, $"PavingStone_{i}");
        }
    }

    #endregion

    #region Pathways

    private void GeneratePathways()
    {
        foreach (var radius in pathRingRadii)
        {
            BuildPathRing(radius);
        }

        foreach (var angle in new[] { 0f, 90f, 180f, 270f })
        {
            BuildPathBranch(angle);
        }
    }

    private void BuildPathRing(float radius)
    {
        var step = 360f / pathSegmentsPerRing;
        for (var i = 0; i < pathSegmentsPerRing; i++)
        {
            var midAngle = (i + 0.5f) * step;
            var jitterOffset = (Mathf.PerlinNoise(radius * 0.1f, i * 0.15f) - 0.5f) * pathJitter;
            var segRadius = radius + jitterOffset;
            var segLength = 2f * Mathf.PI * segRadius / pathSegmentsPerRing * 1.05f;

            var pos = PointOnCircle(segRadius, midAngle, 0.02f);
            Spawn(pathSegmentPrefab, () => BuildFallbackPathSegment(segLength, pathWidth), pos,
                TangentRotation(midAngle), pathGroup, $"PathRing{radius}_{i}");
        }
    }

    private void BuildPathBranch(float angleDeg)
    {
        var startRadius = centralRingRadius + 1f;
        var endRadius = islandRadius - 3f;
        var segmentCount = Mathf.Max(1, Mathf.CeilToInt((endRadius - startRadius) / 2.5f));

        for (var i = 0; i < segmentCount; i++)
        {
            var t0 = i / (float)segmentCount;
            var t1 = (i + 1) / (float)segmentCount;
            var r0 = Mathf.Lerp(startRadius, endRadius, t0);
            var r1 = Mathf.Lerp(startRadius, endRadius, t1);
            var rMid = (r0 + r1) * 0.5f;

            var lateralJitter = (Mathf.PerlinNoise(angleDeg * 0.05f, i * 0.3f) - 0.5f) * pathJitter * 4f;
            var jitteredAngle = angleDeg + lateralJitter;

            var pos = PointOnCircle(rMid, jitteredAngle, 0.02f);
            Spawn(pathSegmentPrefab, () => BuildFallbackPathSegment(r1 - r0 + 0.3f, pathWidth), pos,
                TangentRotation(jitteredAngle - 90f), pathGroup, $"PathBranch{angleDeg}_{i}");
        }
    }

    #endregion

    #region North Zone - Crystal Shrine

    private void GenerateNorthZone()
    {
        const float zoneAngle = 90f;

        var shrinePos = PointOnCircle(20f, zoneAngle);
        Spawn(crystalShrinePrefab, BuildFallbackCrystalShrine, shrinePos, Quaternion.identity, northGroup, "CrystalShrine");

        for (var i = 0; i < pineTreeCount; i++)
        {
            var angle = zoneAngle + UnityEngine.Random.Range(-35f, 35f);
            var radius = UnityEngine.Random.Range(16f, 27f);
            var pos = PointOnCircle(radius, angle);
            Spawn(PickFromPool(pineTreePrefabPool), BuildFallbackPineTree, pos,
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), northGroup, $"PineTree_{i}");
        }

        var cropCenter = PointOnCircle(29f, zoneAngle + 18f);
        Spawn(cropPatchPrefab, () => BuildFallbackCropPatch(cropRows, cropColumns), cropCenter,
            Quaternion.Euler(0f, zoneAngle, 0f), northGroup, "CropPatch");
    }

    #endregion

    #region West Zone - Pond

    private void GenerateWestZone()
    {
        const float zoneAngle = 180f;

        var pondCenter = PointOnCircle(20f, zoneAngle);
        var pond = Spawn(pondPrefab, () => BuildFallbackCylinder(9f, 0.4f, new Color(0.08f, 0.4f, 0.55f, 0.8f), false, true),
            pondCenter, Quaternion.identity, westGroup, "Pond");
        ApplyWaterMaterialOverride(pond);

        for (var i = 0; i < lilyPadCount; i++)
        {
            var offset = UnityEngine.Random.insideUnitCircle * 7f;
            var pos = pondCenter + new Vector3(offset.x, 0.22f, offset.y);
            Spawn(lilyPadPrefab, BuildFallbackLilyPad, pos,
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), westGroup, $"LilyPad_{i}");
        }

        var dockPos = pondCenter + new Vector3(9f, 0.2f, 0f);
        Spawn(dockPrefab, () => BuildFallbackDock(6f), dockPos, Quaternion.Euler(0f, 180f, 0f), westGroup, "Dock");

        for (var i = 0; i < fruitTreeCount; i++)
        {
            var angle = zoneAngle + UnityEngine.Random.Range(-40f, 40f);
            var radius = UnityEngine.Random.Range(14f, 30f);
            var pos = PointOnCircle(radius, angle);
            Spawn(PickFromPool(fruitTreePrefabPool), BuildFallbackFruitTree, pos,
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), westGroup, $"FruitTree_{i}");
        }
    }

    #endregion

    #region East Zone - Well & Rest Area

    private void GenerateEastZone()
    {
        const float zoneAngle = 0f;

        var wellPos = PointOnCircle(20f, zoneAngle);
        Spawn(eastWellPrefab, BuildFallbackWell, wellPos, Quaternion.identity, eastGroup, "EastStoneWell");

        for (var i = 0; i < benchCount; i++)
        {
            var t = benchCount > 1 ? i / (float)(benchCount - 1) : 0.5f;
            var angle = zoneAngle + Mathf.Lerp(-30f, 30f, t);
            var pos = PointOnCircle(24f, angle);
            Spawn(benchPrefab, BuildFallbackBench, pos, Quaternion.Euler(0f, -angle + 90f, 0f), eastGroup, $"Bench_{i}");
        }

        for (var i = 0; i < lanternCount; i++)
        {
            var t = lanternCount > 1 ? i / (float)(lanternCount - 1) : 0.5f;
            var angle = zoneAngle + Mathf.Lerp(-45f, 45f, t);
            var radius = UnityEngine.Random.Range(15f, 31f);
            var pos = PointOnCircle(radius, angle);
            Spawn(lanternPrefab, BuildFallbackLantern, pos, Quaternion.identity, eastGroup, $"Lantern_{i}");
        }
    }

    #endregion

    #region Periphery

    private void GeneratePeriphery()
    {
        var edgeRadius = islandRadius - 3f;
        var zoneAngles = new[] { 0f, 90f, 180f };

        for (var i = 0; i < flowerBushCount; i++)
        {
            var angle = PickPeripheryAngle(zoneAngles);
            var radius = UnityEngine.Random.Range(edgeRadius - 4f, edgeRadius);
            Spawn(PickFromPool(flowerBushPrefabPool), BuildFallbackFlowerBush, PointOnCircle(radius, angle),
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), peripheryGroup, $"FlowerBush_{i}");
        }

        for (var i = 0; i < rockCount; i++)
        {
            var angle = PickPeripheryAngle(zoneAngles);
            var radius = UnityEngine.Random.Range(edgeRadius - 5f, edgeRadius + 1f);
            var scale = UnityEngine.Random.Range(0.5f, 1.3f);
            Spawn(PickFromPool(rockPrefabPool), () => BuildFallbackRock(scale), PointOnCircle(radius, angle),
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), peripheryGroup, $"Rock_{i}");
        }

        for (var i = 0; i < fenceSegmentCount; i++)
        {
            var angle = PickPeripheryAngle(zoneAngles);
            Spawn(PickFromPool(fencePrefabPool), () => BuildFallbackFenceSegment(2.2f), PointOnCircle(edgeRadius, angle),
                TangentRotation(angle), peripheryGroup, $"PeripheryFence_{i}");
        }
    }

    private float PickPeripheryAngle(float[] excludeAngles)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var angle = UnityEngine.Random.Range(0f, 360f);
            var tooClose = false;
            foreach (var exclude in excludeAngles)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(angle, exclude)) < 20f)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose) return angle;
        }
        return 270f; // south side is always free of named zones
    }

    #endregion

    #region Characters

    private void GenerateCharacters()
    {
        if (stickmanCount <= 0) return;

        for (var i = 0; i < stickmanCount; i++)
        {
            var angle = UnityEngine.Random.Range(0f, 360f);
            var radius = UnityEngine.Random.Range(10f, islandRadius - 6f);
            var pos = PointOnCircle(radius, angle, 0.9f);
            Spawn(stickmanPrefab, BuildFallbackStickman, pos,
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), characterGroup, $"Stickman_{i}");
        }
    }

    // Reference: grey humanoid with clear head + torso + legs silhouette.
    // Old fallback was a single Capsule — no head, no legs visible.
    private GameObject BuildFallbackStickman()
    {
        var stickman   = NewEmpty("FallbackStickman");
        var skinColor  = new Color(0.75f, 0.75f, 0.75f); // grey like the reference
        var shirtColor = new Color(0.55f, 0.55f, 0.60f); // slightly blue-grey shirt

        // Body / torso
        AddPart(stickman, PrimitiveType.Capsule,
            new Vector3(0, 0.72f, 0), new Vector3(0.30f, 0.36f, 0.30f), shirtColor);
        // Head
        AddPart(stickman, PrimitiveType.Sphere,
            new Vector3(0, 1.30f, 0), Vector3.one * 0.28f, skinColor);
        // Legs
        AddPart(stickman, PrimitiveType.Capsule,
            new Vector3(-0.10f, 0.22f, 0), new Vector3(0.14f, 0.24f, 0.14f), shirtColor);
        AddPart(stickman, PrimitiveType.Capsule,
            new Vector3( 0.10f, 0.22f, 0), new Vector3(0.14f, 0.24f, 0.14f), shirtColor);
        return stickman;
    }

    #endregion

    #region Fallback Composite Builders

    // Reference image: Great Tree is massive — thick trunk, huge multi-tier dark
    // green canopy filling the center, blue magical glow at base. Old fallback
    // (6f tall, 3.2f radius) was barely visible behind the path stones.
    private GameObject BuildFallbackTree(float trunkHeight, float trunkRadius, float foliageRadius, Color trunkColor, Color foliageColor)
    {
        var tree = NewEmpty("FallbackTree");

        // Trunk — thicker and taller than before; the Oak prefab's trunk is thick.
        float th = trunkHeight * 1.6f;  // 6f→9.6f
        float tr = trunkRadius * 1.8f;  // 0.9f→1.62f
        AddPart(tree, PrimitiveType.Cylinder,
            new Vector3(0, th * 0.5f, 0),
            new Vector3(tr * 2f, th * 0.5f, tr * 2f),
            trunkColor);

        // Gnarled mid-trunk roots — ring of small cylinders at base
        for (int r = 0; r < 6; r++)
        {
            float ra = r * 60f * Mathf.Deg2Rad;
            AddPart(tree, PrimitiveType.Cylinder,
                new Vector3(Mathf.Sin(ra) * tr * 1.4f, 0.8f, Mathf.Cos(ra) * tr * 1.4f),
                new Vector3(tr * 0.35f, 0.8f, tr * 0.35f),
                trunkColor,
                localRot: Quaternion.Euler(12f, r * 60f, 0f));
        }

        // Three-tier canopy — large dark green spheres stacked for an imposing silhouette.
        float fr = foliageRadius * 1.9f;  // 3.2f→6.08f — much bigger
        Color dark  = new Color(0.15f, 0.38f, 0.12f); // dark forest canopy
        Color mid   = new Color(0.22f, 0.50f, 0.16f);
        Color light = new Color(0.30f, 0.60f, 0.20f);

        // Bottom tier — widest
        AddPart(tree, PrimitiveType.Sphere,
            new Vector3(0, th + fr * 0.55f, 0),
            new Vector3(fr * 2.2f, fr * 1.6f, fr * 2.2f), dark);
        // Secondary lobes around bottom tier for irregular silhouette
        for (int l = 0; l < 5; l++)
        {
            float la = l * 72f * Mathf.Deg2Rad;
            float lr = fr * 0.85f;
            AddPart(tree, PrimitiveType.Sphere,
                new Vector3(Mathf.Sin(la)*lr, th + fr * 0.4f, Mathf.Cos(la)*lr),
                Vector3.one * fr * 1.1f, mid);
        }
        // Mid tier
        AddPart(tree, PrimitiveType.Sphere,
            new Vector3(0, th + fr * 1.55f, 0),
            new Vector3(fr * 1.6f, fr * 1.4f, fr * 1.6f), mid);
        // Top tier — pointed, lighter
        AddPart(tree, PrimitiveType.Sphere,
            new Vector3(0, th + fr * 2.6f, 0),
            new Vector3(fr * 0.9f, fr * 1.1f, fr * 0.9f), light);

        return tree;
    }

    // Reference image: tall dark pointed conifers around the perimeter — old
    // fallback was only ~3 units tall and 3 sphere tiers. Updated: 5-tier
    // tapering cones, deeper green, overall height ~7 units.
    private GameObject BuildFallbackPineTree()
    {
        var tree = NewEmpty("FallbackPineTree");
        var trunkColor   = new Color(0.28f, 0.18f, 0.10f); // darker bark
        var foliageColor = new Color(0.10f, 0.28f, 0.12f); // very dark pine green
        var tipColor     = new Color(0.14f, 0.34f, 0.14f);

        // Trunk
        AddPart(tree, PrimitiveType.Cylinder,
            new Vector3(0, 0.7f, 0), new Vector3(0.22f, 0.7f, 0.22f), trunkColor);

        // 5 tapering foliage tiers — each smaller and higher, cone silhouette
        float[] tierY     = { 1.1f, 2.2f, 3.2f, 4.1f, 5.0f };
        float[] tierSizeX = { 2.2f, 1.75f, 1.35f, 0.95f, 0.6f };
        float[] tierSizeY = { 1.2f, 1.1f, 1.0f, 0.9f, 0.85f };
        for (int t = 0; t < 5; t++)
        {
            Color c = t < 3 ? foliageColor : tipColor;
            AddPart(tree, PrimitiveType.Sphere,
                new Vector3(0, tierY[t], 0),
                new Vector3(tierSizeX[t], tierSizeY[t], tierSizeX[t]), c);
        }
        // Tip spike
        AddPart(tree, PrimitiveType.Cylinder,
            new Vector3(0, 5.9f, 0), new Vector3(0.08f, 0.45f, 0.08f), tipColor);
        return tree;
    }

    // Reference: mid-size round-canopy fruit trees with visible colored fruits
    // on the outer edge of the canopy. Old fallback was one big sphere + 5
    // tiny fruit dots — barely visible. Updated: trunk + 3 canopy lobes + 8
    // well-spaced fruit spheres.
    private GameObject BuildFallbackFruitTree()
    {
        var tree = NewEmpty("FallbackFruitTree");
        var trunkColor   = new Color(0.38f, 0.25f, 0.13f);
        var foliageColor = new Color(0.20f, 0.48f, 0.18f);
        var foliageDark  = new Color(0.14f, 0.36f, 0.12f);
        var fruitColor   = new Color(0.85f, 0.22f, 0.12f);

        // Trunk — taller than before
        AddPart(tree, PrimitiveType.Cylinder,
            new Vector3(0, 1.1f, 0), new Vector3(0.38f, 1.1f, 0.38f), trunkColor);

        // Main canopy sphere
        AddPart(tree, PrimitiveType.Sphere,
            new Vector3(0, 3.0f, 0), Vector3.one * 3.0f, foliageColor);
        // Two side lobes for natural irregular shape
        AddPart(tree, PrimitiveType.Sphere,
            new Vector3(-1.0f, 2.7f, 0.4f), Vector3.one * 2.0f, foliageDark);
        AddPart(tree, PrimitiveType.Sphere,
            new Vector3(0.9f, 2.8f, -0.5f), Vector3.one * 1.8f, foliageDark);

        // 8 fruits spread evenly around canopy surface — large enough to read
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            float r = 1.3f;
            var offset = new Vector3(
                Mathf.Cos(angle) * r,
                3.0f + UnityEngine.Random.Range(-0.5f, 0.5f),
                Mathf.Sin(angle) * r);
            AddPart(tree, PrimitiveType.Sphere, offset, Vector3.one * 0.30f, fruitColor);
        }
        return tree;
    }

    private GameObject BuildFallbackRock(float baseScale)
    {
        var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var jitter = new Vector3(UnityEngine.Random.Range(0.7f, 1.3f), UnityEngine.Random.Range(0.5f, 1f), UnityEngine.Random.Range(0.7f, 1.3f));
        rock.transform.localScale = Vector3.Scale(Vector3.one * baseScale, jitter);
        rock.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-10f, 10f));
        ApplyFallbackMaterial(rock, new Color(0.45f, 0.43f, 0.4f));
        StripCollider(rock);
        return rock;
    }

    private GameObject BuildFallbackFlowerBush()
    {
        var bush = NewEmpty("FallbackFlowerBush");
        var leafColor = new Color(0.2f, 0.45f, 0.18f);
        var flowerColors = new[]
        {
            new Color(0.9f, 0.3f, 0.5f),
            new Color(0.95f, 0.8f, 0.2f),
            new Color(0.85f, 0.85f, 0.9f)
        };

        AddPart(bush, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.7f, leafColor);

        var flowerColor = flowerColors[UnityEngine.Random.Range(0, flowerColors.Length)];
        for (var i = 0; i < 4; i++)
        {
            var offset = UnityEngine.Random.insideUnitSphere * 0.3f + Vector3.up * 0.35f;
            AddPart(bush, PrimitiveType.Sphere, offset, Vector3.one * 0.15f, flowerColor);
        }

        return bush;
    }

    private GameObject BuildFallbackFenceSegment(float length)
    {
        var fence = NewEmpty("FallbackFenceSegment");
        var postColor = new Color(0.42f, 0.3f, 0.18f);
        AddPart(fence, PrimitiveType.Cube, new Vector3(-length * 0.5f, 0.4f, 0), new Vector3(0.12f, 0.8f, 0.12f), postColor);
        AddPart(fence, PrimitiveType.Cube, new Vector3(length * 0.5f, 0.4f, 0), new Vector3(0.12f, 0.8f, 0.12f), postColor);
        AddPart(fence, PrimitiveType.Cube, new Vector3(0, 0.55f, 0), new Vector3(length, 0.1f, 0.08f), postColor);
        AddPart(fence, PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(length, 0.1f, 0.08f), postColor);
        return fence;
    }

    private GameObject BuildFallbackBench()
    {
        var bench = NewEmpty("FallbackBench");
        var woodColor = new Color(0.45f, 0.3f, 0.16f);
        AddPart(bench, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), new Vector3(1.4f, 0.1f, 0.5f), woodColor);
        AddPart(bench, PrimitiveType.Cube, new Vector3(0, 0.8f, -0.2f), new Vector3(1.4f, 0.6f, 0.08f), woodColor);
        AddPart(bench, PrimitiveType.Cube, new Vector3(-0.6f, 0.22f, 0), new Vector3(0.1f, 0.45f, 0.45f), woodColor);
        AddPart(bench, PrimitiveType.Cube, new Vector3(0.6f, 0.22f, 0), new Vector3(0.1f, 0.45f, 0.45f), woodColor);
        return bench;
    }

    private GameObject BuildFallbackLantern()
    {
        var lantern = NewEmpty("FallbackLantern");
        var poleColor = new Color(0.2f, 0.2f, 0.22f);
        var glowColor = new Color(1f, 0.75f, 0.4f);

        AddPart(lantern, PrimitiveType.Cylinder, new Vector3(0, 1.2f, 0), new Vector3(0.08f, 1.2f, 0.08f), poleColor);
        AddPart(lantern, PrimitiveType.Cube, new Vector3(0, 2.5f, 0), Vector3.one * 0.3f, glowColor, false, true);

        var light = lantern.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.range = 6f;
        light.intensity = 1.4f;

        return lantern;
    }

    private GameObject BuildFallbackCrystalShrine()
    {
        var shrine = NewEmpty("FallbackCrystalShrine");
        var stoneColor = new Color(0.4f, 0.38f, 0.42f);
        var crystalColor = new Color(0.5f, 0.3f, 0.95f);

        AddPart(shrine, PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0), new Vector3(1.6f, 0.4f, 1.6f), stoneColor);
        AddPart(shrine, PrimitiveType.Cylinder, new Vector3(0, 0.9f, 0), new Vector3(0.9f, 0.5f, 0.9f), stoneColor);
        AddPart(shrine, PrimitiveType.Cube, new Vector3(0, 1.9f, 0), new Vector3(0.4f, 1.1f, 0.4f), crystalColor,
            false, true, Quaternion.Euler(0f, 45f, 0f));

        var light = shrine.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = crystalColor;
        light.range = 8f;
        light.intensity = 1.8f;

        return shrine;
    }

    private GameObject BuildFallbackWell()
    {
        var well = NewEmpty("FallbackWell");
        var stoneColor = new Color(0.45f, 0.43f, 0.4f);
        var roofColor = new Color(0.4f, 0.22f, 0.14f);

        AddPart(well, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(1.4f, 0.5f, 1.4f), stoneColor);
        AddPart(well, PrimitiveType.Cylinder, new Vector3(-0.9f, 1.2f, 0), new Vector3(0.1f, 1.2f, 0.1f), roofColor);
        AddPart(well, PrimitiveType.Cylinder, new Vector3(0.9f, 1.2f, 0), new Vector3(0.1f, 1.2f, 0.1f), roofColor);
        AddPart(well, PrimitiveType.Cube, new Vector3(0, 2.3f, 0), new Vector3(2.2f, 0.15f, 1.6f), roofColor);

        return well;
    }

    private GameObject BuildFallbackDock(float length)
    {
        var dock = NewEmpty("FallbackDock");
        var woodColor = new Color(0.42f, 0.28f, 0.16f);
        AddPart(dock, PrimitiveType.Cube, new Vector3(0, 0.05f, length * 0.5f), new Vector3(1.4f, 0.1f, length), woodColor);

        for (var i = 0; i < 4; i++)
        {
            var z = i * (length / 3f) + 0.3f;
            AddPart(dock, PrimitiveType.Cylinder, new Vector3(-0.6f, -0.3f, z), new Vector3(0.08f, 0.4f, 0.08f), woodColor);
            AddPart(dock, PrimitiveType.Cylinder, new Vector3(0.6f, -0.3f, z), new Vector3(0.08f, 0.4f, 0.08f), woodColor);
        }

        return dock;
    }

    private GameObject BuildFallbackLilyPad()
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.transform.localScale = new Vector3(UnityEngine.Random.Range(0.4f, 0.7f), 0.02f, UnityEngine.Random.Range(0.4f, 0.7f));
        ApplyFallbackMaterial(pad, new Color(0.18f, 0.45f, 0.2f));
        StripCollider(pad);
        return pad;
    }

    private GameObject BuildFallbackCropPatch(int rows, int columns)
    {
        var patch = NewEmpty("FallbackCropPatch");
        var soilColor = new Color(0.32f, 0.22f, 0.14f);
        var cropColor = new Color(0.35f, 0.55f, 0.2f);

        var width = columns * 0.6f;
        var depthSize = rows * 0.6f;

        AddPart(patch, PrimitiveType.Cube, new Vector3(0, 0.03f, 0), new Vector3(width, 0.06f, depthSize), soilColor);

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                var pos = new Vector3(-width * 0.5f + 0.3f + c * 0.6f, 0.15f, -depthSize * 0.5f + 0.3f + r * 0.6f);
                AddPart(patch, PrimitiveType.Sphere, pos, Vector3.one * 0.22f, cropColor);
            }
        }

        AddFenceBorder(patch, width, depthSize);

        return patch;
    }

    private void AddFenceBorder(GameObject parent, float width, float depthSize)
    {
        var nearPrefab = PickFromPool(fencePrefabPool);
        var farPrefab = PickFromPool(fencePrefabPool);

        GameObject nearFence = nearPrefab != null ? Instantiate(nearPrefab) : BuildFallbackFenceSegment(width);
        nearFence.transform.SetParent(parent.transform, false);
        nearFence.transform.localPosition = new Vector3(0, 0, -depthSize * 0.5f - 0.1f);

        GameObject farFence = farPrefab != null ? Instantiate(farPrefab) : BuildFallbackFenceSegment(width);
        farFence.transform.SetParent(parent.transform, false);
        farFence.transform.localPosition = new Vector3(0, 0, depthSize * 0.5f + 0.1f);
        farFence.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private GameObject BuildFallbackPavingStone(float size)
    {
        var stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stone.transform.localScale = new Vector3(size, 0.06f, size);
        ApplyFallbackMaterial(stone, new Color(0.55f, 0.53f, 0.5f));
        StripCollider(stone);
        return stone;
    }

    private GameObject BuildFallbackPathSegment(float length, float width)
    {
        var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seg.transform.localScale = new Vector3(length, 0.05f, width);
        ApplyFallbackMaterial(seg, new Color(0.5f, 0.38f, 0.24f));
        StripCollider(seg);
        return seg;
    }

    #endregion
}

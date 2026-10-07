#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildFloatingIsland
{
    const string GenFolder = "Assets/_Generated";

    static readonly string[] RockPrefabPaths =
    {
        "Assets/Blinktool/Low poly rocks/Prefabs/Mossy/Rocks_Mossy_17.prefab",
        "Assets/Blinktool/Low poly rocks/Prefabs/Granite/Rocks_Granite_37.prefab",
        "Assets/Blinktool/Low poly rocks/Prefabs/Sandstone/Rocks_Sandstone_25.prefab",
        "Assets/Blinktool/Low poly rocks/Prefabs/Mossy/Rocks_Mossy_45.prefab",
        "Assets/Blinktool/Low poly rocks/Prefabs/Granite/Rocks_Granite_7.prefab",
        "Assets/Blinktool/Low poly rocks/Prefabs/Sandstone/Rocks_Sandstone_43.prefab",
    };

    static readonly Vector3[] RockPositions =
    {
        new Vector3(20f, -1f, 6f),
        new Vector3(-19f, 0.5f, -9f),
        new Vector3(4f, -2f, 23f),
        new Vector3(-11f, 1f, 19f),
        new Vector3(17f, -0.5f, -18f),
        new Vector3(-22f, -1.5f, 3f),
    };

    static readonly Vector3[] CloudCenters =
    {
        new Vector3(20f, 9f, 5f),
        new Vector3(-18f, 11f, -8f),
        new Vector3(5f, 13f, 22f),
        new Vector3(-10f, 8f, 18f),
        new Vector3(15f, 10f, -20f),
    };

    [MenuItem("Tools/Radiant Orchard/Build Floating Island - Step 1")]
    static void Build()
    {
        Undo.SetCurrentGroupName("Build Floating Island Step 1");
        int undoGroup = Undo.GetCurrentGroup();

        if (!AssetDatabase.IsValidFolder(GenFolder))
            AssetDatabase.CreateFolder("Assets", "_Generated");

        Mesh sourceSphere = GetBuiltinUnitSphereMesh();

        // FIX: Dark lush green (#2D6B1F) to match reference image — old value
        // (0.298, 0.604, 0.173) was lime-bright and matched nothing in the ref.
        Material grassMat = GetOrCreateMaterial(GenFolder + "/Mat_IslandGrass.mat", new Color(0.176f, 0.420f, 0.122f), 0.12f, false);
        // Rock/cliff: slightly warmer, darker brown so it reads as stone not sand.
        Material rockMat = GetOrCreateMaterial(GenFolder + "/Mat_IslandRock.mat", new Color(0.42f, 0.36f, 0.28f), 0.18f, false);
        // Ocean: deeper, richer blue — reference shows saturated tropical sea.
        Material waterMat = GetOrCreateMaterial(GenFolder + "/Mat_OceanWater.mat", new Color(0.10f, 0.45f, 0.72f, 0.85f), 0.88f, true);
        Material cloudMat = GetOrCreateMaterial(GenFolder + "/Mat_CloudWhite.mat", new Color(0.96f, 0.96f, 0.96f), 0.1f, false);

        BuildIslandBase(sourceSphere, grassMat, rockMat);
        BuildEnvironment(sourceSphere, waterMat, cloudMat);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Floating Island Step 1 built: island base mesh, ocean plane, floating rocks, cloud wisps.");
    }

    static void BuildIslandBase(Mesh sourceSphere, Material grassMat, Material rockMat)
    {
        GameObject islandRoot = GameObject.Find("Island");
        if (islandRoot == null)
        {
            islandRoot = new GameObject("Island");
            Undo.RegisterCreatedObjectUndo(islandRoot, "Create Island");
        }

        Transform oldBase = islandRoot.transform.Find("FloatingislandmainBASE");
        if (oldBase != null)
            Undo.DestroyObjectImmediate(oldBase.gameObject);

        GameObject baseGO = new GameObject("FloatingislandmainBASE", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        Undo.RegisterCreatedObjectUndo(baseGO, "Create Floating Island Base");
        baseGO.transform.SetParent(islandRoot.transform, false);
        baseGO.transform.localPosition = Vector3.zero;
        // FIX: Scale increased from (16,5,16) → (40,8,40) to match
        // IslandSceneGenerator.islandRadius=40. Old scale made the island
        // mesh only 16 world-units wide while the entire scene layout (paths,
        // zones, river) assumed a 40-unit radius — objects were spawning far
        // outside the visible island edge. Y=8 gives more imposing cliff depth.
        baseGO.transform.localScale = new Vector3(40f, 8f, 40f);

        string meshPath = GenFolder + "/Mesh_FloatingIslandBase.asset";
        AssetDatabase.DeleteAsset(meshPath);
        Mesh islandMesh = BuildIslandMesh(sourceSphere);
        AssetDatabase.CreateAsset(islandMesh, meshPath);

        baseGO.GetComponent<MeshFilter>().sharedMesh = islandMesh;
        baseGO.GetComponent<MeshRenderer>().sharedMaterials = new[] { grassMat, rockMat };
        baseGO.GetComponent<MeshCollider>().sharedMesh = islandMesh;
    }

    static void BuildEnvironment(Mesh sourceSphere, Material waterMat, Material cloudMat)
    {
        GameObject env = GameObject.Find("Environment_Step1");
        if (env == null)
        {
            env = new GameObject("Environment_Step1");
            Undo.RegisterCreatedObjectUndo(env, "Create Environment");
        }
        else
        {
            for (int i = env.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(env.transform.GetChild(i).gameObject);
        }

        GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(ocean, "Create Ocean");
        Object.DestroyImmediate(ocean.GetComponent<Collider>());
        ocean.name = "Ocean";
        ocean.transform.SetParent(env.transform, false);
        // FIX: Ocean was at y=-30 — so far below the island (top at y≈0) that
        // it was invisible from the isometric camera. Reference image shows the
        // ocean surface just below the island's cliff base. Island Y scale is 8,
        // so the bottom hangs to about y=-8; put ocean at y=-9 so it's just
        // below the rock underside and visible from a 50° camera pitch.
        ocean.transform.position = new Vector3(0f, -9f, 0f);
        // Scale 50×50 = 500×500 world-unit plane — fills the horizon fully.
        ocean.transform.localScale = new Vector3(50f, 1f, 50f);
        ocean.GetComponent<MeshRenderer>().sharedMaterial = waterMat;

        string cloudMeshPath = GenFolder + "/Mesh_CloudPuff.asset";
        AssetDatabase.DeleteAsset(cloudMeshPath);
        Mesh cloudMesh = BuildFlatShadedUnitSphere(sourceSphere);
        AssetDatabase.CreateAsset(cloudMesh, cloudMeshPath);

        GameObject rocksParent = new GameObject("FloatingRocks");
        Undo.RegisterCreatedObjectUndo(rocksParent, "Create Floating Rocks Root");
        rocksParent.transform.SetParent(env.transform, false);
        SpawnFloatingRocks(rocksParent.transform);

        GameObject cloudsParent = new GameObject("CloudWisps");
        Undo.RegisterCreatedObjectUndo(cloudsParent, "Create Cloud Wisps Root");
        cloudsParent.transform.SetParent(env.transform, false);
        SpawnClouds(cloudsParent.transform, cloudMesh, cloudMat);
    }

    static void SpawnFloatingRocks(Transform parent)
    {
        for (int i = 0; i < RockPrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockPrefabPaths[i]);
            if (prefab == null) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Create Floating Rock");
            instance.transform.localPosition = RockPositions[i % RockPositions.Length];
            instance.transform.localRotation = Quaternion.Euler(0f, (i * 57f) % 360f, 0f);
            instance.transform.localScale = Vector3.one * Random.Range(0.6f, 1.1f);
        }
    }

    static void SpawnClouds(Transform parent, Mesh cloudMesh, Material cloudMat)
    {
        for (int c = 0; c < CloudCenters.Length; c++)
        {
            GameObject cloud = new GameObject("CloudWisp_" + (c + 1));
            Undo.RegisterCreatedObjectUndo(cloud, "Create Cloud Wisp");
            cloud.transform.SetParent(parent, false);
            cloud.transform.localPosition = CloudCenters[c];

            int puffCount = 3 + (c % 3);
            for (int p = 0; p < puffCount; p++)
            {
                GameObject puff = new GameObject("Puff_" + (p + 1), typeof(MeshFilter), typeof(MeshRenderer));
                puff.transform.SetParent(cloud.transform, false);
                float angle = p * (360f / puffCount);
                float radius = 1.2f + p * 0.3f;
                puff.transform.localPosition = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    Mathf.Sin(p) * 0.3f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
                puff.transform.localScale = new Vector3(2.2f, 1.1f, 1.6f) * Random.Range(0.8f, 1.2f);
                puff.GetComponent<MeshFilter>().sharedMesh = cloudMesh;
                puff.GetComponent<MeshRenderer>().sharedMaterial = cloudMat;
            }
        }
    }

    static Mesh GetBuiltinUnitSphereMesh()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh clone = Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
        Object.DestroyImmediate(temp);
        return clone;
    }

    // Duplicates one vertex per triangle corner (hard edges) for the faceted low-poly look,
    // and returns each original (pre-deform) vertex Y so callers can key deform/material logic off it.
    static void GetFlatShadedArrays(Mesh source, out Vector3[] verts, out int[] tris, out Vector2[] uvs, out float[] origY)
    {
        Vector3[] sv = source.vertices;
        int[] st = source.triangles;
        Vector2[] suv = source.uv;

        verts = new Vector3[st.Length];
        tris = new int[st.Length];
        uvs = new Vector2[st.Length];
        origY = new float[st.Length];

        for (int i = 0; i < st.Length; i++)
        {
            int srcIndex = st[i];
            verts[i] = sv[srcIndex];
            uvs[i] = (suv != null && suv.Length == sv.Length) ? suv[srcIndex] : Vector2.zero;
            origY[i] = sv[srcIndex].y;
            tris[i] = i;
        }
    }

    static Mesh BuildIslandMesh(Mesh sourceSphere)
    {
        GetFlatShadedArrays(sourceSphere, out var verts, out var tris, out var uvs, out var origY);

        const float topFlatten = 0.55f;
        const float bottomStretch = 2.4f;
        const float taperPower = 1.4f;
        const float taperAmount = 0.9f;
        const float minTaperFactor = 0.06f;
        const float grassThreshold = -0.08f;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            if (v.y >= 0f)
            {
                v.y *= topFlatten;
            }
            else
            {
                float n = Mathf.Clamp01(-v.y / 0.5f);
                float taperFactor = Mathf.Max(minTaperFactor, 1f - Mathf.Pow(n, taperPower) * taperAmount);
                v.x *= taperFactor;
                v.z *= taperFactor;
                v.y *= bottomStretch;
            }
            verts[i] = v;
        }

        var grassTris = new List<int>();
        var rockTris = new List<int>();
        for (int i = 0; i < tris.Length; i += 3)
        {
            float avgY = (origY[tris[i]] + origY[tris[i + 1]] + origY[tris[i + 2]]) / 3f;
            var bucket = avgY > grassThreshold ? grassTris : rockTris;
            bucket.Add(tris[i]);
            bucket.Add(tris[i + 1]);
            bucket.Add(tris[i + 2]);
        }

        var mesh = new Mesh { name = "FloatingIslandBase_Generated" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(grassTris, 0);
        mesh.SetTriangles(rockTris, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BuildFlatShadedUnitSphere(Mesh sourceSphere)
    {
        GetFlatShadedArrays(sourceSphere, out var verts, out var tris, out var uvs, out _);
        var mesh = new Mesh { name = "CloudPuff_Generated" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Material GetOrCreateMaterial(string path, Color color, float smoothness, bool transparent)
    {
        // No pipeline asset assigned in Graphics Settings means the project is on the
        // Built-in Render Pipeline even though the URP package is installed — using a
        // URP shader in that case renders pink, so pick the shader that matches reality.
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        Shader shader = isURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        mat.color = color;
        mat.SetFloat(isURP ? "_Smoothness" : "_Glossiness", smoothness);

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
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                mat.SetFloat("_Mode", 3f); // Transparent
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

        EditorUtility.SetDirty(mat);
        return mat;
    }
}
#endif

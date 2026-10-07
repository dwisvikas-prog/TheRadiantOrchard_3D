#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

// Hanging moss/vines around the cliff edge, and a tall waterfall pouring
// straight down from the island's underside into a mist/cloud bank below —
// the "floating island in the sky" silhouette from the reference, distinct
// from the two side waterfalls GenerateWaterFeatures.cs already built.
public static class GenerateIslandAtmosphere
{
    [MenuItem("Tools/Radiant Orchard/Generate Island Atmosphere (Vines + Bottom Waterfall)")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Island Atmosphere");
        int undoGroup = Undo.GetCurrentGroup();

        var root = FindOrCreateGroup("IslandAtmosphere");
        ClearChildren(root);

        BuildHangingVines(root, radius: 39f, count: 22);
        BuildRockyCliffBase(root);

        // Real rising-mist ParticleSystems at the base of the two side
        // waterfalls from GenerateWaterFeatures.cs (radius 39, base of their
        // 12-unit column sits around y=-16) — replaces the earlier static
        // sphere "mist bank" with actual moving particles.
        BuildMistParticles(root, PointOnCircle(180f, 40f) + new Vector3(0f, -14.5f, 0f), "WaterfallMist_Bottom");
        BuildMistParticles(root, PointOnCircle(90f, 40f) + new Vector3(0f, -14.5f, 0f), "WaterfallMist_Right");

        BuildSkyClouds(root);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: hanging vines, bottom waterfall, mist at all waterfalls, and sky clouds generated. Save the scene (Ctrl+S).");
    }

    private static void BuildHangingVines(Transform parent, float radius, int count)
    {
        var vineColor = new Color(0.16f, 0.38f, 0.14f);

        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count) + Random.Range(-4f, 4f);
            float rad = angle * Mathf.Deg2Rad;
            var edgePos = new Vector3(Mathf.Sin(rad) * radius, -1f, Mathf.Cos(rad) * radius);

            float length = Random.Range(3f, 7f);

            var vine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(vine, "Create Vine");
            vine.name = "Vine_" + i;
            vine.transform.SetParent(parent, false);
            vine.transform.localPosition = edgePos + new Vector3(0f, -length * 0.5f, 0f);
            vine.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
            vine.transform.localScale = new Vector3(0.12f, length * 0.5f, 0.12f);
            ApplyColor(vine, vineColor);
            Object.DestroyImmediate(vine.GetComponent<Collider>());

            // a couple of small leaf clusters along the vine
            int leafCount = Random.Range(2, 4);
            for (int l = 0; l < leafCount; l++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(leaf, "Create Vine Leaf");
                leaf.name = "VineLeaf";
                leaf.transform.SetParent(vine.transform, false);
                float t = (l + 1) / (float)(leafCount + 1);
                leaf.transform.localPosition = new Vector3(0f, 1f - t * 2f, 0f);
                leaf.transform.localScale = new Vector3(2.2f, 0.7f, 2.2f);
                ApplyColor(leaf, new Color(0.22f, 0.48f, 0.18f));
                Object.DestroyImmediate(leaf.GetComponent<Collider>());
            }
        }
    }

    // Real rock prefabs (not a primitive) clustered in a downward-tapering
    // ring around the island's underside, so it reads as a chunky floating
    // rock mass instead of the bare disc edge / the single pillar this
    // replaced.
    private static readonly string[] CliffRockPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Menhir_Rock_02.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_River_Rock_Pile_02.prefab",
    };

    // Real rising-mist particles at a waterfall base — slow upward drift,
    // fading in then out, instead of static spheres.
    private static void BuildMistParticles(Transform parent, Vector3 center, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Mist Particles");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 3f;
        main.startSpeed = 0.6f;
        main.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startColor = new Color(1f, 1f, 1f, 0.5f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 18f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 3.5f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        // x/y/z curves must share one mode (TwoConstants here) or Unity
        // logs "Particle Velocity curves must all be in the same mode" every frame.
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(1f, 1.4f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.45f, 0.25f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var particleShader = Shader.Find(isURP ? "Universal Render Pipeline/Particles/Unlit" : "Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");

        var mat = new Material(particleShader) { color = Color.white };
        if (!isURP)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        renderer.material = mat;
    }

    private static void BuildRockyCliffBase(Transform parent)
    {
        var pool = new System.Collections.Generic.List<GameObject>();
        foreach (var path in CliffRockPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) pool.Add(prefab);
        }
        if (pool.Count == 0) return;

        var mossyGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Blinktool/Low poly rocks/Prefabs/Granite" });
        foreach (var guid in mossyGuids)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null) pool.Add(prefab);
        }

        const int ringCount = 3;
        float[] radii = { 38f, 33f, 26f };
        float[] depths = { -1f, -4f, -7f };
        int[] countsPerRing = { 14, 10, 6 };

        for (int ring = 0; ring < ringCount; ring++)
        {
            for (int i = 0; i < countsPerRing[ring]; i++)
            {
                float angle = i * (360f / countsPerRing[ring]) + Random.Range(-8f, 8f);
                var pos = PointOnCircle(angle, radii[ring] + Random.Range(-1.5f, 1.5f));
                pos.y = depths[ring] + Random.Range(-1f, 1f);

                var prefab = pool[Random.Range(0, pool.Count)];
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                Undo.RegisterCreatedObjectUndo(instance, "Create Cliff Rock");
                instance.transform.localPosition = pos;
                instance.transform.localRotation = Quaternion.Euler(
                    Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                float scale = Random.Range(1.2f, 2.4f);
                instance.transform.localScale = Vector3.one * scale;

                var collider = instance.GetComponentInChildren<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }
        }
    }

    private static void BuildMistBank(Transform parent, Vector3 center, int puffCount, float spread)
    {
        var mistColor = new Color(1f, 1f, 1f, 0.55f);

        for (int i = 0; i < puffCount; i++)
        {
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(puff, "Create Mist Puff");
            puff.name = "MistPuff_" + i;
            puff.transform.SetParent(parent, false);
            var offset = Random.insideUnitSphere;
            offset.y *= 0.3f;
            puff.transform.localPosition = center + offset * spread;
            float scale = Random.Range(spread * 0.6f, spread * 1.1f);
            puff.transform.localScale = new Vector3(scale, scale * 0.5f, scale);
            ApplyColor(puff, mistColor, true);
            Object.DestroyImmediate(puff.GetComponent<Collider>());
        }
    }

    private static void BuildSkyClouds(Transform parent)
    {
        var cloudColor = new Color(1f, 1f, 1f, 0.85f);

        for (int c = 0; c < 8; c++)
        {
            float angle = c * 45f + Random.Range(-12f, 12f);
            float radius = Random.Range(48f, 62f);
            float height = Random.Range(6f, 16f);
            var center = PointOnCircle(angle, radius) + new Vector3(0f, height, 0f);

            var cloud = new GameObject("SkyCloud_" + c);
            Undo.RegisterCreatedObjectUndo(cloud, "Create Sky Cloud");
            cloud.transform.SetParent(parent, false);
            cloud.transform.localPosition = center;

            int puffCount = Random.Range(3, 5);
            for (int p = 0; p < puffCount; p++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(puff, "Create Cloud Puff");
                puff.name = "Puff_" + p;
                puff.transform.SetParent(cloud.transform, false);
                puff.transform.localPosition = new Vector3(
                    Random.Range(-3f, 3f), Random.Range(-0.6f, 0.6f), Random.Range(-2f, 2f));
                float scale = Random.Range(2.5f, 4.5f);
                puff.transform.localScale = new Vector3(scale, scale * 0.6f, scale);
                ApplyColor(puff, cloudColor, true);
                Object.DestroyImmediate(puff.GetComponent<Collider>());
            }
        }
    }

    private static Vector3 PointOnCircle(float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
    }

    private static void ApplyColor(GameObject go, Color color, bool transparent = false)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var mat = new Material(shader) { color = color };

        if (transparent)
        {
            if (isURP)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                mat.SetFloat("_Mode", 3f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
            }
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = mat;
    }

    private static Transform FindOrCreateGroup(string groupName)
    {
        var go = GameObject.Find(groupName);
        if (go == null)
        {
            go = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + groupName);
        }
        return go.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }
}
#endif

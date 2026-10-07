#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click wrapper around the project's existing IslandSceneGenerator
// (Assets/myassets/IslandSceneGenerator.cs) so it runs against whatever scene
// is currently open in the Editor, instead of only via the batch-mode path
// (BuildIslandSceneBatch, which targets newmvp.unity specifically and hasn't
// completed successfully in this project yet).
public static class GenerateFullIslandLayout
{
    [MenuItem("Tools/Radiant Orchard/Generate Full Island Layout")]
    private static void Generate()
    {
        // FIX: Re-enable (not hide) the BuildFloatingIsland objects so the
        // custom cliff mesh + ocean plane are always visible alongside the
        // generator's content. Earlier passes hid them to avoid overlap, but
        // the scene currently has neither active — so hiding produces a bare
        // grey void instead of a floating island over ocean.
        ShowIfPresent("Island");
        ShowIfPresent("Environment_Step1");

        var go = GameObject.Find("IslandGenerator");
        if (go == null)
        {
            go = new GameObject("IslandGenerator");
            Undo.RegisterCreatedObjectUndo(go, "Create IslandGenerator");
        }

        var generator = go.GetComponent<IslandSceneGenerator>();
        if (generator == null)
            generator = go.AddComponent<IslandSceneGenerator>();

        var so = new SerializedObject(generator);

        // The reference spec wants 6-7 plain grey humanoids walking/sitting
        // around the island — exactly what the generator's own fallback
        // stickman already is. Radiant Orchard's one interactive NPC (Phase
        // 4/5, "Characters" group) is separate and unaffected.
        so.FindProperty("stickmanCount").intValue = 7; // reference shows 6-7 grey humanoids

        // Real assets (Oak Tree, wells, rocks, pine/fruit trees, fences) use
        // their own Built-in-RP materials/shaders and render correctly now
        // that the project is back on Built-in RP (see RevertToBuiltIn.cs) —
        // auto-discovery is back on so the generator uses them instead of
        // primitive placeholders.
        so.FindProperty("autoDiscoverFromProject").boolValue = true;

        // Both candidate "Built-in-compatible" water materials tried earlier
        // turned out to be Shader Graph (UNS_Water AND the Houidisoft one,
        // confirmed live via Unity_GetConsoleLogs/RunCommand) — Shader Graph
        // can never run on Built-in RP at all. Leave waterMaterialOverride
        // null so the generator's own fallback cylinder is used, then
        // stamp it with a guaranteed-safe Standard/transparent material below.
        so.ApplyModifiedPropertiesWithoutUndo();

        generator.GenerateIslandScene();

        // Verified live (Unity_RunCommand): under Built-in RP the real Oak
        // Tree and island assets render correctly on their own — no force
        // override needed there anymore. Water is the one exception.
        ApplySafeWaterMaterial("WaterPlane");
        ApplySafeWaterMaterial("Pond");
        AddTreeGlow("WisdomTree");
        TintTreeLeavesGreen("WisdomTree");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: full island layout generated (old 'Island'/'Environment_Step1' hidden, not deleted — re-enable them in the Hierarchy if you want them back). Save the scene (Ctrl+S) to keep it.");
    }

    private static void ShowIfPresent(string objectName)
    {
        var go = GameObject.Find(objectName);
        // GameObject.Find only finds active objects — use Resources trick for inactive ones.
        if (go == null)
        {
            // Search all root objects including inactive.
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
                if (r.name == objectName) { go = r; break; }
        }
        if (go != null && !go.activeSelf)
        {
            Undo.RecordObject(go, "Show " + objectName);
            go.SetActive(true);
        }
    }

    private static void HideIfPresent(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null && go.activeSelf)
        {
            Undo.RecordObject(go, "Hide " + objectName);
            go.SetActive(false);
        }
    }

    // Stamps every object named objectName (there can be more than one — e.g.
    // two "Pond"s, this generator's own plus GenerateWaterFeatures') with a
    // guaranteed Built-in-RP-safe transparent water material. Both
    // auto-discoverable water materials in this project turned out to be
    // Shader Graph (URP/HDRP-only), confirmed live via Unity_RunCommand.
    private static void ApplySafeWaterMaterial(string objectName)
    {
        var shader = Shader.Find("Standard");
        // Same color as WaterFixer.cs's own default (Assets/myassets/
        // WaterFixer.cs) plus a scrolling ripple texture and
        // ConcentricRippleEffect (Assets/myassets/effects/PondWaterEffect.cs)
        // — a flat color alone left this reading as a static plastic blob.
        var mat = new Material(shader) { color = new Color(0.05f, 0.35f, 0.55f, 0.75f) };
        mat.mainTexture = CreateRippleTexture();
        mat.mainTextureScale = new Vector2(20f, 20f);
        mat.SetFloat("_Glossiness", 0.85f);
        mat.SetFloat("_Mode", 3f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        foreach (var renderer in GameObject.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (renderer.gameObject.name != objectName) continue;

            renderer.sharedMaterial = mat;
            var ripple = renderer.gameObject.GetComponent<ConcentricRippleEffect>();
            if (ripple == null) ripple = renderer.gameObject.AddComponent<ConcentricRippleEffect>();
            ripple.rippleSpeed = 1.5f;
            ripple.frequency = 8f;
            ripple.amplitude = 0.01f;
        }
    }

    private static Texture2D CreateRippleTexture()
    {
        const int width = 16, height = 64;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        var deep = new Color(0.04f, 0.28f, 0.46f);
        var light = new Color(0.15f, 0.55f, 0.75f);
        for (int y = 0; y < height; y++)
        {
            float wave = (Mathf.Sin(y / (float)height * Mathf.PI * 8f) + 1f) * 0.5f;
            var rowColor = Color.Lerp(deep, light, wave * 0.6f);
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, rowColor);
        }
        tex.Apply();
        return tex;
    }

    private static void AddTreeGlow(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go == null) return;

        var glowGO = go.transform.Find("RootGlow")?.gameObject;
        if (glowGO == null)
        {
            glowGO = new GameObject("RootGlow", typeof(Light));
            Undo.RegisterCreatedObjectUndo(glowGO, "Create Tree Root Glow");
            glowGO.transform.SetParent(go.transform, false);
        }

        glowGO.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        var light = glowGO.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.4f, 0.75f, 1f);
        light.range = 10f;
        light.intensity = 3f;
    }

    // The Oak Tree asset's own leaf texture is autumn-yellow, and its
    // "_MainColor" tint defaults to white (no shift) — instanced (not
    // editing the shared .mat asset) so the source asset stays untouched.
    private static void TintTreeLeavesGreen(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go == null) return;

        var greenTint = new Color(0.55f, 0.85f, 0.4f, 1f);

        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || !mats[i].name.StartsWith("Branches")) continue;

                var instance = new Material(mats[i]) { name = mats[i].name + " (Green Instance)" };
                if (instance.HasProperty("_MainColor")) instance.SetColor("_MainColor", greenTint);
                if (instance.HasProperty("_Color")) instance.SetColor("_Color", greenTint);
                if (instance.HasProperty("_BaseColor")) instance.SetColor("_BaseColor", greenTint);
                mats[i] = instance;
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
    }
}
#endif

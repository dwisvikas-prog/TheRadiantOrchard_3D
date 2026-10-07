#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// =============================================================================
//  GenerateSeaAndSky.cs
//  Tools → Radiant Orchard → Generate Sea And Sky
//
//  Adds to the scene:
//    [1] OCEAN PLANE   — large deep dark-blue water surface under the island
//    [2] OCEAN DEPTH   — thick dark cylinder below island so no grey void shows
//    [3] HORIZON HAZE  — soft gradient cylinder ring around the horizon
//    [4] SKY DOME      — large inverted sphere above everything,
//                        warm sky gradient (light blue top → horizon peach)
//    [5] DISTANT ROCKS — 6 small floating rock silhouettes on horizon
//    [6] CAMERA SKY    — sets Camera.backgroundColor to sky blue
//    [7] AMBIENT LIGHT — sets RenderSettings ambient to warm sky tone
//    [8] FOG           — subtle distance fog matching sea colour
//
//  Run via:  Tools → Radiant Orchard → Generate Sea And Sky
// =============================================================================
public static class GenerateSeaAndSky
{
    // ── reference-image colours ──────────────────────────────────────────
    // Ocean: dark teal-blue like the reference (#1A4A6E deep, #2060A0 mid)
    static readonly Color OceanDeep    = HC("112244");  // very dark navy at depth
    static readonly Color OceanSurface = HC("1A5A8A");  // dark teal-blue surface
    static readonly Color OceanShallow = HC("2878AA");  // lighter near island edge

    // Sky: bright tropical sky
    static readonly Color SkyTop       = HC("4A90C8");  // mid blue at top
    static readonly Color SkyMid       = HC("72B8E8");  // lighter middle
    static readonly Color SkyHorizon   = HC("B8D8F0");  // pale haze at horizon

    // Horizon haze / fog colour
    static readonly Color FogColor     = HC("8AB8D8");

    // Distant rock silhouette
    static readonly Color RockSilh     = HC("0A1828");

    [MenuItem("Tools/Radiant Orchard/Generate Sea And Sky")]
    static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Sea And Sky");
        int undoGroup = Undo.GetCurrentGroup();

        // Remove old environment if re-running
        DestroyIfExists("Environment_SeaSky");

        var root = new GameObject("Environment_SeaSky");
        Undo.RegisterCreatedObjectUndo(root, "SeaSky Root");

        BuildOcean(root.transform);
        BuildSkyDome(root.transform);
        BuildHorizonHaze(root.transform);
        BuildDistantRocks(root.transform);
        SetCameraAndLighting();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Sea and Sky generated. Save with Ctrl+S.");
    }

    // =========================================================================
    // [1+2]  OCEAN — large water plane + deep dark cylinder beneath island
    // =========================================================================
    static void BuildOcean(Transform root)
    {
        // ── Surface plane ─────────────────────────────────────────────────
        // Unity's Plane = 10×10 units. Scale 80 → 800×800 world units.
        // Positioned at y = -10 so it sits well below the island base.
        var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(ocean, "Ocean");
        ocean.name = "OceanSurface";
        ocean.transform.SetParent(root, false);
        ocean.transform.localPosition = new Vector3(0f, -10f, 0f);
        ocean.transform.localScale    = new Vector3(80f, 1f, 80f);
        Object.DestroyImmediate(ocean.GetComponent<Collider>());

        // Apply ocean material — transparent-ish dark teal
        ApplyOceanMat(ocean, OceanSurface, 0.72f, transparent: true);

        // ── Second deeper plane for far-away water (larger, slightly lower) ─
        var oceanFar = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(oceanFar, "OceanFar");
        oceanFar.name = "OceanFar";
        oceanFar.transform.SetParent(root, false);
        oceanFar.transform.localPosition = new Vector3(0f, -10.5f, 0f);
        oceanFar.transform.localScale    = new Vector3(200f, 1f, 200f);
        Object.DestroyImmediate(oceanFar.GetComponent<Collider>());
        ApplyMat(oceanFar, OceanDeep, 0.30f);

        // ── Depth cylinder — hides the grey void under the island ─────────
        // Tall dark cylinder centered below the island, fills the "hole"
        var depth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(depth, "OceanDepth");
        depth.name = "OceanDepth";
        depth.transform.SetParent(root, false);
        depth.transform.localPosition = new Vector3(0f, -30f, 0f);
        depth.transform.localScale    = new Vector3(50f, 22f, 50f);
        Object.DestroyImmediate(depth.GetComponent<Collider>());
        ApplyMat(depth, OceanDeep, 0.10f);

        // ── Shallow ring around island base (brighter water close to shore) ─
        var shallow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(shallow, "ShallowRing");
        shallow.name = "ShallowRing";
        shallow.transform.SetParent(root, false);
        shallow.transform.localPosition = new Vector3(0f, -9.6f, 0f);
        shallow.transform.localScale    = new Vector3(52f, 0.04f, 52f);
        Object.DestroyImmediate(shallow.GetComponent<Collider>());
        ApplyMat(shallow, OceanShallow, 0.55f);
    }

    // =========================================================================
    // [3]  HORIZON HAZE — soft cylinder ring at the sea/sky boundary
    // =========================================================================
    static void BuildHorizonHaze(Transform root)
    {
        // A large, very flat cylinder at horizon height, slightly transparent
        // haze colour — reads as the light scattering where sea meets sky.
        var haze = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(haze, "HorizonHaze");
        haze.name = "HorizonHaze";
        haze.transform.SetParent(root, false);
        haze.transform.localPosition = new Vector3(0f, 18f, 0f);
        haze.transform.localScale    = new Vector3(320f, 0.9f, 320f);
        Object.DestroyImmediate(haze.GetComponent<Collider>());
        ApplyMat(haze, SkyHorizon, 0.05f);
    }

    // =========================================================================
    // [4]  SKY DOME — large inverted sphere, 3-layer gradient colours
    // =========================================================================
    static void BuildSkyDome(Transform root)
    {
        // We approximate a sky gradient with 3 concentric inverted cylinders
        // at different heights, each getting progressively bluer toward zenith.

        var skyRoot = new GameObject("SkyDome");
        Undo.RegisterCreatedObjectUndo(skyRoot, "SkyDome");
        skyRoot.transform.SetParent(root, false);

        // Layer 1 — horizon band (pale blue-white)
        BuildSkyBand(skyRoot.transform, "Sky_Horizon", 15f, 280f, 18f, SkyHorizon);
        // Layer 2 — mid sky (medium blue)
        BuildSkyBand(skyRoot.transform, "Sky_Mid",     60f, 260f, 30f, SkyMid);
        // Layer 3 — zenith (deeper blue) — tall upward-reaching cylinder
        BuildSkyBand(skyRoot.transform, "Sky_Top",    130f, 220f, 55f, SkyTop);

        // Flat disc cap at very top — seals the zenith hole
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(cap, "SkyZenith");
        cap.name = "SkyZenith";
        cap.transform.SetParent(skyRoot.transform, false);
        cap.transform.localPosition = new Vector3(0f, 185f, 0f);
        cap.transform.localScale    = new Vector3(220f, 0.5f, 220f);
        Object.DestroyImmediate(cap.GetComponent<Collider>());
        ApplyMat(cap, SkyTop, 0.05f);
        // Flip normals: we view from inside, so render both sides
        SetDoubleSided(cap);
    }

    static void BuildSkyBand(Transform parent, string name, float y, float diameter, float height, Color col)
    {
        var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(band, name);
        band.name = name;
        band.transform.SetParent(parent, false);
        band.transform.localPosition = new Vector3(0f, y, 0f);
        band.transform.localScale    = new Vector3(diameter, height * 0.5f, diameter);
        Object.DestroyImmediate(band.GetComponent<Collider>());
        ApplyMat(band, col, 0.03f);
        SetDoubleSided(band);
    }

    // =========================================================================
    // [5]  DISTANT ROCKS — silhouette rock islands on the horizon
    // =========================================================================
    static void BuildDistantRocks(Transform root)
    {
        var rockRoot = new GameObject("DistantRocks");
        Undo.RegisterCreatedObjectUndo(rockRoot, "DistantRocks");
        rockRoot.transform.SetParent(root, false);

        // 6 rocks at varying angles and distances on the horizon
        (float angle, float dist, float scaleX, float scaleY, float scaleZ)[] rocks =
        {
            ( 30f, 180f, 12f,  7f, 8f),
            ( 85f, 200f,  8f,  5f, 6f),
            (145f, 190f, 15f,  9f, 7f),
            (210f, 175f,  7f,  4f, 5f),
            (270f, 195f, 10f,  6f, 8f),
            (330f, 185f,  9f,  5f, 6f),
        };

        foreach (var (angle, dist, sx, sy, sz) in rocks)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(rad) * dist, -6f, Mathf.Cos(rad) * dist);

            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(rock, "DistRock");
            rock.name = "DistRock_" + (int)angle;
            rock.transform.SetParent(rockRoot.transform, false);
            rock.transform.localPosition = pos;
            rock.transform.localScale    = new Vector3(sx, sy, sz);
            Object.DestroyImmediate(rock.GetComponent<Collider>());
            ApplyMat(rock, RockSilh, 0.05f);
        }
    }

    // =========================================================================
    // [6+7+8]  CAMERA BACKGROUND + AMBIENT LIGHT + FOG
    // =========================================================================
    static void SetCameraAndLighting()
    {
        // ── Camera background colour ──────────────────────────────────────
        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor    = SkyMid;
            cam.clearFlags         = CameraClearFlags.SolidColor;
        }

        // ── Ambient light — warm skylight tone ────────────────────────────
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.72f, 0.82f); // soft cool-white sky

        // ── Directional light — warm sun angle ───────────────────────────
        var sunGO = GameObject.Find("Directional Light");
        if (sunGO == null) sunGO = GameObject.Find("Sun");
        if (sunGO != null)
        {
            var light = sunGO.GetComponent<Light>();
            if (light != null)
            {
                light.color     = new Color(1.0f, 0.92f, 0.78f); // warm golden sun
                light.intensity = 1.15f;
                sunGO.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            }
        }
        else
        {
            // Create a directional light if none exists
            var lightGO = new GameObject("Sun");
            Undo.RegisterCreatedObjectUndo(lightGO, "Sun");
            var light   = lightGO.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.color     = new Color(1.0f, 0.92f, 0.78f);
            light.intensity = 1.15f;
            lightGO.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            Debug.Log("✓ Created Directional Light (Sun)");
        }

        // ── Distance fog — matches sea colour ─────────────────────────────
        RenderSettings.fog          = true;
        RenderSettings.fogColor     = FogColor;
        RenderSettings.fogMode      = FogMode.Linear;
        RenderSettings.fogStartDistance = 120f;  // starts far — island stays crisp
        RenderSettings.fogEndDistance   = 280f;  // fades out at horizon

        Debug.Log("✓ Camera background: sky blue | Ambient: soft skylight | Fog: linear 120→280");
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    // Standard lit material
    static void ApplyMat(GameObject go, Color color, float smoothness)
    {
        bool isURP = GraphicsSettings.currentRenderPipeline != null;
        var shader  = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        if (!isURP) mat.SetFloat("_Glossiness", smoothness);
        else        mat.SetFloat("_Smoothness",  smoothness);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    // Semi-transparent ocean material
    static void ApplyOceanMat(GameObject go, Color color, float smoothness, bool transparent)
    {
        bool isURP = GraphicsSettings.currentRenderPipeline != null;
        var shader  = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        if (shader == null) shader = Shader.Find("Standard");

        Color c = new Color(color.r, color.g, color.b, 0.88f);
        var mat = new Material(shader) { color = c };

        if (!isURP) mat.SetFloat("_Glossiness", smoothness);
        else        mat.SetFloat("_Smoothness",  smoothness);

        if (transparent)
        {
            if (isURP)
            {
                mat.SetFloat("_Surface", 1f);           // Transparent
                mat.SetFloat("_Blend", 0f);             // Alpha blend
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
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    // Makes a renderer render from both sides (needed for sky dome viewed from inside)
    static void SetDoubleSided(GameObject go)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        // Clone the material so we don't affect shared assets
        var mat = Object.Instantiate(r.sharedMaterial);
        mat.SetFloat("_Cull", 0f);  // 0 = Off (both sides)
        if (mat.HasProperty("_CullMode")) mat.SetFloat("_CullMode", 0f);
        r.sharedMaterial = mat;
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Undo.DestroyObjectImmediate(go);
    }

    static Color HC(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
#endif

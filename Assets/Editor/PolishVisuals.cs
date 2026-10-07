#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Scene-wide "matte & clean" pass.
//
// The material .mat ASSETS for the island's grass/terrain/tree packs were
// already tuned by hand (glossiness ~0.06, specular highlights off). But the
// live scene also carries materials created at generation time and saved INSIDE
// the scene file (IslandSceneGenerator's fallback materials, decorative props),
// which no asset edit can reach — and the scene is serialized binary, so it
// can't be fixed by hand-editing either. This tool walks every renderer in the
// open scene and gives those materials the same treatment, then sets a natural,
// non-blown-out lighting setup.
//
// Deliberately skipped: anything that is supposed to be shiny or glowing —
// water, glass, the magic tree's rune glow, lanterns, clouds, the sky dome —
// detected by material name. Transparency alone also counts as "keep" so a
// water material that isn't named obviously still survives.
public static class PolishVisuals
{
    // Lowered to, never raised: an already-matte material is left alone.
    private const float MatteSmoothness = 0.08f;

    private static readonly string[] KeepShinyMarkers =
    {
        "water", "ocean", "pond", "river", "glass", "ice", "crystal", "glow",
        "lantern", "rune", "magic", "cloud", "sky", "metal", "chrome", "gold",
        "star", "gem", "pondwater", "rainbow", "portal", "portalglow"
    };

    [MenuItem("Tools/Radiant Orchard/Polish Visuals (matte & clean)")]
    private static void Polish()
    {
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Polish Visuals");

        int matteCount = 0;
        int skippedCount = 0;

        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var seen = new System.Collections.Generic.HashSet<Material>();

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null || !seen.Add(mat)) continue;

                if (ShouldStayShiny(mat))
                {
                    skippedCount++;
                    continue;
                }

                bool changed = false;

                if (mat.HasProperty("_Glossiness") &&
                    mat.GetFloat("_Glossiness") > MatteSmoothness)
                {
                    mat.SetFloat("_Glossiness", MatteSmoothness);
                    changed = true;
                }

                if (mat.HasProperty("_Smoothness") &&
                    mat.GetFloat("_Smoothness") > MatteSmoothness)
                {
                    mat.SetFloat("_Smoothness", MatteSmoothness);
                    changed = true;
                }

                if (mat.HasProperty("_SpecularHighlights"))
                {
                    mat.SetFloat("_SpecularHighlights", 0f);
                    changed = true;
                }

                if (mat.HasProperty("_EnvironmentReflections"))
                {
                    mat.SetFloat("_EnvironmentReflections", 0f);
                    changed = true;
                }

                if (mat.HasProperty("_Metallic") && mat.GetFloat("_Metallic") > 0f && mat.GetFloat("_Metallic") < 0.2f)
                {
                    // Only stamp out faint accidental metalness; a real metal
                    // look is left to the material that authored it.
                    mat.SetFloat("_Metallic", 0f);
                    changed = true;
                }

                if (!changed) continue;

                Undo.RecordObject(mat, "Polish Material");
                EditorUtility.SetDirty(mat);
                matteCount++;
            }
        }

        ApplyLighting();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"Radiant Orchard: visual polish applied — {matteCount} material(s) matted, " +
                  $"{skippedCount} deliberately shiny/glowing material(s) left alone, lighting naturalised. " +
                  "Save the scene (Ctrl+S).");
    }

    private static bool ShouldStayShiny(Material mat)
    {
        // Emission means the material is a deliberate glow (magic tree, lanterns,
        // glow roots) — dimming its highlights would fight the art.
        if (mat.IsKeywordEnabled("_EMISSION")) return true;

        if (mat.HasProperty("_EmissionColor") &&
            mat.GetColor("_EmissionColor").maxColorComponent > 0.01f) return true;

        // Transparency is how the project builds water.
        if (mat.renderQueue >= (int)RenderQueue.Transparent) return true;

        string name = mat.name.ToLowerInvariant();
        foreach (var marker in KeepShinyMarkers)
            if (name.Contains(marker)) return true;

        return false;
    }

    // Natural daylight: one soft warm key light and a cool sky bounce, instead of
    // the near-white blowout that made saturated greens read as neon.
    private static void ApplyLighting()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional) continue;

            Undo.RecordObject(light, "Polish Directional Light");
            light.intensity = Mathf.Clamp(light.intensity, 0.8f, 1.15f);
            light.color = new Color(1f, 0.98f, 0.94f);
            EditorUtility.SetDirty(light);
        }

        // Scene-level settings: saved with the scene, so marking it dirty below is
        // what persists them (there's no single object for Undo to record here).
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.45f, 0.52f, 0.60f);
        RenderSettings.ambientEquatorColor = new Color(0.36f, 0.39f, 0.37f);
        RenderSettings.ambientGroundColor = new Color(0.20f, 0.19f, 0.16f);
        RenderSettings.ambientIntensity = 0.9f;
        RenderSettings.reflectionIntensity = 0.25f;
    }
}
#endif

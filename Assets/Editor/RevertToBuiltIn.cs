#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Undoes EnableURP.cs. Several purchased assets — the Oak Tree's custom
// Built-in-only surface shaders in particular — can never work under URP at
// all, so the earlier URP switch traded one set of broken materials for
// another. This reverts the render pipeline to Built-in AND downgrades any
// material the bulk URP converter touched (now stuck on "Universal Render
// Pipeline/Lit", invisible/pink without URP active) back to "Standard".
public static class RevertToBuiltIn
{
    [MenuItem("Tools/Radiant Orchard/Revert to Built-in RP (Fix Real Assets)")]
    private static void Revert()
    {
        GraphicsSettings.defaultRenderPipeline = null;

        int originalQualityLevel = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = null;
        }
        QualitySettings.SetQualityLevel(originalQualityLevel, false);

        int downgraded = 0;
        var standardShader = Shader.Find("Standard");
        var guids = AssetDatabase.FindAssets("t:Material");

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;
            if (!mat.shader.name.StartsWith("Universal Render Pipeline/")) continue;

            Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            Texture mainTex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            float smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f;

            mat.shader = standardShader;
            mat.color = color;
            if (mainTex != null) mat.mainTexture = mainTex;
            mat.SetFloat("_Glossiness", smoothness);

            EditorUtility.SetDirty(mat);
            downgraded++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Radiant Orchard: reverted to Built-in RP and downgraded {downgraded} material(s) back to Standard shader. " +
            "Re-run 'Generate Full Island Layout', 'Generate Orchard Fruit Zones', and 'Generate River, Waterfalls and Dock' afterwards to rebuild their own generated materials for Built-in too.");
    }
}
#endif

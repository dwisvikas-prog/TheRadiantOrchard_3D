#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Assigns the project's own UNS_URP pipeline asset (already present from the
// Ultimate Nature Starter pack) as the active render pipeline. URP is
// installed but was never assigned in Graphics Settings, which is why
// URP-authored materials (the water shader graph, and this session's earlier
// generated materials before their pipeline-detection fix) rendered pink.
public static class EnableURP
{
    private const string URPAssetPath = "Assets/InnerverseInteractive/Ultimate Nature – Starter/Settings/UNS_URP.asset";

    [MenuItem("Tools/Radiant Orchard/Enable URP")]
    private static void Enable()
    {
        var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URPAssetPath);
        if (urpAsset == null)
        {
            Debug.LogError("EnableURP: couldn't find a UniversalRenderPipelineAsset at " + URPAssetPath);
            return;
        }

        GraphicsSettings.defaultRenderPipeline = urpAsset;

        int originalQualityLevel = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urpAsset;
        }
        QualitySettings.SetQualityLevel(originalQualityLevel, false);

        // Every pre-existing Standard-shader material in the project (all the
        // asset packs: rocks, trees, fences, the character, etc.) would now
        // render pink under URP too unless converted — this is Unity's own
        // bulk converter (same one behind Edit > Rendering > Materials >
        // Convert All Built-In Materials to Current SRP), run automatically.
        var upgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(GraphicsSettings.currentRenderPipelineAssetType);
        MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to URP Material");

        AssetDatabase.SaveAssets();
        Debug.Log("Radiant Orchard: URP (" + urpAsset.name + ") is now active, and all Standard-shader " +
            "materials in the project were converted to URP equivalents. " +
            "Re-run 'Generate Full Island Layout' and 'Generate Orchard Fruit Zones' afterwards to rebuild their generated materials too.");
    }
}
#endif

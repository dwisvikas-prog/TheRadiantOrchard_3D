#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Tools → Radiant Orchard → Lock Landscape Orientation
//
// Sets Player Settings so the build itself ships locked to landscape.
// Run this ONCE — settings are saved to ProjectSettings/ProjectSettings.asset
// and persist across all builds.
//
// This is the ONLY reliable way to lock orientation for Android/iOS builds:
// Screen.orientation at runtime works but can briefly show portrait on some
// devices before Awake fires; the Player Settings lock prevents that flash.
public static class LockLandscape
{
    [MenuItem("Tools/Radiant Orchard/Lock Landscape Orientation")]
    static void Lock()
    {
        // ── Android ──────────────────────────────────────────────────────
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        PlayerSettings.allowedAutorotateToPortrait            = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown  = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft       = true;
        PlayerSettings.allowedAutorotateToLandscapeRight      = true;

        // ── iOS ───────────────────────────────────────────────────────────
#if UNITY_IOS || UNITY_STANDALONE
        PlayerSettings.iOS.requiresPersistentWiFi = false; // unrelated but harmless
#endif

        // ── Save ─────────────────────────────────────────────────────────
        AssetDatabase.SaveAssets();

        Debug.Log("✓ Orientation locked to Landscape (Left+Right auto-rotate). " +
                  "Portrait is disabled in Player Settings.");
    }
}
#endif

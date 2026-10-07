#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

// Tools → Radiant Orchard → Setup Onboarding
//
// 1. Adds OnboardingManager to the GameManagers GameObject in the scene
// 2. Resets the "already seen" PlayerPrefs key so it shows again on next Play
// 3. Logs exactly what was done

public static class SetupOnboarding
{
    [MenuItem("Tools/Radiant Orchard/Setup Onboarding")]
    static void Setup()
    {
        // ── Find or create GameManagers ───────────────────────────────────
        var managers = GameObject.Find("GameManagers");
        if (managers == null)
        {
            managers = new GameObject("GameManagers");
            Undo.RegisterCreatedObjectUndo(managers, "Create GameManagers");
            Debug.Log("Created GameManagers GameObject");
        }

        // ── Add OnboardingManager if missing ─────────────────────────────
        var ob = managers.GetComponent<OnboardingManager>();
        if (ob == null)
        {
            ob = Undo.AddComponent<OnboardingManager>(managers);
            Debug.Log("✓ OnboardingManager added to GameManagers");
        }
        else
        {
            Debug.Log("✓ OnboardingManager already on GameManagers");
        }

        // ── Wire the real humanoid guide-character mascot ─────────────────
        var so = new SerializedObject(ob);
        var guideProp = so.FindProperty("guideCharacterPrefab");
        if (guideProp != null)
        {
            var guidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GameData/Prefabs/GuideCharacter.prefab");
            if (guidePrefab != null)
            {
                guideProp.objectReferenceValue = guidePrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("✓ Guide character prefab assigned to OnboardingManager");
            }
            else
            {
                Debug.LogWarning("GuideCharacter.prefab not found — run Tools/Radiant Orchard/Create Guide Character Prefab first, then re-run this.");
            }
        }

        // ── Reset seen-flag so it shows on next Play ──────────────────────
        PlayerPrefs.DeleteKey("RO_Onboarded");
        PlayerPrefs.Save();
        Debug.Log("✓ Onboarding reset — will show on next Play");

        // ── Mark scene dirty ──────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Save scene (Ctrl+S) then press Play to see the tutorial.");
    }

    // Separate menu item to ONLY reset the seen-flag without touching the scene
    [MenuItem("Tools/Radiant Orchard/Reset Onboarding (Show Again)")]
    static void ResetOnly()
    {
        PlayerPrefs.DeleteKey("RO_Onboarded");
        PlayerPrefs.Save();
        Debug.Log("✓ Onboarding reset — will show on next Play");
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

// Makes Phase 4 actually testable: ensures GameState + InteractionController
// exist, then populates the (currently empty) Characters/Fruits groups with
// one demo NPC and one demo zone per fruit type so there's something to tap
// in Play mode. Re-runnable — clears its own "Demo_" prefixed objects first,
// leaves everything else in the scene untouched.
public static class WireInteractionDemo
{
    [MenuItem("Tools/Radiant Orchard/Wire Interaction Demo - Phase 4")]
    private static void Wire()
    {
        Undo.SetCurrentGroupName("Wire Interaction Demo");
        int undoGroup = Undo.GetCurrentGroup();

        var managers = GameObject.Find("GameManagers");
        if (managers == null)
        {
            managers = new GameObject("GameManagers");
            Undo.RegisterCreatedObjectUndo(managers, "Create GameManagers");
        }

        if (managers.GetComponent<GameState>() == null)
            Undo.AddComponent<GameState>(managers);

        if (managers.GetComponent<InteractionController>() == null)
            Undo.AddComponent<InteractionController>(managers);

        if (managers.GetComponent<DebugHud>() == null)
            Undo.AddComponent<DebugHud>(managers);

        WireDemoNpc();
        // Fruit zones are now owned by GenerateOrchardZones.cs (real, signed,
        // compass-positioned zones) — no longer duplicated here.

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(managers);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: interaction demo wired - GameManagers + demo NPC ready. Save the scene (Ctrl+S) to keep this, then hit Play.");
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

    private static void ClearDemoChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child.name.StartsWith("Demo_"))
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static void WireDemoNpc()
    {
        var charactersGroup = FindOrCreateGroup("Characters");
        ClearDemoChildren(charactersGroup);

        var girlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/myassets/girl.fbx");
        GameObject npc = girlPrefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(girlPrefab, charactersGroup)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        Undo.RegisterCreatedObjectUndo(npc, "Create Demo NPC");
        npc.name = "Demo_NPC_Villager";
        npc.transform.SetParent(charactersGroup, false);
        npc.transform.localPosition = new Vector3(3f, 1f, 3f);

        if (npc.GetComponentInChildren<Collider>() == null)
        {
            var capsule = npc.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.4f;
        }

        var interactable = npc.AddComponent<NPCInteractable>();
        interactable.interactableId = "npc_01";
        interactable.requiredVirtueId = "kindness";
        interactable.vibrancyReward = 20;

        var so = new SerializedObject(interactable);
        so.FindProperty("npcId").stringValue = "npc_01";
        so.FindProperty("startingEmotion").enumValueIndex = (int)EmotionState.Lonely;
        so.FindProperty("resolvedEmotion").enumValueIndex = (int)EmotionState.Happy;
        so.FindProperty("linkedObjectiveId").stringValue = "level_01_help_npc_01";
        so.ApplyModifiedPropertiesWithoutUndo();

        var level1 = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GameData/Levels/Level_01.asset");

        var controller = npc.AddComponent<NPCController>();
        var soController = new SerializedObject(controller);
        soController.FindProperty("npcId").stringValue = "npc_01";
        soController.FindProperty("displayName").stringValue = "Villager";
        soController.FindProperty("sourceLevel").objectReferenceValue = level1;
        soController.ApplyModifiedPropertiesWithoutUndo();

        var animator = npc.GetComponent<Animator>();
        if (animator != null)
        {
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/GameData/Animators/NPCEmotion.controller");
            if (animatorController != null) animator.runtimeAnimatorController = animatorController;
        }
    }

}
#endif

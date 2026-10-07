#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using RadiantOrchard;

// Builds a real humanoid onboarding-guide prefab from the girl.fbx model
// (Assets/myassets) instead of reusing the Stickman rescue-target visual.
// Run via Tools -> Radiant Orchard -> Create Guide Character Prefab whenever
// GuideCharacter.prefab needs to be (re)built.
public static class CreateGuideCharacterPrefab
{
    private const string ControllerPath = "Assets/GameData/Animators/GuideCharacter.controller";
    private const string PrefabPath = "Assets/GameData/Prefabs/GuideCharacter.prefab";
    private const string ModelPath = "Assets/myassets/girl.fbx";

    [MenuItem("Tools/Radiant Orchard/Create Guide Character Prefab")]
    private static void Create()
    {
        var idleClip = LoadClip("Assets/myassets/Idle.fbx");
        var talkClip = LoadClip("Assets/myassets/Happy.fbx");
        var pointClip = LoadClip("Assets/myassets/Quick Informal Bow.fbx");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);

        if (idleClip == null || talkClip == null || pointClip == null || model == null)
        {
            Debug.LogError("CreateGuideCharacterPrefab: missing girl.fbx or one of its clips (Idle / Happy / Quick Informal Bow) under Assets/myassets/. Aborting.");
            return;
        }

        EnsureFolder("Assets/GameData");
        EnsureFolder("Assets/GameData/Animators");
        EnsureFolder("Assets/GameData/Prefabs");

        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var sm = controller.layers[0].stateMachine;

        var idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        var talkState = sm.AddState("Talk");
        talkState.motion = talkClip;

        var pointState = sm.AddState("Point");
        pointState.motion = pointClip;

        AssetDatabase.SaveAssets();

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);

        var animator = instance.GetComponent<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        if (animator.avatar == null)
        {
            var avatar = FindAvatar(ModelPath);
            if (avatar != null) animator.avatar = avatar;
            else Debug.LogWarning("CreateGuideCharacterPrefab: no humanoid Avatar found in girl.fbx — assign one manually on GuideCharacter.prefab's Animator.");
        }

        if (instance.GetComponent<GuideCharacter>() == null)
            instance.AddComponent<GuideCharacter>();

        AssetDatabase.DeleteAsset(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);

        Debug.Log("Radiant Orchard: GuideCharacter.prefab created at " + PrefabPath +
                   " — re-run Tools/Radiant Orchard/Setup Onboarding to wire it in.");
    }

    private static Avatar FindAvatar(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is Avatar avatar) return avatar;
        return null;
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = path.Substring(0, path.LastIndexOf('/'));
        var folderName = path.Substring(path.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif

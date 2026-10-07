#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// The girl.fbx character rig has three separate animation-only FBX files next
// to it (Idle / Sad Idle / Happy) but no Animator Controller wiring them
// together yet (audit found zero .controller assets in the project). This
// builds one: AnyState -> Sad / Happy triggers, defaulting to Idle.
public static class CreateNpcEmotionAnimator
{
    private const string ControllerPath = "Assets/GameData/Animators/NPCEmotion.controller";

    [MenuItem("Tools/Radiant Orchard/Create NPC Emotion Animator")]
    private static void Create()
    {
        var idleClip = LoadClip("Assets/myassets/Idle.fbx");
        var sadClip = LoadClip("Assets/myassets/Sad Idle.fbx");
        var happyClip = LoadClip("Assets/myassets/Happy.fbx");

        if (idleClip == null || sadClip == null || happyClip == null)
        {
            Debug.LogError("CreateNpcEmotionAnimator: missing one of the expected clips under Assets/myassets/ (Idle / Sad Idle / Happy). Aborting.");
            return;
        }

        EnsureFolder("Assets/GameData");
        EnsureFolder("Assets/GameData/Animators");

        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter("Sad", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Happy", AnimatorControllerParameterType.Trigger);

        var stateMachine = controller.layers[0].stateMachine;

        var idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;
        stateMachine.defaultState = idleState;

        var sadState = stateMachine.AddState("Sad");
        sadState.motion = sadClip;

        var happyState = stateMachine.AddState("Happy");
        happyState.motion = happyClip;

        AddAnyStateTransition(stateMachine, sadState, "Sad");
        AddAnyStateTransition(stateMachine, happyState, "Happy");

        AssetDatabase.SaveAssets();
        Debug.Log("Radiant Orchard: NPCEmotion.controller created at " + ControllerPath);
    }

    private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState target, string triggerName)
    {
        var transition = stateMachine.AddAnyStateTransition(target);
        transition.AddCondition(AnimatorConditionMode.If, 0, triggerName);
        transition.hasExitTime = false;
        transition.duration = 0.2f;
        transition.canTransitionToSelf = false;
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
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

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildEmptyIslandApk
{
    // Unity menu optional — also callable: -executeMethod BuildEmptyIslandApk.Build
    [MenuItem("Radiant Orchard/Build Empty Island APK (phone)", priority = 50)]
    public static void Build()
    {
        // Force launch scene
        var scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/CoC_BlankGround.unity", true),
            new EditorBuildSettingsScene("Assets/The Main RadiantOrchard_3d.unity", false),
        };
        EditorBuildSettings.scenes = scenes;

        PlayerSettings.bundleVersion = "1.0.2";
        PlayerSettings.Android.bundleVersionCode = 3;

        string dir = Path.Combine(Application.dataPath, "..", "Builds", "Android");
        Directory.CreateDirectory(dir);
        string apk = Path.Combine(dir, "RadiantOrchard_EmptyIsland.apk");

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/CoC_BlankGround.unity" },
            locationPathName = apk,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[Build] OK → " + apk);
            EditorUtility.RevealInFinder(apk);
        }
        else
        {
            Debug.LogError("[Build] FAILED: " + report.summary.result);
        }
    }
}
#endif

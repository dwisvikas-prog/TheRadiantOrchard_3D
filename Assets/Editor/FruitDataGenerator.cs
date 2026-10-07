// LEGACY — superseded by CreateDefaultGameData.cs + SetupLevel1.cs.
// Uses the old flat-namespace FruitData + VirtueGesture which are now in
// Assets/Scripts/_Legacy/ and compiled out. Kept for reference only.
#if false
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor-only utility: generates all 9 FruitData ScriptableObject assets
/// from the PRD's Virtue/Fruit/Gesture matrix in one click.
/// Place this script inside an "Editor" folder (e.g. Assets/Editor/) —
/// Editor scripts must live in a folder literally named "Editor" so Unity
/// excludes them from the game build.
/// </summary>
public static class FruitDataGenerator
{
    const string TargetFolder = "Assets/Data/Fruits";

    [MenuItem("RadiantOrchard/Generate All Fruit Data Assets")]
    public static void GenerateAll()
    {
        EnsureFolderExists();

        CreateFruit("Love", "Strawberry", "Heart / Circulation", VirtueGesture.DoubleTap);
        CreateFruit("Joy", "Pineapple", "Immunity / Energy", VirtueGesture.DoubleTap);
        CreateFruit("Peace", "Watermelon", "Nervous System", VirtueGesture.DoubleTap);
        CreateFruit("Patience", "Lemon", "Detox / Digestion", VirtueGesture.LongPress);
        CreateFruit("Meekness", "Grapes", "Respiratory / Breath", VirtueGesture.LongPress);
        CreateFruit("Self-Control", "Apple", "Brain / Focus", VirtueGesture.LongPress);
        CreateFruit("Kindness", "Peach", "Skin / Glow", VirtueGesture.FastSwipe);
        CreateFruit("Goodness", "Banana", "Muscle / Strength", VirtueGesture.FastSwipe);
        CreateFruit("Faithfulness", "Cherry", "Bone / Stability", VirtueGesture.FastSwipe);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated 9 FruitData assets in " + TargetFolder);
    }

    static void EnsureFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(TargetFolder))
            AssetDatabase.CreateFolder("Assets/Data", "Fruits");
    }

    static void CreateFruit(string virtueName, string fruitName, string healthStat, VirtueGesture gesture)
    {
        string assetPath = $"{TargetFolder}/Fruit_{fruitName}.asset";

        if (File.Exists(assetPath))
        {
            Debug.Log($"Skipped (already exists): {assetPath}");
            return;
        }

        var data = ScriptableObject.CreateInstance<FruitData>();
        data.virtueName = virtueName;
        data.fruitName = fruitName;
        data.healthStat = healthStat;
        data.requiredGesture = gesture;

        AssetDatabase.CreateAsset(data, assetPath);
    }
}
#endif

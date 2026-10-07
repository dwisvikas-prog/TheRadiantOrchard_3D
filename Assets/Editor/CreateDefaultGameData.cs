#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RadiantOrchard;

// One-time / re-runnable setup: seeds the 9 virtues and 9 fruit types as
// ScriptableObject assets (placeholder content — edit the assets directly,
// no code changes needed), wires one example Level, and makes sure a
// GameState instance exists in the currently open scene.
public static class CreateDefaultGameData
{
    private const string VirtueFolder = "Assets/GameData/Virtues";
    private const string FruitFolder  = "Assets/GameData/Fruits";
    private const string FruitDataFolder = "Assets/GameData/FruitData"; // FruitData (gameplay/gesture)
    private const string LevelFolder  = "Assets/GameData/Levels";

    private struct VirtueSeed
    {
        public string id, name, desc;
        public Color color;
    }

    private struct FruitSeed
    {
        public FruitType   type;
        public string      name;
        public Color       color;             // FruitZoneDefinition zone colour
        public Color       placeholderColor;  // FruitData world placeholder colour
        public GestureType gesture;           // which gesture harvests this fruit
    }

    private static readonly VirtueSeed[] Virtues =
    {
        new VirtueSeed { id = "kindness", name = "Kindness", desc = "Doing small, caring things for others without expecting anything back.", color = HexColor("F28FB2") },
        new VirtueSeed { id = "honesty", name = "Honesty", desc = "Telling the truth, even when it's hard.", color = HexColor("5CA9DB") },
        new VirtueSeed { id = "courage", name = "Courage", desc = "Doing the right thing even when you're scared.", color = HexColor("E8703A") },
        new VirtueSeed { id = "patience", name = "Patience", desc = "Staying calm and waiting without giving up.", color = HexColor("9B7EDE") },
        new VirtueSeed { id = "gratitude", name = "Gratitude", desc = "Noticing and appreciating the good things you have.", color = HexColor("F2C14E") },
        new VirtueSeed { id = "generosity", name = "Generosity", desc = "Sharing what you have with others.", color = HexColor("F2795B") },
        new VirtueSeed { id = "perseverance", name = "Perseverance", desc = "Trying again and again until you succeed.", color = HexColor("4C9A6B") },
        new VirtueSeed { id = "respect", name = "Respect", desc = "Treating others and their feelings with care.", color = HexColor("3FA7A0") },
        new VirtueSeed { id = "forgiveness", name = "Forgiveness", desc = "Letting go of anger and giving someone another chance.", color = HexColor("C98BC9") },
    };

    private static readonly FruitSeed[] Fruits =
    {
        // PRD Virtue/Fruit/Gesture matrix — must match FruitDataGenerator.cs
        // (Assets/Editor) and the shipped Assets/GameData/FruitData/*.asset
        // files: Strawberry/Pineapple/Watermelon = DoubleTap,
        // Lemon/Grapes/Apple = LongPress, Peach/Banana/Cherry = FastSwipe.
        // 3 fruits per gesture so the tutorial can demonstrate all three early.
        //
        // Watermelon color fixed: was pink (F06292) — now proper dark green
        // to match the reference image zone colours.
        new FruitSeed { type = FruitType.Watermelon, name = "Watermelon",
            color = HexColor("3A8C28"), placeholderColor = HexColor("4CAF50"),
            gesture = GestureType.DoubleTap },
        new FruitSeed { type = FruitType.Pineapple,  name = "Pineapple",
            color = HexColor("F4C430"), placeholderColor = HexColor("FFD700"),
            gesture = GestureType.DoubleTap },
        new FruitSeed { type = FruitType.Strawberry, name = "Strawberry",
            color = HexColor("E23D5A"), placeholderColor = HexColor("FF4757"),
            gesture = GestureType.DoubleTap },
        new FruitSeed { type = FruitType.Lemon,      name = "Lemon",
            color = HexColor("F7E463"), placeholderColor = HexColor("FFF176"),
            gesture = GestureType.LongPress },
        new FruitSeed { type = FruitType.Grapes,     name = "Grapes",
            color = HexColor("8E44AD"), placeholderColor = HexColor("9B59B6"),
            gesture = GestureType.LongPress },
        new FruitSeed { type = FruitType.Apple,      name = "Apple",
            color = HexColor("E74C3C"), placeholderColor = HexColor("FF6B6B"),
            gesture = GestureType.LongPress },
        new FruitSeed { type = FruitType.Peach,      name = "Peach",
            color = HexColor("FFB07C"), placeholderColor = HexColor("FFAB76"),
            gesture = GestureType.FastSwipe },
        new FruitSeed { type = FruitType.Banana,     name = "Banana",
            color = HexColor("F5D547"), placeholderColor = HexColor("FFEE58"),
            gesture = GestureType.FastSwipe },
        new FruitSeed { type = FruitType.Cherry,     name = "Cherry",
            color = HexColor("C0392B"), placeholderColor = HexColor("E53935"),
            gesture = GestureType.FastSwipe },
    };

    [MenuItem("Tools/Radiant Orchard/Setup Core Game Data")]
    private static void Setup()
    {
        Setup(false);
    }

    // Explicit escape hatch: re-applies seed values onto assets that already
    // exist on disk (clobbers any hand edits — use deliberately).
    [MenuItem("Tools/Radiant Orchard/Setup Core Game Data (Force Overwrite Existing)")]
    private static void SetupForced()
    {
        Setup(true);
    }

    private static void Setup(bool forceOverwrite)
    {
        EnsureFolder("Assets/GameData");
        EnsureFolder(VirtueFolder);
        EnsureFolder(FruitFolder);
        EnsureFolder(FruitDataFolder);
        EnsureFolder(LevelFolder);

        // Build a virtue array indexed by the order they appear so we can
        // assign one virtue per fruit in a round-robin.
        var virtueList = new System.Collections.Generic.List<VirtueDefinition>();
        VirtueDefinition firstVirtue = null;
        foreach (var seed in Virtues)
        {
            var virtue = CreateOrLoad<VirtueDefinition>($"{VirtueFolder}/Virtue_{Capitalize(seed.id)}.asset", forceOverwrite, out var seedVirtue);
            if (seedVirtue)
            {
                virtue.virtueId      = seed.id;
                virtue.displayName   = seed.name;
                virtue.description   = seed.desc;
                virtue.themeColor    = seed.color;
                virtue.vibrancyReward = 10;
                EditorUtility.SetDirty(virtue);
            }
            virtueList.Add(virtue);
            if (firstVirtue == null) firstVirtue = virtue;
        }

        int virtueIdx = 0;
        foreach (var seed in Fruits)
        {
            // ── FruitZoneDefinition (used by GenerateOrchardZones for visual zone colour) ──
            var zone = CreateOrLoad<FruitZoneDefinition>($"{FruitFolder}/Fruit_{seed.type}.asset", forceOverwrite, out var seedZone);
            if (seedZone)
            {
                zone.fruitType              = seed.type;
                zone.displayName            = seed.name;
                zone.fruitColor             = seed.color;
                zone.vibrancyRewardOnHarvest = 5;
                EditorUtility.SetDirty(zone);
            }

            // ── FruitData (used by NPCSpawner / FruitHarvester for gesture + placeholder) ──
            // Creating this was missing entirely — without it no FruitData
            // asset existed for the Stickman/harvesting loop to reference.
            // RadiantOrchard.FruitData must be fully qualified: the legacy
            // global-namespace FruitData (Assets/Scripts/FruitData.cs) always
            // wins over the `using RadiantOrchard;` directive, so an unqualified
            // reference here binds to the wrong class and its fields
            // (fruitType/gestureType/virtue/...) don't exist on it.
            var data = CreateOrLoad<RadiantOrchard.FruitData>($"{FruitDataFolder}/FruitData_{seed.type}.asset", forceOverwrite, out var seedData);
            if (seedData)
            {
                data.fruitType        = seed.type;
                data.displayName      = seed.name;
                data.gestureType      = seed.gesture;
                data.placeholderColor = seed.placeholderColor;
                data.placeholderShape = PlaceholderShape.Sphere;
                data.virtue           = virtueList[virtueIdx % virtueList.Count];
                EditorUtility.SetDirty(data);
            }

            virtueIdx++;
        }

        var level1 = CreateOrLoad<LevelDefinition>($"{LevelFolder}/Level_01.asset", forceOverwrite, out var seedLevel);
        if (seedLevel)
        {
            level1.levelId = "level_01";
            level1.displayName = "Level 1";
            level1.virtue = firstVirtue;
            level1.requiredVibrancy = 100;
        }

        if (level1.objectives.Count == 0)
        {
            level1.objectives.Add(new ObjectiveEntry
            {
                objectiveId = "level_01_help_npc_01",
                description = "Help the lonely villager near the well.",
                type = ObjectiveType.HelpNPC,
                targetId = "npc_01",
                requiredCount = 1,
                vibrancyReward = 20
            });
        }

        if (level1.npcScenarios.Count == 0)
        {
            level1.npcScenarios.Add(new NPCScenarioEntry
            {
                npcId = "npc_01",
                startingEmotion = EmotionState.Lonely,
                problemDescription = "This villager has no one to talk to.",
                thoughtBubbleText = "...",
                requiredVirtueId = firstVirtue != null ? firstVirtue.virtueId : "kindness",
                resolvedEmotion = EmotionState.Happy,
                vibrancyReward = 20
            });
        }

        EditorUtility.SetDirty(level1);

        var managers = GameObject.Find("GameManagers");
        if (managers == null)
        {
            managers = new GameObject("GameManagers");
            Undo.RegisterCreatedObjectUndo(managers, "Create GameManagers");
        }

        if (managers.GetComponent<GameState>() == null)
        {
            Undo.AddComponent<GameState>(managers);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(forceOverwrite
            ? "Radiant Orchard: 9 virtues, 9 FruitZoneDefinitions, 9 FruitData (with gestures), 1 example level, and GameState set up (existing assets overwritten)."
            : "Radiant Orchard: missing game data set up; existing assets were left untouched (use the Force Overwrite menu item to re-apply seed values).");
    }

    // Loads the asset at path if it exists, otherwise creates it. seeded is
    // true when the caller should write seed values onto it: a freshly created
    // asset always needs seeding, an existing one only when forceOverwrite was
    // requested — otherwise re-running this tool would silently clobber valid
    // (possibly hand-edited) asset data, e.g. the PRD gesture mappings in
    // Assets/GameData/FruitData.
    private static T CreateOrLoad<T>(string path, bool forceOverwrite, out bool seeded) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            seeded = true;
        }
        else
        {
            seeded = forceOverwrite;
        }
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = path.Substring(0, path.LastIndexOf('/'));
        var name = path.Substring(path.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
#endif

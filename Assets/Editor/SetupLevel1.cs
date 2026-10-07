#if UNITY_EDITOR
// ============================================================================
//  SetupLevel1.cs  —  Tools → Radiant Orchard → Setup Level 1 Data
//
//  Tasks 3 + 4:
//    • Patches the 3 Level-1 FruitData assets (Strawberry/Pineapple/Watermelon)
//      so each one has:
//        - gestureType = DoubleTap
//        - placeholderColor matching the reference art colours
//        - virtue linked to Kindness / Gratitude / Generosity (closest
//          semantic match to Love / Joy / Peace in the existing virtue set)
//        - symptomIcon wired to the matching Icon_*.png sprite
//    • Patches Level_01.asset:
//        - levelId = "level_01"
//        - displayName = "Level 1 – The Heart of the Orchard"
//        - requiredVibrancy = 100
//        - spawnInterval = 12s  (snappy but not overwhelming for tutorial)
//        - maxActiveStickmen = 3
//        - patienceTime = 30s   (generous – first level)
//        - clears old objectives and adds ONE clean "heal 5 stickmen" objective
//        - virtue linked to Kindness
//        - nextLevel = null  (only 1 level exists yet)
//    • Prints a clear summary so you can confirm in the Console.
// ============================================================================
using UnityEditor;
using UnityEngine;
using RadiantOrchard;
using System.Collections.Generic;

public static class SetupLevel1
{
    [MenuItem("Tools/Radiant Orchard/Setup Level 1 Data")]
    static void Run()
    {
        bool changed = false;

        // ── resolve assets ────────────────────────────────────────────────
        var strawberry = Load<RadiantOrchard.FruitData>("Assets/GameData/FruitData/FruitData_Strawberry.asset");
        var pineapple  = Load<RadiantOrchard.FruitData>("Assets/GameData/FruitData/FruitData_Pineapple.asset");
        var watermelon = Load<RadiantOrchard.FruitData>("Assets/GameData/FruitData/FruitData_Watermelon.asset");

        var vKindness   = Load<VirtueDefinition>("Assets/GameData/Virtues/Virtue_Kindness.asset");
        var vGratitude  = Load<VirtueDefinition>("Assets/GameData/Virtues/Virtue_Gratitude.asset");
        var vGenerosity = Load<VirtueDefinition>("Assets/GameData/Virtues/Virtue_Generosity.asset");

        var level1 = Load<LevelDefinition>("Assets/GameData/Levels/Level_01.asset");

        // Load icon sprites
        var iconStrawberry = Load<Sprite>("Assets/GameData/Sprites/Icon_Strawberry.png");
        var iconPineapple  = Load<Sprite>("Assets/GameData/Sprites/Icon_Pineapple.png");
        var iconWatermelon = Load<Sprite>("Assets/GameData/Sprites/Icon_Watermelon.png");

        // ── TASK 3: Patch FruitData assets ───────────────────────────────
        // Strawberry = Love → closest virtue: Kindness (pink, heart / circulation)
        if (strawberry != null)
        {
            strawberry.fruitType        = FruitType.Strawberry;
            strawberry.displayName      = "Strawberry";
            strawberry.gestureType      = GestureType.DoubleTap;
            strawberry.placeholderColor = HexColor("FF4757"); // vivid red-pink
            strawberry.placeholderShape = PlaceholderShape.Sphere;
            strawberry.virtue           = vKindness;
            strawberry.symptomIcon      = iconStrawberry;
            EditorUtility.SetDirty(strawberry);
            changed = true;
            Debug.Log("✓ FruitData_Strawberry — DoubleTap, Kindness, Icon assigned");
        }
        else Debug.LogWarning("✗ FruitData_Strawberry.asset not found");

        // Pineapple = Joy → closest virtue: Gratitude (golden yellow, immunity / energy)
        if (pineapple != null)
        {
            pineapple.fruitType        = FruitType.Pineapple;
            pineapple.displayName      = "Pineapple";
            pineapple.gestureType      = GestureType.DoubleTap;
            pineapple.placeholderColor = HexColor("FFD700"); // golden yellow
            pineapple.placeholderShape = PlaceholderShape.Sphere;
            pineapple.virtue           = vGratitude;
            pineapple.symptomIcon      = iconPineapple;
            EditorUtility.SetDirty(pineapple);
            changed = true;
            Debug.Log("✓ FruitData_Pineapple — DoubleTap, Gratitude, Icon assigned");
        }
        else Debug.LogWarning("✗ FruitData_Pineapple.asset not found");

        // Watermelon = Peace → closest virtue: Generosity (deep green, nervous system)
        if (watermelon != null)
        {
            watermelon.fruitType        = FruitType.Watermelon;
            watermelon.displayName      = "Watermelon";
            watermelon.gestureType      = GestureType.DoubleTap;
            watermelon.placeholderColor = HexColor("4CAF50"); // lush green
            watermelon.placeholderShape = PlaceholderShape.Sphere;
            watermelon.virtue           = vGenerosity;
            watermelon.symptomIcon      = iconWatermelon;
            EditorUtility.SetDirty(watermelon);
            changed = true;
            Debug.Log("✓ FruitData_Watermelon — DoubleTap, Generosity, Icon assigned");
        }
        else Debug.LogWarning("✗ FruitData_Watermelon.asset not found");

        // ── TASK 4: Patch Level_01 ────────────────────────────────────────
        if (level1 != null)
        {
            level1.levelId      = "level_01";
            level1.displayName  = "Level 1 – Heart of the Orchard";
            level1.virtue       = vKindness;
            level1.nextLevel    = null;   // no Level 2 yet

            // Vibrancy gate — 100 pts = 10 healed stickmen at 10pts each
            level1.requiredVibrancy   = 100;

            // Spawn pacing — tutorial-friendly
            level1.spawnInterval      = 12f;   // new stickman every 12 seconds
            level1.maxActiveStickmen  = 3;     // at most 3 on island at once
            level1.patienceTime       = 30f;   // 30s before stickman walks off

            // Replace objectives with one clean L1 objective
            level1.objectives = new List<ObjectiveEntry>
            {
                new ObjectiveEntry
                {
                    objectiveId    = "level_01_heal_stickmen",
                    description    = "Help 10 Stickmen by delivering the right fruit.",
                    type           = ObjectiveType.HelpNPC,
                    targetId       = "",          // VibrancyObjectiveDriver reports all heals
                    requiredCount  = 10,
                    vibrancyReward = 0            // vibrancy comes from StickmanController.vibrancyReward
                }
            };

            // NPC scenario for persistent named NPCs (optional; tutorial NPC)
            if (level1.npcScenarios == null || level1.npcScenarios.Count == 0)
            {
                level1.npcScenarios = new List<NPCScenarioEntry>
                {
                    new NPCScenarioEntry
                    {
                        npcId              = "npc_tutorial",
                        startingEmotion    = EmotionState.Sad,
                        problemDescription = "The orchard has lost its colour. Help the visitors!",
                        thoughtBubbleText  = "I need fruit...",
                        requiredVirtueId   = "kindness",
                        resolvedEmotion    = EmotionState.Happy,
                        vibrancyReward     = 10
                    }
                };
            }

            EditorUtility.SetDirty(level1);
            changed = true;
            Debug.Log("✓ Level_01 — vibrancy=100, 10-heal objective, spawnInterval=12s, patienceTime=30s");
        }
        else Debug.LogWarning("✗ Level_01.asset not found");

        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("=== Setup Level 1 Data COMPLETE. Run 'Wire Level 1 Scene' next. ===");
        }
    }

    static T Load<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path);

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
#endif

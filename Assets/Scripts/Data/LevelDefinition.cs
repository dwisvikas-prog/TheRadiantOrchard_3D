using System;
using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    [Serializable]
    public class ObjectiveEntry
    {
        public string objectiveId;
        [TextArea] public string description;
        public ObjectiveType type;
        public string targetId; // NPC id / fruit zone id / environmental object id, depending on type
        public int requiredCount = 1;
        public int vibrancyReward = 5;
    }

    [Serializable]
    public class NPCScenarioEntry
    {
        public string npcId;
        public EmotionState startingEmotion = EmotionState.Sad;
        [TextArea] public string problemDescription;
        [TextArea] public string thoughtBubbleText;
        public string requiredVirtueId;
        public EmotionState resolvedEmotion = EmotionState.Happy;
        public int vibrancyReward = 10;
    }

    [CreateAssetMenu(fileName = "Level_", menuName = "Radiant Orchard/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        public string levelId;
        public string displayName;
        public VirtueDefinition virtue;
        public List<ObjectiveEntry> objectives = new List<ObjectiveEntry>();
        public List<NPCScenarioEntry> npcScenarios = new List<NPCScenarioEntry>();
        public int requiredVibrancy = 100;

        // Visitor pacing for this level — NPCSpawner reads these from the active
        // level instead of using its own fixed inspector defaults (see
        // NPCSpawner.ApplyPacing). Defaults match what NPCSpawner/StickmanController
        // used before this existed, so an untouched level behaves the same as today.
        [Header("Visitor Pacing")]
        public float spawnInterval = 15f;
        public int maxActiveStickmen = 3;
        public float patienceTime = 25f;
        public List<string> rewardIds = new List<string>();
        public LevelDefinition nextLevel;
    }
}

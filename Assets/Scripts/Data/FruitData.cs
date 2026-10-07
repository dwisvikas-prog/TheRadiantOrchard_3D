using UnityEngine;

namespace RadiantOrchard
{
    // Data for the gesture/tonic-brewing MVP loop: which gesture harvests this
    // fruit, which virtue its tonic carries, and a placeholder look until real
    // mesh/VFX art replaces the cube/sphere stand-in.
    [CreateAssetMenu(fileName = "FruitData_", menuName = "Radiant Orchard/Fruit Data")]
    public class FruitData : ScriptableObject
    {
        public FruitType fruitType;
        public string displayName;
        public GestureType gestureType;
        public VirtueDefinition virtue;

        [Header("Visual")]
        [Tooltip("Real 3D fruit model/prefab. When set, this replaces the placeholder shape entirely.")]
        public GameObject visualPrefab;

        [Header("Placeholder visual (used only when visualPrefab is not set)")]
        public PlaceholderShape placeholderShape = PlaceholderShape.Sphere;
        public Color placeholderColor = Color.white;

        [Header("UI")]
        [Tooltip("Legacy fruit sprite — Stickman symptom bubble now prefers symptomLabel from the health matrix.")]
        public Sprite symptomIcon;

        [Header("Virtue-Health Matrix (optional overrides — empty = proposal defaults)")]
        public bool useCustomHealthStat;
        public HealthStat healthStat = HealthStat.HeartCirculation;
        [Tooltip("Symptom shown in Stickman thought bubble, e.g. Frustrated / Anxious.")]
        public string symptomLabel;
        [Tooltip("Short health hint under the symptom, e.g. Heart / Breath.")]
        public string healthHint;
        public Color healVfxColor = Color.clear;

        [Header("Spirit / Healing effect (ambient NPC self-care loop)")]
        [Tooltip("How much this fruit restores an NPC's Spirit (0-1 scale) when consumed.")]
        [Range(0f, 1f)] public float spiritRestoreAmount = 0.3f;
        [Tooltip("How much this fruit heals an NPC's health/injury when consumed.")]
        [Range(0f, 1f)] public float healingAmount = 0f;
        [Tooltip("Which NPC conditions this fruit is actually useful for. Empty = useful for none (NPCs will reject it).")]
        public NPCCondition[] suitableConditions = { NPCCondition.LowSpirit };
        [Tooltip("0-1: lower = rarer, used to bias which fruit an NPC prefers when multiple are suitable.")]
        [Range(0f, 1f)] public float rarity = 1f;
        [Tooltip("Emotion an NPC shows after this fruit successfully helps it.")]
        public EmotionState emotionalResponse = EmotionState.Grateful;

        // Data-driven suitability check used by the NPC brain (StickmanController's
        // ambient loop) — adding a new fruit only means authoring a new asset with
        // these fields set; no NPC code needs to change.
        public bool IsSuitableFor(NPCCondition condition)
        {
            if (suitableConditions == null) return false;
            for (int i = 0; i < suitableConditions.Length; i++)
                if (suitableConditions[i] == condition) return true;
            return false;
        }
    }
}

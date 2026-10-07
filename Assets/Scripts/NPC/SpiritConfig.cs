using UnityEngine;

namespace RadiantOrchard
{
    // Shared tuning for every NPC's Spirit — one asset, reused by every
    // NPCSpiritComponent, so balance changes never require touching NPC code
    // or every individual prefab instance.
    [CreateAssetMenu(fileName = "SpiritConfig", menuName = "Radiant Orchard/Spirit Config")]
    public class SpiritConfig : ScriptableObject
    {
        [Header("Thresholds (0-1 Spirit scale)")]
        [Range(0f, 1f)] public float lowSpiritBelow = 0.55f;
        [Range(0f, 1f)] public float sickBelow = 0.25f;

        [Header("Ambient decay (the 'living orchard' — Spirit fades over time)")]
        [Tooltip("Spirit lost per second while roaming happily. 0 disables ambient decay.")]
        public float decayPerSecond = 0.01f;

        [Header("Starting value for newly healed NPCs entering ambient life")]
        [Range(0f, 1f)] public float startingSpirit = 0.85f;
    }
}

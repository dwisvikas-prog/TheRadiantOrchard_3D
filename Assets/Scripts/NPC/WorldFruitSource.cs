using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    // A fruit an NPC can autonomously notice, search for, walk to and consume —
    // separate from FruitHarvester (which is the player's tap-gesture harvest
    // target tied 1:1 to a specific waiting Stickman). Placing this on a world
    // object is all that's needed to make it something NPCs can seek out; no
    // NPC code needs to change to add a new fruit type, only a new FruitData
    // asset + this component referencing it.
    //
    // Self-registers in a static list (no per-frame FindObjectsOfType scans) so
    // NPCs can do a cheap proximity query.
    public class WorldFruitSource : MonoBehaviour
    {
        [SerializeField] private FruitData fruitData;
        [SerializeField] private float interactRadius = 1.4f;
        [SerializeField] private bool consumedOnUse = false; // false = a communal tree/patch that regrows instantly

        public FruitData FruitData => fruitData;
        public float InteractRadius => interactRadius;
        public bool IsAvailable => isActiveAndEnabled && fruitData != null;

        private static readonly List<WorldFruitSource> active = new List<WorldFruitSource>();
        public static IReadOnlyList<WorldFruitSource> Active => active;

        private void OnEnable() { if (!active.Contains(this)) active.Add(this); }
        private void OnDisable() { active.Remove(this); }

        // Finds the closest available source suitable for the given condition,
        // preferring rarer/stronger fruit when several are suitable. Squared
        // distance only — no sqrt, no allocations, single pass over the small
        // active list (bounded by how many fruit sources actually exist in the
        // orchard, not total scene object count).
        public static WorldFruitSource FindBestFor(Vector3 fromPosition, NPCCondition condition, float maxRange)
        {
            WorldFruitSource best = null;
            float bestScore = float.NegativeInfinity;
            float maxRangeSqr = maxRange * maxRange;

            for (int i = 0; i < active.Count; i++)
            {
                var source = active[i];
                if (source == null || !source.IsAvailable) continue;
                if (!source.fruitData.IsSuitableFor(condition)) continue;

                float distSqr = (source.transform.position - fromPosition).sqrMagnitude;
                if (distSqr > maxRangeSqr) continue;

                // Closer + rarer/stronger fruit scores higher; distance dominates
                // so NPCs don't trek across the island for a marginally better fruit.
                float proximityScore = 1f - Mathf.Sqrt(distSqr) / maxRange;
                float qualityScore = Mathf.Max(source.fruitData.spiritRestoreAmount, source.fruitData.healingAmount) * source.fruitData.rarity;
                float score = proximityScore * 2f + qualityScore;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = source;
                }
            }

            return best;
        }

        // Applies this fruit's effect to the NPC and reports what it gave, so
        // the caller can react (drive emotion, VFX, etc.) — data-driven, no
        // per-fruit special-casing in the NPC brain.
        public void Consume(NPCSpiritComponent spirit)
        {
            if (fruitData == null || spirit == null) return;

            if (fruitData.spiritRestoreAmount > 0f) spirit.RestoreSpirit(fruitData.spiritRestoreAmount);
            if (fruitData.healingAmount > 0f) spirit.ApplyHealing(fruitData.healingAmount);

            if (consumedOnUse) gameObject.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}

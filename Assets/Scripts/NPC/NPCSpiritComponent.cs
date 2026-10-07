using System;
using UnityEngine;

namespace RadiantOrchard
{
    // Per-NPC Spirit value driving NPCCondition/NPCNeed — independent of
    // StickmanController's own player-triggered rescue flow (Idle_Sad/Receiving/
    // Celebrate), which is untouched. This only governs the *ambient* self-care
    // loop: a healed NPC's Spirit slowly fades while it lives its life, and once
    // low enough it needs to find a suitable fruit on its own (see
    // StickmanController's ambient sub-state machine + WorldFruitSource).
    //
    // Condition is recomputed only when Spirit actually changes (event-driven),
    // not polled every frame — see perf requirement: no unnecessary Update work.
    public class NPCSpiritComponent : MonoBehaviour
    {
        [SerializeField] private SpiritConfig config;
        [SerializeField, Range(0f, 1f)] private float spirit01 = 0.85f;
        [SerializeField, Range(0f, 1f)] private float health01 = 1f;

        public event Action<NPCCondition> OnConditionChanged;

        public float Spirit01 => spirit01;
        public float Health01 => health01;
        public NPCCondition Condition { get; private set; } = NPCCondition.Healthy;

        public NPCNeed CurrentNeed
        {
            get
            {
                switch (Condition)
                {
                    case NPCCondition.Sick:
                    case NPCCondition.Injured:
                        return NPCNeed.Healing;
                    case NPCCondition.LowSpirit:
                        return NPCNeed.SpiritRestore;
                    default:
                        return NPCNeed.None;
                }
            }
        }

        private bool decayEnabled;

        private void Awake()
        {
            if (config != null) spirit01 = config.startingSpirit;
            RecomputeCondition();
        }

        // Ambient decay only runs once this NPC has entered its "living" phase
        // (after being healed and roaming happily) — StickmanController toggles
        // this so a freshly spawned, still-sad Stickman doesn't decay before the
        // player has even met it.
        public void SetAmbientDecayEnabled(bool enabled) => decayEnabled = enabled;

        // Called when a pooled Stickman is reinitialized for a new visitor —
        // Awake() only runs once for a pooled instance, so without this a
        // reused NPC would keep whatever low Spirit it ended its previous life
        // with instead of starting fresh.
        public void ResetToFull()
        {
            spirit01 = config != null ? config.startingSpirit : 0.85f;
            health01 = 1f;
            decayEnabled = false;
            decayAccumulator = 0f;
            RecomputeCondition();
        }

        private float decayAccumulator;

        private void Update()
        {
            if (!decayEnabled || config == null || config.decayPerSecond <= 0f) return;

            // Batch the small per-second decay instead of writing spirit01 (and
            // re-running the condition check) every single frame.
            decayAccumulator += config.decayPerSecond * Time.deltaTime;
            if (decayAccumulator < 0.005f) return;

            SetSpirit(spirit01 - decayAccumulator);
            decayAccumulator = 0f;
        }

        public void RestoreSpirit(float amount)
        {
            if (amount <= 0f) return;
            SetSpirit(spirit01 + amount);
        }

        public void ApplyHealing(float amount)
        {
            if (amount <= 0f) return;
            health01 = Mathf.Clamp01(health01 + amount);
            RecomputeCondition();
        }

        private void SetSpirit(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, spirit01)) return;
            spirit01 = clamped;
            RecomputeCondition();
        }

        private void RecomputeCondition()
        {
            NPCCondition next;
            if (health01 < 0.4f) next = NPCCondition.Injured;
            else if (config != null && spirit01 < config.sickBelow) next = NPCCondition.Sick;
            else if (config != null && spirit01 < config.lowSpiritBelow) next = NPCCondition.LowSpirit;
            else next = NPCCondition.Healthy;

            if (next == Condition) return;
            Condition = next;
            OnConditionChanged?.Invoke(Condition);
        }
    }
}

using UnityEngine;

namespace RadiantOrchard
{
    // Tapping a waiting Stickman previously did nothing (no Interactable on
    // the prefab, so InteractionController's raycast found no target to talk
    // to). This shows the same "needs X fruit" info as its SymptomBubble, as
    // a coach tip, so a tap always tells the player what to do next.
    [RequireComponent(typeof(StickmanController))]
    public class StickmanTapPrompt : Interactable
    {
        private StickmanController stickman;

        private void Awake()
        {
            stickman = GetComponent<StickmanController>();
            vibrancyReward = 0;
        }

        protected override bool CanInteract(GameObject interactor) =>
            stickman != null && stickman.CanReceiveTonic;

        protected override bool OnInteracted(GameObject interactor)
        {
            if (stickman == null || stickman.RequiredFruit == null) return false;
            var entry = FruitHealthMatrix.Resolve(stickman.RequiredFruit);
            EmptyIslandCoachBar.SetTip($"{entry.symptomLabel} — needs {stickman.RequiredFruit.displayName}!");
            return false; // informational tap, no vibrancy reward
        }
    }
}

using UnityEngine;

namespace RadiantOrchard
{
    public class NPCInteractable : Interactable
    {
        [SerializeField] private string npcId;
        [SerializeField] private EmotionState startingEmotion = EmotionState.Sad;
        [SerializeField] private EmotionState resolvedEmotion = EmotionState.Happy;
        [SerializeField] private string linkedObjectiveId;

        public string NpcId => npcId;

        private void Start()
        {
            if (GameState.Instance == null) return;
            if (GameState.Instance.GetNpcEmotion(npcId) == EmotionState.Neutral)
                GameState.Instance.SetNpcEmotion(npcId, startingEmotion);
        }

        // Already-resolved NPCs don't re-trigger the reward on repeat taps.
        protected override bool CanInteract(GameObject interactor)
        {
            if (GameState.Instance == null) return true;
            return GameState.Instance.GetNpcEmotion(npcId) != resolvedEmotion;
        }

        protected override bool OnInteracted(GameObject interactor)
        {
            if (GameState.Instance == null) return false;

            GameState.Instance.SetNpcEmotion(npcId, resolvedEmotion);

            if (!string.IsNullOrEmpty(linkedObjectiveId))
            {
                int progress = GameState.Instance.GetObjectiveProgress(linkedObjectiveId) + 1;
                GameState.Instance.SetObjectiveProgress(linkedObjectiveId, progress);
            }

            return true;
        }
    }
}

using UnityEngine;

namespace RadiantOrchard
{
    // Visual/data side of an NPC: reads emotion from GameState (the single
    // source of truth — this script holds no emotion state of its own), plays
    // the matching animation, and shows the level's scenario text in a thought
    // bubble. NPCInteractable (write side) is what actually changes GameState
    // when the player taps this NPC.
    [RequireComponent(typeof(Animator))]
    public class NPCController : MonoBehaviour
    {
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;
        [SerializeField] private LevelDefinition sourceLevel;

        private Animator animator;
        private ThoughtBubble thoughtBubble;

        public string NpcId => npcId;
        public string DisplayName => displayName;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            thoughtBubble = GetComponentInChildren<ThoughtBubble>();
            if (thoughtBubble == null)
            {
                var bubbleGO = new GameObject("ThoughtBubble");
                bubbleGO.transform.SetParent(transform, false);
                thoughtBubble = bubbleGO.AddComponent<ThoughtBubble>();
            }
        }

        private void Start()
        {
            if (GameState.Instance != null)
                GameState.Instance.NpcEmotionChanged += OnAnyNpcEmotionChanged;

            var currentEmotion = GameState.Instance != null ? GameState.Instance.GetNpcEmotion(npcId) : EmotionState.Neutral;
            RefreshVisual(currentEmotion);
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.NpcEmotionChanged -= OnAnyNpcEmotionChanged;
        }

        private void OnAnyNpcEmotionChanged(string changedNpcId, EmotionState emotion)
        {
            if (changedNpcId != npcId) return;
            RefreshVisual(emotion);
        }

        private void RefreshVisual(EmotionState emotion)
        {
            bool isPositive = emotion == EmotionState.Happy || emotion == EmotionState.Excited ||
                               emotion == EmotionState.Grateful || emotion == EmotionState.Peaceful;

            if (animator != null && emotion != EmotionState.Neutral)
                animator.SetTrigger(isPositive ? "Happy" : "Sad");

            if (thoughtBubble == null) return;

            if (isPositive)
            {
                thoughtBubble.SetVisible(false);
                return;
            }

            var scenario = FindScenario();
            string text = scenario != null && !string.IsNullOrEmpty(scenario.thoughtBubbleText)
                ? scenario.thoughtBubbleText
                : emotion.ToString();

            thoughtBubble.SetText(text);
            thoughtBubble.SetVisible(true);
        }

        private NPCScenarioEntry FindScenario()
        {
            if (sourceLevel == null) return null;
            foreach (var scenario in sourceLevel.npcScenarios)
            {
                if (scenario.npcId == npcId) return scenario;
            }
            return null;
        }
    }
}

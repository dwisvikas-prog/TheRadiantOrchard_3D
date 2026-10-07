using UnityEngine;

namespace RadiantOrchard
{
    // Wisdom Tree Micro-Quiz: fires after every quizIntervalLevels-th level
    // completes (Level 5, 10, 15... by default), matching the master spec's
    // "every 5 levels" trigger — not a heal-count trigger. Questions come from
    // a QuizDatabase asset so the bank can grow without touching this script.
    public class QuizManager : MonoBehaviour
    {
        [SerializeField] private QuizPanelUI panelUI;
        [SerializeField] private QuizDatabase database;
        [SerializeField] private int quizIntervalLevels = 5;
        [SerializeField] private float goldenHarvestMultiplier = 2f;
        [SerializeField] private float goldenHarvestDuration = 30f;

        private void Start()
        {
            if (GameState.Instance != null)
                GameState.Instance.LevelCompleted += OnLevelCompleted;
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.LevelCompleted -= OnLevelCompleted;
        }

        private void OnLevelCompleted(string levelId)
        {
            int levelNumber = ExtractLevelNumber(levelId);
            if (quizIntervalLevels <= 0 || levelNumber <= 0 || levelNumber % quizIntervalLevels != 0) return;
            if (database == null || database.questions == null || database.questions.Count == 0 || panelUI == null) return;

            var question = database.questions[Random.Range(0, database.questions.Count)];
            panelUI.Show(question, OnAnswerSelected);
        }

        // "level_05" / "Level_05" / "level05" all resolve to 5 — same digit
        // extraction LevelIndicatorUI already uses, so the two stay consistent.
        private static int ExtractLevelNumber(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return 0;
            var digits = new System.Text.StringBuilder();
            foreach (var c in levelId)
                if (char.IsDigit(c)) digits.Append(c);
            return digits.Length == 0 ? 0 : int.Parse(digits.ToString());
        }

        private void OnAnswerSelected(int chosenIndex, QuizQuestion question)
        {
            bool correct = chosenIndex == question.correctIndex;

            if (correct)
            {
                if (GameState.Instance != null)
                    GameState.Instance.StartVibrancyMultiplier(goldenHarvestMultiplier, goldenHarvestDuration);
                SfxPlayer.Instance?.PlayQuizCorrect();
                var cam = Camera.main;
                var burstPos = cam != null ? cam.transform.position + cam.transform.forward * 6f : transform.position;
                SimpleVfx.Burst(burstPos, new Color(1f, 0.82f, 0.1f), count: 40, speed: 5f, size: 0.3f, lifetime: 1.3f);
            }
            else
            {
                SfxPlayer.Instance?.PlayQuizIncorrect();
            }

            panelUI.ShowResult(correct);
        }
    }
}

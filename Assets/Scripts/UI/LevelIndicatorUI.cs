using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    public class LevelIndicatorUI : MonoBehaviour
    {
        [SerializeField] private Text levelText;

        private void Start()
        {
            if (GameState.Instance == null) return;
            GameState.Instance.LevelStarted += OnLevelStarted;
            OnLevelStarted(GameState.Instance.CurrentLevelId);
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.LevelStarted -= OnLevelStarted;
        }

        private void OnLevelStarted(string levelId)
        {
            if (levelText != null) levelText.text = "Level " + ExtractNumber(levelId);
        }

        private static string ExtractNumber(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return "1";

            var digits = new System.Text.StringBuilder();
            foreach (var c in levelId)
                if (char.IsDigit(c)) digits.Append(c);

            if (digits.Length == 0) return "1";
            return int.Parse(digits.ToString()).ToString();
        }
    }
}

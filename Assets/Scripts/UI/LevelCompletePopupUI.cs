using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    public class LevelCompletePopupUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text messageText;
        [SerializeField] private Button continueButton;

        private void Awake()
        {
            if (continueButton != null) continueButton.onClick.AddListener(Hide);
            SetVisible(false);
        }

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
            if (messageText != null) messageText.text = "Level Complete!";
            SetVisible(true);
        }

        private void Hide() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }
    }
}

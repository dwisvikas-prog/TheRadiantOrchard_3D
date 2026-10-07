using System;
using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // Simple 3-button quiz panel. QuizManager owns the question bank and
    // scoring logic; this only shows/hides and reports which button was
    // pressed back through the callback passed to Show().
    public class QuizPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text questionText;
        [SerializeField] private Button[] optionButtons; // exactly 3, in order
        [SerializeField] private Text[] optionLabels;     // matches optionButtons
        [SerializeField] private Text resultText;
        [SerializeField] private float resultDisplayTime = 1.5f;

        private Action<int, QuizQuestion> onAnswered;
        private QuizQuestion currentQuestion;

        private void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i; // capture per-button
                if (optionButtons[i] != null)
                    optionButtons[i].onClick.AddListener(() => SelectAnswer(index));
            }

            if (resultText != null) resultText.gameObject.SetActive(false);

            // Deliberately NOT calling SetVisible(false) here: this component
            // lives on the panel itself, which starts inactive in the scene —
            // so Awake() is deferred until the panel's first SetActive(true)
            // (Show()'s own call). Hiding it again from inside that same
            // synchronous activation would undo the very Show() that woke it.
        }

        public void Show(QuizQuestion question, Action<int, QuizQuestion> callback)
        {
            currentQuestion = question;
            onAnswered = callback;

            if (questionText != null) questionText.text = question.questionText;
            for (int i = 0; i < optionLabels.Length; i++)
            {
                if (optionLabels[i] == null) continue;
                optionLabels[i].text = i < question.options.Length ? question.options[i] : string.Empty;
            }

            SetButtonsInteractable(true);
            if (resultText != null) resultText.gameObject.SetActive(false);
            SetVisible(true);
        }

        public void ShowResult(bool correct)
        {
            SetButtonsInteractable(false);

            if (resultText != null)
            {
                resultText.text = correct ? "Correct! Golden Harvest active!" : "Not quite.";
                resultText.gameObject.SetActive(true);
            }

            Invoke(nameof(HidePanel), resultDisplayTime);
        }

        private void SelectAnswer(int index)
        {
            var question = currentQuestion;
            var callback = onAnswered;
            callback?.Invoke(index, question);
        }

        private void HidePanel()
        {
            SetVisible(false);
            if (resultText != null) resultText.gameObject.SetActive(false);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (optionButtons == null) return;
            foreach (var b in optionButtons)
                if (b != null) b.interactable = interactable;
        }

        private void SetVisible(bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }
    }
}

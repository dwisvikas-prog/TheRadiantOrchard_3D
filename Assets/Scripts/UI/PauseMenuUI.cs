using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
            SetPanelVisible(false);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f; // never leave the game frozen if this object goes away mid-pause
        }

        public void TogglePause()
        {
            if (Time.timeScale == 0f) Resume();
            else Pause();
        }

        public void Pause()
        {
            Time.timeScale = 0f;
            SetPanelVisible(true);
        }

        public void Resume()
        {
            Time.timeScale = 1f;
            SetPanelVisible(false);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            // GameState survives the scene reload (DontDestroyOnLoad), so without
            // this the reload alone leaves vibrancy/progress exactly as they were
            // and "Restart" visibly does nothing. Restart means "start over from
            // the beginning", not "retry whatever level I'm currently on".
            GameState.Instance?.RestartGameFromBeginning();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }

        private void SetPanelVisible(bool visible)
        {
            if (pausePanel != null) pausePanel.SetActive(visible);
        }
    }
}

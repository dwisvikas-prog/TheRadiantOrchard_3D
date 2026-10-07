using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // One button toggles EditModeManager on/off and shows a small hint
    // banner while active. All the actual drag logic lives in
    // EditModeManager; this is just the on-screen switch for it.
    public class EditModeToggleUI : MonoBehaviour
    {
        [SerializeField] private EditModeManager editModeManager;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Text buttonLabel;
        [SerializeField] private GameObject hintBanner;

        private void Awake()
        {
            if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
            SetHintVisible(false);
        }

        private void Toggle()
        {
            if (editModeManager == null) return;

            if (editModeManager.EditModeActive)
            {
                editModeManager.ExitEditMode();
                if (buttonLabel != null) buttonLabel.text = "Edit Island";
                SetHintVisible(false);
            }
            else
            {
                editModeManager.EnterEditMode();
                if (buttonLabel != null) buttonLabel.text = "Done Editing";
                SetHintVisible(true);
            }
        }

        private void SetHintVisible(bool visible)
        {
            if (hintBanner != null) hintBanner.SetActive(visible);
        }
    }
}

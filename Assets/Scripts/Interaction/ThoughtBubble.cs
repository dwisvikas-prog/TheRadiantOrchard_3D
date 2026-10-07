using UnityEngine;

namespace RadiantOrchard
{
    // Minimal placeholder speech-bubble: billboarded world-space text above an
    // NPC's head. Swap for real bubble art/UI later; SetText/SetVisible stay
    // the same for callers either way.
    public class ThoughtBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Color textColor = Color.white;

        private TextMesh label;
        private Camera targetCamera;

        private void Awake()
        {
            targetCamera = Camera.main;

            var textGO = new GameObject("ThoughtBubbleText");
            textGO.transform.SetParent(transform, false);
            textGO.transform.localPosition = localOffset;

            label = textGO.AddComponent<TextMesh>();
            label.fontSize = 48;
            label.characterSize = 0.12f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = textColor;
            label.text = string.Empty;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || label == null) return;
            label.transform.rotation = targetCamera.transform.rotation;
        }

        public void SetText(string text)
        {
            if (label != null) label.text = text;
        }

        public void SetVisible(bool visible)
        {
            if (label != null) label.gameObject.SetActive(visible);
        }
    }
}

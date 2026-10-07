using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // World-space "try this one" indicator: a pulsing icon + gesture label
    // that parents itself above whatever fruit TutorialManager wants the
    // player to try next. Billboarded to camera, same pattern as
    // SymptomBubble/ThoughtBubble.
    public class TutorialPointerUI : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private Vector2 iconSize = new Vector2(0.7f, 0.7f);
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmount = 0.25f;
        [SerializeField] private Color iconColor = new Color(1f, 0.85f, 0.2f);
        // Fraction of the screen kept clear at the edges when the pointed fruit is
        // outside the view — see AnchorToViewport.
        [SerializeField] [Range(0f, 0.4f)] private float edgeMargin = 0.08f;

        private Canvas canvas;
        private RectTransform iconRect;
        private Text label;
        private Camera targetCamera;
        private Transform followTarget;

        private void Awake()
        {
            targetCamera = Camera.main;

            var canvasGO = new GameObject("TutorialPointerCanvas", typeof(RectTransform), typeof(Canvas));
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localScale = Vector3.one * 0.01f;

            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(canvasGO.transform, false);
            var icon = iconGO.GetComponent<Image>();
            icon.color = iconColor;
            iconRect = icon.rectTransform;
            iconRect.sizeDelta = iconSize / 0.01f;
            iconRect.anchoredPosition = Vector2.zero;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(canvasGO.transform, false);
            label = labelGO.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.rectTransform.sizeDelta = new Vector2(400f, 80f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 55f);

            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            // Camera.main is null whenever the rig isn't tagged MainCamera — the
            // pointer would then never billboard and never track its fruit, and it
            // renders as nothing at all (it lives in the 3D scene, not on the HUD).
            if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();
            if (targetCamera == null || canvas == null) return;
            // On Android the first few frames after activity launch can report a
            // zero-size camera pixel rect before the surface finishes initializing.
            // WorldToViewportPoint/ViewportToWorldPoint divide by that rect, so a
            // 0x0 rect produces NaN screen positions — skip following this frame
            // instead of feeding NaN into the transform.
            if (targetCamera.pixelWidth <= 0 || targetCamera.pixelHeight <= 0) return;

            if (followTarget != null)
                transform.position = AnchorToViewport(followTarget.position + localOffset);

            canvas.transform.rotation = targetCamera.transform.rotation;

            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            if (iconRect != null) iconRect.localScale = Vector3.one * pulse;
        }

        // Keeps the marker on screen. Fruits live out on the orchard paths across
        // the island, so the fruit being pointed at is often just outside the
        // view — a purely world-space marker then renders off-screen and the
        // gesture hint silently vanishes exactly when the player needs it. For an
        // off-screen target the viewport position is clamped to a margin, which
        // pins the pointer to the screen edge nearest the fruit while it keeps
        // tracking the real position (so it slides back to the fruit as the
        // player pans over).
        private Vector3 AnchorToViewport(Vector3 worldPosition)
        {
            Vector3 viewport = targetCamera.WorldToViewportPoint(worldPosition);
            if (viewport.z <= 0f) return worldPosition; // behind the camera — nothing sensible to clamp to

            float x = Mathf.Clamp(viewport.x, edgeMargin, 1f - edgeMargin);
            float y = Mathf.Clamp(viewport.y, edgeMargin, 1f - edgeMargin);

            // Already visible: keep the exact world placement so the marker sits
            // right on top of the fruit rather than being nudged inward.
            if (viewport.x == x && viewport.y == y) return worldPosition;

            // Reuse the target's own depth along the camera's forward axis so the
            // marker keeps a readable scale instead of snapping to the near plane.
            return targetCamera.ViewportToWorldPoint(new Vector3(x, y, viewport.z));
        }

        public void PointAt(Transform target, string gestureLabel)
        {
            followTarget = target;
            if (target != null) transform.position = target.position + localOffset;
            if (label != null) label.text = gestureLabel;
            SetVisible(true);
        }

        public void Hide()
        {
            followTarget = null;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (canvas != null) canvas.gameObject.SetActive(visible);
        }
    }
}

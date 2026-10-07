using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RadiantOrchard
{
    // ── UICanvasGuard ────────────────────────────────────────────────────────
    // Attaches to any Canvas in the scene and self-heals at runtime:
    //   • Forces Screen Space - Overlay so the HUD is always on top and
    //     always receives raycasts (World Space / Camera modes break touch
    //     when DioramaController moves the camera).
    //   • Ensures a GraphicRaycaster is present (missing = no UI clicks).
    //   • Ensures a single EventSystem + StandaloneInputModule exists in the
    //     scene (missing = ALL clicks / touches silently ignored).
    //   • Blocks invisible full-screen panels from swallowing input by
    //     disabling Raycast Target on Image components that are fully
    //     transparent and have no meaningful alpha (common "click blocker"
    //     accident when devs forget to uncheck Raycast Target on bg images).
    //
    // Add this component to every Canvas GameObject, OR add it once to any
    // persistent manager and let it find all Canvases on its own (set
    // autoFindAllCanvases = true).
    [ExecuteAlways]
    public class UICanvasGuard : MonoBehaviour
    {
        [Tooltip("If true, fixes every Canvas in the scene, not just the one on this GameObject.")]
        [SerializeField] private bool autoFindAllCanvases = true;

        [Tooltip("Alpha threshold below which an Image's Raycast Target is disabled automatically.")]
        [SerializeField] [Range(0f, 1f)] private float invisibleAlphaThreshold = 0.01f;

        private void OnEnable()  => Heal();
        private void Start()     => Heal();

#if UNITY_EDITOR
        // Also heal every time the scene is modified in Edit mode so the fix
        // is visible immediately without entering Play mode.
        private void OnValidate() => Heal();
#endif

        private void Heal()
        {
            EnsureEventSystem();

            if (autoFindAllCanvases)
            {
                foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    HealCanvas(canvas);
            }
            else
            {
                var canvas = GetComponent<Canvas>();
                if (canvas != null) HealCanvas(canvas);
            }
        }

        // ── Per-Canvas fixes ─────────────────────────────────────────────────
        private void HealCanvas(Canvas canvas)
        {
            // Fix 0: never touch a World Space canvas. Those are in-scene UI that
            // is deliberately positioned in 3D — SymptomBubble above a Stickman's
            // head, TutorialPointerUI above the fruit it's pointing at. Forcing
            // them to Overlay throws away that positioning (a World Space canvas
            // renders from its own RectTransform size, so a 0.01-scaled bubble
            // becomes an invisible speck at the origin) — the symptom icons and
            // the "Double-tap!" gesture hint silently disappeared the moment this
            // guard's Start() ran, because every bubble/pointer canvas is created
            // in Awake and Awake runs before any Start.
            if (canvas.renderMode == RenderMode.WorldSpace) return;

            // Fix 1: Render mode must be Screen Space - Overlay for a mobile
            // HUD — a Camera-mode canvas needs an assigned camera and breaks
            // when the camera rig pans/zooms.
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.Log($"[UICanvasGuard] Fixed Canvas '{canvas.name}': renderMode → ScreenSpaceOverlay", canvas);
            }

            // Fix 2: GraphicRaycaster is what translates screen touches into
            // UI pointer events — without it no button, slider, or scroll rect
            // ever receives input.
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"[UICanvasGuard] Added missing GraphicRaycaster to '{canvas.name}'", canvas);
            }

            // Fix 3: Disable Raycast Target on invisible Image components so
            // they can't act as invisible click-blockers in front of real UI.
            foreach (var img in canvas.GetComponentsInChildren<Image>(true))
            {
                if (img.raycastTarget && img.color.a < invisibleAlphaThreshold)
                {
                    img.raycastTarget = false;
                    Debug.Log($"[UICanvasGuard] Disabled Raycast Target on invisible Image '{img.name}' (alpha={img.color.a:F3})", img);
                }
            }
        }

        // ── EventSystem guard ────────────────────────────────────────────────
        // Called before every Canvas heal so it's only ever created once.
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            Debug.Log("[UICanvasGuard] Created missing EventSystem + StandaloneInputModule.");
        }
    }
}

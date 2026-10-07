using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // =========================================================================
    //  OnboardingManager  — first-time step-by-step game tutorial
    //
    //  Shows on very first launch (PlayerPrefs key "RO_Onboarded" = 0).
    //  Each step has:
    //    • A text instruction  ("A grey visitor has arrived!")
    //    • A sub-text hint     ("Look for the bubble above their head")
    //    • An animated hand/arrow that tracks a world object on screen
    //    • A gesture demo icon (tap / double-tap / swipe / pinch)
    //    • "OK / Got it" button OR auto-advance when the player does the action
    //
    //  Steps:
    //    0  Welcome — "Welcome to The Radiant Orchard!"
    //    1  Look at stickman — arrow points at first stickman
    //    2  Read bubble — "See the bubble? It shows which fruit they need"
    //    3  Find fruit — arrow points at the fruit, gesture icon shows Double-Tap
    //    4  Double-tap fruit — auto-advance when first harvest happens
    //    5  Watch tonic fly — "The tonic flies to the visitor!"
    //    6  See stickman heal — auto-advance on first StickmanHealed event
    //    7  Pan camera — "Drag ONE finger to explore the island"
    //    8  Rotate camera — "Twist TWO fingers to rotate your view"
    //    9  Done — "You're ready! Keep healing visitors to fill the Vibrancy bar"
    // =========================================================================
    public class OnboardingManager : MonoBehaviour
    {
        const string DoneKey = "RO_Onboarded";

        [Header("Panel root (assign in Inspector or auto-created)")]
        [SerializeField] private GameObject panelRoot;

        [Header("Guide character (optional — mascot that walks/points/talks)")]
        [SerializeField] private GameObject guideCharacterPrefab;
        [SerializeField] private string guideName = "Sage";
        [SerializeField] private Vector3 guideLocalOffset = new Vector3(-0.55f, -0.35f, 1.4f);
        [SerializeField] private Vector3 guideLocalEuler  = new Vector3(0f, 200f, 0f);
        [SerializeField] private float   guideScale = 0.35f;

        private GuideCharacter guide;

        // These are created at runtime if not assigned
        private Text    titleText;
        private Text    bodyText;
        private Button  okButton;
        private Text    okLabel;
        private Image   gestureIcon;
        private RectTransform handArrow;

        // Sprite assignments — assign in Inspector, or leave null for fallback shapes
        [Header("Gesture sprites (optional — fallback shapes used if null)")]
        [SerializeField] private Sprite spriteTap;
        [SerializeField] private Sprite spriteDoubleTap;
        [SerializeField] private Sprite spriteSwipe;
        [SerializeField] private Sprite spritePinch;
        [SerializeField] private Sprite spriteRotate;

        // ── state ─────────────────────────────────────────────────────────
        private int     step = 0;
        private bool    waitingForAction = false;  // true = don't show OK button
        private bool    active = false;
        private Camera  cam;

        // World-space target the arrow tracks this step (null = no tracking)
        private Transform trackTarget;

        // ── step definitions ──────────────────────────────────────────────
        private struct Step
        {
            public string title;
            public string body;
            public string ok;           // null = wait for action, not shown
            public GestureIconType icon;
        }

        private enum GestureIconType { None, Tap, DoubleTap, Swipe, Pinch, Rotate }

        private static readonly Step[] Steps =
        {
            // 0 – Welcome
            new Step {
                title = "Welcome to\nThe Radiant Orchard! 🌿",
                body  = "Grey Stickmen are arriving on the island.",
                ok    = "Let's go!",
                icon  = GestureIconType.None
            },
            // 1 – Spot the stickman
            new Step {
                title = "A visitor has arrived!",
                body  = "A grey figure is waiting.\nLook for them on the island.",
                ok    = "I see them!",
                icon  = GestureIconType.None
            },
            // 2 – Read the bubble
            new Step {
                title = "Read the bubble 💬",
                body  = "The fruit icon above their head shows\nwhich fruit they need.",
                ok    = "Got it",
                icon  = GestureIconType.None
            },
            // 3 – Find the fruit
            new Step {
                title = "Find the fruit!",
                body  = "Spot the glowing fruit on the island.\nThe arrow will guide you.",
                ok    = "I found it!",
                icon  = GestureIconType.None
            },
            // 4 – Double-tap fruit (auto-advance on harvest)
            new Step {
                title = "Double-tap the fruit! 👆👆",
                body  = "Quickly tap TWICE on the fruit\nto harvest it.",
                ok    = null,            // wait for actual double-tap
                icon  = GestureIconType.DoubleTap
            },
            // 5 – Watch tonic fly
            new Step {
                title = "The tonic is flying! ✨",
                body  = "Watch it travel to the visitor.",
                ok    = "Amazing!",
                icon  = GestureIconType.None
            },
            // 6 – Stickman healed (auto-advance on StickmanHealed)
            new Step {
                title = "They're healed! 🎉",
                body  = "The visitor gains colour and dances!\nThey'll wander the island happily now.",
                ok    = null,            // auto-advance when heal fires
                icon  = GestureIconType.None
            },
            // 7 – Pan camera
            new Step {
                title = "Explore the island 🗺️",
                body  = "Drag with ONE finger\nto pan around the island.",
                ok    = "Got it",
                icon  = GestureIconType.Swipe
            },
            // 8 – Rotate
            new Step {
                title = "Rotate your view 🔄",
                body  = "Use TWO fingers to twist\nand rotate the island view.",
                ok    = "Got it",
                icon  = GestureIconType.Rotate
            },
            // 9 – Done
            new Step {
                title = "You're ready! 🌟",
                body  = "Keep healing visitors to fill the\nVibrancy bar and complete the level.",
                ok    = "Play!",
                icon  = GestureIconType.None
            },
        };

        // ── lifecycle ─────────────────────────────────────────────────────
        private void Start()
        {
            cam = Camera.main;

            if (PlayerPrefs.GetInt(DoneKey, 0) == 1)
            {
                // Already seen — don't show again
                gameObject.SetActive(false);
                return;
            }

            BuildUI();
            SpawnGuide();
            SubscribeEvents();
            step   = 0;
            active = true;
            ShowStep(step);
        }

        private void OnDestroy() => UnsubscribeEvents();

        private void SubscribeEvents()
        {
            if (GameState.Instance != null)
                GameState.Instance.StickmanHealed += OnStickmanHealed;
            // Listen to harvest via a static event on FruitHarvester
            FruitHarvester.OnAnyHarvested += OnFruitHarvested;
        }

        private void UnsubscribeEvents()
        {
            if (GameState.Instance != null)
                GameState.Instance.StickmanHealed -= OnStickmanHealed;
            FruitHarvester.OnAnyHarvested -= OnFruitHarvested;
        }

        // ── event handlers ─────────────────────────────────────────────────
        private void OnFruitHarvested()
        {
            if (!active || step != 4) return;
            AdvanceToStep(5);
        }

        private void OnStickmanHealed()
        {
            if (!active || step != 6) return;
            AdvanceToStep(7);
        }

        // ── step logic ─────────────────────────────────────────────────────
        private void ShowStep(int s)
        {
            if (s >= Steps.Length) { FinishOnboarding(); return; }

            var data = Steps[s];

            titleText.text = guide != null && !string.IsNullOrEmpty(guideName)
                ? $"{guideName}: {data.title}"
                : data.title;
            bodyText.text  = data.body;

            if (guide != null) guide.PlayTalk();

            bool hasOk = !string.IsNullOrEmpty(data.ok);
            okButton.gameObject.SetActive(hasOk);
            if (hasOk && okLabel != null) okLabel.text = data.ok;

            SetGestureIcon(data.icon);

            // Choose tracking target for this step
            trackTarget = null;
            if (s == 1) trackTarget = FindStickman();
            if (s == 2) trackTarget = FindStickman();
            if (s == 3 || s == 4) trackTarget = FindFruit();

            if (guide != null && trackTarget != null) guide.PlayPoint();

            UpdateArrow();

            // Pulse the hand arrow
            StopAllCoroutines();
            StartCoroutine(PulseArrow());

            waitingForAction = !hasOk;
        }

        private void AdvanceToStep(int s)
        {
            step = s;
            ShowStep(step);
        }

        private void OnOkClicked()
        {
            AdvanceToStep(step + 1);
        }

        private void FinishOnboarding()
        {
            active = false;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            if (panelRoot != null) panelRoot.SetActive(false);
            if (guide != null) Destroy(guide.gameObject);
        }

        // ── guide character ──────────────────────────────────────────────
        private void SpawnGuide()
        {
            if (guideCharacterPrefab == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            var go = Instantiate(guideCharacterPrefab, cam.transform);
            go.name = "OnboardingGuide";
            go.transform.localPosition = guideLocalOffset;
            go.transform.localEulerAngles = guideLocalEuler;
            go.transform.localScale = Vector3.one * guideScale;

            guide = go.GetComponent<GuideCharacter>();
            if (guide == null) guide = go.AddComponent<GuideCharacter>();
            guide.PlayIdle();
        }

        // ── arrow tracking ─────────────────────────────────────────────────
        private void LateUpdate()
        {
            if (!active || trackTarget == null || handArrow == null) return;
            UpdateArrow();
        }

        private void UpdateArrow()
        {
            if (handArrow == null) return;

            if (trackTarget == null)
            {
                handArrow.gameObject.SetActive(false);
                return;
            }

            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            handArrow.gameObject.SetActive(true);

            // Convert world pos to screen pos
            Vector3 screenPos = cam.WorldToScreenPoint(trackTarget.position + Vector3.up * 1.5f);
            if (screenPos.z < 0) { handArrow.gameObject.SetActive(false); return; }

            // Clamp to screen edges
            float margin = 60f;
            screenPos.x = Mathf.Clamp(screenPos.x, margin, Screen.width  - margin);
            screenPos.y = Mathf.Clamp(screenPos.y, margin, Screen.height - margin);

            // Position on ScreenSpaceOverlay canvas — RectTransform anchoredPosition
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRoot.GetComponent<RectTransform>(),
                screenPos,
                null,   // null = overlay canvas, no cam needed
                out Vector2 localPt);

            handArrow.anchoredPosition = localPt;

            // Point arrow toward target
            Vector2 centre = Vector2.zero;
            Vector2 dir    = (new Vector2(screenPos.x, screenPos.y) -
                              new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)).normalized;
            float   angle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            handArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private IEnumerator PulseArrow()
        {
            while (active)
            {
                float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                if (handArrow != null)
                    handArrow.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.15f, t);
                yield return null;
            }
        }

        // ── scene finders ───────────────────────────────────────────────────
        private Transform FindStickman()
        {
            var all = FindObjectsByType<StickmanController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var s in all)
                if (s != null && s.gameObject.activeSelf &&
                    s.CurrentState == StickmanState.Idle_Sad)
                    return s.transform;
            return null;
        }

        private Transform FindFruit()
        {
            var all = FindObjectsByType<FruitHarvester>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var f in all)
                if (f != null && f.gameObject.activeSelf && f.IsHarvestable)
                    return f.transform;
            return null;
        }

        // ── UI BUILD ───────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Create ScreenSpaceOverlay canvas if not assigned
            if (panelRoot == null)
            {
                var cvGO = new GameObject("OnboardingCanvas");
                cvGO.transform.SetParent(transform, false);
                var cv = cvGO.AddComponent<Canvas>();
                cv.renderMode  = RenderMode.ScreenSpaceOverlay;
                cv.sortingOrder = 999;   // on top of everything
                cvGO.AddComponent<CanvasScaler>().uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                var scaler = cvGO.GetComponent<CanvasScaler>();
                scaler.referenceResolution  = new Vector2(1080, 607); // 16:9 landscape
                scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight   = 0.5f;
                cvGO.AddComponent<GraphicRaycaster>();
                panelRoot = cvGO;
            }

            var root = panelRoot.GetComponent<RectTransform>();

            // ── Semi-transparent backdrop at the bottom ───────────────────
            var backdrop = MakeRect("Backdrop", root);
            SetAnchors(backdrop, new Vector2(0f, 0f), new Vector2(1f, 0f));
            backdrop.offsetMin  = Vector2.zero;
            backdrop.offsetMax  = new Vector2(0f, 220f);
            var bgImg = backdrop.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.78f);

            // ── Gesture icon (left side) ──────────────────────────────────
            var gIconRT = MakeRect("GestureIcon", backdrop);
            gIconRT.anchorMin = gIconRT.anchorMax = new Vector2(0f, 0.5f);
            gIconRT.pivot     = new Vector2(0f, 0.5f);
            gIconRT.anchoredPosition = new Vector2(24f, 0f);
            gIconRT.sizeDelta = new Vector2(140f, 140f);
            gestureIcon = gIconRT.gameObject.AddComponent<Image>();
            gestureIcon.color = new Color(1f, 0.88f, 0.2f);
            gestureIcon.preserveAspect = true;

            // ── Title text ────────────────────────────────────────────────
            var titleRT = MakeRect("Title", backdrop);
            SetAnchors(titleRT, new Vector2(0.15f, 0.52f), new Vector2(1f, 1f));
            titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;
            titleText = titleRT.gameObject.AddComponent<Text>();
            titleText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize  = 34;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color     = new Color(1f, 0.92f, 0.4f);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleText.verticalOverflow   = VerticalWrapMode.Overflow;

            // ── Body text ─────────────────────────────────────────────────
            var bodyRT = MakeRect("Body", backdrop);
            SetAnchors(bodyRT, new Vector2(0.15f, 0.05f), new Vector2(0.82f, 0.52f));
            bodyRT.offsetMin = bodyRT.offsetMax = Vector2.zero;
            bodyText = bodyRT.gameObject.AddComponent<Text>();
            bodyText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bodyText.fontSize  = 24;
            bodyText.color     = Color.white;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow   = VerticalWrapMode.Overflow;

            // ── OK button (right side) ────────────────────────────────────
            var btnRT = MakeRect("OkButton", backdrop);
            btnRT.anchorMin = btnRT.anchorMax = new Vector2(1f, 0.5f);
            btnRT.pivot     = new Vector2(1f, 0.5f);
            btnRT.anchoredPosition = new Vector2(-18f, 0f);
            btnRT.sizeDelta = new Vector2(160f, 70f);

            var btnImg = btnRT.gameObject.AddComponent<Image>();
            btnImg.color = new Color(0.22f, 0.72f, 0.35f);

            okButton = btnRT.gameObject.AddComponent<Button>();
            var colours       = okButton.colors;
            colours.pressedColor  = new Color(0.15f, 0.55f, 0.25f);
            colours.highlightedColor = new Color(0.30f, 0.85f, 0.45f);
            okButton.colors = colours;
            okButton.onClick.AddListener(OnOkClicked);

            var lblRT = MakeRect("Label", btnRT);
            SetAnchors(lblRT, Vector2.zero, Vector2.one);
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            okLabel = lblRT.gameObject.AddComponent<Text>();
            okLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            okLabel.fontSize  = 28;
            okLabel.fontStyle = FontStyle.Bold;
            okLabel.color     = Color.white;
            okLabel.alignment = TextAnchor.MiddleCenter;

            // ── Hand / arrow indicator ────────────────────────────────────
            handArrow = MakeRect("HandArrow", root);
            handArrow.sizeDelta = new Vector2(64f, 64f);
            var arrowImg = handArrow.gameObject.AddComponent<Image>();
            arrowImg.color = new Color(1f, 0.92f, 0.2f, 0.9f);
            // Build a simple arrow shape with a rotated square as fallback
            // (no sprite needed). If spriteTap is set it will be overridden.
            handArrow.gameObject.SetActive(false);
        }

        private void SetGestureIcon(GestureIconType type)
        {
            if (gestureIcon == null) return;

            Sprite s = null;
            Color  c = new Color(1f, 0.88f, 0.2f);

            switch (type)
            {
                case GestureIconType.Tap:       s = spriteTap;       break;
                case GestureIconType.DoubleTap: s = spriteDoubleTap; c = new Color(0.2f, 0.8f, 1f); break;
                case GestureIconType.Swipe:     s = spriteSwipe;     c = new Color(1f, 0.6f, 0.2f); break;
                case GestureIconType.Pinch:     s = spritePinch;     c = new Color(0.8f, 0.4f, 1f); break;
                case GestureIconType.Rotate:    s = spriteRotate;    c = new Color(0.4f, 1f, 0.6f); break;
                case GestureIconType.None:
                    gestureIcon.gameObject.SetActive(false);
                    return;
            }

            gestureIcon.gameObject.SetActive(true);
            gestureIcon.sprite = s;
            gestureIcon.color  = c;
        }

        // ── UI helpers ─────────────────────────────────────────────────────
        private static RectTransform MakeRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin  = min;
            rt.anchorMax  = max;
            rt.pivot      = new Vector2(0.5f, 0.5f);
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;
        }
    }
}

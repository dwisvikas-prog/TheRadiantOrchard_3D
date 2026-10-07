using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RadiantOrchard
{
    public enum EmptyIslandPhase
    {
        Pad = 0,
        Landmarks = 1,
        PlantWishTree = 2,
        WellWish = 3,
        FirstVisitor = 4,
        FirstPlant = 5,
        CoreLoop = 6
    }

    /// <summary>
    /// Strict one-by-one onboarding. Never starts the next step until the current
    /// gate is done. Only one phase UI lives at a time.
    /// </summary>
    public class EmptyIslandPhaseRunner : MonoBehaviour
    {
        public const string PrefsKey = "RO_EmptyIslandPhase_v2";

        EmptyIslandPhase phase;
        bool enterBusy;
        bool waitingForGate; // true until player finishes current step action
        EmptyIslandPhase? queued;
        Action pendingAfterToast;

        // Landmarks-only Continue CTA
        GameObject landmarksContinueBtn;

        public EmptyIslandPhase Current => phase;
        public static bool BlocksWorldInput { get; private set; }
        public static EmptyIslandPhaseRunner Instance { get; private set; }

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<EmptyIslandPhaseRunner>() != null) return;
            new GameObject("EmptyIslandPhaseRunner").AddComponent<EmptyIslandPhaseRunner>();
        }

        void Awake()
        {
            Instance = this;
            EnsureEventSystem();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            BlocksWorldInput = false;
        }

        void Start()
        {
#if UNITY_EDITOR
            EmptyIslandProgress.ClearAll();
            phase = EmptyIslandPhase.Landmarks;
#else
            phase = ResolveStartPhase();
#endif
            SavePhase();
            Debug.Log("[EmptyIslandPhase] START → " + phase + " (sequential)");
            StartCoroutine(EnterPhaseRoutine(phase));
        }

        void OnEnable()
        {
            FirstWishTreePlant.OnCompleted += OnWishTreePlanted;
            WellWishController.OnWishCompleted += OnWishCompleted;
            FirstVisitorArrival.OnArrived += OnVisitorArrived;
            FirstPlantTutorial.OnCompleted += OnPlantCompleted;
        }

        void OnDisable()
        {
            FirstWishTreePlant.OnCompleted -= OnWishTreePlanted;
            WellWishController.OnWishCompleted -= OnWishCompleted;
            FirstVisitorArrival.OnArrived -= OnVisitorArrived;
            FirstPlantTutorial.OnCompleted -= OnPlantCompleted;
            BlocksWorldInput = false;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F6) && phase < EmptyIslandPhase.CoreLoop && !enterBusy)
            {
                ForceSkipCurrentGate();
            }
#endif
        }

        EmptyIslandPhase ResolveStartPhase()
        {
            if (EmptyIslandProgress.PlantDone) return EmptyIslandPhase.CoreLoop;
            if (EmptyIslandProgress.WishDone) return EmptyIslandPhase.FirstVisitor;
            if (FirstWishTreePlant.IsPlanted) return EmptyIslandPhase.WellWish;
            return EmptyIslandPhase.Landmarks;
        }

        void SavePhase()
        {
            PlayerPrefs.SetInt(PrefsKey, (int)phase);
            PlayerPrefs.Save();
        }

        IEnumerator EnterPhaseRoutine(EmptyIslandPhase p)
        {
            while (enterBusy) yield return null;
            enterBusy = true;
            waitingForGate = true;
            phase = p;
            SavePhase();
            Debug.Log("[EmptyIslandPhase] ▶ STEP " + ((int)p) + " " + p);

            // Kill every other step's UI first — only THIS step may show
            CleanupAllStepUIs(except: p);

            try
            {
                // 1) Intro toast — must tap Next (no auto-skip spam)
                bool dismissed = false;
                ShowPhaseToast(p, () => dismissed = true);
                while (!dismissed) yield return null;

                // 2) Enable ONLY this step's systems
                ActivatePhaseSystems(p);
                ApplyCoachAndCamera(p);
                ShowLandmarksContinueIfNeeded(p);

                // 3) WAIT here until gate completes (Notify* clears waitingForGate via AdvanceTo)
                // For Landmarks, gate = Continue button
                while (waitingForGate && phase == p)
                    yield return null;
            }
            finally
            {
                enterBusy = false;
                BlocksWorldInput = false;
                HideToast();
                HideLandmarksContinue();
            }

            // Advance was requested while we waited
            if (queued.HasValue)
            {
                var next = queued.Value;
                queued = null;
                if ((int)next > (int)phase)
                    yield return EnterPhaseRoutine(next);
            }
        }

        void CleanupAllStepUIs(EmptyIslandPhase except)
        {
            if (except != EmptyIslandPhase.PlantWishTree)
            {
                var plant = FindFirstObjectByType<FirstWishTreePlant>();
                if (plant != null) Destroy(plant.gameObject);
                var spot = GameObject.Find(FirstWishTreePlant.SpotName);
                if (spot != null) Destroy(spot);
            }

            if (except != EmptyIslandPhase.WellWish)
            {
                var wish = FindFirstObjectByType<WellWishController>();
                if (wish != null) Destroy(wish.gameObject);
            }

            // Visitor arrival object can stay during FirstPlant (visitor must exist)
            if (except != EmptyIslandPhase.FirstVisitor && except != EmptyIslandPhase.FirstPlant)
            {
                var arrival = FindFirstObjectByType<FirstVisitorArrival>();
                if (arrival != null && except < EmptyIslandPhase.FirstVisitor)
                    Destroy(arrival.gameObject);
            }

            if (except != EmptyIslandPhase.FirstPlant)
            {
                var tut = FindFirstObjectByType<FirstPlantTutorial>();
                if (tut != null) Destroy(tut.gameObject);
            }

            if (except != EmptyIslandPhase.CoreLoop)
            {
                var hud = FindFirstObjectByType<EmptyIslandHUD>();
                if (hud != null) Destroy(hud.gameObject);
                var guide = FindFirstObjectByType<EmptyIslandNextGuide>();
                if (guide != null) Destroy(guide.gameObject);
            }
        }

        void ActivatePhaseSystems(EmptyIslandPhase p)
        {
            EnsureEventSystem();

            switch (p)
            {
                case EmptyIslandPhase.Landmarks:
                    // look around only — Continue button unlocks next
                    waitingForGate = true;
                    break;

                case EmptyIslandPhase.PlantWishTree:
                    waitingForGate = true;
                    FirstWishTreePlant.Ensure();
                    break;

                case EmptyIslandPhase.WellWish:
                    waitingForGate = true;
                    FirstWishTreePlant.EnsureTreeVisible(); // keep green tree on screen
                    WellWishController.Ensure();
                    break;

                case EmptyIslandPhase.FirstVisitor:
                    waitingForGate = true;
                    FirstVisitorArrival.Ensure();
                    break;

                case EmptyIslandPhase.FirstPlant:
                    waitingForGate = true;
                    if (FindFirstObjectByType<GestureManager>() == null)
                        new GameObject("GestureManager").AddComponent<GestureManager>();
                    if (GameState.Instance == null)
                        new GameObject("GameState").AddComponent<GameState>();
                    // Visitor already arrived — don't re-spawn
                    FirstPlantTutorial.Ensure();
                    break;

                case EmptyIslandPhase.CoreLoop:
                    waitingForGate = false;
                    if (FindFirstObjectByType<GestureManager>() == null)
                        new GameObject("GestureManager").AddComponent<GestureManager>();
                    if (GameState.Instance == null)
                        new GameObject("GameState").AddComponent<GameState>();
                    EmptyIslandHUD.Ensure();
                    FindFirstObjectByType<EmptyIslandHUD>()?.UnlockFruit(FruitType.Strawberry);
                    EmptyIslandNextGuide.Ensure();
                    EmptyIslandMoreVisitors.Ensure();
                    MoodFruitCoach.Ensure();
                    EmptyIslandDecorationReward.Ensure();
                    EmptyIslandEditUI.Ensure();
                    EmptyIslandShopUI.Ensure();
                    FruitPlantLessonFlow.CleanupOrphanPlantSpots();
                    break;
            }
        }

        void ApplyCoachAndCamera(EmptyIslandPhase p)
        {
            EmptyIslandCoachBar.Ensure();
            switch (p)
            {
                case EmptyIslandPhase.Landmarks:
                    // Tip + button come from ShowLandmarksContinueIfNeeded (inside girl bubble)
                    break;
                case EmptyIslandPhase.PlantWishTree:
                    EmptyIslandCoachBar.SetTip("Wish Tree growing on your island…");
                    EmptyIslandCameraFocus.Focus(new Vector3(0f, 0.5f, 1.5f), 22f);
                    break;
                case EmptyIslandPhase.WellWish:
                    EmptyIslandCoachBar.SetTip("STEP 3 — Tap well → Drop Coin (no typing)");
                    EmptyIslandCameraFocus.FocusWell();
                    break;
                case EmptyIslandPhase.FirstVisitor:
                    EmptyIslandCoachBar.SetTip("STEP 4/5 — Watch your first visitor arrive");
                    break;
                case EmptyIslandPhase.FirstPlant:
                    EmptyIslandCoachBar.SetTip("STEP 5 — Frustrated → Strawberry → DOUBLE-TAP");
                    EmptyIslandCameraFocus.FocusVisitor();
                    // Lesson UI owns top; mood tip only if no plant tutorial yet
                    if (FindFirstObjectByType<FirstPlantTutorial>() == null)
                        MoodFruitCoach.SuggestForMood("Frustrated", FruitType.Strawberry);
                    break;
                case EmptyIslandPhase.CoreLoop:
                    // NextGuide owns tips until dismissed — don't stack coach here
                    if (PlayerPrefs.GetInt(EmptyIslandNextGuide.PrefsKey, 0) == 1)
                        EmptyIslandCoachBar.SetTip("New visitors bring new moods — match the fruit!");
                    else
                        EmptyIslandCoachBar.Hide();
                    EmptyIslandCameraFocus.FocusVisitor();
                    break;
            }
        }

        void ShowLandmarksContinueIfNeeded(EmptyIslandPhase p)
        {
            HideLandmarksContinue();
            if (p != EmptyIslandPhase.Landmarks) return;

            // Inside girl bubble only — no separate bottom bar
            landmarksContinueBtn = null;
            EmptyIslandCoachBar.SetTip(
                "Drag to look, pinch to zoom.\nWhen ready — plant your Wish Tree!",
                true,
                () =>
                {
                    if (phase != EmptyIslandPhase.Landmarks) return;
                    AdvanceTo(EmptyIslandPhase.PlantWishTree);
                },
                "Plant Wish Tree");
        }

        void HideLandmarksContinue()
        {
            // Legacy external button cleanup (older sessions)
            if (landmarksContinueBtn != null)
            {
                var canvas = landmarksContinueBtn.GetComponentInParent<Canvas>();
                if (canvas != null) Destroy(canvas.gameObject);
                else Destroy(landmarksContinueBtn);
                landmarksContinueBtn = null;
            }
            var leftover = GameObject.Find("LandmarksContinueCanvas");
            if (leftover != null) Destroy(leftover);
        }

        public void AdvanceTo(EmptyIslandPhase next)
        {
            if ((int)next <= (int)phase)
            {
                Debug.Log("[EmptyIslandPhase] ignore advance to " + next);
                return;
            }

            // Only allow +1 step (strict sequence — no skipping ahead)
            if ((int)next > (int)phase + 1)
            {
                Debug.Log("[EmptyIslandPhase] blocked skip " + phase + " → " + next + " (must go one by one)");
                next = phase + 1;
            }

            Debug.Log("[EmptyIslandPhase] gate done → " + next);
            waitingForGate = false;

            if (enterBusy)
            {
                queued = next;
                return;
            }

            queued = null;
            StartCoroutine(EnterPhaseRoutine(next));
        }

        public void NotifyWishTreePlanted()
        {
            if (phase != EmptyIslandPhase.PlantWishTree) return;
            AdvanceTo(EmptyIslandPhase.WellWish);
        }

        public void NotifyWishDone()
        {
            if (phase != EmptyIslandPhase.WellWish) return;
            AdvanceTo(EmptyIslandPhase.FirstVisitor);
        }

        public void NotifyVisitorArrived()
        {
            if (phase != EmptyIslandPhase.FirstVisitor) return;
            AdvanceTo(EmptyIslandPhase.FirstPlant);
        }

        public void NotifyPlantDone()
        {
            if (phase != EmptyIslandPhase.FirstPlant) return;
            AdvanceTo(EmptyIslandPhase.CoreLoop);
        }

        void OnWishTreePlanted() => NotifyWishTreePlanted();
        void OnWishCompleted(string _) => NotifyWishDone();
        void OnVisitorArrived() => NotifyVisitorArrived();
        void OnPlantCompleted() => NotifyPlantDone();

        void ForceSkipCurrentGate()
        {
            switch (phase)
            {
                case EmptyIslandPhase.Landmarks:
                    AdvanceTo(EmptyIslandPhase.PlantWishTree); break;
                case EmptyIslandPhase.PlantWishTree:
                    FirstWishTreePlant.SeedForDebug();
                    NotifyWishTreePlanted(); break;
                case EmptyIslandPhase.WellWish:
                    EmptyIslandProgress.SeedWishForDebug();
                    NotifyWishDone(); break;
                case EmptyIslandPhase.FirstVisitor:
                    NotifyVisitorArrived(); break;
                case EmptyIslandPhase.FirstPlant:
                    EmptyIslandProgress.SeedPlantForDebug();
                    NotifyPlantDone(); break;
            }
        }

        void ShowPhaseToast(EmptyIslandPhase p, Action onDismiss)
        {
            GetToastCopy(p, out string title, out string body, out string ok);
            if (string.IsNullOrEmpty(title))
            {
                onDismiss?.Invoke();
                return;
            }

            // Compact Girl3 bubble — NO full-screen dim/panel (island stays visible)
            BlocksWorldInput = true;
            pendingAfterToast = onDismiss;
            string tip = title + "\n" + body;
            EmptyIslandCoachBar.SetTip(tip, true, () =>
            {
                BlocksWorldInput = false;
                var cb = pendingAfterToast;
                pendingAfterToast = null;
                cb?.Invoke();
            }, ok);
        }

        void HideToast()
        {
            BlocksWorldInput = false;
        }

        static void GetToastCopy(EmptyIslandPhase p, out string title, out string body, out string ok)
        {
            ok = "Next";
            switch (p)
            {
                case EmptyIslandPhase.Landmarks:
                    title = "Step 1 — Your Empty Island";
                    body = "Look around first.\nDrag to pan, pinch to zoom.\nThen tap Plant Wish Tree.";
                    ok = "Got it";
                    break;
                case EmptyIslandPhase.PlantWishTree:
                    title = "Step 2 — Plant Wish Tree";
                    body = "Your Wish Tree will grow bright green.\nWatch it appear on the island.";
                    ok = "Grow it";
                    break;
                case EmptyIslandPhase.WellWish:
                    title = "Step 3 — Make a Wish";
                    body = "Tap the Stone Well (or Make a Wish).\nThen Drop Coin — no typing.";
                    ok = "Got it";
                    break;
                case EmptyIslandPhase.FirstVisitor:
                    title = "Step 4 — First Visitor";
                    body = "Watch their mood bubble.\nWe'll suggest fruit + gesture.";
                    ok = "Watch";
                    break;
                case EmptyIslandPhase.FirstPlant:
                    title = "Step 5 — Heal with fruit";
                    body = "Frustrated → Strawberry → DOUBLE-TAP.\nFollow the on-screen tip.";
                    ok = "Let's plant";
                    break;
                case EmptyIslandPhase.CoreLoop:
                    title = "Orchard ready!";
                    body = "You learned the loop.\nNew moods need different fruits.";
                    ok = "Show fruit guide";
                    break;
                default:
                    title = null;
                    body = null;
                    break;
            }
        }

        static void EnsureEventSystem()
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es != null)
            {
                if (es.GetComponent<StandaloneInputModule>() == null)
                    es.gameObject.AddComponent<StandaloneInputModule>();
                return;
            }
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}

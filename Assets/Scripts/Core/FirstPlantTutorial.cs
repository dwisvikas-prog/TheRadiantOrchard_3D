using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // PRD Phase 2 Step 4 — Guided first plant:
    // Guide → plant spot → Double-Tap harvest → tonic → heal FirstVisitor.
    public class FirstPlantTutorial : MonoBehaviour
    {
        public const string PrefsKey = "RO_FirstPlantDone_v1";

        Canvas canvas;
        GameObject panel;
        Text titleText;
        Text bodyText;
        Text okLabel;
        Button nextBtn;
        int step;
        bool waitingHarvest;
        bool waitingHeal;
        FruitHarvester fruit;
        StickmanController visitor;
        Transform plantSpot;
        bool started;

        static readonly (string title, string body, string ok)[] Steps =
        {
            ("Visitor is Frustrated!",
             FruitLessonBook.Get(FruitType.Strawberry).FullMeaningCard() +
             "\n\nFirst plant a real STRAWBERRY BUSH.",
             "Find soil"),
            ("Plant strawberry bush",
             "TAP the GLOWING SOIL only.\n✓ Green ring = yes\n✗ Not empty ground / sky\n\nYou plant it — no auto plant.",
             null),
            ("Harvest — Double-tap!",
             "GREEN circle = ON the strawberry\nRED bar = do NOT tap ground\n\nDOUBLE-TAP the fruit yourself.",
             null),
            ("Tonic flying…",
             "Watch it heal them — Frustrated → Love / Heart.",
             null),
            ("They feel better!",
             "Each mood needs a different fruit + gesture.\nYou'll learn all 9 on this first level.",
             "Show all 9 fruits")
        };

        public static event System.Action OnCompleted;

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (PlayerPrefs.GetInt(PrefsKey, 0) == 1)
            {
                EmptyIslandPhaseRunner.Instance?.NotifyPlantDone();
                return;
            }
            if (FindFirstObjectByType<FirstPlantTutorial>() != null) return;
            var go = new GameObject("FirstPlantTutorial");
            go.AddComponent<FirstPlantTutorial>();
        }

        void Start()
        {
            if (started) return;
            if (PlayerPrefs.GetInt(PrefsKey, 0) == 1)
            {
                if (EmptyIslandPhaseRunner.Instance != null)
                    EmptyIslandPhaseRunner.Instance.NotifyPlantDone();
                else
                    OnCompleted?.Invoke();
                Destroy(gameObject);
                return;
            }
            started = true;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            ActiveFruitTracker.Set(FruitType.Strawberry);
            EnsureCoreSystems();

            // Wait until FirstVisitor finished walk (controller enabled)
            float timeout = 45f;
            while (timeout > 0f)
            {
                visitor = FindVisitor();
                if (visitor != null && visitor.enabled) break;
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (visitor == null || !visitor.enabled)
            {
                Debug.LogWarning("[FirstPlant] No ready FirstVisitor — retrying arrival.");
                FirstVisitorArrival.Ensure();
                timeout = 45f;
                while (timeout > 0f)
                {
                    visitor = FindVisitor();
                    if (visitor != null && visitor.enabled) break;
                    timeout -= Time.deltaTime;
                    yield return null;
                }
            }
            if (visitor == null || !visitor.enabled)
            {
                Debug.LogError("[FirstPlant] No FirstVisitor after retry — advance anyway.");
                // Never softlock the phase runner
                if (EmptyIslandPhaseRunner.Instance != null)
                    EmptyIslandPhaseRunner.Instance.NotifyPlantDone();
                else
                    OnCompleted?.Invoke();
                Destroy(gameObject);
                yield break;
            }

            BuildUI();
            step = 0;
            ShowStep();

            while (step < Steps.Length)
                yield return null;
        }

        StickmanController FindVisitor()
        {
            var go = GameObject.Find(FirstVisitorArrival.VisitorName);
            if (go != null) return go.GetComponent<StickmanController>();
            return FindFirstObjectByType<StickmanController>();
        }

        void EnsureCoreSystems()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            if (FindFirstObjectByType<GestureManager>() == null)
                new GameObject("GestureManager").AddComponent<GestureManager>();

            if (GameState.Instance == null)
                new GameObject("GameState").AddComponent<GameState>();
        }

        void BuildUI()
        {
            // No second panel — Girl alone guides First Plant
            canvas = null;
            panel = null;
            titleText = null;
            bodyText = null;
            nextBtn = null;
            okLabel = null;
        }

        void ShowStep()
        {
            if (step < 0 || step >= Steps.Length) return;
            EmptyIslandGuideStack.Push(EmptyIslandGuideStack.Layer.Lesson);
            MoodFruitCoach.HideAll();

            waitingHarvest = false;
            waitingHeal = false;

            switch (step)
            {
                case 0:
                    EmptyIslandCoachBar.SetTip(
                        "Frustrated → Strawberry\nDOUBLE-TAP on fruit\n✓ fruit  ✗ ground",
                        true, () => Advance(), "Let's plant");
                    EmptyIslandCameraFocus.FocusVisitor();
                    break;
                case 1:
                    EmptyIslandCoachBar.SetTip("Tap glowing soil — plant strawberry");
                    SpawnPlantSpot();
                    EmptyIslandCameraFocus.FocusPlantSpot();
                    break;
                case 2:
                    waitingHarvest = true;
                    FruitHarvester.OnAnyHarvested += OnHarvested;
                    if (fruit == null) GrowTreeAndFruit();
                    fruit?.BindGestures();
                    if (fruit != null)
                        GestureWhereGuide.Show(fruit.transform, FruitLessonBook.Get(FruitType.Strawberry));
                    StartCoroutine(HarvestTimeoutFallback());
                    break;
                case 3:
                    EmptyIslandCoachBar.SetTip("Tonic flying — watch them heal");
                    waitingHeal = true;
                    if (GameState.Instance != null)
                        GameState.Instance.StickmanHealed += OnHealed;
                    EmptyIslandCameraFocus.FocusVisitor();
                    StartCoroutine(HealTimeoutFallback());
                    break;
                case 4:
                    EmptyIslandCoachBar.SetTip(
                        "Nice! Vibrancy up top.\nNew visitors = new fruits — one at a time",
                        true, () => Finish(), "OK");
                    EmptyIslandCameraFocus.FocusVisitor();
                    break;
            }
        }

        void OnNext()
        {
            // Steps driven by girl OK / plant / harvest
            if (step == 1 || step == 2) return;
            if (step == Steps.Length - 1)
            {
                Finish();
                return;
            }
            Advance();
        }

        void Advance()
        {
            Unhook();
            step++;
            if (step >= Steps.Length)
            {
                Finish();
                return;
            }
            ShowStep();
        }

        void Unhook()
        {
            FruitHarvester.OnAnyHarvested -= OnHarvested;
            if (GameState.Instance != null)
                GameState.Instance.StickmanHealed -= OnHealed;
        }

        void OnHarvested()
        {
            if (!waitingHarvest) return;
            waitingHarvest = false;
            GestureWhereGuide.HideAll();
            Advance();
        }

        void OnHealed()
        {
            if (!waitingHeal) return;
            waitingHeal = false;
            Advance();
        }

        IEnumerator HarvestTimeoutFallback()
        {
            yield return new WaitForSeconds(90f);
            if (!waitingHarvest) yield break;
            waitingHarvest = false;
            GestureWhereGuide.HideAll();
            EmptyIslandCoachBar.SetTip("Gesture timed out — continuing");
            Advance();
        }

        IEnumerator HealTimeoutFallback()
        {
            yield return new WaitForSeconds(6f);
            if (!waitingHeal) yield break;
            waitingHeal = false;
            EmptyIslandCoachBar.SetTip("They feel better — keep going!");
            Advance();
        }

        void SpawnPlantSpot()
        {
            if (plantSpot != null) return;

            var tracker = OrchardTreeTracker.Ensure();
            Vector3 pos = EditModeManager.SnapToCoCTile(tracker.PickRandomSpot());

            var spot = new GameObject("FirstPlantSpot");
            spot.transform.SetParent(tracker.transform, true);
            plantSpot = spot.transform;
            plantSpot.position = pos;

            // Glowing soil patch (tap target)
            var soil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            soil.name = "Soil";
            soil.transform.SetParent(plantSpot, false);
            soil.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            soil.transform.localScale = new Vector3(2.4f, 0.06f, 2.4f);
            Object.Destroy(soil.GetComponent<Collider>());
            var soilMr = soil.GetComponent<MeshRenderer>();
            var soilMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var soilC = new Color(0.42f, 0.28f, 0.14f);
            soilMat.color = soilC;
            if (soilMat.HasProperty("_BaseColor")) soilMat.SetColor("_BaseColor", soilC);
            soilMat.EnableKeyword("_EMISSION");
            if (soilMat.HasProperty("_EmissionColor"))
                soilMat.SetColor("_EmissionColor", new Color(0.35f, 0.85f, 0.25f) * 0.6f);
            soilMr.sharedMaterial = soilMat;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "GlowRing";
            ring.transform.SetParent(plantSpot, false);
            ring.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            ring.transform.localScale = new Vector3(2.7f, 0.02f, 2.7f);
            Object.Destroy(ring.GetComponent<Collider>());
            var ringMr = ring.GetComponent<MeshRenderer>();
            var ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var ringC = new Color(0.45f, 1f, 0.35f, 0.7f);
            ringMat.color = ringC;
            if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", ringC);
            ringMr.sharedMaterial = ringMat;

            var label = new GameObject("Hint");
            label.transform.SetParent(plantSpot, false);
            label.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var tm = label.AddComponent<TextMesh>();
            tm.text = "TAP TO PLANT";
            tm.fontSize = 48;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            // Click collider
            var hit = spot.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, 0.5f, 0f);
            hit.size = new Vector3(2.8f, 1.2f, 2.8f);
            spot.AddComponent<FirstPlantSpotClick>().Init(this);
            IslandTapTrack.ShowTapHere(plantSpot, "TAP SOIL");
        }

        public void OnPlantSpotTapped()
        {
            if (step != 1) return;
            GrowTreeAndFruit();
            Advance();
        }

        void GrowTreeAndFruit()
        {
            if (plantSpot == null) return;

            var hint = plantSpot.Find("Hint");
            if (hint != null) hint.gameObject.SetActive(false);
            var ring = plantSpot.Find("GlowRing");
            if (ring != null) ring.gameObject.SetActive(false);

            var lesson = FruitLessonBook.Get(FruitType.Strawberry);
            Transform treeVis = plantSpot.Find("FruitTree_Strawberry");
            if (treeVis == null)
            {
                // Real strawberry bush from available Resources (Bush)
                treeVis = FruitTreeCatalog.SpawnTree(FruitType.Strawberry, plantSpot, plantSpot.position).transform;
            }
            OrchardTreeTracker.Ensure().Register(FruitType.Strawberry, treeVis, plantSpot.position);

            // Strip soil hole / glow so re-click isn't a fake plant target
            var hint2 = plantSpot.Find("Hint");
            if (hint2 != null) Object.Destroy(hint2.gameObject);
            var ring2 = plantSpot.Find("GlowRing");
            if (ring2 != null) Object.Destroy(ring2.gameObject);
            var soil = plantSpot.Find("Soil");
            if (soil != null) Object.Destroy(soil.gameObject);
            var click = plantSpot.GetComponent<FirstPlantSpotClick>();
            if (click != null) Object.Destroy(click);
            var col = plantSpot.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            if (treeVis != null && treeVis.IsChildOf(plantSpot))
            {
                var tracker = OrchardTreeTracker.Ensure();
                treeVis.SetParent(tracker.transform, true);
            }
            IslandTapTrack.Hide();

            var fruitData = FruitTreeCatalog.MakeFruitData(lesson);
            if (visitor == null) return;

            Vector3 fruitPos = plantSpot.position + new Vector3(0.5f, 1.0f, 0.25f);
            fruit = FruitHarvester.GetPooled(fruitData, fruitPos, 2.4f);
            fruit.Initialize(fruitData, visitor);
            fruit.BindGestures();

            Debug.Log("[FirstPlant] Strawberry bush planted + fruit ready (Double-Tap).");
        }

        void Finish()
        {
            Unhook();
            GestureWhereGuide.HideAll();
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Lesson);
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Gesture);
            PlayerPrefs.SetInt(PrefsKey, 1);
            PlayerPrefs.Save();
            FruitTeachProgress.MarkTaught(FruitType.Strawberry);
            ActiveFruitTracker.Clear();
            TreeNameBoard.RefreshAllTracked();
            FindFirstObjectByType<EmptyIslandHUD>()?.UnlockFruit(FruitType.Strawberry);
            EmptyIslandCoachBar.SetNewEvent("Strawberry learned!");
            EmptyIslandCoachBar.GoIdleQuiet();
            if (EmptyIslandPhaseRunner.Instance != null)
                EmptyIslandPhaseRunner.Instance.NotifyPlantDone();
            else
                OnCompleted?.Invoke();
            if (canvas != null) Destroy(canvas.gameObject);
            Destroy(gameObject);
            Debug.Log("[FirstPlant] Step 4 complete — first heal done.");
        }

        void OnDestroy() => Unhook();

        void LateUpdate()
        {
            // Billboard plant hint
            if (plantSpot == null) return;
            var hint = plantSpot.Find("Hint");
            var cam = Camera.main;
            if (hint != null && cam != null)
                hint.rotation = Quaternion.LookRotation(hint.position - cam.transform.position);
        }

    }

    // Plant soil tap — screen-space + raycast (fixes IsChildOf self-hit bug)
    public class FirstPlantSpotClick : MonoBehaviour
    {
        FirstPlantTutorial owner;
        Camera cam;
        const float ScreenRadiusPx = 120f;

        public void Init(FirstPlantTutorial tutorial) => owner = tutorial;

        void Update()
        {
            if (owner == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            bool tap = false;
            Vector2 screen = Vector2.zero;

            if (Input.GetMouseButtonDown(0))
            {
                tap = true;
                screen = Input.mousePosition;
            }
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    tap = true;
                    screen = t.position;
                }
            }
            if (!tap) return;
            if (EmptyIslandPhaseRunner.BlocksWorldInput) return;

            // Don't block on full UI — only skip if clearly on the Next button area
            // (bottom-right). World plant taps near center must work.

            // 1) Generous screen-space near plant spot
            Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.3f);
            if (sp.z > 0f && Vector2.Distance(new Vector2(sp.x, sp.y), screen) <= 160f)
            {
                owner.OnPlantSpotTapped();
                return;
            }

            // 2) RaycastAll — ground often blocks single Raycast
            var ray = cam.ScreenPointToRay(screen);
            var hits = Physics.RaycastAll(ray, 250f);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i].transform;
                if (h == transform || h.IsChildOf(transform))
                {
                    owner.OnPlantSpotTapped();
                    return;
                }
            }
        }
    }
}

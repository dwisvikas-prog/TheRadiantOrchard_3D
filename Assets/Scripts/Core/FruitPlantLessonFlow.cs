using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// Same plant-tree lesson for EVERY new fruit.
    /// User does each step — no "Do it for me".
    /// Real available CoC trees, random spots, tracked on island.
    /// </summary>
    public class FruitPlantLessonFlow : MonoBehaviour
    {
        FruitLessonBook.Lesson lesson;
        StickmanController visitor;
        Transform plantSpot;
        Transform treeRoot;
        FruitHarvester fruit;
        Canvas canvas;
        RectTransform panelRt;
        Text titleText;
        Text bodyText;
        Button nextBtn;
        Text okLabel;
        int step; // 0 meaning, 1 plant soil, 2 harvest, 3 done
        bool busy;
        bool reuseExisting;
        OrchardTreeTracker tracker;
        Coroutine harvestTimeoutCo;
        bool torndown;

        public static bool IsBusy =>
            FindFirstObjectByType<FruitPlantLessonFlow>() != null ||
            QuietFruitAssist.IsActive;

        /// <summary>
        /// A visitor's patience ran out before they were healed — if this flow
        /// belongs to them, tear it down so IsBusy clears. Without this the
        /// next visitor (and Shop/Edit, which also gate on IsBusy) would be
        /// stuck forever waiting on a lesson that can never finish.
        /// </summary>
        public static void CancelFor(StickmanController v)
        {
            if (v == null) return;
            var flow = FindFirstObjectByType<FruitPlantLessonFlow>();
            if (flow != null && flow.visitor == v)
                flow.Teardown();
            QuietFruitAssist.CancelFor(v);
        }

        /// <summary>
        /// Idempotent cleanup shared by CancelFor() and the harvest-wait
        /// timeout fallback. Safe to call more than once — guarded by
        /// `torndown` so a second call (e.g. patience GiveUp racing the
        /// timeout fallback) never double-despawns the fruit or double-fires
        /// any event. Does NOT mark the fruit taught, heal, or award
        /// Vibrancy — this is a recovery path, not a completion path.
        /// </summary>
        void Teardown()
        {
            if (torndown) return;
            torndown = true;
            if (harvestTimeoutCo != null) StopCoroutine(harvestTimeoutCo);
            harvestTimeoutCo = null;
            FruitHarvester.OnAnyHarvested -= OnHarvested;
            GestureWhereGuide.HideAll();
            ActiveFruitTracker.Clear();
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Lesson);
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Gesture);
            if (canvas != null) Destroy(canvas.gameObject);
            // Its target visitor is gone — IsHarvestable checks
            // targetStickman.CanReceiveTonic, which is now permanently
            // false, so this fruit would otherwise sit on the tree dead:
            // visible, but every future tap on it silently does nothing.
            if (fruit != null) fruit.Despawn();
            Destroy(gameObject);
        }

        public static void MinimizePanel()
        {
            var flow = FindFirstObjectByType<FruitPlantLessonFlow>();
            if (flow == null || flow.panelRt == null) return;
            OrchardUITheme.Place(flow.panelRt, 0.08f, 0.91f, 0.92f, 0.98f);
            if (flow.bodyText != null) flow.bodyText.gameObject.SetActive(false);
            if (flow.nextBtn != null) flow.nextBtn.gameObject.SetActive(false);
        }

        public static void StartFor(FruitLessonBook.Lesson lesson, StickmanController visitor)
        {
            if (!Application.isPlaying) return;
            if (IsBusy) return;

            // Already taught this fruit + tree exists → quiet mode (top alert + info)
            bool taught = FruitTeachProgress.IsTaught(lesson.fruit);
            bool hasTree = OrchardTreeTracker.Ensure().Has(lesson.fruit);
            if (taught && hasTree)
            {
                QuietFruitAssist.StartFor(lesson, visitor);
                return;
            }

            var go = new GameObject("FruitPlantLesson_" + lesson.fruitName);
            var flow = go.AddComponent<FruitPlantLessonFlow>();
            flow.Begin(lesson, visitor);
        }

        void Begin(FruitLessonBook.Lesson L, StickmanController v)
        {
            lesson = L;
            visitor = v;
            ActiveFruitTracker.Set(L.fruit);
            tracker = OrchardTreeTracker.Ensure();
            CleanupOrphanPlantSpots();
            reuseExisting = tracker.Has(L.fruit);
            EnsureEventSystem();
            if (FindFirstObjectByType<GestureManager>() == null)
                new GameObject("GestureManager").AddComponent<GestureManager>();
            EmptyIslandGuideStack.Push(EmptyIslandGuideStack.Layer.Lesson);
            BuildUI();
            ShowStep(0);
        }

        void BuildUI()
        {
            // No cream panel — Girl CoachBar is the only guide UI
            canvas = null;
            panelRt = null;
            titleText = null;
            bodyText = null;
            nextBtn = null;
            okLabel = null;
        }

        void ShowStep(int s)
        {
            step = s;
            var g = FruitLessonBook.GestureShort(lesson.gesture).ToUpperInvariant();
            var pick = FruitTreeCatalog.Get(lesson.fruit);

            if (s == 0)
            {
                string tip = lesson.mood + " → " + lesson.fruitName + "\n" +
                             lesson.virtue + " / " + lesson.heals + "\n" +
                             g + " on fruit  ✓  ground ✗";
                EmptyIslandCoachBar.SetTip(tip, true, () =>
                {
                    if (reuseExisting)
                    {
                        AttachToExistingTree();
                        ShowStep(2);
                    }
                    else ShowStep(1);
                }, reuseExisting ? "Show tree" : "Find soil");
            }
            else if (s == 1)
            {
                MoodFruitCoach.HideAll();
                SpawnPlantSpot();
                EmptyIslandCameraFocus.FocusSmooth(plantSpot.position + Vector3.up * 0.6f, 13f, 1.5f);
                EmptyIslandCoachBar.SetTip(
                    "Tap glowing soil — plant " + lesson.fruitName + "\n✓ green ring  ✗ empty ground");
            }
            else if (s == 2)
            {
                EnsureFruitReady();
                if (fruit != null)
                    GestureWhereGuide.Show(fruit.transform, lesson);
                // Conservative fallback (matches FirstPlantTutorial's 90s
                // harvest-wait convention): if the harvest gesture/event never
                // arrives — visitor patience aside — don't leave the player
                // stuck here forever waiting on a fruit that can't be reached.
                if (harvestTimeoutCo != null) StopCoroutine(harvestTimeoutCo);
                harvestTimeoutCo = StartCoroutine(HarvestTimeoutFallback());
            }
            else
            {
                Finish();
            }
        }

        void OnNext()
        {
            // Legacy — meaning step now uses CoachBar OK
            if (busy) return;
            if (step == 0)
            {
                if (reuseExisting)
                {
                    AttachToExistingTree();
                    ShowStep(2);
                }
                else ShowStep(1);
            }
        }

        void SpawnPlantSpot()
        {
            if (plantSpot != null) return;
            Vector3 pos = tracker.PickRandomSpot();
            pos = EditModeManager.SnapToCoCTile(pos);

            var spot = new GameObject("FruitPlantSpot_" + lesson.fruitName);
            // Keep spots under tracker so trees persist after lesson UI closes
            spot.transform.SetParent(tracker.transform, true);
            plantSpot = spot.transform;
            plantSpot.position = pos;

            MakeDisc(plantSpot, "Soil", new Vector3(0f, 0.05f, 0f), new Vector3(2.8f, 0.08f, 2.8f),
                new Color(0.42f, 0.28f, 0.14f), true, keepCollider: true);
            MakeDisc(plantSpot, "GlowRing", new Vector3(0f, 0.12f, 0f), new Vector3(3.1f, 0.02f, 3.1f),
                new Color(0.4f, 1f, 0.35f), true, keepCollider: false);

            var label = new GameObject("Hint");
            label.transform.SetParent(plantSpot, false);
            label.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            var tm = label.AddComponent<TextMesh>();
            tm.text = "TAP SOIL\nplant " + lesson.fruitName;
            tm.fontSize = 42;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            label.AddComponent<TreeNameBillboard>();

            var hit = spot.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, 0.6f, 0f);
            hit.size = new Vector3(3.4f, 1.4f, 3.4f);
            spot.AddComponent<FruitPlantSpotClick>().Init(this);

            IslandTapTrack.ShowTapHere(plantSpot, "TAP SOIL\n" + lesson.fruitName);
        }

        void AttachToExistingTree()
        {
            var planted = tracker.Find(lesson.fruit);
            if (planted == null || planted.root == null)
            {
                reuseExisting = false;
                ShowStep(1);
                return;
            }
            treeRoot = OrchardTreeTracker.ResolveTreeVisual(planted.root) ?? planted.root;
            plantSpot = treeRoot;
            // If visual missing, force a fresh plant
            if (OrchardTreeTracker.ResolveTreeVisual(treeRoot) == null &&
                !treeRoot.name.StartsWith("FruitTree_"))
            {
                reuseExisting = false;
                treeRoot = null;
                plantSpot = null;
                ShowStep(1);
                return;
            }
            EmptyIslandCameraFocus.Focus(planted.position + Vector3.up * 1f, 13f);
        }

        public void OnPlantSpotTapped()
        {
            if (step != 1 || busy) return;
            GrowTreeAndFruit();
            ShowStep(2);
        }

        void EnsureFruitReady()
        {
            if (fruit != null) return;
            if (treeRoot == null && plantSpot != null)
                GrowTreeAndFruit();
            else if (treeRoot != null)
            {
                // Ensure visual tree exists before fruit
                if (OrchardTreeTracker.ResolveTreeVisual(treeRoot) == null &&
                    !treeRoot.name.StartsWith("FruitTree_"))
                    GrowTreeAndFruit();
                else
                    SpawnFruitOnTree();
            }
        }

        void GrowTreeAndFruit()
        {
            if (plantSpot == null) SpawnPlantSpot();
            if (plantSpot == null) return;

            // Always ensure a real FruitTree_ visual exists for this fruit
            var existingVisual = OrchardTreeTracker.ResolveTreeVisual(plantSpot);
            if (treeRoot == null || OrchardTreeTracker.ResolveTreeVisual(treeRoot) == null)
            {
                if (existingVisual != null)
                    treeRoot = existingVisual;
                else
                {
                    // Parent tree under tracker (not lesson GO) so it never gets destroyed
                    var host = plantSpot;
                    treeRoot = FruitTreeCatalog.SpawnTree(lesson.fruit, host, host.position).transform;
                }
                tracker.Register(lesson.fruit, treeRoot, plantSpot.position);
            }

            // Remove soil hole / glow ring so re-clicking the tree does nothing fake
            ClearPlantMarkers(reparentTree: true);

            SpawnFruitOnTree();
        }

        void ClearPlantMarkers(bool reparentTree)
        {
            if (plantSpot == null) return;

            bool isSpot = plantSpot.name.StartsWith("FruitPlantSpot_");
            if (reparentTree && isSpot && treeRoot != null)
            {
                if (treeRoot.IsChildOf(plantSpot) || treeRoot == plantSpot)
                    treeRoot.SetParent(tracker != null ? tracker.transform : null, true);
            }

            // Hide / destroy soil disc + ring + hint (the "hole" players keep tapping)
            for (int i = plantSpot.childCount - 1; i >= 0; i--)
            {
                var c = plantSpot.GetChild(i);
                if (c == null) continue;
                if (c.name == "Soil" || c.name == "GlowRing" || c.name == "Hint")
                    Object.Destroy(c.gameObject);
            }

            var click = plantSpot.GetComponent<FruitPlantSpotClick>();
            if (click != null) Object.Destroy(click);
            var col = plantSpot.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            if (isSpot)
            {
                var spotGo = plantSpot.gameObject;
                plantSpot = treeRoot;
                Object.Destroy(spotGo);
            }
            else
            {
                var hint = plantSpot.Find("Hint");
                if (hint != null) hint.gameObject.SetActive(false);
                var ring = plantSpot.Find("GlowRing");
                if (ring != null) ring.gameObject.SetActive(false);
            }

            IslandTapTrack.Hide();
        }

        /// <summary>Old lessons left glowing soil holes under trees — wipe them.</summary>
        public static void CleanupOrphanPlantSpots()
        {
            var liveFlow = Object.FindFirstObjectByType<FruitPlantLessonFlow>();
            var tracker = OrchardTreeTracker.Ensure();

            // Spots parented under tracker
            if (tracker != null)
                PurgeSpotChildren(tracker.transform, liveFlow);

            // Orphan root spots
            var roots = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            for (int i = 0; i < roots.Length; i++)
            {
                var t = roots[i];
                if (t == null || t.parent != null) continue;
                if (!t.name.StartsWith("FruitPlantSpot_")) continue;
                if (liveFlow != null && IsOwnedByFlow(t, liveFlow)) continue;
                SalvageTreeAndDestroySpot(t, tracker);
            }
        }

        static bool IsOwnedByFlow(Transform spot, FruitPlantLessonFlow flow)
        {
            if (flow == null || spot == null) return false;
            // Live click with non-destroyed owner means active plant step
            var click = spot.GetComponent<FruitPlantSpotClick>();
            return click != null && click.HasLiveOwner;
        }

        static void PurgeSpotChildren(Transform parent, FruitPlantLessonFlow liveFlow)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null) continue;
                if (!child.name.StartsWith("FruitPlantSpot_")) continue;
                if (IsOwnedByFlow(child, liveFlow)) continue;
                SalvageTreeAndDestroySpot(child, parent.GetComponent<OrchardTreeTracker>());
            }
        }

        static void SalvageTreeAndDestroySpot(Transform spot, OrchardTreeTracker tracker)
        {
            if (spot == null) return;
            Transform parent = tracker != null ? tracker.transform : null;
            for (int c = spot.childCount - 1; c >= 0; c--)
            {
                var nested = spot.GetChild(c);
                if (nested == null) continue;
                if (nested.name.StartsWith("FruitTree_"))
                    nested.SetParent(parent, true);
            }
            Object.Destroy(spot.gameObject);
        }

        void SpawnFruitOnTree()
        {
            if (fruit != null) return;
            var host = OrchardTreeTracker.ResolveTreeVisual(treeRoot) ?? treeRoot ?? plantSpot;
            if (host == null) return;

            var fruitData = FruitTreeCatalog.MakeFruitData(lesson);
            Vector3 fruitPos = host.position + new Vector3(0.55f, 1.35f, 0.25f);
            if (lesson.fruit != FruitType.Strawberry)
                fruitPos = host.position + new Vector3(0.65f, 1.75f, 0.3f);

            // Bigger fruit so gesture target is obvious
            fruit = FruitHarvester.GetPooled(fruitData, fruitPos, 2.4f);
            if (visitor != null)
                fruit.Initialize(fruitData, visitor);
            else
                fruit.Initialize(fruitData, null);
            fruit.BindGestures();

            FruitHarvester.OnAnyHarvested -= OnHarvested;
            FruitHarvester.OnAnyHarvested += OnHarvested;

            FindFirstObjectByType<EmptyIslandHUD>()?.UnlockFruit(lesson.fruit);
            // Camera track happens in GestureWhereGuide (slow → fruit) — don't steal it here
        }

        IEnumerator HarvestTimeoutFallback()
        {
            yield return new WaitForSeconds(90f);
            if (step != 2 || torndown) yield break;
            Teardown();
        }

        void OnHarvested()
        {
            if (step != 2) return;
            if (harvestTimeoutCo != null) StopCoroutine(harvestTimeoutCo);
            harvestTimeoutCo = null;
            FruitHarvester.OnAnyHarvested -= OnHarvested;
            GestureWhereGuide.HideAll();
            IslandTapTrack.Hide();
            EmptyIslandCoachBar.SetTip(lesson.fruitName + " → " + lesson.virtue + " healing " + lesson.heals);
            StartCoroutine(FinishAfterHeal());
        }

        IEnumerator FinishAfterHeal()
        {
            busy = true;
            if (canvas != null) canvas.gameObject.SetActive(false);

            bool healed = false;
            System.Action onHeal = () => healed = true;
            if (GameState.Instance != null)
                GameState.Instance.StickmanHealed += onHeal;

            float t = 8f;
            while (t > 0f && !healed)
            {
                t -= Time.deltaTime;
                yield return null;
            }

            if (GameState.Instance != null)
                GameState.Instance.StickmanHealed -= onHeal;

            Finish();
        }

        void Finish()
        {
            FruitHarvester.OnAnyHarvested -= OnHarvested;
            GestureWhereGuide.HideAll();
            FruitTeachProgress.MarkTaught(lesson.fruit);
            ActiveFruitTracker.Clear();
            EmptyIslandLevelProgress.CheckLevel1Completion();
            TreeNameBoard.RefreshAllTracked();
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Lesson);
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Gesture);
            if (canvas != null) Destroy(canvas.gameObject);
            EmptyIslandCoachBar.SetNewEvent(lesson.fruitName + " learned!");
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            FruitHarvester.OnAnyHarvested -= OnHarvested;
        }

        static void MakeDisc(Transform parent, string name, Vector3 local, Vector3 scale, Color c, bool emit,
            bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            if (!keepCollider)
                Object.Destroy(go.GetComponent<Collider>());
            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (emit)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 0.7f);
            }
            mr.sharedMaterial = mat;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    public class FruitPlantSpotClick : MonoBehaviour
    {
        FruitPlantLessonFlow owner;
        const float ScreenRadiusPx = 160f;

        public bool HasLiveOwner => owner != null;

        public void Init(FruitPlantLessonFlow flow) => owner = flow;

        void Update()
        {
            if (owner == null)
            {
                StripDeadSpot();
                return;
            }
            if (EmptyIslandPhaseRunner.BlocksWorldInput) return;

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

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cam = Camera.main;
            if (cam == null) return;

            // 1) Generous screen-space tap near glowing soil (phone-friendly)
            Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.35f);
            if (sp.z > 0f && Vector2.Distance(new Vector2(sp.x, sp.y), screen) <= ScreenRadiusPx)
            {
                owner.OnPlantSpotTapped();
                return;
            }

            // 2) RaycastAll — ground often blocks single Raycast before soil collider
            var ray = cam.ScreenPointToRay(screen);
            var hits = Physics.RaycastAll(ray, 400f);
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

        void StripDeadSpot()
        {
            // Remove glowing hole only — keep any FruitTree_ child
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c == null) continue;
                if (c.name == "Soil" || c.name == "GlowRing" || c.name == "Hint")
                    Object.Destroy(c.gameObject);
            }
            var col = GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            bool hasTree = false;
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                if (c != null && c.name.StartsWith("FruitTree_"))
                {
                    hasTree = true;
                    var tracker = OrchardTreeTracker.Ensure();
                    c.SetParent(tracker.transform, true);
                }
            }

            if (name.StartsWith("FruitPlantSpot_"))
                Object.Destroy(gameObject);
            else
                Object.Destroy(this);
        }
    }
}

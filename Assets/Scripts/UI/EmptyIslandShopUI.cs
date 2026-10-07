using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// CoC-style Shop: side button → bottom catalog → tap tile to place props.
    /// </summary>
    public class EmptyIslandShopUI : MonoBehaviour
    {
        Canvas canvas;
        GameObject shopPanel;
        GameObject dim;
        Transform cardsRoot;
        Text titleText;
        Text subtitleText;
        IslandShopCatalog.Category? filter;
        IslandShopCatalog.Entry? pending;
        GameObject ghost;
        bool placing;
        Coroutine placeCo;

        public static bool IsOpen => instance != null && instance.shopPanel != null && instance.shopPanel.activeSelf;

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<EmptyIslandShopUI>() != null) return;
            new GameObject("EmptyIslandShopUI").AddComponent<EmptyIslandShopUI>();
        }

        void Awake() => instance = this;
        void Start() => Build();

        void OnDestroy()
        {
            CancelPlace();
        }

        void Build()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            canvas = OrchardUITheme.MakeCanvas(transform, "IslandShopCanvas", 240);

            // Shop button now lives in the top HUD row (EmptyIslandHUD) —
            // opened via ToggleFromOutside() so it sits next to Pause/Edit.

            BuildPanel();
            SetShopOpen(false);
        }

        static EmptyIslandShopUI instance;

        public static void ToggleFromOutside()
        {
            Ensure();
            instance = instance != null ? instance : FindFirstObjectByType<EmptyIslandShopUI>();
            instance?.ToggleShop();
        }

        void BuildPanel()
        {
            dim = OrchardUITheme.MakeImage(canvas.transform, "Dim", OrchardUITheme.DimOverlay).gameObject;
            OrchardUITheme.Stretch(dim.GetComponent<RectTransform>());
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.targetGraphic = dim.GetComponent<Image>();
            dimBtn.onClick.AddListener(() => SetShopOpen(false));

            shopPanel = OrchardUITheme.MakeImage(canvas.transform, "ShopPanel",
                OrchardUITheme.PanelCream).gameObject;
            OrchardUITheme.Place(shopPanel.GetComponent<RectTransform>(), 0.02f, 0.02f, 0.98f, 0.36f);

            titleText = OrchardUITheme.MakeText(shopPanel.transform, "Title", 26, FontStyle.Bold,
                TextAnchor.MiddleLeft, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(titleText.rectTransform, 0.04f, 0.78f, 0.62f, 0.96f);
            titleText.text = "Build — Island";
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Shop's own instruction lives INSIDE the shop panel (not the
            // shared coach bubble) so it can't ever end up layered under it.
            subtitleText = OrchardUITheme.MakeText(shopPanel.transform, "Subtitle", 14, FontStyle.Normal,
                TextAnchor.MiddleLeft, OrchardUITheme.InkMuted);
            OrchardUITheme.Place(subtitleText.rectTransform, 0.04f, 0.72f, 0.9f, 0.79f);
            subtitleText.text = "Pick something — then tap a tile on your island";
            subtitleText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var close = OrchardUITheme.MakeGreenButton(shopPanel.transform, "Close", "X", 22);
            OrchardUITheme.Place(close.GetComponent<RectTransform>(), 0.88f, 0.78f, 0.97f, 0.96f);
            var closeImg = close.GetComponent<Image>();
            if (closeImg != null) closeImg.color = new Color(0.65f, 0.35f, 0.28f, 1f);
            close.onClick.AddListener(() => SetShopOpen(false));

            BuildCategoryTabs(shopPanel.transform);

            // Horizontal scroll of cards
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(shopPanel.transform, false);
            OrchardUITheme.Place(scrollGo.GetComponent<RectTransform>(), 0.02f, 0.06f, 0.98f, 0.72f);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            var viewport = OrchardUITheme.MakeImage(scrollGo.transform, "Viewport",
                new Color(1f, 1f, 1f, 0.01f));
            OrchardUITheme.Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(800f, 0f);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 8, 8);
            hlg.spacing = 14f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            scroll.content = contentRt;
            cardsRoot = content.transform;

            RebuildCards();
        }

        void BuildCategoryTabs(Transform parent)
        {
            string[] names = { "All", "Nature", "Rocks", "Trees" };
            float x0 = 0.04f;
            float w = 0.18f;
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                var btn = OrchardUITheme.MakeGreenButton(parent, "Tab_" + name, name, 16);
                float a = x0 + i * (w + 0.02f);
                OrchardUITheme.Place(btn.GetComponent<RectTransform>(), a, 0.62f, a + w, 0.76f);
                int captured = i;
                btn.onClick.AddListener(() =>
                {
                    filter = captured == 0
                        ? (IslandShopCatalog.Category?)null
                        : (IslandShopCatalog.Category)(captured - 1);
                    RebuildCards();
                });
            }
        }

        void RebuildCards()
        {
            if (cardsRoot == null) return;
            for (int i = cardsRoot.childCount - 1; i >= 0; i--)
                Destroy(cardsRoot.GetChild(i).gameObject);

            for (int i = 0; i < IslandShopCatalog.All.Length; i++)
            {
                var e = IslandShopCatalog.All[i];
                if (filter.HasValue && e.category != filter.Value) continue;
                if (!IslandShopCatalog.IsAvailable(e)) continue;
                MakeCard(e);
            }
        }

        void MakeCard(IslandShopCatalog.Entry entry)
        {
            var card = OrchardUITheme.MakeImage(cardsRoot, "Card_" + entry.id,
                new Color(1f, 1f, 1f, 0.92f));
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 132f;
            le.minWidth = 132f;
            le.preferredHeight = 150f;

            // Start with the swatch — the real snapshot (async, one render
            // frame) swaps it in as soon as it's ready.
            var icon = OrchardUITheme.MakeImage(card.transform, "Icon", entry.swatch);
            OrchardUITheme.Place(icon.rectTransform, 0.12f, 0.38f, 0.88f, 0.92f);
            ShopIconCapture.GetIconAsync(entry, sprite =>
            {
                if (icon == null) return; // card was destroyed (panel closed / rebuilt) before capture finished
                icon.sprite = sprite;
                icon.color = Color.white;
                icon.preserveAspect = true;
            });

            var name = OrchardUITheme.MakeText(card.transform, "Name", 16, FontStyle.Bold,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(name.rectTransform, 0.04f, 0.04f, 0.96f, 0.34f);
            name.text = entry.label;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card;
            var copy = entry;
            btn.onClick.AddListener(() => SelectItem(copy));
        }

        void ToggleShop()
        {
            if (shopPanel != null && shopPanel.activeSelf)
            {
                SetShopOpen(false);
                return;
            }
            if (FruitPlantLessonFlow.IsBusy)
            {
                EmptyIslandCoachBar.SetTip("Finish the fruit lesson first — then open Shop");
                return;
            }
            if (placing)
            {
                EmptyIslandCoachBar.SetTip("Tap a free tile to place — or Cancel");
                return;
            }
            SetShopOpen(true);
        }

        const int ShopSortOrderClosed = 240;
        const int ShopSortOrderOpen = 900; // above every other panel — see class list below

        void SetShopOpen(bool open)
        {
            if (shopPanel != null) shopPanel.SetActive(open);
            if (dim != null) dim.SetActive(open);
            if (canvas != null) canvas.sortingOrder = open ? ShopSortOrderOpen : ShopSortOrderClosed;

            if (open)
            {
                RebuildCards();
                // Nothing else should be visible/poking through while the
                // player is focused on the Shop — hide every other floating
                // panel instead of letting them layer on top of it.
                EmptyIslandCoachBar.Hide();
                EmptyIslandVisitorAlert.HideAll();
            }
            else
            {
                EmptyIslandCoachBar.GoIdleQuiet();
            }
        }

        void SelectItem(IslandShopCatalog.Entry entry)
        {
            pending = entry;
            SetShopOpen(false);
            CancelPlace();
            placing = true;

            Vector3 start = EditModeManager.SnapToCoCTile(OrchardTreeTracker.Ensure().PickDecorationSpot());
            ghost = SpawnVisual(entry, start, ghost: true);
            IslandTapTrack.ShowTapHere(ghost.transform, "TAP TILE\n" + entry.label);
            EmptyIslandCameraFocus.FocusSmooth(start + Vector3.up * 0.5f, 13f, 1.2f);
            EmptyIslandCoachBar.SetTip("Tap a free tile to place " + entry.label);

            if (placeCo != null) StopCoroutine(placeCo);
            placeCo = StartCoroutine(WaitTapPlace());
        }

        IEnumerator WaitTapPlace()
        {
            yield return null;
            yield return null;

            float timeout = 60f;
            while (placing && ghost != null && timeout > 0f)
            {
                timeout -= Time.deltaTime;

                // Cancel with Escape / second shop press handled by Toggle
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelPlace();
                    EmptyIslandCoachBar.SetTip("Placement cancelled");
                    yield break;
                }

                bool tapped = false;
                Vector2 screen = Vector2.zero;
                int fingerId = -1;

                if (Input.touchCount == 1)
                {
                    var t = Input.GetTouch(0);
                    if (t.phase == TouchPhase.Began)
                    {
                        tapped = true;
                        screen = t.position;
                        fingerId = t.fingerId;
                    }
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    tapped = true;
                    screen = Input.mousePosition;
                }

                if (!tapped)
                {
                    yield return null;
                    continue;
                }

                if (EventSystem.current != null)
                {
                    bool overUi = fingerId >= 0
                        ? EventSystem.current.IsPointerOverGameObject(fingerId)
                        : EventSystem.current.IsPointerOverGameObject();
                    if (overUi)
                    {
                        yield return null;
                        continue;
                    }
                }

                var cam = Camera.main;
                if (cam == null)
                {
                    yield return null;
                    continue;
                }

                var ray = cam.ScreenPointToRay(screen);
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (!plane.Raycast(ray, out float dist))
                {
                    yield return null;
                    continue;
                }

                Vector3 p = EditModeManager.SnapToCoCTile(ray.GetPoint(dist));
                if (!IsSpotOk(p))
                {
                    EmptyIslandCoachBar.SetTip("Tile blocked — try another free spot");
                    ghost.transform.position = p;
                    IslandTapTrack.ShowTapHere(ghost.transform, "TAP FREE TILE");
                    yield return null;
                    continue;
                }

                var entry = pending.Value;
                Destroy(ghost);
                ghost = null;
                IslandTapTrack.Hide();

                var go = SpawnVisual(entry, p, ghost: false);
                EditModeManager.Ensure().RegisterDecoration(go.transform);
                DecorationInfoTarget.Ensure(go, entry.label, "A " + entry.category + " decoration for your island.");
                if (GameState.Instance != null)
                    GameState.Instance.AddDecoration(GameState.Instance.UnlockedDecorations.Count, p);
                EmptyIslandLevelProgress.RegisterDecorationPlaced();

                IslandTapTrack.ShowPlaced(p, entry.label);
                EmptyIslandCoachBar.SetTip(entry.label + " placed! Open Shop for more");
                placing = false;
                pending = null;
                placeCo = null;
                yield break;
            }

            CancelPlace();
            EmptyIslandCoachBar.SetTip("Shop placement timed out — open Shop again");
        }

        void CancelPlace()
        {
            placing = false;
            pending = null;
            if (placeCo != null)
            {
                StopCoroutine(placeCo);
                placeCo = null;
            }
            if (ghost != null)
            {
                Destroy(ghost);
                ghost = null;
            }
            IslandTapTrack.Hide();
        }

        static bool IsSpotOk(Vector3 p)
        {
            var well = GameObject.Find("StoneWell");
            if (well != null && Vector3.Distance(p, well.transform.position) < 6.5f) return false;
            var wish = GameObject.Find("WisdomTree") ?? GameObject.Find("WishTree")
                       ?? GameObject.Find("WishTree_Green");
            if (wish != null && Vector3.Distance(p, wish.transform.position) < 5.5f) return false;

            var tracker = FindFirstObjectByType<OrchardTreeTracker>();
            if (tracker != null)
            {
                foreach (var t in tracker.All)
                {
                    if (t.root == null) continue;
                    if (Vector3.Distance(p, t.position) < 5f) return false;
                }
            }

            var decos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            for (int i = 0; i < decos.Length; i++)
            {
                var go = decos[i];
                if (go == null || go.parent != null) continue;
                if (!go.name.StartsWith("Decoration_")) continue;
                if (Vector3.Distance(p, go.position) < 2.2f) return false;
            }
            return true;
        }

        static GameObject SpawnVisual(IslandShopCatalog.Entry entry, Vector3 pos, bool ghost)
        {
            pos = EditModeManager.SnapToCoCTile(pos);
            pos.y = 0f;

            GameObject root;
            if (!string.IsNullOrEmpty(entry.resourcePath))
            {
                var prefab = Resources.Load<GameObject>(entry.resourcePath);
                if (prefab != null)
                {
                    root = Object.Instantiate(prefab);
                    root.name = "Decoration_" + entry.label;
                    root.transform.position = pos;
                    FitHeight(root, entry.height);
                }
                else
                    root = MakeFallback(entry, pos);
            }
            else
                root = MakeGrassPatch(entry, pos);

            if (ghost)
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    foreach (var m in r.materials)
                    {
                        var c = entry.swatch;
                        c.a = 0.4f;
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                        m.color = c;
                    }
                }
            }
            return root;
        }

        static GameObject MakeGrassPatch(IslandShopCatalog.Entry entry, Vector3 pos)
        {
            var root = new GameObject("Decoration_" + entry.label);
            root.transform.position = pos;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad.name = "Grass";
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            quad.transform.localScale = new Vector3(1.6f, 0.08f, 1.6f);
            Object.Destroy(quad.GetComponent<Collider>());
            var mr = quad.GetComponent<MeshRenderer>();
            var mat = Resources.Load<Material>("CoC_BaseGrass");
            if (mat != null) mr.sharedMaterial = mat;
            else
            {
                mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mr.material.color = entry.swatch;
                if (mr.material.HasProperty("_BaseColor"))
                    mr.material.SetColor("_BaseColor", entry.swatch);
            }
            // Small tufts
            for (int i = 0; i < 3; i++)
            {
                var tuft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tuft.transform.SetParent(root.transform, false);
                tuft.transform.localPosition = new Vector3(
                    Random.Range(-0.45f, 0.45f), 0.12f, Random.Range(-0.45f, 0.45f));
                tuft.transform.localScale = new Vector3(0.12f, Random.Range(0.18f, 0.32f), 0.12f);
                Object.Destroy(tuft.GetComponent<Collider>());
                var tmr = tuft.GetComponent<MeshRenderer>();
                tmr.sharedMaterial = mr.sharedMaterial;
            }
            return root;
        }

        static GameObject MakeFallback(IslandShopCatalog.Entry entry, Vector3 pos)
        {
            var root = new GameObject("Decoration_" + entry.label);
            root.transform.position = pos;
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, entry.height * 0.35f, 0f);
            body.transform.localScale = new Vector3(0.6f, entry.height * 0.35f, 0.6f);
            Object.Destroy(body.GetComponent<Collider>());
            var mr = body.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mr.material.color = entry.swatch;
            if (mr.material.HasProperty("_BaseColor"))
                mr.material.SetColor("_BaseColor", entry.swatch);
            return root;
        }

        static void FitHeight(GameObject go, float targetH)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float h = Mathf.Max(0.01f, b.size.y);
            go.transform.localScale *= (targetH / h);
            var p = go.transform.position;
            p.y = 0f;
            go.transform.position = p;
        }
    }
}

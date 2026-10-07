using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// After every 2 heals on Empty Island — decoration reward.
    /// Player picks: auto-place (recommended spot) OR tap-to-place themselves.
    /// Uses existing EditModeManager so later they can still move pieces.
    /// </summary>
    public class EmptyIslandDecorationReward : MonoBehaviour
    {
        const int HealsPerReward = 2;
        int healCount;
        bool offering;
        GameObject pendingGhost;
        string pendingLabel;
        Color pendingColor;
        Canvas offerCanvas;
        EditModeManager editMode;

        static readonly string[] DecoNames =
        {
            "Garden Rock", "Flower Bush", "Pine Accent", "Pear Accent", "Plum Accent"
        };

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<EmptyIslandDecorationReward>() != null) return;
            new GameObject("EmptyIslandDecorationReward").AddComponent<EmptyIslandDecorationReward>();
        }

        void Start()
        {
            if (GameState.Instance == null)
                new GameObject("GameState").AddComponent<GameState>();

            editMode = FindFirstObjectByType<EditModeManager>();
            if (editMode == null)
            {
                var go = new GameObject("EditModeManager");
                editMode = go.AddComponent<EditModeManager>();
            }

            GameState.Instance.StickmanHealed += OnHealed;
        }

        void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.StickmanHealed -= OnHealed;
            CancelPlaceMode();
        }

        void OnHealed()
        {
            healCount++;
            if (offering) return;
            if (healCount % HealsPerReward != 0) return;
            StartCoroutine(OfferSoon());
        }

        IEnumerator OfferSoon()
        {
            offering = true;
            yield return new WaitForSeconds(1.2f);
            // Wait until plant lesson / gesture guide done
            while (FruitPlantLessonFlow.IsBusy || FindFirstObjectByType<GestureWhereGuide>() != null)
                yield return new WaitForSeconds(0.5f);
            ShowOffer();
        }

        void ShowOffer()
        {
            EmptyIslandGuideStack.Push(EmptyIslandGuideStack.Layer.Reward);
            if (offerCanvas != null) Destroy(offerCanvas.gameObject);

            int idx = (GameState.Instance != null ? GameState.Instance.UnlockedDecorations.Count : 0) % DecoNames.Length;
            pendingLabel = DecoNames[idx];
            pendingColor = Color.HSVToRGB((idx * 0.17f) % 1f, 0.45f, 0.85f);

            offerCanvas = OrchardUITheme.MakeCanvas(transform, "DecoRewardCanvas", 920);
            var panel = OrchardUITheme.MakeImage(offerCanvas.transform, "Panel", OrchardUITheme.PanelCream);
            OrchardUITheme.Place(panel.rectTransform, 0.08f, 0.32f, 0.92f, 0.68f);

            var title = OrchardUITheme.MakeText(panel.transform, "T", 30, FontStyle.Bold,
                TextAnchor.UpperCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(title.rectTransform, 0.05f, 0.72f, 0.95f, 0.95f);
            title.text = "Island reward!";

            var body = OrchardUITheme.MakeText(panel.transform, "B", 20, FontStyle.Normal,
                TextAnchor.UpperCenter, OrchardUITheme.InkMuted);
            OrchardUITheme.Place(body.rectTransform, 0.06f, 0.38f, 0.94f, 0.72f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.text = "You healed visitors — unlock a " + pendingLabel +
                        ".\nChoose a spot for your island.";

            var autoBtn = OrchardUITheme.MakeGreenButton(panel.transform, "Auto", "Place for me", 20);
            OrchardUITheme.Place(autoBtn.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.36f, 0.32f);
            autoBtn.onClick.AddListener(PlaceRecommended);

            var meBtn = OrchardUITheme.MakeGreenButton(panel.transform, "Me", "I'll place it", 20);
            OrchardUITheme.Place(meBtn.GetComponent<RectTransform>(), 0.38f, 0.08f, 0.68f, 0.32f);
            meBtn.onClick.AddListener(BeginTapToPlace);

            var cancelBtn = OrchardUITheme.MakeGreenButton(panel.transform, "Cancel", "Later", 18);
            OrchardUITheme.Place(cancelBtn.GetComponent<RectTransform>(), 0.70f, 0.08f, 0.94f, 0.32f);
            var cancelImg = cancelBtn.GetComponent<Image>();
            if (cancelImg != null) cancelImg.color = new Color(0.55f, 0.4f, 0.35f, 1f);
            cancelBtn.onClick.AddListener(() =>
            {
                CloseOffer();
                EmptyIslandCoachBar.SetTip("Reward saved for later — keep healing visitors!");
            });

            EmptyIslandCoachBar.SetTip("Decoration reward — place it on your island!");
        }

        void PlaceRecommended()
        {
            Vector3 pos = EditModeManager.SnapToCoCTile(PickRecommendedSpot());
            SpawnDecoration(pos);
            CloseOffer();
            IslandTapTrack.ShowPlaced(pos, pendingLabel);
            EmptyIslandCoachBar.SetTip(pendingLabel + " placed — Edit to move on tiles");
        }

        void SpawnDecoration(Vector3 pos)
        {
            pos = EditModeManager.SnapToCoCTile(pos);
            int index = GameState.Instance != null ? GameState.Instance.UnlockedDecorations.Count : 0;
            if (GameState.Instance != null)
                GameState.Instance.AddDecoration(index, pos);

            editMode = EditModeManager.Ensure();
            var root = SpawnDecorationVisual(pos, pendingLabel, pendingColor, ghost: false);
            editMode.RegisterDecoration(root.transform);
            DecorationInfoTarget.Ensure(root, pendingLabel, "A reward decoration you placed on the island.");
            EmptyIslandLevelProgress.RegisterDecorationPlaced();
        }

        void BeginTapToPlace()
        {
            if (offerCanvas != null) Destroy(offerCanvas.gameObject);
            offerCanvas = null;
            // Allow character guide tips during place mode
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Reward);

            Vector3 ghostPos = EditModeManager.SnapToCoCTile(PickRecommendedSpot());
            pendingGhost = SpawnDecorationVisual(ghostPos, pendingLabel, pendingColor, ghost: true);
            IslandTapTrack.ShowTapHere(pendingGhost.transform, "TAP TILE\nto place");
            EmptyIslandCoachBar.SetTip("TAP a free tile to place " + pendingLabel + " — or Edit later");
            StartCoroutine(WaitForTapPlace());
        }

        IEnumerator WaitForTapPlace()
        {
            yield return null;
            yield return null;

            float timeout = 45f;
            while (pendingGhost != null && timeout > 0f)
            {
                timeout -= Time.deltaTime;

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
                if (cam != null)
                {
                    var ray = cam.ScreenPointToRay(screen);
                    var plane = new Plane(Vector3.up, Vector3.zero);
                    if (plane.Raycast(ray, out float dist))
                    {
                        Vector3 p = EditModeManager.SnapToCoCTile(ray.GetPoint(dist));
                        if (IsSpotOk(p))
                        {
                            Destroy(pendingGhost);
                            pendingGhost = null;
                            IslandTapTrack.Hide();
                            SpawnDecoration(p);
                            offering = false;
                            IslandTapTrack.ShowPlaced(p, pendingLabel);
                            EmptyIslandCoachBar.SetTip(pendingLabel + " placed! Use Edit to shift tiles like CoC");
                            yield break;
                        }
                        EmptyIslandCoachBar.SetTip("Tile blocked — try another free tile");
                        pendingGhost.transform.position = p;
                        IslandTapTrack.ShowTapHere(pendingGhost.transform, "TAP FREE TILE");
                    }
                }
                yield return null;
            }

            // Timeout — auto place so we never softlock
            if (pendingGhost != null)
            {
                Destroy(pendingGhost);
                pendingGhost = null;
                PlaceRecommended();
            }
        }

        static GameObject SpawnDecorationVisual(Vector3 pos, string label, Color tint, bool ghost)
        {
            // Prefer real HQ props already in Resources
            string[] paths =
            {
                BestAssets.Rock1, BestAssets.Rock2, BestAssets.Bush,
                BestAssets.ForestPine, BestAssets.ForestPear, BestAssets.ForestPlum
            };
            int pick = Mathf.Abs(label.GetHashCode()) % paths.Length;
            var prefab = Resources.Load<GameObject>(paths[pick]);

            GameObject root;
            if (prefab != null)
            {
                root = Object.Instantiate(prefab);
                root.name = "Decoration_" + label;
                root.transform.position = pos;
                FitDeco(root, 1.2f);
            }
            else
            {
                root = new GameObject("Decoration_" + label);
                root.transform.position = pos;
                var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                baseGO.transform.SetParent(root.transform, false);
                baseGO.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                baseGO.transform.localScale = new Vector3(0.7f, 0.25f, 0.7f);
                Object.Destroy(baseGO.GetComponent<Collider>());
            }

            if (ghost)
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    foreach (var m in r.materials)
                    {
                        var c = tint;
                        c.a = 0.45f;
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                        m.color = c;
                    }
                }
            }

            var tag = new GameObject("Label");
            tag.transform.SetParent(root.transform, false);
            tag.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var tm = tag.AddComponent<TextMesh>();
            tm.text = label;
            tm.fontSize = 36;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
            tag.AddComponent<TreeNameBillboard>();
            return root;
        }

        static void FitDeco(GameObject go, float targetH)
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

        Vector3 PickRecommendedSpot()
        {
            var tracker = OrchardTreeTracker.Ensure();
            for (int i = 0; i < 30; i++)
            {
                var p = tracker.PickDecorationSpot();
                if (IsSpotOk(p)) return p;
            }
            return tracker.PickDecorationSpot();
        }

        bool IsSpotOk(Vector3 p)
        {
            var well = GameObject.Find("StoneWell");
            if (well != null && Vector3.Distance(p, well.transform.position) < 7f) return false;
            var wish = GameObject.Find("WisdomTree") ?? GameObject.Find("WishTree")
                       ?? GameObject.Find("WishTree_Green");
            if (wish != null && Vector3.Distance(p, wish.transform.position) < 6f) return false;
            var tracker = FindFirstObjectByType<OrchardTreeTracker>();
            if (tracker != null)
            {
                foreach (var t in tracker.All)
                {
                    if (t.root == null) continue;
                    if (Vector3.Distance(p, t.position) < 5.5f) return false;
                }
            }
            return true;
        }

        void CloseOffer()
        {
            CancelPlaceMode();
            if (offerCanvas != null) Destroy(offerCanvas.gameObject);
            offerCanvas = null;
            offering = false;
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Reward);
        }

        void CancelPlaceMode()
        {
            if (pendingGhost != null)
            {
                Destroy(pendingGhost);
                pendingGhost = null;
            }
        }
    }
}

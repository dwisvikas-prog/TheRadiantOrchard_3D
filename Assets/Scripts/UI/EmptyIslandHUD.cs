using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // Compact mobile HUD — Vibrancy + Pause/Edit + fruit tray (unlocked only, one-by-one).
    public class EmptyIslandHUD : MonoBehaviour
    {
        static readonly FruitType[] FruitOrder =
        {
            FruitType.Strawberry, FruitType.Pineapple, FruitType.Watermelon,
            FruitType.Lemon, FruitType.Grapes, FruitType.Apple,
            FruitType.Peach, FruitType.Banana, FruitType.Cherry
        };

        Canvas canvas;
        Image fillImage;
        Text vibrancyLabel;
        Transform fruitTray;
        Image[] fruitIcons;
        Image[] fruitSlotBg;
        Text[] fruitLabels;
        readonly bool[] unlocked = new bool[9];
        float targetFill;
        float displayedFill;
        bool subscribed;

        EditModeManager editMode;
        Text editLabel;

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<EmptyIslandHUD>() != null) return;
            var go = new GameObject("EmptyIslandHUD");
            go.AddComponent<EmptyIslandHUD>();
        }

        void Start() => Build();
        void OnEnable() => TrySubscribe();

        void OnDisable()
        {
            if (subscribed && GameState.Instance != null)
                GameState.Instance.VibrancyChanged -= OnVibrancy;
            subscribed = false;
        }

        void Build()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            if (GameState.Instance == null)
                new GameObject("GameState").AddComponent<GameState>();

            unlocked[0] = true; // Strawberry from first plant

            canvas = OrchardUITheme.MakeCanvas(transform, "OrchardHUDCanvas", 200);
            BuildCompactTop(canvas.transform);
            BuildFruitTray(canvas.transform);
            editMode = EditModeManager.Ensure();
            EmptyIslandShopUI.Ensure();
            EmptyIslandVisitorAlert.Ensure();
            TreeNameBoard.RefreshAllTracked();
            EmptyIslandCoachBar.Ensure();
            EmptyIslandCoachBar.GoIdleQuiet();
            TrySubscribe();
        }

        void BuildCompactTop(Transform root)
        {
            var bar = OrchardUITheme.MakeImage(root, "TopMeter", new Color(0.08f, 0.12f, 0.08f, 0.78f));
            OrchardUITheme.Place(bar.rectTransform, 0.14f, 0.935f, 0.72f, 0.975f);

            var lvlBg = OrchardUITheme.MakeImage(root, "LvlPill", new Color(0.95f, 0.78f, 0.25f, 0.95f));
            OrchardUITheme.Anchored(lvlBg.rectTransform, new Vector2(0.06f, 0.955f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(72f, 40f));
            var lvl = OrchardUITheme.MakeText(lvlBg.transform, "L", 20, FontStyle.Bold,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Stretch(lvl.rectTransform);
            lvl.text = "Lv 1";
            lvl.horizontalOverflow = HorizontalWrapMode.Overflow;

            var leafSp = OrchardUITheme.LoadSprite(BestAssets.UiLeaf);
            var leaf = OrchardUITheme.MakeImage(bar.transform, "Leaf", Color.white, leafSp);
            OrchardUITheme.Place(leaf.rectTransform, 0.02f, 0.12f, 0.10f, 0.88f);

            var track = OrchardUITheme.MakeImage(bar.transform, "Track", OrchardUITheme.BarTrack);
            OrchardUITheme.Place(track.rectTransform, 0.12f, 0.22f, 0.78f, 0.78f);

            var fillSp = OrchardUITheme.LoadSprite(BestAssets.UiVibrancy);
            fillImage = OrchardUITheme.MakeImage(track.transform, "Fill", OrchardUITheme.VibrancyFill, fillSp);
            OrchardUITheme.Stretch(fillImage.rectTransform);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            fillImage.preserveAspect = false;

            vibrancyLabel = OrchardUITheme.MakeText(bar.transform, "VLabel", 18, FontStyle.Bold,
                TextAnchor.MiddleRight, Color.white);
            OrchardUITheme.Place(vibrancyLabel.rectTransform, 0.78f, 0.1f, 0.98f, 0.9f);
            vibrancyLabel.text = "0";
            vibrancyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Pause (right) + Edit beside it
            var pause = OrchardUITheme.MakeImage(root, "Pause", new Color(1f, 1f, 1f, 0.22f));
            OrchardUITheme.Anchored(pause.rectTransform, new Vector2(0.955f, 0.955f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(44f, 44f));
            var pTxt = OrchardUITheme.MakeText(pause.transform, "P", 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Stretch(pTxt.rectTransform);
            pTxt.text = "II";
            pTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var pauseBtn = pause.gameObject.AddComponent<Button>();
            pauseBtn.targetGraphic = pause;
            pauseBtn.onClick.AddListener(() =>
            {
                var menu = FindFirstObjectByType<PauseMenuUI>();
                if (menu != null) menu.TogglePause();
                else Time.timeScale = Time.timeScale > 0.01f ? 0f : 1f;
            });

            var editBg = OrchardUITheme.MakeImage(root, "Edit", OrchardUITheme.ButtonGreen);
            OrchardUITheme.Anchored(editBg.rectTransform, new Vector2(0.86f, 0.955f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(56f, 40f));
            editLabel = OrchardUITheme.MakeText(editBg.transform, "E", 16, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Stretch(editLabel.rectTransform);
            editLabel.text = "Edit";
            editLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            var editBtn = editBg.gameObject.AddComponent<Button>();
            editBtn.targetGraphic = editBg;
            editBtn.onClick.AddListener(ToggleEdit);

            // Zoom Out — decent fixed step, replaces the old auto idle zoom-out
            var zoomBg = OrchardUITheme.MakeImage(root, "ZoomOut", new Color(1f, 1f, 1f, 0.22f));
            OrchardUITheme.Anchored(zoomBg.rectTransform, new Vector2(0.955f, 0.885f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(44f, 44f));
            var zoomTxt = OrchardUITheme.MakeText(zoomBg.transform, "Z", 20, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Stretch(zoomTxt.rectTransform);
            zoomTxt.text = "-";
            zoomTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var zoomBtn = zoomBg.gameObject.AddComponent<Button>();
            zoomBtn.targetGraphic = zoomBg;
            zoomBtn.onClick.AddListener(() =>
            {
                var cam = Camera.main;
                var dio = cam != null ? cam.GetComponent<DioramaController>() : null;
                if (dio != null) dio.ManualZoomOut();
            });

            // Shop — same top-right row, left of Edit
            var shopBg = OrchardUITheme.MakeImage(root, "ShopTop", new Color(0.95f, 0.72f, 0.18f, 0.98f));
            OrchardUITheme.Anchored(shopBg.rectTransform, new Vector2(0.74f, 0.955f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(64f, 40f));
            var shopTxt = OrchardUITheme.MakeText(shopBg.transform, "S", 15, FontStyle.Bold,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Stretch(shopTxt.rectTransform);
            shopTxt.text = "Shop";
            shopTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var shopBtn = shopBg.gameObject.AddComponent<Button>();
            shopBtn.targetGraphic = shopBg;
            shopBtn.onClick.AddListener(() => EmptyIslandShopUI.ToggleFromOutside());

            // Info ("?") — which gesture each fruit needs, always available
            var infoBg = OrchardUITheme.MakeImage(root, "GestureInfo", new Color(0.25f, 0.55f, 0.85f, 0.95f));
            OrchardUITheme.Anchored(infoBg.rectTransform, new Vector2(0.955f, 0.815f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(44f, 44f));
            var infoTxt = OrchardUITheme.MakeText(infoBg.transform, "I", 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Stretch(infoTxt.rectTransform);
            infoTxt.text = "?";
            infoTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var infoBtn = infoBg.gameObject.AddComponent<Button>();
            infoBtn.targetGraphic = infoBg;
            infoBtn.onClick.AddListener(() => GestureLegendCardUI.Toggle());
        }

        void ToggleEdit()
        {
            if (editMode == null) editMode = EditModeManager.Ensure();
            if (editMode.EditModeActive)
            {
                editMode.ExitEditMode();
                if (editLabel != null) editLabel.text = "Edit";
            }
            else
            {
                if (FruitPlantLessonFlow.IsBusy)
                {
                    EmptyIslandCoachBar.SetTip("Finish this fruit lesson first — then Edit");
                    return;
                }
                editMode.EnterEditMode();
                if (editLabel != null) editLabel.text = "Done";
            }
        }

        void BuildFruitTray(Transform root)
        {
            var tray = OrchardUITheme.MakeImage(root, "FruitTray", new Color(0.99f, 0.96f, 0.88f, 0.88f));
            fruitTray = tray.transform;
            OrchardUITheme.Place(tray.rectTransform, 0.24f, 0.012f, 0.76f, 0.1f);

            fruitIcons = new Image[FruitOrder.Length];
            fruitSlotBg = new Image[FruitOrder.Length];
            fruitLabels = new Text[FruitOrder.Length];
            for (int i = 0; i < FruitOrder.Length; i++)
            {
                var slotBg = OrchardUITheme.MakeImage(tray.transform, "S" + i, new Color(1f, 1f, 1f, 0.32f));
                fruitSlotBg[i] = slotBg;
                var iconSp = OrchardUITheme.LoadSprite(BestAssets.FruitIconRoot + FruitOrder[i]);
                var icon = OrchardUITheme.MakeImage(slotBg.transform, "I", Color.white, iconSp);
                OrchardUITheme.Place(icon.rectTransform, 0.08f, 0.3f, 0.92f, 0.98f);
                icon.preserveAspect = true;
                fruitIcons[i] = icon;

                var nameLbl = OrchardUITheme.MakeText(slotBg.transform, "N", 11, FontStyle.Bold,
                    TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
                OrchardUITheme.Place(nameLbl.rectTransform, 0.02f, 0f, 0.98f, 0.28f);
                nameLbl.text = FruitOrder[i].ToString();
                nameLbl.horizontalOverflow = HorizontalWrapMode.Overflow;
                nameLbl.resizeTextForBestFit = true;
                nameLbl.resizeTextMinSize = 7;
                nameLbl.resizeTextMaxSize = 12;
                fruitLabels[i] = nameLbl;

                slotBg.gameObject.SetActive(false);
            }
            RefreshFruitTrayLayout();
        }

        void RefreshFruitTrayLayout()
        {
            if (fruitIcons == null || fruitTray == null) return;
            int count = 0;
            for (int i = 0; i < unlocked.Length; i++)
                if (unlocked[i]) count++;
            if (count < 1) count = 1;

            // Tray width grows with unlocked count (still compact)
            float half = Mathf.Clamp(0.08f + count * 0.045f, 0.12f, 0.38f);
            var trayRt = fruitTray.GetComponent<RectTransform>();
            if (trayRt != null)
                OrchardUITheme.Place(trayRt, 0.5f - half, 0.012f, 0.5f + half, 0.065f);

            float pad = 0.04f;
            float slot = count > 0 ? (1f - pad * 2f) / count : 1f;
            int shown = 0;
            for (int i = 0; i < FruitOrder.Length; i++)
            {
                var slotGo = fruitIcons[i] != null ? fruitIcons[i].transform.parent.gameObject : null;
                if (slotGo == null) continue;
                if (!unlocked[i])
                {
                    slotGo.SetActive(false);
                    continue;
                }
                slotGo.SetActive(true);
                float x0 = pad + slot * shown + 0.01f;
                float x1 = pad + slot * (shown + 1) - 0.01f;
                OrchardUITheme.Place(slotGo.GetComponent<RectTransform>(), x0, 0.1f, x1, 0.9f);
                fruitIcons[i].color = Color.white;
                shown++;
            }
        }

        void TrySubscribe()
        {
            if (subscribed || GameState.Instance == null) return;
            GameState.Instance.VibrancyChanged += OnVibrancy;
            subscribed = true;
            OnVibrancy(GameState.Instance.CurrentVibrancy, GameState.Instance.MaxVibrancy);
        }

        void OnVibrancy(int current, int max)
        {
            targetFill = max > 0 ? (float)current / max : 0f;
            if (vibrancyLabel != null)
                vibrancyLabel.text = max > 0 ? current + "/" + max : current.ToString();
        }

        void Update()
        {
            if (!subscribed) TrySubscribe();
            if (fillImage == null) return;
            displayedFill = Mathf.MoveTowards(displayedFill, targetFill, Time.unscaledDeltaTime * 1.8f);
            fillImage.fillAmount = displayedFill;
            UpdateActiveFruitBlink();
        }

        void UpdateActiveFruitBlink()
        {
            if (fruitSlotBg == null) return;
            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 6f) * 0.5f; // 0..1
            for (int i = 0; i < FruitOrder.Length; i++)
            {
                var bg = fruitSlotBg[i];
                if (bg == null || !bg.gameObject.activeSelf) continue;

                bool isActive = ActiveFruitTracker.HasActive && ActiveFruitTracker.Current == FruitOrder[i];
                if (isActive)
                {
                    bg.color = Color.Lerp(new Color(1f, 0.85f, 0.3f, 0.5f),
                        new Color(1f, 0.95f, 0.55f, 0.95f), pulse);
                    bg.transform.localScale = Vector3.one * (1f + pulse * 0.1f);
                }
                else
                {
                    bg.color = new Color(1f, 1f, 1f, 0.32f);
                    bg.transform.localScale = Vector3.one;
                }
            }
        }

        public void UnlockFruit(FruitType type)
        {
            if (fruitIcons == null) return;
            for (int i = 0; i < FruitOrder.Length; i++)
            {
                if (FruitOrder[i] != type) continue;
                bool wasNew = !unlocked[i];
                unlocked[i] = true;
                RefreshFruitTrayLayout();
                if (wasNew)
                {
                    var L = FruitLessonBook.Get(type);
                    EmptyIslandCoachBar.SetNewEvent(
                        L.fruitName + " unlocked! " +
                        FruitLessonBook.GestureShort(L.gesture));
                }
                return;
            }
        }
    }
}

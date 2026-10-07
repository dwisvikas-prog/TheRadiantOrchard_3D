using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// Top-of-screen visitor indicator + info icon (CoC-style, after fruit is taught).
    /// Tap (i) to learn which tree / which gesture — player tracks themselves.
    /// </summary>
    public class EmptyIslandVisitorAlert : MonoBehaviour
    {
        Canvas canvas;
        GameObject banner;
        GameObject infoPanel;
        Text bannerText;
        Text infoBody;
        FruitLessonBook.Lesson lesson;
        StickmanController visitor;
        Transform treeHint;
        bool visible;

        public static EmptyIslandVisitorAlert Ensure()
        {
            var a = FindFirstObjectByType<EmptyIslandVisitorAlert>();
            if (a != null) return a;
            var go = new GameObject("EmptyIslandVisitorAlert");
            return go.AddComponent<EmptyIslandVisitorAlert>();
        }

        public static void ShowArrival(FruitLessonBook.Lesson L, StickmanController v, Transform tree = null)
        {
            var a = Ensure();
            a.Show(L, v, tree);
        }

        public static void HideAll()
        {
            var a = FindFirstObjectByType<EmptyIslandVisitorAlert>();
            if (a != null) a.Hide();
        }

        void Awake() => Build();

        void Build()
        {
            if (canvas != null) return;
            canvas = OrchardUITheme.MakeCanvas(transform, "VisitorAlertCanvas", 260);

            banner = OrchardUITheme.MakeImage(canvas.transform, "Banner",
                new Color(0.95f, 0.55f, 0.18f, 0.95f)).gameObject;
            OrchardUITheme.Anchored(banner.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(280f, 48f));

            bannerText = OrchardUITheme.MakeText(banner.transform, "T", 20, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Place(bannerText.rectTransform, 0.04f, 0.1f, 0.72f, 0.9f);
            bannerText.text = "Visitor!";
            bannerText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var infoBtn = OrchardUITheme.MakeImage(banner.transform, "Info",
                new Color(1f, 1f, 1f, 0.95f));
            OrchardUITheme.Place(infoBtn.rectTransform, 0.76f, 0.12f, 0.94f, 0.88f);
            var iTxt = OrchardUITheme.MakeText(infoBtn.transform, "i", 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Stretch(iTxt.rectTransform);
            iTxt.text = "i";
            iTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var btn = infoBtn.gameObject.AddComponent<Button>();
            btn.targetGraphic = infoBtn;
            btn.onClick.AddListener(ToggleInfo);

            infoPanel = OrchardUITheme.MakeImage(canvas.transform, "InfoPanel",
                OrchardUITheme.PanelCream).gameObject;
            OrchardUITheme.Anchored(infoPanel.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(340f, 150f));

            infoBody = OrchardUITheme.MakeText(infoPanel.transform, "Body", 18, FontStyle.Normal,
                TextAnchor.UpperLeft, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(infoBody.rectTransform, 0.06f, 0.32f, 0.94f, 0.94f);

            var goBtn = OrchardUITheme.MakeGreenButton(infoPanel.transform, "Go", "Show tree", 18);
            OrchardUITheme.Place(goBtn.GetComponent<RectTransform>(), 0.08f, 0.06f, 0.48f, 0.28f);
            goBtn.onClick.AddListener(FocusTree);

            var closeBtn = OrchardUITheme.MakeGreenButton(infoPanel.transform, "Close", "OK", 18);
            OrchardUITheme.Place(closeBtn.GetComponent<RectTransform>(), 0.52f, 0.06f, 0.92f, 0.28f);
            var cImg = closeBtn.GetComponent<Image>();
            if (cImg != null) cImg.color = new Color(0.55f, 0.4f, 0.32f, 1f);
            closeBtn.onClick.AddListener(() =>
            {
                if (infoPanel != null) infoPanel.SetActive(false);
            });

            banner.SetActive(false);
            infoPanel.SetActive(false);
        }

        void Show(FruitLessonBook.Lesson L, StickmanController v, Transform tree)
        {
            if (canvas == null) Build();
            lesson = L;
            visitor = v;
            treeHint = tree;
            visible = true;

            string g = FruitLessonBook.GestureShort(L.gesture).ToUpperInvariant();
            bannerText.text = "Visitor!  " + L.mood;
            infoBody.text =
                L.mood + " visitor\n" +
                "Go to: " + FruitTreeCatalog.Get(L.fruit).label + "\n" +
                "Then: " + g + " the " + L.fruitName;

            banner.SetActive(true);
            infoPanel.SetActive(false);
            EmptyIslandCoachBar.GoIdleQuiet();
        }

        void ToggleInfo()
        {
            if (infoPanel == null) return;
            bool open = !infoPanel.activeSelf;
            infoPanel.SetActive(open);
            if (open)
                EmptyIslandCoachBar.SetNewEvent(
                    "Tap Show tree — then " + FruitLessonBook.GestureShort(lesson.gesture) +
                    " the " + lesson.fruitName);
        }

        void FocusTree()
        {
            if (treeHint == null)
            {
                var planted = OrchardTreeTracker.Ensure().Find(lesson.fruit);
                if (planted != null) treeHint = planted.root;
            }
            if (treeHint == null) return;

            EmptyIslandCameraFocus.FocusSmooth(treeHint.position + Vector3.up * 0.7f, 11.5f, 1.4f);
            string g = FruitLessonBook.GestureShort(lesson.gesture).ToUpperInvariant();
            IslandTapTrack.ShowGestureOnTree(treeHint, g);
            if (infoPanel != null) infoPanel.SetActive(false);
        }

        public void Hide()
        {
            visible = false;
            if (banner != null) banner.SetActive(false);
            if (infoPanel != null) infoPanel.SetActive(false);
            IslandTapTrack.Hide();
        }

        void Update()
        {
            if (!visible || banner == null || !banner.activeSelf) return;
            // Soft pulse so player notices
            float s = 1f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.04f;
            banner.transform.localScale = new Vector3(s, s, 1f);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// "?" button card — lists every fruit and which gesture harvests it, so
    /// the player always has a reference without hunting for world text.
    /// </summary>
    public class GestureLegendCardUI : MonoBehaviour
    {
        static GestureLegendCardUI instance;

        Canvas canvas;
        GameObject dim;
        GameObject card;

        public static void Toggle()
        {
            if (instance == null)
            {
                var go = new GameObject("GestureLegendCardUI");
                instance = go.AddComponent<GestureLegendCardUI>();
                instance.Build();
            }
            instance.ToggleInternal();
        }

        void Build()
        {
            canvas = OrchardUITheme.MakeCanvas(transform, "GestureLegendCanvas", 400);

            dim = OrchardUITheme.MakeImage(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.22f)).gameObject;
            OrchardUITheme.Stretch(dim.GetComponent<RectTransform>());
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.targetGraphic = dim.GetComponent<Image>();
            dimBtn.onClick.AddListener(Hide);

            card = OrchardUITheme.MakeImage(canvas.transform, "Card", OrchardUITheme.PanelCream).gameObject;
            OrchardUITheme.Anchored(card.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(380f, 460f));

            var title = OrchardUITheme.MakeText(card.transform, "Title", 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(title.rectTransform, 0.06f, 0.9f, 0.94f, 0.98f);
            title.text = "Fruit Gestures";

            var close = OrchardUITheme.MakeGreenButton(card.transform, "Close", "X", 16);
            OrchardUITheme.Place(close.GetComponent<RectTransform>(), 0.88f, 0.91f, 0.98f, 0.99f);
            var closeImg = close.GetComponent<Image>();
            if (closeImg != null) closeImg.color = new Color(0.55f, 0.4f, 0.32f, 1f);
            close.onClick.AddListener(Hide);

            BuildRows(card.transform);

            dim.SetActive(false);
            card.SetActive(false);
        }

        void BuildRows(Transform parent)
        {
            var fruits = FruitLessonBook.All;
            int n = fruits.Length;
            float top = 0.86f;
            float bottom = 0.03f;
            float rowH = (top - bottom) / n;

            for (int i = 0; i < n; i++)
            {
                var lesson = fruits[i];
                float y1 = top - rowH * i;
                float y0 = y1 - rowH + 0.006f;

                var row = OrchardUITheme.MakeImage(parent, "Row" + i,
                    i % 2 == 0 ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0f));
                OrchardUITheme.Place(row.rectTransform, 0.04f, y0, 0.96f, y1);

                var iconSp = OrchardUITheme.LoadSprite(BestAssets.FruitIconRoot + lesson.fruit);
                var icon = OrchardUITheme.MakeImage(row.transform, "Icon", Color.white, iconSp);
                OrchardUITheme.Place(icon.rectTransform, 0.02f, 0.08f, 0.16f, 0.92f);
                icon.preserveAspect = true;

                var name = OrchardUITheme.MakeText(row.transform, "Name", 15, FontStyle.Bold,
                    TextAnchor.MiddleLeft, OrchardUITheme.InkBrown);
                OrchardUITheme.Place(name.rectTransform, 0.19f, 0f, 0.6f, 1f);
                name.text = lesson.fruitName;

                var gesture = OrchardUITheme.MakeText(row.transform, "Gesture", 14, FontStyle.Bold,
                    TextAnchor.MiddleRight, new Color(0.18f, 0.5f, 0.25f, 1f));
                OrchardUITheme.Place(gesture.rectTransform, 0.6f, 0f, 0.97f, 1f);
                gesture.text = FruitLessonBook.GestureShort(lesson.gesture).ToUpperInvariant();
            }
        }

        void ToggleInternal()
        {
            bool open = card != null && card.activeSelf;
            SetOpen(!open);
        }

        void SetOpen(bool open)
        {
            if (dim != null) dim.SetActive(open);
            if (card != null) card.SetActive(open);
        }

        void Hide() => SetOpen(false);
    }
}

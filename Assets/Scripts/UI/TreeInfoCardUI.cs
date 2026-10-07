using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// CoC-style "tap the building" info card — real fruit image, virtue,
    /// what it heals and why. Opened by TreeInfoTapController.
    /// </summary>
    public class TreeInfoCardUI : MonoBehaviour
    {
        static TreeInfoCardUI instance;

        Canvas canvas;
        GameObject dim;
        GameObject card;

        public static void Show(FruitType fruit)
        {
            if (instance == null)
            {
                var go = new GameObject("TreeInfoCardUI");
                instance = go.AddComponent<TreeInfoCardUI>();
                instance.Build();
            }
            instance.Populate(fruit);
        }

        void Build()
        {
            canvas = OrchardUITheme.MakeCanvas(transform, "TreeInfoCanvas", 380);

            // Light dim — just enough to say "this is a popup", not enough to
            // black out the island behind it.
            dim = OrchardUITheme.MakeImage(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.2f)).gameObject;
            OrchardUITheme.Stretch(dim.GetComponent<RectTransform>());
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.targetGraphic = dim.GetComponent<Image>();
            dimBtn.onClick.AddListener(Hide);

            card = OrchardUITheme.MakeImage(canvas.transform, "Card", OrchardUITheme.PanelCream).gameObject;
            OrchardUITheme.Anchored(card.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(320f, 340f));

            dim.SetActive(false);
            card.SetActive(false);
        }

        void Populate(FruitType fruit)
        {
            for (int i = card.transform.childCount - 1; i >= 0; i--)
                Destroy(card.transform.GetChild(i).gameObject);

            var lesson = FruitLessonBook.Get(fruit);
            var matrix = FruitHealthMatrix.Get(fruit);

            // Header band in the fruit's own theme color
            var header = OrchardUITheme.MakeImage(card.transform, "Header", matrix.themeColor);
            OrchardUITheme.Place(header.rectTransform, 0f, 0.82f, 1f, 1f);

            var title = OrchardUITheme.MakeText(header.transform, "Title", 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Place(title.rectTransform, 0.06f, 0.16f, 0.94f, 0.86f);
            title.text = lesson.fruitName;

            var close = OrchardUITheme.MakeGreenButton(card.transform, "Close", "X", 18);
            OrchardUITheme.Place(close.GetComponent<RectTransform>(), 0.88f, 0.9f, 0.98f, 0.99f);
            var closeImg = close.GetComponent<Image>();
            if (closeImg != null) closeImg.color = new Color(0.55f, 0.4f, 0.32f, 1f);
            close.onClick.AddListener(Hide);

            // Real fruit image, circular backing plate
            var plate = OrchardUITheme.MakeImage(card.transform, "Plate", Color.white);
            OrchardUITheme.Anchored(plate.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(110f, 110f));

            var iconSp = OrchardUITheme.LoadSprite(BestAssets.FruitIconRoot + fruit);
            var icon = OrchardUITheme.MakeImage(plate.transform, "Icon", Color.white, iconSp);
            OrchardUITheme.Stretch(icon.rectTransform);
            icon.preserveAspect = true;

            // Virtue pill
            var pill = OrchardUITheme.MakeImage(card.transform, "VirtuePill", matrix.themeColor);
            OrchardUITheme.Anchored(pill.rectTransform, new Vector2(0.5f, 0.49f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(170f, 28f));
            var pillTxt = OrchardUITheme.MakeText(pill.transform, "T", 14, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Stretch(pillTxt.rectTransform);
            pillTxt.text = "Virtue: " + lesson.virtue;

            // Benefits body
            var body = OrchardUITheme.MakeText(card.transform, "Body", 15, FontStyle.Normal,
                TextAnchor.UpperLeft, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(body.rectTransform, 0.08f, 0.16f, 0.92f, 0.42f);
            body.text =
                "Feeling: " + lesson.feelings + "\n" +
                "Heals: " + lesson.healDetail + "\n" +
                "Body system: " + lesson.heals;
            body.lineSpacing = 1.1f;

            // Gesture badge at the bottom
            string g = FruitLessonBook.GestureShort(lesson.gesture).ToUpperInvariant();
            var gestureBg = OrchardUITheme.MakeImage(card.transform, "Gesture", new Color(0.2f, 0.6f, 0.3f, 1f));
            OrchardUITheme.Place(gestureBg.rectTransform, 0.14f, 0.03f, 0.86f, 0.13f);
            var gestureTxt = OrchardUITheme.MakeText(gestureBg.transform, "G", 16, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Stretch(gestureTxt.rectTransform);
            gestureTxt.text = g + " to harvest";

            dim.SetActive(true);
            card.SetActive(true);
        }

        void Hide()
        {
            if (dim != null) dim.SetActive(false);
            if (card != null) card.SetActive(false);
        }
    }
}

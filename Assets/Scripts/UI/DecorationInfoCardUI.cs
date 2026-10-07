using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>Small CoC-style "what is this" card for non-fruit world objects (rocks, bushes, pines...).</summary>
    public class DecorationInfoCardUI : MonoBehaviour
    {
        static DecorationInfoCardUI instance;

        Canvas canvas;
        GameObject dim;
        GameObject card;
        Text titleText;
        Text bodyText;

        public static void Show(string title, string description)
        {
            if (instance == null)
            {
                var go = new GameObject("DecorationInfoCardUI");
                instance = go.AddComponent<DecorationInfoCardUI>();
                instance.Build();
            }
            instance.Populate(title, description);
        }

        void Build()
        {
            canvas = OrchardUITheme.MakeCanvas(transform, "DecorationInfoCanvas", 370);

            dim = OrchardUITheme.MakeImage(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.2f)).gameObject;
            OrchardUITheme.Stretch(dim.GetComponent<RectTransform>());
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.targetGraphic = dim.GetComponent<Image>();
            dimBtn.onClick.AddListener(Hide);

            card = OrchardUITheme.MakeImage(canvas.transform, "Card", OrchardUITheme.PanelCream).gameObject;
            OrchardUITheme.Anchored(card.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(300f, 180f));

            titleText = OrchardUITheme.MakeText(card.transform, "Title", 20, FontStyle.Bold,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(titleText.rectTransform, 0.08f, 0.72f, 0.92f, 0.92f);

            bodyText = OrchardUITheme.MakeText(card.transform, "Body", 17, FontStyle.Normal,
                TextAnchor.UpperLeft, OrchardUITheme.InkMuted);
            OrchardUITheme.Place(bodyText.rectTransform, 0.1f, 0.24f, 0.9f, 0.68f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var close = OrchardUITheme.MakeGreenButton(card.transform, "Close", "OK", 18);
            OrchardUITheme.Place(close.GetComponent<RectTransform>(), 0.32f, 0.06f, 0.68f, 0.2f);
            close.onClick.AddListener(Hide);

            dim.SetActive(false);
            card.SetActive(false);
        }

        void Populate(string title, string description)
        {
            titleText.text = title;
            bodyText.text = description;
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

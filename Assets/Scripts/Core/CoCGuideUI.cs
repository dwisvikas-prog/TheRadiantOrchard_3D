using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // Clash-of-Clans style guide: bottom speech bubble + Next button.
    // Teaches pan/zoom, plots, harvest, edit/upgrade/collect.
    public class CoCGuideUI : MonoBehaviour
    {
        public const string SeenKey = "RO_CoCGuideSeen_v1";

        struct Step
        {
            public string title;
            public string body;
        }

        static readonly Step[] Steps =
        {
            new Step {
                title = "Welcome, Chief!",
                body  = "This is your Radiant Orchard village.\nGrey visitors need fruit tonics — heal them to grow Vibrancy!"
            },
            new Step {
                title = "Look around",
                body  = "Drag with one finger to pan.\nPinch (or scroll) to zoom in and out — just like Clash of Clans."
            },
            new Step {
                title = "Fruit plots",
                body  = "The 9 coloured plots are your fruit buildings.\nEach fruit heals a different need."
            },
            new Step {
                title = "Help a visitor",
                body  = "When a Stickman arrives, read their bubble,\nthen harvest the matching fruit with the right gesture."
            },
            new Step {
                title = "Village tools",
                body  = "E = Edit Mode (drag plots on the grid)\nU = Upgrade nearest plot\nC = Collect from nearest plot"
            },
            new Step {
                title = "You're ready!",
                body  = "Keep healing visitors, upgrade your plots,\nand fill the Vibrancy bar. Have fun, Chief!"
            },
        };

        Canvas canvas;
        Text titleText;
        Text bodyText;
        Text okLabel;
        Button nextBtn;
        int step;
        bool active;

        public static void Ensure()
        {
            if (FindFirstObjectByType<CoCGuideUI>() != null) return;
            var go = new GameObject("CoCGuideUI");
            go.AddComponent<CoCGuideUI>();
        }

        void Start()
        {
            if (PlayerPrefs.GetInt(SeenKey, 0) == 1)
            {
                Destroy(gameObject);
                return;
            }
            BuildUI();
            step = 0;
            active = true;
            ShowStep();
        }

        void BuildUI()
        {
            canvas = OrchardUITheme.MakeCanvas(transform, "CoCGuideCanvas", 500);

            var dim = OrchardUITheme.MakeImage(canvas.transform, "Dim", OrchardUITheme.DimOverlay);
            OrchardUITheme.Stretch(dim.rectTransform);

            var panel = OrchardUITheme.MakeImage(canvas.transform, "Panel", OrchardUITheme.PanelCream);
            OrchardUITheme.Place(panel.rectTransform, 0.04f, 0.11f, 0.96f, 0.30f);

            var accent = OrchardUITheme.MakeImage(panel.transform, "Accent", OrchardUITheme.AccentOrange);
            OrchardUITheme.Place(accent.rectTransform, 0f, 0f, 0.025f, 1f);

            titleText = OrchardUITheme.MakeText(panel.transform, "Title", 36, FontStyle.Bold,
                TextAnchor.UpperLeft, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(titleText.rectTransform, 0.06f, 0.55f, 0.70f, 0.92f);

            bodyText = OrchardUITheme.MakeText(panel.transform, "Body", 26, FontStyle.Normal,
                TextAnchor.UpperLeft, OrchardUITheme.InkMuted);
            OrchardUITheme.Place(bodyText.rectTransform, 0.06f, 0.12f, 0.68f, 0.58f);

            nextBtn = OrchardUITheme.MakeGreenButton(panel.transform, "NextBtn", "Next", 28);
            OrchardUITheme.Place(nextBtn.GetComponent<RectTransform>(), 0.72f, 0.22f, 0.96f, 0.78f);
            nextBtn.onClick.AddListener(OnNext);
            okLabel = nextBtn.GetComponentInChildren<Text>();
        }

        void ShowStep()
        {
            if (step < 0 || step >= Steps.Length) return;
            titleText.text = Steps[step].title;
            bodyText.text = Steps[step].body;
            okLabel.text = step >= Steps.Length - 1 ? "Let's go!" : "Next";
        }

        void OnNext()
        {
            step++;
            if (step >= Steps.Length)
            {
                Finish();
                return;
            }
            ShowStep();
        }

        void Finish()
        {
            active = false;
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            if (canvas != null) Destroy(canvas.gameObject);
            Destroy(gameObject);
            Debug.Log("[CoCGuide] Done — core orchard loop + village tools ready.");
        }

    }
}

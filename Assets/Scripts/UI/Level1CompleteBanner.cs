using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// Shown once, the moment the player has been taught all 9 fruits.
    /// Confirms Level 1 done and hands off to Level 2 (multi-visitor + build goal).
    /// </summary>
    public class Level1CompleteBanner : MonoBehaviour
    {
        static bool wired;

        public static void Wire()
        {
            if (wired) return;
            wired = true;
            EmptyIslandLevelProgress.OnLevel1Complete += Show;
        }

        static void Show()
        {
            SfxPlayer.Instance?.PlayLevelComplete();
            var go = new GameObject("Level1CompleteBanner");
            go.AddComponent<Level1CompleteBanner>().Build();
        }

        void Build()
        {
            var canvas = OrchardUITheme.MakeCanvas(transform, "Level1CompleteCanvas", 400);

            var dim = OrchardUITheme.MakeImage(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
            OrchardUITheme.Stretch(dim.rectTransform);

            var card = OrchardUITheme.MakeImage(canvas.transform, "Card", OrchardUITheme.PanelCream);
            OrchardUITheme.Anchored(card.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 260f));

            var title = OrchardUITheme.MakeText(card.transform, "Title", 26, FontStyle.Bold,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(title.rectTransform, 0.06f, 0.68f, 0.94f, 0.9f);
            title.text = "Level 1 Complete!";

            var body = OrchardUITheme.MakeText(card.transform, "Body", 18, FontStyle.Normal,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(body.rectTransform, 0.08f, 0.36f, 0.92f, 0.66f);
            body.text = "You learned all 9 fruits and their virtues.\n" +
                        "Level 2 starts now — more visitors at once,\n" +
                        "and you'll get to decorate the island.";

            var btn = OrchardUITheme.MakeGreenButton(card.transform, "Continue", "Start Level 2", 20);
            OrchardUITheme.Place(btn.GetComponent<RectTransform>(), 0.26f, 0.1f, 0.74f, 0.28f);
            btn.onClick.AddListener(Continue);
        }

        void Continue()
        {
            EmptyIslandLevelProgress.AdvanceToLevel2();
            Level2Intro.Show();
            Destroy(gameObject);
        }
    }
}

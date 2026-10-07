using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// Attractive CoC-style mood bubble above Stickman.
    /// Soft cream card + colored mood chip + tail dots — readable, cute, not a flat box.
    /// </summary>
    public class SymptomBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.45f, 0f);

        Transform canvasTransform;
        Image frameImg;
        Image fillImg;
        Image chipImg;
        Image glowImg;
        Text symptomText;
        Text hintText;
        Image[] tailDots;
        Camera cam;
        float bobPhase;
        bool visible;

        const float Scale = 0.0105f;
        const float CardW = 118f;
        const float CardH = 58f;

        void Awake()
        {
            cam = Camera.main;
            Build();
            SetVisible(false);
        }

        void Build()
        {
            var cvGO = new GameObject("BubbleCanvas", typeof(RectTransform), typeof(Canvas));
            cvGO.transform.SetParent(transform, false);
            cvGO.transform.localPosition = offset;
            cvGO.transform.localScale = Vector3.one * Scale;

            var cv = cvGO.GetComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            cv.overrideSorting = true;
            cv.sortingOrder = 220;
            canvasTransform = cvGO.transform;

            // Soft glow behind card
            glowImg = MakeImg(cvGO.transform, "Glow",
                new Color(1f, 0.85f, 0.45f, 0.22f), new Vector2(CardW + 18f, CardH + 16f), Vector2.zero);

            // Gold frame
            frameImg = MakeImg(cvGO.transform, "Frame",
                new Color(0.95f, 0.78f, 0.28f, 1f), new Vector2(CardW, CardH), Vector2.zero);

            // Cream fill inset
            fillImg = MakeImg(frameImg.transform, "Fill",
                new Color(1f, 0.98f, 0.94f, 0.98f),
                new Vector2(CardW - 6f, CardH - 6f), Vector2.zero);

            // Colored mood chip (top strip)
            chipImg = MakeImg(fillImg.transform, "MoodChip",
                new Color(0.95f, 0.45f, 0.4f, 1f),
                new Vector2(CardW - 18f, 18f), new Vector2(0f, 14f));
            var chipRt = chipImg.rectTransform;
            chipRt.sizeDelta = new Vector2(CardW - 18f, 18f);

            // Symptom (mood) on chip
            symptomText = MakeLabel(chipImg.transform, "Symptom", 15, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            Stretch(symptomText.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
            symptomText.text = "Needs help";

            // Hint under chip
            hintText = MakeLabel(fillImg.transform, "Hint", 12, FontStyle.Normal,
                TextAnchor.MiddleCenter, new Color(0.38f, 0.28f, 0.18f, 1f));
            Stretch(hintText.rectTransform, 0.06f, 0.06f, 0.94f, 0.42f);
            hintText.text = "";

            // Speech tail dots (thought-bubble style)
            float[] sizes = { 11f, 7.5f, 4.5f };
            float baseY = -CardH * 0.52f;
            tailDots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float s = sizes[i];
                tailDots[i] = MakeImg(cvGO.transform, "Tail" + i,
                    new Color(0.95f, 0.78f, 0.28f, 0.95f),
                    Vector2.one * s,
                    new Vector2(-6f - i * 5f, baseY - i * 7f));
            }

            // Tiny white inner on largest tail
            MakeImg(tailDots[0].transform, "TailFill",
                new Color(1f, 0.98f, 0.94f, 1f),
                Vector2.one * 7f, Vector2.zero);
        }

        static Image MakeImg(Transform parent, string name, Color c, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return img;
        }

        static Text MakeLabel(Transform parent, string name, int size, FontStyle style,
            TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = ResolveUiFont();
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            // Soft readable outline
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
            outline.effectDistance = new Vector2(0.6f, -0.6f);
            return t;
        }

        static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || canvasTransform == null) return;
            canvasTransform.rotation = cam.transform.rotation;

            if (!visible) return;
            // Gentle bob — lively, not distracting
            bobPhase += Time.deltaTime * 2.2f;
            float bob = Mathf.Sin(bobPhase) * 0.04f;
            canvasTransform.localPosition = offset + Vector3.up * bob;
        }

        public void SetSymptom(string symptomLabel, string healthHint, Color themeColor)
        {
            if (symptomText != null)
                symptomText.text = string.IsNullOrEmpty(symptomLabel) ? "Needs help" : symptomLabel;
            if (hintText != null)
                hintText.text = string.IsNullOrEmpty(healthHint) ? "" : healthHint;

            var c = themeColor;
            c.a = 1f;
            if (chipImg != null) chipImg.color = c;

            // Warm frame tint from mood
            if (frameImg != null)
                frameImg.color = Color.Lerp(new Color(0.95f, 0.78f, 0.28f, 1f), c, 0.35f);
            if (glowImg != null)
            {
                var g = Color.Lerp(c, Color.white, 0.45f);
                g.a = 0.28f;
                glowImg.color = g;
            }
            if (tailDots != null)
            {
                for (int i = 0; i < tailDots.Length; i++)
                {
                    if (tailDots[i] == null) continue;
                    var tc = Color.Lerp(new Color(0.95f, 0.78f, 0.28f, 1f), c, 0.4f);
                    tc.a = 0.95f;
                    tailDots[i].color = tc;
                }
            }
        }

        public void SetIcon(Sprite _) { }

        public void SetTint(Color color)
        {
            SetSymptom(symptomText != null ? symptomText.text : "",
                hintText != null ? hintText.text : "", color);
        }

        public void SetVisible(bool on)
        {
            visible = on;
            if (canvasTransform != null)
                canvasTransform.gameObject.SetActive(on);
            if (on) bobPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        static Font ResolveUiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) return font;
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null) return font;
            try { return Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 16); }
            catch { return null; }
        }
    }
}

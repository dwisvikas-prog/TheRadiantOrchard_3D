using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // Shared CoC / orchard look — cream panels, leaf green CTAs, warm ink text.
    public static class OrchardUITheme
    {
        public static readonly Color PanelCream   = new Color(0.99f, 0.96f, 0.88f, 0.96f);
        public static readonly Color PanelDark    = new Color(0.12f, 0.18f, 0.12f, 0.72f);
        public static readonly Color AccentOrange = new Color(0.92f, 0.48f, 0.12f, 1f);
        public static readonly Color ButtonGreen  = new Color(0.22f, 0.68f, 0.30f, 1f);
        public static readonly Color ButtonGreenHi = new Color(0.30f, 0.78f, 0.38f, 1f);
        public static readonly Color ButtonGreenPressed = new Color(0.14f, 0.48f, 0.20f, 1f);
        public static readonly Color InkBrown     = new Color(0.22f, 0.14f, 0.08f, 1f);
        public static readonly Color InkMuted     = new Color(0.32f, 0.24f, 0.16f, 1f);
        public static readonly Color VibrancyFill = new Color(0.35f, 0.88f, 0.42f, 1f);
        public static readonly Color BarTrack     = new Color(0.18f, 0.22f, 0.16f, 0.55f);
        public static readonly Color DimOverlay   = new Color(0f, 0f, 0f, 0.32f);

        public static readonly Vector2 RefResolution = new Vector2(1080f, 1920f);

        public static Font Font()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public static Sprite LoadSprite(string resourcesPath)
        {
            var s = Resources.Load<Sprite>(resourcesPath);
            if (s != null) return s;
            var tex = Resources.Load<Texture2D>(resourcesPath);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        public static Canvas MakeCanvas(Transform parent, string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = RefResolution;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Image MakeImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            return img;
        }

        public static Text MakeText(Transform parent, string name, int size, FontStyle style,
            TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Font();
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button MakeGreenButton(Transform parent, string name, string label, int fontSize = 28)
        {
            var img = MakeImage(parent, name, ButtonGreen);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = ButtonGreenHi;
            colors.pressedColor = ButtonGreenPressed;
            colors.selectedColor = ButtonGreen;
            btn.colors = colors;

            var t = MakeText(img.transform, "Label", fontSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            Stretch(t.rectTransform);
            t.text = label;
            return btn;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Anchored(RectTransform rt, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// CoC-style build timer — a radial progress ring + countdown seconds
    /// hovering over the sapling while the Wish Tree grows, instead of it
    /// just popping into existence.
    /// </summary>
    public class WishTreeGrowTimer : MonoBehaviour
    {
        Canvas canvas;
        Image ring;
        Text countText;
        Vector3 worldPos;
        Camera cam;

        public static WishTreeGrowTimer Show(Vector3 worldPos, float seconds)
        {
            var go = new GameObject("WishTreeGrowTimer");
            var t = go.AddComponent<WishTreeGrowTimer>();
            t.worldPos = worldPos;
            t.cam = Camera.main;
            t.Build();
            t.SetProgress(0f, seconds);
            return t;
        }

        void Build()
        {
            canvas = OrchardUITheme.MakeCanvas(transform, "GrowTimerCanvas", 150);
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140f, 140f);
            rt.localScale = Vector3.one * 0.01f;

            var track = OrchardUITheme.MakeImage(canvas.transform, "Track", new Color(0f, 0f, 0f, 0.35f));
            OrchardUITheme.Stretch(track.rectTransform);

            ring = OrchardUITheme.MakeImage(canvas.transform, "Ring", new Color(0.95f, 0.78f, 0.25f, 0.95f));
            OrchardUITheme.Place(ring.rectTransform, 0.1f, 0.1f, 0.9f, 0.9f);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;

            countText = OrchardUITheme.MakeText(canvas.transform, "Count", 44, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            OrchardUITheme.Stretch(countText.rectTransform);
            countText.text = "";
        }

        public void SetProgress(float t01, float secondsLeft)
        {
            if (ring != null) ring.fillAmount = Mathf.Clamp01(t01);
            if (countText != null) countText.text = Mathf.CeilToInt(secondsLeft).ToString();
        }

        public void Hide() => Destroy(gameObject);

        void LateUpdate()
        {
            transform.position = worldPos;
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}

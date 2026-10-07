using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// A small "+10" style number that pops up, drifts, and fades — the reward
    /// payoff CoC-style games always show so a gain never feels silent.
    /// </summary>
    public class FloatingNumberPopup : MonoBehaviour
    {
        const float Life = 0.9f;
        const float RiseHeight = 0.9f;

        TextMesh label;
        float t;
        Camera cam;

        public static void Show(Vector3 worldPos, string text, Color? color = null)
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("FloatingNumber");
            go.transform.position = worldPos;
            var p = go.AddComponent<FloatingNumberPopup>();
            p.Build(text, color ?? new Color(1f, 0.92f, 0.35f));
        }

        void Build(string text, Color color)
        {
            cam = Camera.main;
            label = gameObject.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 56;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            label.fontStyle = FontStyle.Bold;

            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 50;

            if (cam != null) transform.rotation = cam.transform.rotation;
        }

        void Update()
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Life);

            transform.position += Vector3.up * (RiseHeight * Time.deltaTime / Life);
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;

            if (label != null)
            {
                var c = label.color;
                c.a = 1f - Mathf.Pow(u, 2f);
                label.color = c;
                float scale = Mathf.Lerp(0.6f, 1f, Mathf.Min(1f, t / 0.15f));
                transform.localScale = Vector3.one * scale;
            }

            if (t >= Life) Destroy(gameObject);
        }
    }
}

using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>Marks a non-fruit object (rock, bush, pine, reward decoration) as tappable for a short info card.</summary>
    public class DecorationInfoTarget : MonoBehaviour
    {
        public string title;
        public string description;

        /// <summary>Adds the marker + a tap collider (sized to the renderer bounds) if missing.</summary>
        public static void Ensure(GameObject go, string title, string description)
        {
            if (go == null) return;
            var t = go.GetComponent<DecorationInfoTarget>();
            if (t == null) t = go.AddComponent<DecorationInfoTarget>();
            t.title = title;
            t.description = description;

            if (go.GetComponentInChildren<Collider>() != null) return;

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            var col = go.AddComponent<BoxCollider>();
            col.center = go.transform.InverseTransformPoint(b.center);
            col.size = b.size;
        }
    }
}

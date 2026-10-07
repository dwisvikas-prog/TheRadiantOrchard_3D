using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Makes the fruit itself shine/pulse to say "tap/swipe/hold me" — a
    /// separate glow halo + gentle scale-breathe, never touching the fruit's
    /// own material (editing a real imported model's material at runtime was
    /// turning it pink — a missing/stripped shader variant for the emission
    /// keyword we were enabling).
    /// </summary>
    public class FruitGlowHighlight : MonoBehaviour
    {
        Vector3 baseScale = Vector3.one;
        Transform fruit;
        GameObject halo;
        Material haloMat;

        static FruitGlowHighlight active;

        public static void Show(Transform fruit)
        {
            HideAll();
            if (fruit == null) return;

            var go = fruit.gameObject.AddComponent<FruitGlowHighlight>();
            go.fruit = fruit;
            go.baseScale = fruit.localScale;
            go.Setup();
            active = go;
        }

        public static void HideAll()
        {
            if (active != null) Destroy(active);
            active = null;
        }

        void Setup()
        {
            // Always-available built-in shader — no pipeline/shader-stripping risk.
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            haloMat = new Material(shader);

            halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            halo.name = "GlowHalo";
            Destroy(halo.GetComponent<Collider>());
            halo.transform.SetParent(fruit, false);
            halo.transform.localPosition = Vector3.zero;
            halo.transform.localScale = Vector3.one * 1.8f;

            var mr = halo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = haloMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        void Update()
        {
            if (fruit == null) { Destroy(this); return; }

            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.5f; // 0..1
            fruit.localScale = baseScale * (1f + pulse * 0.14f);

            if (halo != null)
            {
                var cam = Camera.main;
                if (cam != null) halo.transform.rotation = cam.transform.rotation;
                float s = 1.6f + pulse * 0.5f;
                halo.transform.localScale = Vector3.one * s;
            }
            if (haloMat != null)
                haloMat.color = new Color(1f, 0.92f, 0.4f, 0.25f + pulse * 0.35f);
        }

        void OnDestroy()
        {
            if (fruit != null) fruit.localScale = baseScale;
            if (halo != null) Destroy(halo);
            if (haloMat != null) Destroy(haloMat);
        }
    }
}

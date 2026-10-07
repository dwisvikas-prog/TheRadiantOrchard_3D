using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RadiantOrchard
{
    // Sends a spawned tonic along a quadratic Bezier from its start position
    // to the target Stickman, then invokes the callback and either returns
    // itself to the pool (placeholder tonics, see below) or destroys itself
    // (a real tonicPrefab assigned in the Inspector, once that art exists).
    public class TonicFlight : MonoBehaviour
    {
        public float flightDuration = 0.8f;
        public float arcHeight = 2f;

        // Sideways offset applied to the Bezier's control point, randomized per
        // flight — without this every tonic traces the exact same line, which
        // reads as robotic once several Stickmen are being helped at once.
        public float lateralVariance = 0.5f;

        private bool pooled;

        public void FlyTo(Vector3 target, System.Action onComplete)
        {
            StartCoroutine(FlightRoutine(target, onComplete));
        }

        private IEnumerator FlightRoutine(Vector3 target, System.Action onComplete)
        {
            Vector3 start = transform.position;
            Vector3 control = BuildControlPoint(start, target);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / flightDuration;
                transform.position = QuadraticBezier(start, control, target, Mathf.Clamp01(t));
                yield return null;
            }
            transform.position = target;
            SfxPlayer.Instance?.PlayTonicPop();
            onComplete?.Invoke();

            if (pooled) ReturnToPool(this);
            else Destroy(gameObject);
        }

        // Midpoint between start/target, raised by arcHeight and nudged
        // sideways by a random amount — one control point is enough for a
        // quadratic Bezier to read as a real arc rather than a straight lerp.
        private Vector3 BuildControlPoint(Vector3 start, Vector3 target)
        {
            Vector3 mid = Vector3.Lerp(start, target, 0.5f);
            mid.y += arcHeight;

            Vector3 flat = new Vector3(target.x - start.x, 0f, target.z - start.z);
            if (flat.sqrMagnitude > 0.0001f)
            {
                Vector3 side = Vector3.Cross(flat.normalized, Vector3.up);
                mid += side * Random.Range(-lateralVariance, lateralVariance);
            }

            return mid;
        }

        private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        // --- Placeholder-tonic pool ------------------------------------------
        // Every fruit harvest used to Instantiate a brand-new primitive + a
        // brand-new Material (via BuildPlaceholderTonic) and Destroy() it
        // ~1-2s later — the highest-frequency Instantiate/Destroy churn in the
        // current build (Issue Register: Object pooling). GetPooled/ReturnToPool
        // replace that with a reused instance; color still varies per fruit via
        // a MaterialPropertyBlock over one shared material, so pooled tonics
        // keep batching with each other too.

        private static readonly Stack<TonicFlight> pool = new Stack<TonicFlight>();
        private static Material sharedMaterial;

        public static TonicFlight GetPooled(Vector3 position, Color color)
        {
            TonicFlight flight;
            if (pool.Count > 0)
            {
                flight = pool.Pop();
                flight.transform.position = position;
                flight.gameObject.SetActive(true);
            }
            else
            {
                flight = BuildTemplate(position);
            }

            flight.SetPlaceholderColor(color);
            return flight;
        }

        private static void ReturnToPool(TonicFlight flight)
        {
            flight.gameObject.SetActive(false);
            pool.Push(flight);
        }

        private static TonicFlight BuildTemplate(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Tonic (Pooled)";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.3f;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = GetSharedMaterial();

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0.02f;
            trail.material = GetSharedMaterial();
            trail.minVertexDistance = 0.05f;

            var flight = go.AddComponent<TonicFlight>();
            flight.pooled = true;
            return flight;
        }

        private static Material GetSharedMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;
            bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
            sharedMaterial = new Material(shader) { enableInstancing = true };
            return sharedMaterial;
        }

        private void SetPlaceholderColor(Color color)
        {
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);

            var rend = GetComponent<Renderer>();
            if (rend != null) rend.SetPropertyBlock(mpb);

            var trail = GetComponent<TrailRenderer>();
            if (trail != null) trail.SetPropertyBlock(mpb);
        }
    }
}

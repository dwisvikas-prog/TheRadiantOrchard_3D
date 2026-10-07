using System.Collections;
using UnityEngine;

namespace RadiantOrchard
{
    // Smooth CoC-style camera tracks to story / action / visitor targets.
    public static class EmptyIslandCameraFocus
    {
        /// <summary>Quick punch on an impact moment (heal complete, etc.).</summary>
        public static void Shake(float duration = 0.15f, float magnitude = 0.18f)
        {
            var d = GetDiorama();
            if (d != null) d.Shake(duration, magnitude);
        }

        public static void FocusWell()
        {
            var well = StoneWellSetup.FindWell();
            if (well != null) FocusSmooth(well.position + Vector3.up * 0.5f, 14f, 1.0f);
        }

        public static void FocusVisitor()
        {
            var t = FindBestVisitor();
            if (t != null) FocusVisitor(t);
            else FocusWell();
        }

        public static void FocusVisitor(Transform visitor)
        {
            if (visitor == null) { FocusWell(); return; }
            FocusSmooth(visitor.position + Vector3.up * 0.85f, 11.5f, 1.2f);
        }

        public static void FocusPlantSpot()
        {
            var spot = GameObject.Find("FirstPlantSpot");
            if (spot != null) FocusSmooth(spot.transform.position + Vector3.up * 0.4f, 12f);
            else FocusVisitor();
        }

        public static void Focus(Vector3 world, float zoom)
        {
            FocusSmooth(world, zoom, 1.0f);
        }

        /// <summary>Smooth glide to click/gesture target (no snap / no jitter).</summary>
        public static void FocusSmooth(Vector3 world, float zoom = 10.5f, float duration = 1.15f)
        {
            var d = GetDiorama();
            if (d == null) return;
            float z = zoom > 0f ? zoom : 10.5f;
            d.FocusOn(world, z, Mathf.Max(0.6f, duration));
        }

        public static void FocusHard(Vector3 world, float zoom = 10.5f)
        {
            FocusSmooth(world, zoom, 1.2f);
        }

        /// <summary>
        /// Keep camera glued to visitor for the whole walk-in, then settle.
        /// </summary>
        public static IEnumerator TrackWalkingVisitor(Transform visitor, Vector3 endPos, float walkApproxSeconds = 2.5f)
        {
            if (visitor == null) yield break;
            var d = GetDiorama();
            if (d == null) yield break;

            d.StartFollow(visitor, 12f);

            float elapsed = 0f;
            float maxTime = Mathf.Max(4f, walkApproxSeconds + 2f);
            const float arriveDist = 0.4f;

            while (visitor != null && elapsed < maxTime)
            {
                Vector3 flat = visitor.position;
                flat.y = 0f;
                Vector3 end = endPos;
                end.y = 0f;
                if ((flat - end).sqrMagnitude <= arriveDist * arriveDist)
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Hold on them a beat after they stop
            yield return new WaitForSeconds(0.7f);
            d.StopFollow();

            if (visitor != null)
                FocusSmooth(visitor.position + Vector3.up * 0.85f, 11.5f, 0.75f);
            else
                FocusSmooth(endPos + Vector3.up * 0.85f, 11.5f, 0.75f);
        }

        static Transform FindBestVisitor()
        {
            var first = GameObject.Find(FirstVisitorArrival.VisitorName);
            if (first != null) return first.transform;

            // Latest Visitor_* (new arrivals) — prefer higher numeric suffix
            StickmanController best = null;
            int bestScore = int.MinValue;
            var all = Object.FindObjectsByType<StickmanController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                string n = all[i].name;
                if (!(n.StartsWith("Visitor_") || n == FirstVisitorArrival.VisitorName))
                    continue;

                int score = 0;
                int us = n.LastIndexOf('_');
                if (us >= 0 && us + 1 < n.Length)
                    int.TryParse(n.Substring(us + 1), out score);

                if (best == null || score >= bestScore)
                {
                    best = all[i];
                    bestScore = score;
                }
            }
            return best != null ? best.transform : null;
        }

        static DioramaController GetDiorama()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            return cam.GetComponent<DioramaController>();
        }
    }
}

using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Keeps standing/roaming stickmen from stacking on top of each other.
    /// Skips anyone currently mid-WalkTo (that coroutine owns their position
    /// outright each frame — pushing them there would just get overwritten
    /// next frame) so it only ever nudges idle/waiting/roaming ones apart.
    /// </summary>
    public class StickmanSeparation : MonoBehaviour
    {
        const float MinDistance = 1.35f;
        const float PushPerSecond = 1.0f; // gentle — no jitter/snapping

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<StickmanSeparation>() != null) return;
            new GameObject("StickmanSeparation").AddComponent<StickmanSeparation>();
        }

        void LateUpdate()
        {
            var all = FindObjectsByType<StickmanController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < all.Length; i++)
            {
                var a = all[i];
                if (a == null || StickmanGentleWalk.Walking.Contains(a.transform)) continue;

                for (int j = i + 1; j < all.Length; j++)
                {
                    var b = all[j];
                    if (b == null || StickmanGentleWalk.Walking.Contains(b.transform)) continue;

                    Vector3 pa = a.transform.position; pa.y = 0f;
                    Vector3 pb = b.transform.position; pb.y = 0f;
                    Vector3 delta = pa - pb;
                    float dist = delta.magnitude;
                    if (dist >= MinDistance || dist < 0.0001f) continue;

                    Vector3 dir = delta / dist;
                    float overlap = MinDistance - dist;
                    float step = Mathf.Min(overlap * 0.5f, PushPerSecond * Time.deltaTime);
                    a.transform.position += dir * step;
                    b.transform.position -= dir * step;
                }
            }
        }

        /// <summary>
        /// Pick a spot near `desired` that keeps min distance from every
        /// active stickman — used when choosing where a new visitor should
        /// walk to / spawn, so they don't arrive stacked on someone else.
        /// </summary>
        public static Vector3 FindFreeSpot(Vector3 desired, float minDist = 1.8f)
        {
            desired.y = 0f;
            if (IsFree(desired, minDist)) return desired;

            for (int ring = 1; ring <= 6; ring++)
            {
                float radius = ring * (minDist * 0.6f);
                const int samples = 8;
                for (int s = 0; s < samples; s++)
                {
                    float ang = (360f / samples) * s * Mathf.Deg2Rad;
                    Vector3 candidate = desired + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radius;
                    if (IsFree(candidate, minDist)) return candidate;
                }
            }
            return desired;
        }

        static bool IsFree(Vector3 p, float minDist)
        {
            var all = FindObjectsByType<StickmanController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                Vector3 q = all[i].transform.position; q.y = 0f;
                if ((p - q).sqrMagnitude < minDist * minDist) return false;
            }
            return true;
        }
    }
}

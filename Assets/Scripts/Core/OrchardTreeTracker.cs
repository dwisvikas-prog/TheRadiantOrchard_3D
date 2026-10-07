using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Tracks every planted fruit tree on Empty Island.
    /// Trees stay far from well / wish tree / each other — still tracked.
    /// </summary>
    public class OrchardTreeTracker : MonoBehaviour
    {
        public class PlantedTree
        {
            public FruitType fruit;
            public Transform root;
            public Vector3 position;
        }

        readonly List<PlantedTree> planted = new List<PlantedTree>();
        // Keep fruit trees well apart — CoC orchard feel, not a cluster
        static readonly float MinTreeSpacing = 11f;
        static readonly float MinWellDist = 9.5f;
        static readonly float MinWishDist = 8f;

        public static OrchardTreeTracker Ensure()
        {
            var t = FindFirstObjectByType<OrchardTreeTracker>();
            if (t != null) return t;
            var go = new GameObject("OrchardTreeTracker");
            return go.AddComponent<OrchardTreeTracker>();
        }

        public IReadOnlyList<PlantedTree> All => planted;

        public PlantedTree Find(FruitType fruit)
        {
            for (int i = 0; i < planted.Count; i++)
            {
                if (planted[i].fruit != fruit) continue;
                if (planted[i].root == null) continue;
                // Must still have a real FruitTree_ child (or be the tree itself)
                if (ResolveTreeVisual(planted[i].root) == null) continue;
                return planted[i];
            }
            return null;
        }

        public bool Has(FruitType fruit) => Find(fruit) != null;

        /// <summary>Actual tree mesh transform (not empty soil parent).</summary>
        public static Transform ResolveTreeVisual(Transform root)
        {
            if (root == null) return null;
            if (root.name.StartsWith("FruitTree_")) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c != null && c.name.StartsWith("FruitTree_")) return c;
            }
            return null;
        }

        public void Register(FruitType fruit, Transform root, Vector3 pos)
        {
            pos = EditModeManager.SnapToCoCTile(pos);
            // Prefer the FruitTree_ visual as tracked root so edit/move keeps the tree
            Transform track = ResolveTreeVisual(root) ?? root;
            for (int i = 0; i < planted.Count; i++)
            {
                if (planted[i].fruit == fruit)
                {
                    planted[i].root = track;
                    planted[i].position = pos;
                    if (track != null) track.position = pos;
                    EditModeManager.Ensure().RegisterTree(track);
                    return;
                }
            }
            planted.Add(new PlantedTree { fruit = fruit, root = track, position = pos });
            if (track != null)
            {
                track.position = pos;
                EditModeManager.Ensure().RegisterTree(track);
                var pick = FruitTreeCatalog.Get(fruit);
                TreeNameBoard.Ensure(track, pick.label, pick.fruitColor, fruit);
            }
        }

        /// <summary>After CoC edit drag — keep tracker in sync.</summary>
        public void NotifyMoved(Transform root, Vector3 pos)
        {
            pos = EditModeManager.SnapToCoCTile(pos);
            for (int i = 0; i < planted.Count; i++)
            {
                if (planted[i].root == root)
                {
                    planted[i].position = pos;
                    return;
                }
            }
        }

        Vector3 WellPos()
        {
            var well = GameObject.Find("StoneWell");
            return well != null ? well.transform.position : Vector3.zero;
        }

        Vector3 WishPos()
        {
            var wish = GameObject.Find("WisdomTree") ?? GameObject.Find("WishTree")
                       ?? GameObject.Find("WishTree_Green");
            return wish != null ? wish.transform.position : WellPos() + new Vector3(3f, 0f, 2f);
        }

        /// <summary>Far from well + wish + other trees; still on pad & preferably on screen.</summary>
        public Vector3 PickRandomSpot()
        {
            Vector3 center = WellPos();
            Vector3 wish = WishPos();
            var cam = Camera.main;
            Vector3 camForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (camForward.sqrMagnitude < 0.01f) camForward = Vector3.forward;
            Vector3 camRight = Vector3.Cross(Vector3.up, camForward).normalized;

            // Each new tree starts in a different pie-slice around the well
            float slotAng = planted.Count * 67f;

            for (int attempt = 0; attempt < 90; attempt++)
            {
                float ang = (slotAng + attempt * 29f + Random.Range(-12f, 12f)) * Mathf.Deg2Rad;
                // Outer ring — far from well AND from each other
                float rad = Random.Range(14f, 20.5f);
                Vector3 p = center + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);

                // Mild camera bias only (don't pull trees into a cluster)
                if (cam != null && attempt < 25)
                {
                    Vector3 camFlat = cam.transform.position;
                    camFlat.y = 0f;
                    Vector3 viewPt = camFlat + camForward * Random.Range(12f, 18f) +
                                     camRight * Random.Range(-10f, 10f);
                    p = Vector3.Lerp(p, viewPt, 0.22f);
                }
                p.y = 0f;
                p = EditModeManager.SnapToCoCTile(p);

                if (!IsFarEnough(p, center, wish, MinTreeSpacing)) continue;
                if (Mathf.Abs(p.x - center.x) > 21f || Mathf.Abs(p.z - center.z) > 21f) continue;

                if (cam != null && attempt < 50)
                {
                    var sp = cam.WorldToScreenPoint(p + Vector3.up * 0.5f);
                    if (sp.z < 3f || sp.x < Screen.width * 0.05f || sp.x > Screen.width * 0.95f ||
                        sp.y < Screen.height * 0.12f || sp.y > Screen.height * 0.82f)
                        continue;
                }
                return p;
            }

            // Fallback: pick the candidate farthest from every existing tree
            return PickFarthestFallback(center, wish);
        }

        Vector3 PickFarthestFallback(Vector3 center, Vector3 wish)
        {
            Vector3 best = center + new Vector3(16f, 0f, 0f);
            float bestScore = -1f;
            int n = Mathf.Max(1, planted.Count);
            for (int i = 0; i < 36; i++)
            {
                float ang = (i * (360f / 36f) + n * 17f) * Mathf.Deg2Rad;
                float rad = 15f + (i % 4) * 1.6f;
                Vector3 p = center + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                p = EditModeManager.SnapToCoCTile(p);
                p.y = 0f;
                if (Vector3.Distance(p, center) < MinWellDist) continue;
                if (Vector3.Distance(p, wish) < MinWishDist) continue;
                if (Mathf.Abs(p.x - center.x) > 21f || Mathf.Abs(p.z - center.z) > 21f) continue;

                float minDist = 999f;
                for (int t = 0; t < planted.Count; t++)
                {
                    if (planted[t].root == null) continue;
                    minDist = Mathf.Min(minDist, Vector3.Distance(p, planted[t].position));
                }
                if (minDist > bestScore)
                {
                    bestScore = minDist;
                    best = p;
                }
            }
            return best;
        }

        /// <summary>Decoration spots — also far from trees/well.</summary>
        public Vector3 PickDecorationSpot()
        {
            Vector3 center = WellPos();
            Vector3 wish = WishPos();
            for (int i = 0; i < 40; i++)
            {
                float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float rad = Random.Range(8f, 16f);
                Vector3 p = center + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                p.y = 0f;
                if (IsFarEnough(p, center, wish, 7f)) return p;
            }
            float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Cos(a) * 12f, 0f, Mathf.Sin(a) * 12f);
        }

        bool IsFarEnough(Vector3 p, Vector3 well, Vector3 wish, float treeMin)
        {
            if (Vector3.Distance(p, well) < MinWellDist) return false;
            if (Vector3.Distance(p, wish) < MinWishDist) return false;
            for (int i = 0; i < planted.Count; i++)
            {
                if (planted[i].root == null) continue;
                if (Vector3.Distance(p, planted[i].position) < treeMin) return false;
            }
            return true;
        }
    }
}

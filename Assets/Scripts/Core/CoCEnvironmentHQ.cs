using UnityEngine;

namespace RadiantOrchard
{
    // Upgrades Empty Island primitives → real HQ prefabs already in the project:
    // Waldemarst Sakura (Wish Tree), Ultimate Nature spruce/water/rocks, Polytope pine.
    public static class CoCEnvironmentHQ
    {
        const string Tag = "CoC_HQ_Applied_v2"; // v2 = same trees as newmvp (ALP oak + Polytope)

        public static void Apply(Transform worldRoot)
        {
            if (worldRoot == null) return;
            var oldMark = worldRoot.Find("CoC_HQ_Applied");
            if (oldMark != null) DestroyGo(oldMark.gameObject);
            if (worldRoot.Find(Tag) != null) return;

            var marker = new GameObject(Tag);
            marker.transform.SetParent(worldRoot, false);

            ReplaceWishTree(worldRoot);
            ReplaceOuterForest(worldRoot);
            ReplaceWater(worldRoot);
            ScatterShoreDetails(worldRoot);

            Debug.Log("[CoCEnvironmentHQ] Same trees as other scenes: ALP Oak wish + Polytope fruit/pine forest.");
        }

        static void ReplaceWishTree(Transform worldRoot)
        {
            var marks = worldRoot.Find(CoCBlankGround.LandmarksName);
            if (marks == null) return;

            var old = marks.Find("WisdomTree");
            Vector3 pos = old != null ? old.position : new Vector3(0f, 0f, 1.5f);
            if (old != null) DestroyGo(old.gameObject);

            var prefab = Resources.Load<GameObject>(BestAssets.WishTree);
            if (prefab == null) return;

            var tree = Object.Instantiate(prefab, marks);
            tree.name = "WisdomTree";
            tree.transform.position = pos;
            FitHeight(tree, 2.8f); // same ALP oak as other scenes, CoC-scaled height
            EnsureGlow(tree);
        }

        static void ReplaceOuterForest(Transform worldRoot)
        {
            var oldForest = worldRoot.Find("OuterForest");
            if (oldForest != null) DestroyGo(oldForest.gameObject);

            var forest = new GameObject("OuterForest").transform;
            forest.SetParent(worldRoot, false);

            // Best curated Polytope + bush set (see BestAssets)
            var pool = new[]
            {
                Resources.Load<GameObject>(BestAssets.ForestPine),
                Resources.Load<GameObject>(BestAssets.ForestGreen),
                Resources.Load<GameObject>(BestAssets.ForestApple),
                Resources.Load<GameObject>(BestAssets.ForestPear),
                Resources.Load<GameObject>(BestAssets.ForestPlum),
                Resources.Load<GameObject>(BestAssets.Bush),
            };

            int valid = 0;
            foreach (var p in pool) if (p != null) valid++;
            if (valid == 0) return;

            float ring = CoCBlankGround.PadHalf + CoCBlankGround.FringeExtra + 5.5f;
            const int count = 42;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                Vector3 p = SquareRing(ring + Random.Range(0f, 7f), t);
                p.x += Random.Range(-1f, 1f);
                p.z += Random.Range(-1f, 1f);
                if (Mathf.Abs(p.x) < CoCBlankGround.PadHalf + 0.5f &&
                    Mathf.Abs(p.z) < CoCBlankGround.PadHalf + 0.5f)
                    continue;

                GameObject prefab = null;
                for (int tries = 0; tries < 8 && prefab == null; tries++)
                    prefab = pool[Random.Range(0, pool.Length)];
                if (prefab == null) continue;

                var tree = Object.Instantiate(prefab, forest);
                tree.name = "HQTree_" + i;
                tree.transform.localPosition = p;
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                bool bush = prefab.name.IndexOf("Bush", System.StringComparison.OrdinalIgnoreCase) >= 0;
                // Match orchard scene look — fruit/pine ~1.6–2.2 tiles on CoC pad
                FitHeight(tree, bush ? Random.Range(0.7f, 1.1f) : Random.Range(1.5f, 2.3f));
            }
        }

        static void ReplaceWater(Transform worldRoot)
        {
            var old = worldRoot.Find("WaterHint");
            if (old != null) DestroyGo(old.gameObject);

            var waterPrefab = Resources.Load<GameObject>(BestAssets.Water);
            var waterRoot = new GameObject("Water").transform;
            waterRoot.SetParent(worldRoot, false);

            float z = -(CoCBlankGround.PadHalf + CoCBlankGround.FringeExtra + 12f);

            if (waterPrefab != null)
            {
                for (int i = -2; i <= 2; i++)
                {
                    var w = Object.Instantiate(waterPrefab, waterRoot);
                    w.name = "RiverPatch_" + i;
                    w.transform.localPosition = new Vector3(i * 10f, -0.85f, z);
                    w.transform.localScale = new Vector3(2.2f, 1f, 1.6f);
                }
                return;
            }

            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "RiverFallback";
            plane.transform.SetParent(waterRoot, false);
            plane.transform.localPosition = new Vector3(0f, -0.9f, z);
            plane.transform.localScale = new Vector3(6f, 1f, 2.2f);
            var col = plane.GetComponent<Collider>();
            if (col != null) DestroyGo(col.gameObject == plane ? null : null);
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            var mr = plane.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var m = new Material(sh);
                var c = new Color(0.15f, 0.45f, 0.72f, 0.85f);
                m.color = c;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.92f);
                mr.sharedMaterial = m;
            }
        }

        static void ScatterShoreDetails(Transform worldRoot)
        {
            var rocks = new GameObject("ShoreDetails").transform;
            rocks.SetParent(worldRoot, false);

            var rockA = Resources.Load<GameObject>(BestAssets.Rock1);
            var rockB = Resources.Load<GameObject>(BestAssets.Rock2);
            var bush = Resources.Load<GameObject>(BestAssets.Bush);
            if (rockA == null && rockB == null && bush == null) return;

            float z = -(CoCBlankGround.PadHalf + CoCBlankGround.FringeExtra + 8f);
            for (int i = 0; i < 18; i++)
            {
                float x = Random.Range(-CoCBlankGround.PadHalf - 2f, CoCBlankGround.PadHalf + 2f);
                float zz = z + Random.Range(-4f, 3f);
                GameObject prefab = (i % 3 == 0) ? bush : ((i % 2 == 0) ? rockA : rockB);
                if (prefab == null) prefab = rockA != null ? rockA : (rockB != null ? rockB : bush);
                if (prefab == null) continue;

                var go = Object.Instantiate(prefab, rocks);
                go.name = "Shore_" + i;
                go.transform.localPosition = new Vector3(x, 0f, zz);
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                bool isBush = prefab.name.IndexOf("Bush", System.StringComparison.OrdinalIgnoreCase) >= 0;
                FitHeight(go, isBush ? Random.Range(0.5f, 0.9f) : Random.Range(0.35f, 0.7f));
            }
        }

        static void FitHeight(GameObject go, float targetHeight)
        {
            if (go == null) return;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0)
            {
                go.transform.localScale = Vector3.one * Mathf.Max(0.2f, targetHeight * 0.35f);
                return;
            }

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float h = Mathf.Max(0.01f, b.size.y);
            float s = targetHeight / h;
            go.transform.localScale *= s;
        }

        static void EnsureGlow(GameObject tree)
        {
            if (tree.GetComponentInChildren<Light>() != null) return;
            var glow = new GameObject("WishGlow");
            glow.transform.SetParent(tree.transform, false);
            glow.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.95f);
            light.intensity = 1.35f;
            light.range = 6f;
        }

        static Vector3 SquareRing(float half, float t01)
        {
            float peri = half * 8f;
            float d = t01 * peri;
            float side = half * 2f;
            if (d < side) return new Vector3(-half + d, 0f, -half);
            d -= side;
            if (d < side) return new Vector3(half, 0f, -half + d);
            d -= side;
            if (d < side) return new Vector3(half - d, 0f, half);
            d -= side;
            return new Vector3(-half, 0f, half - d);
        }

        static void DestroyGo(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }
    }
}

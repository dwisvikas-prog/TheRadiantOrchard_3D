using UnityEngine;

namespace RadiantOrchard
{
    // Clash-of-Clans style EMPTY orchard pad (PRD Phase 1):
    // colored checkered grass + fringe + outer forest.
    // Only two permanent landmarks: Stone Well + Wisdom Tree.
    public static class CoCBlankGround
    {
        public const string RootName = "CoC_BlankWorld";
        public const string LandmarksName = "Landmarks";
        // Clash of Clans home village is a FIXED 44×44 tile grid (not device-scaled).
        // 1 world unit = 1 tile → full pad side = 44, half = 22.
        public const float TileCount = 44f;
        public const float PadHalf = TileCount * 0.5f; // 22
        public const float FringeExtra = 2f;
        public const float PadY = 0.05f;

        public static Bounds Build(Vector3 origin)
        {
            var old = GameObject.Find(RootName);
            if (old != null)
            {
                if (Application.isPlaying) Object.Destroy(old);
                else Object.DestroyImmediate(old);
            }

            var root = new GameObject(RootName);
            root.transform.position = origin;

            BuildPlayablePad(root.transform);
            BuildDarkFringe(root.transform);
            BuildOuterForest(root.transform);
            BuildWaterHint(root.transform);
            BuildEmptyIslandLandmarks(root.transform);
            root.AddComponent<EmptyIslandBootstrap>();
            CoCEnvironmentHQ.Apply(root.transform);

            float side = (PadHalf + FringeExtra + 8f) * 2f;
            Debug.Log($"[CoCBlankGround] Empty Island {PadHalf * 2f:F0}x{PadHalf * 2f:F0} — Well + Wisdom Tree only.");
            return new Bounds(origin + Vector3.up * 2f, new Vector3(side, 12f, side));
        }

        static void BuildPlayablePad(Transform parent)
        {
            var pad = Prim(parent, "PlayableGrass", PrimitiveType.Cube,
                new Vector3(0f, PadY - 0.15f, 0f),
                new Vector3(PadHalf * 2f, 0.3f, PadHalf * 2f),
                Color.white);
            var mr = pad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = CheckerGrassMaterial();
        }

        static void BuildDarkFringe(Transform parent)
        {
            float half = PadHalf + FringeExtra;
            // Dark green embankment under / around pad (slightly lower)
            Prim(parent, "DarkFringe", PrimitiveType.Cube,
                new Vector3(0f, PadY - 0.35f, 0f),
                new Vector3(half * 2f, 0.5f, half * 2f),
                new Color(0.18f, 0.42f, 0.16f));
        }

        static void BuildOuterForest(Transform parent)
        {
            var forest = Child(parent, "OuterForest", Vector3.zero);
            float ring = PadHalf + FringeExtra + 6f;
            var canopy = new Color(0.18f, 0.72f, 0.22f);
            var trunk = new Color(0.35f, 0.22f, 0.12f);

            // CoC-scale bush/trees (~1 tile). 1 world unit = 1 CoC tile.
            int count = 56;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                Vector3 p = SquareRingPoint(ring + Random.Range(0f, 8f), t);
                p.x += Random.Range(-0.8f, 0.8f);
                p.z += Random.Range(-0.8f, 0.8f);
                if (Mathf.Abs(p.x) < PadHalf + 1f && Mathf.Abs(p.z) < PadHalf + 1f) continue;

                var tree = Child(forest, "Tree_" + i, p);
                float h = Random.Range(0.9f, 1.6f);
                float canopyS = Random.Range(0.85f, 1.25f);
                Prim(tree, "Trunk", PrimitiveType.Cylinder,
                    new Vector3(0f, h * 0.28f, 0f),
                    new Vector3(0.12f, h * 0.28f, 0.12f), trunk);
                Prim(tree, "Canopy", PrimitiveType.Sphere,
                    new Vector3(0f, h * 0.75f, 0f),
                    Vector3.one * canopyS, canopy);
            }
        }

        // PRD Phase 1 — empty island: only Stone Well + Wisdom Tree on the pad.
        static void BuildEmptyIslandLandmarks(Transform parent)
        {
            var marks = Child(parent, LandmarksName, Vector3.zero);
            BuildWisdomTree(marks, new Vector3(0f, 0f, 1.5f));
            BuildStoneWell(marks, new Vector3(0f, 0f, -3.5f));
        }

        // Editor / runtime: kill oversized oak prefab and place CoC-scale tree.
        public static void ReplaceWisdomTreeCoCScale()
        {
            var root = GameObject.Find(RootName);
            Transform parent = root != null
                ? root.transform.Find(LandmarksName)
                : null;
            if (parent == null)
            {
                var existing = GameObject.Find("WisdomTree");
                parent = existing != null ? existing.transform.parent : null;
            }
            if (parent == null)
            {
                Debug.LogWarning("[CoCBlankGround] No Landmarks / WisdomTree parent found.");
                return;
            }

            // Destroy ALL WisdomTree / oak instances under landmarks (and loose)
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i);
                if (c.name == "WisdomTree" || c.name.Contains("Oak") || c.name.Contains("oak"))
                {
                    if (Application.isPlaying) Object.Destroy(c.gameObject);
                    else Object.DestroyImmediate(c.gameObject);
                }
            }
            var loose = GameObject.Find("WisdomTree");
            if (loose != null)
            {
                if (Application.isPlaying) Object.Destroy(loose);
                else Object.DestroyImmediate(loose);
            }

            BuildWisdomTree(parent, new Vector3(0f, 0f, 1.5f));
            Debug.Log("[CoCBlankGround] WisdomTree replaced with CoC-scale tree (~1.5 tiles).");
        }

        public static void BuildWisdomTree(Transform parent, Vector3 pos)
        {
            // CoC landmark tree — a bit taller than fringe trees, NOT a giant oak.
            // Footprint ~1.5 tiles, height ~2.2 tiles.
            var tree = Child(parent, "WisdomTree", pos);
            var trunk = new Color(0.38f, 0.24f, 0.14f);
            var barkDark = new Color(0.28f, 0.16f, 0.10f);
            var canopy = new Color(0.18f, 0.72f, 0.22f);
            var canopyDeep = new Color(0.12f, 0.58f, 0.18f);
            var moss = new Color(0.22f, 0.65f, 0.25f);

            Prim(tree, "Trunk", PrimitiveType.Cylinder,
                new Vector3(0f, 0.7f, 0f), new Vector3(0.28f, 0.7f, 0.28f), trunk);
            Prim(tree, "TrunkBase", PrimitiveType.Cylinder,
                new Vector3(0f, 0.16f, 0f), new Vector3(0.42f, 0.16f, 0.42f), barkDark);
            Prim(tree, "Canopy_Main", PrimitiveType.Sphere,
                new Vector3(0f, 1.75f, 0f), Vector3.one * 1.55f, canopy);
            Prim(tree, "Canopy_L", PrimitiveType.Sphere,
                new Vector3(-0.55f, 1.55f, 0.15f), Vector3.one * 0.95f, canopyDeep);
            Prim(tree, "Canopy_R", PrimitiveType.Sphere,
                new Vector3(0.5f, 1.6f, -0.1f), Vector3.one * 0.9f, canopy);
            Prim(tree, "RootMoss", PrimitiveType.Cylinder,
                new Vector3(0f, 0.04f, 0f), new Vector3(1.1f, 0.04f, 1.1f), moss);
        }

        static void BuildStoneWell(Transform parent, Vector3 pos)
        {
            // CoC decoration scale — ~1.5–2 tiles wide (not a building mega-prop).
            var w = Child(parent, "StoneWell", pos);
            var stone = new Color(0.52f, 0.50f, 0.46f);
            var stoneDark = new Color(0.40f, 0.38f, 0.35f);
            var wood = new Color(0.42f, 0.26f, 0.14f);
            var water = new Color(0.28f, 0.58f, 0.82f);
            var moss = new Color(0.32f, 0.52f, 0.28f);

            Prim(w, "Base", PrimitiveType.Cylinder,
                new Vector3(0f, 0.28f, 0f), new Vector3(1.15f, 0.28f, 1.15f), stone);
            Prim(w, "Rim", PrimitiveType.Cylinder,
                new Vector3(0f, 0.58f, 0f), new Vector3(1.25f, 0.08f, 1.25f), stoneDark);
            Prim(w, "Water", PrimitiveType.Cylinder,
                new Vector3(0f, 0.42f, 0f), new Vector3(0.85f, 0.04f, 0.85f), water);
            Prim(w, "Post_L", PrimitiveType.Cube,
                new Vector3(-0.6f, 1.0f, 0f), new Vector3(0.1f, 0.85f, 0.1f), wood);
            Prim(w, "Post_R", PrimitiveType.Cube,
                new Vector3(0.6f, 1.0f, 0f), new Vector3(0.1f, 0.85f, 0.1f), wood);
            Prim(w, "Roof", PrimitiveType.Cube,
                new Vector3(0f, 1.5f, 0f), new Vector3(1.5f, 0.1f, 0.75f), wood);
            Prim(w, "Bucket", PrimitiveType.Cylinder,
                new Vector3(0f, 0.95f, 0f), new Vector3(0.28f, 0.16f, 0.28f), wood);
            Prim(w, "Moss", PrimitiveType.Cylinder,
                new Vector3(0.45f, 0.1f, 0.35f), new Vector3(0.35f, 0.05f, 0.35f), moss);
        }

        static void BuildWaterHint(Transform parent)
        {
            // Soft water strip south of fringe (like CoC shoreline peek)
            float z = -(PadHalf + FringeExtra + 14f);
            Prim(parent, "WaterHint", PrimitiveType.Cube,
                new Vector3(0f, -1.2f, z),
                new Vector3((PadHalf + 20f) * 2f, 1.5f, 18f),
                new Color(0.15f, 0.45f, 0.75f));
        }

        static Vector3 SquareRingPoint(float half, float t01)
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

        static Material CheckerGrassMaterial()
        {
            var existing = Resources.Load<Material>("CoC_BlankChecker");
            if (existing != null) return existing;

            var tex = Resources.Load<Texture2D>("CoC_BlankCheckerTex");
            if (tex == null) tex = MakeCheckerTexture(64, 8);

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Texture");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            // Light CoC grass tint
            var tint = new Color(0.55f, 0.78f, 0.38f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            mat.color = tint;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
            // Tile checkers ≈ 1 CoC tile each (44 tiles / 8 tex cells = 5.5)
            var tile = new Vector2(TileCount / 8f, TileCount / 8f);
            if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tile);
            if (mat.HasProperty("_MainTex")) mat.mainTextureScale = tile;
            return mat;
        }

        static Texture2D MakeCheckerTexture(int res, int cells)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            int cell = res / cells;
            Color a = new Color(0.62f, 0.82f, 0.40f);
            Color b = new Color(0.48f, 0.72f, 0.32f);
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                bool on = ((x / cell) + (y / cell)) % 2 == 0;
                tex.SetPixel(x, y, on ? a : b);
            }
            tex.Apply();
            tex.name = "CoC_BlankCheckerTex_Runtime";
            return tex;
        }

        static Transform Child(Transform parent, string name, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            return go.transform;
        }

        static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard")
                             ?? Shader.Find("Unlit/Color");
                var mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.06f);
                mr.sharedMaterial = mat;
            }
            return go;
        }
    }
}

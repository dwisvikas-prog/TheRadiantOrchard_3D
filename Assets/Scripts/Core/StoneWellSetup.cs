using UnityEngine;

namespace RadiantOrchard
{
    // Clean CoC-scale stone well — procedural, always upright, easy to see & tap.
    public static class StoneWellSetup
    {
        public const string WellName = "StoneWell";
        public static readonly Vector3 DefaultLocalPos = new Vector3(0f, 0f, -4f);

        public static Transform EnsureGoodWell(Transform preferredParent = null)
        {
            Vector3 worldPos = DefaultLocalPos;
            Transform parent = preferredParent;

            var old = FindWell();
            if (old != null)
            {
                worldPos = old.position;
                worldPos.y = 0f;
                if (parent == null) parent = old.parent;
                DestroyGo(old.gameObject);
            }

            if (parent == null)
            {
                var root = GameObject.Find(CoCBlankGround.RootName);
                if (root != null)
                {
                    var marks = root.transform.Find(CoCBlankGround.LandmarksName);
                    parent = marks != null ? marks : root.transform;
                }
            }
            if (parent == null)
                parent = new GameObject(CoCBlankGround.LandmarksName).transform;

            if (worldPos.sqrMagnitude < 0.01f)
                worldPos = parent.TransformPoint(DefaultLocalPos);
            worldPos.y = 0f;

            var well = BuildProceduralWell(parent, worldPos);
            EnsureClickCollider(well);
            EnsureGlow(well);
            return well;
        }

        public static Transform FixInScene(Transform marksOrNull = null) => EnsureGoodWell(marksOrNull);

        public static Transform FindWell()
        {
            var go = GameObject.Find(WellName);
            if (go != null) return go.transform;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name == WellName) return t;
            return null;
        }

        public static Transform BuildProceduralWell(Transform parent, Vector3 worldPos)
        {
            var root = new GameObject(WellName);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var stone = new Color(0.62f, 0.60f, 0.56f);
            var stoneDark = new Color(0.45f, 0.43f, 0.40f);
            var wood = new Color(0.48f, 0.30f, 0.14f);
            var woodDark = new Color(0.34f, 0.20f, 0.10f);
            var water = new Color(0.22f, 0.58f, 0.86f);
            var moss = new Color(0.28f, 0.52f, 0.26f);

            // Stack bottom → top (clear silhouette)
            Prim(root.transform, "Pad", PrimitiveType.Cylinder,
                new Vector3(0f, 0.08f, 0f), new Vector3(1.7f, 0.08f, 1.7f), stoneDark);
            Prim(root.transform, "Base", PrimitiveType.Cylinder,
                new Vector3(0f, 0.40f, 0f), new Vector3(1.35f, 0.32f, 1.35f), stone);
            Prim(root.transform, "Rim", PrimitiveType.Cylinder,
                new Vector3(0f, 0.78f, 0f), new Vector3(1.45f, 0.10f, 1.45f), stoneDark);
            Prim(root.transform, "Water", PrimitiveType.Cylinder,
                new Vector3(0f, 0.55f, 0f), new Vector3(0.90f, 0.06f, 0.90f), water);

            Prim(root.transform, "Post_L", PrimitiveType.Cube,
                new Vector3(-0.78f, 1.45f, 0f), new Vector3(0.16f, 1.25f, 0.16f), wood);
            Prim(root.transform, "Post_R", PrimitiveType.Cube,
                new Vector3(0.78f, 1.45f, 0f), new Vector3(0.16f, 1.25f, 0.16f), wood);
            Prim(root.transform, "Beam", PrimitiveType.Cube,
                new Vector3(0f, 2.10f, 0f), new Vector3(1.85f, 0.14f, 0.14f), woodDark);
            Prim(root.transform, "Roof", PrimitiveType.Cube,
                new Vector3(0f, 2.32f, 0f), new Vector3(2.05f, 0.14f, 1.15f), wood);

            Prim(root.transform, "Bucket", PrimitiveType.Cylinder,
                new Vector3(0f, 1.35f, 0f), new Vector3(0.34f, 0.20f, 0.34f), woodDark);
            Prim(root.transform, "Moss", PrimitiveType.Sphere,
                new Vector3(0.6f, 0.35f, 0.45f), Vector3.one * 0.38f, moss);

            return root.transform;
        }

        public static void EnsureClickCollider(Transform well)
        {
            if (well == null) return;
            foreach (var c in well.GetComponentsInChildren<Collider>(true))
            {
                if (c.transform == well) continue;
                if (Application.isPlaying) Object.Destroy(c);
                else Object.DestroyImmediate(c);
            }

            var box = well.GetComponent<BoxCollider>();
            if (box == null) box = well.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.2f, 0f);
            box.size = new Vector3(3.0f, 2.8f, 3.0f);
            box.isTrigger = false;
        }

        public static void EnsureGlow(Transform well)
        {
            if (well == null || well.Find("WellGlow") != null) return;
            var glow = new GameObject("WellGlow");
            glow.transform.SetParent(well, false);
            glow.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.93f, 0.7f);
            light.intensity = 1.6f;
            light.range = 6f;
        }

        // Disabled — TextMesh looked broken in isometric. Use screen "Make a Wish" button instead.
        public static void SetTapHintVisible(Transform well, bool on)
        {
            if (well == null) return;
            var hint = well.Find("TapHint");
            if (hint != null) DestroyGo(hint.gameObject);
        }

        static GameObject Prim(Transform parent, string name, PrimitiveType type,
            Vector3 local, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            DestroyCollider(go);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color");
                var mat = new Material(sh);
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
                mr.sharedMaterial = mat;
            }
            return go;
        }

        static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col == null) return;
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
        }

        static void DestroyGo(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        public static void ApplyUpright(Transform well, float yawDegrees) { }
        public static void FitHeight(Transform well, float targetHeight) { }
        public static void SeatOnGround(Transform well, float groundY) { }
    }
}

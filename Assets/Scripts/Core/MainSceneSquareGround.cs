using UnityEngine;
using UnityEngine.SceneManagement;

namespace RadiantOrchard
{
    // Clash-of-Clans style village base:
    //   • flat square green grass top
    //   • thicker brown dirt/cliff slab underneath
    public static class MainSceneSquareGround
    {
        const string RootName = "CoC_BaseGround";
        const string GrassName = "BaseGrass";
        const string CliffName = "BaseCliff";
        const string SquareAlias = "SQUARE_GROUND";

        // Village pad size (world units). CoC feel = clear square slab.
        // Public so village layout pads/spaces from THIS square — not old circular island.
        public const float GrassHalf = 55f;
        const float GrassHeight = 0.55f;
        const float GrassTopY = 0.12f;

        // Cliff slightly larger + thicker so grass sits on a brown block.
        const float CliffExtra = 2.5f;
        const float CliffHeight = 6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRun()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            TryAuto(SceneManager.GetActiveScene());
        }

        static void OnLoaded(Scene scene, LoadSceneMode mode) => TryAuto(scene);

        static void TryAuto(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (!IsMain(scene)) return;
            Apply();
        }

        static bool IsMain(Scene scene)
        {
            string n = scene.name ?? "";
            string p = (scene.path ?? "").Replace('\\', '/');
            return p.EndsWith("The Main RadiantOrchard_3d.unity") ||
                   n.IndexOf("RadiantOrchard", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   n.IndexOf("Main Radiant", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   GameObject.Find("IslandMesh") != null;
        }

        public static Bounds Apply()
        {
            Vector3 center = FindCenter();
            HideRoundIslandTop();
            BuildBase(center);
            CoCVillageLayout.Apply(center);

            float side = (GrassHalf + CliffExtra) * 2f;
            Debug.Log($"[MainSceneSquareGround] CoC base + clean village — grass {GrassHalf * 2f:F0}x{GrassHalf * 2f:F0}");
            return new Bounds(new Vector3(center.x, 3f, center.z), new Vector3(side, 14f, side));
        }

        static Vector3 FindCenter()
        {
            var island = GameObject.Find("IslandMesh")
                         ?? GameObject.Find("FloatingislandmainBASE")
                         ?? GameObject.Find("Island");
            if (island != null)
                return new Vector3(island.transform.position.x, 0f, island.transform.position.z);
            return Vector3.zero;
        }

        static void HideRoundIslandTop()
        {
            string[] names = { "IslandMesh", "FloatingislandmainBASE", "IslandBase", "GrassGround" };
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null) continue;
                bool hit = false;
                for (int i = 0; i < names.Length; i++)
                    if (t.name == names[i]) { hit = true; break; }
                if (!hit) continue;
                // Don't hide our new base children if somehow named same.
                if (t.root != null && t.root.name == RootName) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                    if (r != null) r.enabled = false;
            }

            // Old single-cube helper from earlier experiments
            var old = GameObject.Find(SquareAlias);
            if (old != null && (old.transform.parent == null || old.transform.parent.name != RootName))
            {
                if (Application.isPlaying) Object.Destroy(old);
                else Object.DestroyImmediate(old);
            }
        }

        static void BuildBase(Vector3 center)
        {
            var root = GameObject.Find(RootName);
            if (root == null) root = new GameObject(RootName);
            root.transform.SetPositionAndRotation(new Vector3(center.x, 0f, center.z), Quaternion.identity);

            // --- Cliff slab (brown) ---
            float cliffHalf = GrassHalf + CliffExtra;
            float cliffY = GrassTopY - GrassHeight - CliffHeight * 0.5f + 0.05f;
            var cliff = EnsureChildCube(root.transform, CliffName);
            cliff.transform.localPosition = new Vector3(0f, cliffY, 0f);
            cliff.transform.localRotation = Quaternion.identity;
            cliff.transform.localScale = new Vector3(cliffHalf * 2f, CliffHeight, cliffHalf * 2f);
            SetMat(cliff, CliffMaterial());

            // --- Grass top (green) ---
            float grassY = GrassTopY - GrassHeight * 0.5f;
            var grass = EnsureChildCube(root.transform, GrassName);
            grass.transform.localPosition = new Vector3(0f, grassY, 0f);
            grass.transform.localRotation = Quaternion.identity;
            grass.transform.localScale = new Vector3(GrassHalf * 2f, GrassHeight, GrassHalf * 2f);
            SetMat(grass, GrassMaterial());

            // Alias so older framing tools still find a ground object
            var alias = GameObject.Find(SquareAlias);
            if (alias == null)
            {
                alias = new GameObject(SquareAlias);
                alias.transform.SetParent(root.transform, false);
            }
            else if (alias.transform.parent != root.transform)
                alias.transform.SetParent(root.transform, false);
            alias.transform.localPosition = grass.transform.localPosition;
            alias.transform.localScale = grass.transform.localScale;
        }

        static GameObject EnsureChildCube(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Object.Destroy(col);
                    else Object.DestroyImmediate(col);
                }
            }

            var mf = go.GetComponent<MeshFilter>() ?? go.AddComponent<MeshFilter>();
            mf.sharedMesh = BuiltinCube();
            if (go.GetComponent<MeshRenderer>() == null) go.AddComponent<MeshRenderer>();
            return go;
        }

        static void SetMat(GameObject go, Material mat)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            mr.enabled = true;
            mr.sharedMaterial = mat;
        }

        static Mesh BuiltinCube()
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(tmp);
            else Object.DestroyImmediate(tmp);
            return mesh;
        }

        static Material GrassMaterial()
        {
            // Prefer authored Resources mat if present
            var fromRes = Resources.Load<Material>("CoC_BaseGrass");
            if (fromRes != null) return fromRes;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            // Clash-like lush grass
            var c = new Color(0.45f, 0.82f, 0.30f, 1f);
            ApplyColor(mat, c, 0.04f);
            return mat;
        }

        static Material CliffMaterial()
        {
            var fromRes = Resources.Load<Material>("CoC_BaseCliff");
            if (fromRes != null) return fromRes;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            // Warm dirt / rock cliff
            var c = new Color(0.55f, 0.40f, 0.26f, 1f);
            ApplyColor(mat, c, 0.12f);
            return mat;
        }

        static void ApplyColor(Material mat, Color c, float smooth)
        {
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
        }
    }
}

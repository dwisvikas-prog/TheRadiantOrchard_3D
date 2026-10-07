using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RadiantOrchard
{
    /// <summary>
    /// Step 2 — Wish Tree. Auto-plants a bright UNLIT green tree when the step starts
    /// so it always shows (button was unreliable / prefs said planted but mesh missing).
    /// </summary>
    public class FirstWishTreePlant : MonoBehaviour
    {
        public const string PrefsKey = "RO_WishTreePlanted_v1";
        public const string TreeName = "WisdomTree";
        public const string SpotName = "WishTreePlantSpot";

        static readonly Vector3 PlantPos = new Vector3(0f, 0f, 1.5f);
        static readonly Color LeafGreen = new Color(0.2f, 1f, 0.25f, 1f);
        static readonly Color Bark = new Color(0.45f, 0.28f, 0.12f, 1f);

        Canvas canvas;
        Text statusLabel;
        bool finished;

        public static event System.Action OnCompleted;
        public static bool IsPlanted => PlayerPrefs.GetInt(PrefsKey, 0) == 1;

        public static void Ensure()
        {
            if (!Application.isPlaying) return;

            // Always show the tree if prefs say planted
            if (IsPlanted)
            {
                EnsureTreeVisible();
                EmptyIslandPhaseRunner.Instance?.NotifyWishTreePlanted();
                return;
            }

            if (FindFirstObjectByType<FirstWishTreePlant>() != null) return;
            new GameObject("FirstWishTreePlant").AddComponent<FirstWishTreePlant>();
        }

        public static void EnsureTreeVisible()
        {
            var existing = GameObject.Find(TreeName);
            if (existing != null)
            {
                existing.SetActive(true);
                existing.transform.localScale = Vector3.one * 1.2f;
                return;
            }
            SpawnUnlitGreenTree(PlantPos);
        }

        public static void SeedForDebug()
        {
            EnsureTreeVisible();
            PlayerPrefs.SetInt(PrefsKey, 1);
            PlayerPrefs.Save();
        }

        void Start()
        {
            EnsureEventSystem();
            BuildUI();
            // AUTO PLANT — don't wait for a broken button click
            StartCoroutine(AutoPlantSequence());
        }

        const float GrowSeconds = 10f;

        IEnumerator AutoPlantSequence()
        {
            // Clear any leftover
            ClearOldTrees();
            yield return null;

            // CoC-style: a sapling + build timer first, not an instant tree —
            // makes the very first thing the player plants feel like it's
            // actually "building", not just popping into existence.
            var sapling = SpawnSapling(PlantPos);
            SnapCameraToTree(PlantPos);
            var timerUi = WishTreeGrowTimer.Show(PlantPos + Vector3.up * 1.6f, GrowSeconds);

            float t = 0f;
            while (t < GrowSeconds)
            {
                t += Time.deltaTime;
                float remaining = Mathf.Max(0f, GrowSeconds - t);
                timerUi.SetProgress(t / GrowSeconds, remaining);
                if (sapling != null)
                    sapling.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 0.55f, t / GrowSeconds);
                yield return null;
            }
            timerUi.Hide();
            if (sapling != null) Destroy(sapling);

            var tree = SpawnUnlitGreenTree(PlantPos);
            if (tree == null)
            {
                EmptyIslandCoachBar.SetTip("Wish Tree failed — try again");
                yield break;
            }

            // Quick pop once the build timer finishes
            tree.transform.localScale = Vector3.zero;
            float p = 0f;
            while (p < 1f)
            {
                p += Time.deltaTime * 2.2f;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p));
                tree.transform.localScale = Vector3.one * 1.25f * u;
                yield return null;
            }
            tree.transform.localScale = Vector3.one * 1.25f;
            tree.SetActive(true);

            // Tap the grown tree → CoC-style info card
            DecorationInfoTarget.Ensure(tree, "Wish Tree",
                "The heart of your island. Every coin dropped in the Stone Well " +
                "helps it grow, and its glow is what draws visitors here.");

            PlayerPrefs.SetInt(PrefsKey, 1);
            PlayerPrefs.Save();
            EmptyIslandCoachBar.SetTip("Your Wish Tree has grown!");

            yield return new WaitForSeconds(1.2f);
            FinishStep();
        }

        static GameObject SpawnSapling(Vector3 worldPos)
        {
            var root = new GameObject("WishTreeSapling");
            root.transform.position = worldPos;
            root.transform.localScale = Vector3.one * 0.35f;

            MakeUnlit(root.transform, "Stem", PrimitiveType.Cylinder,
                new Vector3(0f, 0.5f, 0f), new Vector3(0.25f, 0.5f, 0.25f), Bark);
            MakeUnlit(root.transform, "Leaf", PrimitiveType.Sphere,
                new Vector3(0f, 1.1f, 0f), Vector3.one * 0.9f, LeafGreen);
            return root;
        }

        void FinishStep()
        {
            if (finished) return;
            finished = true;
            if (EmptyIslandPhaseRunner.Instance != null)
                EmptyIslandPhaseRunner.Instance.NotifyWishTreePlanted();
            else
                OnCompleted?.Invoke();
            if (canvas != null) Destroy(canvas.gameObject);
            Destroy(gameObject);
        }

        static void ClearOldTrees()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null) continue;
                if (t.name != TreeName && !t.name.StartsWith("WisdomTree")) continue;
                if (!t.gameObject.scene.IsValid()) continue;
                Object.Destroy(t.gameObject);
            }
            var spot = GameObject.Find(SpotName);
            if (spot != null) Object.Destroy(spot);
        }

        static void SnapCameraToTree(Vector3 pos)
        {
            EmptyIslandCameraFocus.Focus(pos + Vector3.up * 1.5f, 16f);
            var cam = Camera.main;
            if (cam == null) return;
            // Hard snap if FocusOn is too slow / missing
            var d = cam.GetComponent<DioramaController>();
            if (d != null && d.target != null)
                d.FocusOn(pos + Vector3.up * 1.2f, 16f, 0.35f);
        }

        /// <summary>Unlit bright green — always visible in URP/Built-in.</summary>
        public static GameObject SpawnUnlitGreenTree(Vector3 worldPos)
        {
            var root = new GameObject(TreeName);
            root.transform.position = worldPos;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Use Unlit so lighting never makes it invisible
            MakeUnlit(root.transform, "Trunk", PrimitiveType.Cylinder,
                new Vector3(0f, 1f, 0f), new Vector3(0.55f, 1f, 0.55f), Bark);
            MakeUnlit(root.transform, "Base", PrimitiveType.Cylinder,
                new Vector3(0f, 0.15f, 0f), new Vector3(0.9f, 0.15f, 0.9f),
                new Color(0.32f, 0.2f, 0.1f));
            MakeUnlit(root.transform, "Canopy", PrimitiveType.Sphere,
                new Vector3(0f, 2.6f, 0f), Vector3.one * 2.6f, LeafGreen);
            MakeUnlit(root.transform, "CanopyL", PrimitiveType.Sphere,
                new Vector3(-0.9f, 2.2f, 0.3f), Vector3.one * 1.6f,
                new Color(0.12f, 0.75f, 0.18f));
            MakeUnlit(root.transform, "CanopyR", PrimitiveType.Sphere,
                new Vector3(0.85f, 2.3f, -0.25f), Vector3.one * 1.5f,
                new Color(0.25f, 0.95f, 0.3f));
            MakeUnlit(root.transform, "CanopyTop", PrimitiveType.Sphere,
                new Vector3(0f, 3.4f, 0f), Vector3.one * 1.2f,
                new Color(0.35f, 1f, 0.4f));

            var glow = new GameObject("WishGlow");
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LeafGreen;
            light.intensity = 3f;
            light.range = 12f;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 4.6f, 0f);
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = "WISH TREE";
            tm.fontSize = 80;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            tm.fontStyle = FontStyle.Bold;
            labelGo.AddComponent<WishTreeLabelBillboard>();

            // Layer default, always active
            root.SetActive(true);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                r.enabled = true;

            Debug.Log("[WishTreePlant] UNLIT green tree at " + worldPos);
            return root;
        }

        static void MakeUnlit(Transform parent, string name, PrimitiveType type,
            Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());

            var mr = go.GetComponent<MeshRenderer>();
            // Prefer Unlit — never depends on scene lights
            var sh = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
            var mat = new Material(sh);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mr.sharedMaterial = mat;
            mr.enabled = true;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void BuildUI()
        {
            // No separate card — Girl bubble owns all Wish Tree tips
            canvas = null;
            statusLabel = null;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    public class WishTreeLabelBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}

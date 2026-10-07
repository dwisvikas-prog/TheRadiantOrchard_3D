using UnityEngine;

namespace RadiantOrchard
{
    // Auto-applies CoC Wish Tree + real green — no Tools menu needed.
    [ExecuteAlways]
    public class EmptyIslandBootstrap : MonoBehaviour
    {
        static readonly Color RealLeafGreen = new Color(0.18f, 0.72f, 0.22f, 1f);
        static readonly Color RealBark = new Color(0.40f, 0.26f, 0.14f, 1f);

        void Awake()
        {
            // Real HQ assets (Sakura, spruce, river) — already in project Resources/CoC_HQ
            CoCEnvironmentHQ.Apply(transform);
            EnsureWishTree(); // fallback only if HQ wish tree missing
            StoneWellSetup.EnsureGoodWell();
            if (Application.isPlaying)
            {
                // Never leave a hidden HQ tree confusing the plant step
                var tree = GameObject.Find("WisdomTree");
                if (tree != null && !FirstWishTreePlant.IsPlanted)
                    tree.SetActive(false);
                else if (FirstWishTreePlant.IsPlanted)
                    FirstWishTreePlant.EnsureTreeVisible();

                ApplyCloseUpCamera();
                EmptyIslandPhaseRunner.Ensure();
                Level1CompleteBanner.Wire();
                Level2Intro.Wire();
                StickmanSeparation.Ensure();
                TreeInfoTapController.Ensure();
                SfxPlayer.Ensure();

                // Catch-up: a save where all 9 fruits were already taught in a
                // prior session, but CurrentLevel never advanced past 1 (e.g.
                // the completion event fired in a session that never wired
                // Level1CompleteBanner in time) would otherwise never re-check.
                EmptyIslandLevelProgress.CheckLevel1Completion();
            }
        }

        static void ApplyCloseUpCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Only ONE camera rig — Orbit fights Diorama and kills CoC pan feel
            var orbit = cam.GetComponent<CameraOrbitController>();
            if (orbit != null) orbit.enabled = false;

            var diorama = cam.GetComponent<DioramaController>();
            if (diorama == null) diorama = cam.gameObject.AddComponent<DioramaController>();
            if (diorama.target == null)
            {
                var focus = GameObject.Find("CameraFocus");
                if (focus != null) diorama.target = focus.transform;
                else
                {
                    var go = new GameObject("CameraFocus");
                    go.transform.position = Vector3.zero;
                    diorama.target = go.transform;
                }
            }
            diorama.enabled = true;
            diorama.ApplyEmptyIslandCloseUp();
        }

#if UNITY_EDITOR
        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                CoCEnvironmentHQ.Apply(transform);
                EnsureWishTree();
                StoneWellSetup.EnsureGoodWell();
            }
        }
#endif

        void EnsureWishTree()
        {
            var existing = GameObject.Find("WisdomTree");

            // HQ sakura / good tree already placed — leave textures alone
            if (existing != null && transform.Find("CoC_HQ_Applied_v2") != null && !IsGiantOrWrongTree(existing))
                return;

            if (existing != null && !IsGiantOrWrongTree(existing))
                return;

            Transform parent = null;
            Vector3 pos = new Vector3(0f, 0f, 1.5f);

            if (existing != null)
            {
                parent = existing.transform.parent;
                pos = existing.transform.position;
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }

            if (parent == null)
            {
                var marks = transform.Find(CoCBlankGround.LandmarksName);
                parent = marks != null ? marks : transform;
            }

        // Prefer HQ wish tree
            var src = Resources.Load<GameObject>(BestAssets.WishTree)
                      ?? Resources.Load<GameObject>("CoC_WishTree/WishTree");
            if (src == null)
            {
                CoCBlankGround.ReplaceWisdomTreeCoCScale();
                existing = GameObject.Find("WisdomTree");
                if (existing != null) TintRealGreen(existing);
                return;
            }

            var tree = Instantiate(src, parent);
            tree.name = "WisdomTree";
            tree.transform.position = pos;
            // Fit CoC landmark height — measure then scale from identity baseline
            tree.transform.localScale = Vector3.one;
            var rends = tree.GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float h = Mathf.Max(0.01f, b.size.y);
                tree.transform.localScale = Vector3.one * (2.4f / h);
            }
            else tree.transform.localScale = Vector3.one * 0.45f;

            EnsureGlow(tree);
            EnsureCollider(tree);
        }

        // Allow ALP oak when we intentionally FitHeight it (HQ path).
        // Only flag as wrong if still giant (>6u) after placement.
        static bool IsGiantOrWrongTree(GameObject go)
        {
            if (go == null) return true;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return false;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.size.y > 6f || b.size.x > 10f;
        }

        static void TintRealGreen(GameObject tree)
        {
            var greenMat = Resources.Load<Material>("WishTree_RealGreen");
            var rends = tree.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in rends)
            {
                if (mr == null) continue;
                var count = mr.sharedMaterials != null ? mr.sharedMaterials.Length : 1;
                if (count < 1) count = 1;
                var mats = new Material[count];
                for (int i = 0; i < count; i++)
                {
                    Material mat;
                    if (greenMat != null)
                        mat = new Material(greenMat);
                    else
                    {
                        var sh = Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Standard")
                                 ?? Shader.Find("Unlit/Color");
                        mat = new Material(sh);
                        mat.color = RealLeafGreen;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RealLeafGreen);
                    }

                    bool bark = mr.name.ToLowerInvariant().Contains("trunk")
                                || mr.name.ToLowerInvariant().Contains("bark")
                                || (count > 1 && i == 0);
                    if (bark && count > 1)
                    {
                        mat.color = RealBark;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RealBark);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", RealBark);
                    }
                    else
                    {
                        mat.color = RealLeafGreen;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RealLeafGreen);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", RealLeafGreen);
                    }
                    mats[i] = mat;
                }
                mr.sharedMaterials = mats;
            }
        }

        static void EnsureGlow(GameObject tree)
        {
            if (tree.GetComponentInChildren<Light>() != null) return;
            var glow = new GameObject("WishGlow");
            glow.transform.SetParent(tree.transform, false);
            glow.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.85f, 1f, 0.55f);
            light.intensity = 1.1f;
            light.range = 5f;
        }

        static void EnsureCollider(GameObject tree)
        {
            if (tree.GetComponentInChildren<Collider>() != null) return;
            var col = tree.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.7f;
            col.height = 2.2f;
        }
    }
}

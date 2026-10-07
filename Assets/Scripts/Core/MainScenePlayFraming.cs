using UnityEngine;
using UnityEngine.SceneManagement;

namespace RadiantOrchard
{
    // Auto-frames Main Radiant Orchard scenes in Play Mode so the island fills the screen.
    public static class MainScenePlayFraming
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryApply(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryApply(scene);

        static void TryApply(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            string n = scene.name ?? "";
            bool match =
                n.IndexOf("RadiantOrchard", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Radiant", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.Equals("newmvp", System.StringComparison.OrdinalIgnoreCase) ||
                n.IndexOf("Main", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!match) return;
            ApplyFraming(n);
        }

        public static void ApplyFraming(string sceneName)
        {
            TameEnvironmentGiants();

            bool isMain =
                sceneName.IndexOf("RadiantOrchard", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                sceneName.IndexOf("Main Radiant", System.StringComparison.OrdinalIgnoreCase) >= 0;

            Bounds squareBounds;
            Vector3 focus;
            float radius;
            if (isMain)
            {
                squareBounds = MainSceneSquareGround.Apply();
                focus = new Vector3(squareBounds.center.x,
                    Mathf.Clamp(squareBounds.center.y, 1f, 8f), squareBounds.center.z);
                radius = Mathf.Max(squareBounds.extents.x, squareBounds.extents.z, 18f);
            }
            else
            {
                focus = ComputeFocus(out radius);
                squareBounds = new Bounds(focus, new Vector3(radius * 2f, 10f, radius * 2f));
            }

            var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogError("[MainScenePlayFraming] No camera found.");
                return;
            }

            var focusGo = GameObject.Find("CameraFocus");
            if (focusGo == null) focusGo = new GameObject("CameraFocus");
            focusGo.transform.position = focus;

            var orbit = cam.GetComponent<CameraOrbitController>();
            if (orbit != null) orbit.enabled = false;
            var flyover = cam.GetComponent<IntroCameraFlyover>();
            if (flyover != null) flyover.enabled = false;

            var diorama = cam.GetComponent<DioramaController>();
            if (diorama == null) diorama = cam.gameObject.AddComponent<DioramaController>();

            // Start zoomed OUT so whole square + corners are visible (CoC overview).
            float pitchDeg = 52f;
            float fovDeg = 40f;
            float fitRadius = isMain
                ? MainSceneSquareGround.GrassHalf * 1.25f
                : radius;

            float closeZoom = 22f;
            float farZoom = Mathf.Clamp(fitRadius * 1.85f, 90f, 130f);
            float startZoom = farZoom * 0.92f; // almost fully out — corners visible

            diorama.target = focusGo.transform;
            diorama.isometricPitch = pitchDeg;
            diorama.fieldOfView = fovDeg;
            diorama.lookDownBias = Mathf.Clamp(fitRadius * 0.06f, 2f, 6f);
            diorama.portraitFieldOfView = 34f;
            diorama.islandRadius = fitRadius;
            diorama.panMarginFraction = 0.7f;
            diorama.touchPanSpeed = 0.035f;
            diorama.mousePanSpeed = 0.12f;
            diorama.zoomSpeed = 0.08f;
            diorama.scrollZoomSpeed = 8f;
            diorama.fruitGuardPixels = 40f;

            cam.orthographic = false;
            cam.fieldOfView = fovDeg;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.72f, 0.91f, 1f);

            const float cocYawDeg = 35f;
            diorama.ConfigureCoCView(cocYawDeg, closeZoom, farZoom, startZoom);

            float pitch = pitchDeg * Mathf.Deg2Rad;
            float yaw = cocYawDeg * Mathf.Deg2Rad;
            float dist = startZoom;
            cam.transform.position = focus + new Vector3(
                dist * Mathf.Sin(yaw) * Mathf.Cos(pitch),
                dist * Mathf.Sin(pitch),
                -dist * Mathf.Cos(yaw) * Mathf.Cos(pitch));
            cam.transform.LookAt(focus + Vector3.down * diorama.lookDownBias);

            FixPinkStickmen();
            CoCGuideUI.Ensure();

            Debug.Log($"[MainScenePlayFraming] CoC overview zoom start={startZoom:F0} (corners visible)");
        }

        static void TameEnvironmentGiants()
        {
            string[] hideIfHuge =
            {
                "Sky_Horizon", "Sky_Mid", "Sky_Top", "SkyZenith", "HorizonHaze"
            };

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null) continue;

                foreach (var name in hideIfHuge)
                {
                    if (t.name != name) continue;
                    var s = t.localScale;
                    if (s.x > 80f || s.z > 80f || t.position.y > 25f)
                    {
                        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                            r.enabled = false;
                    }
                }

                if (t.name == "OceanSurface" || t.name == "Ocean" || t.name == "OceanFar")
                {
                    if (t.position.y > -2f)
                        t.position = new Vector3(t.position.x, t.name == "OceanFar" ? -10.5f : -10f, t.position.z);
                }

                if (t.name == "OceanDepth" && t.position.y > -5f)
                    t.position = new Vector3(t.position.x, -30f, t.position.z);
            }
        }

        static Vector3 ComputeFocus(out float radius)
        {
            Bounds? bounds = null;
            foreach (var r in Object.FindObjectsByType<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                string root = r.transform.root != null ? r.transform.root.name : r.name;
                if (root.Contains("Environment_SeaSky")) continue;
                if (r.name.StartsWith("Sky") || r.name.StartsWith("Ocean") || r.name.Contains("Horizon")) continue;
                if (r is SpriteRenderer) continue;
                if (r.bounds.size.magnitude > 350f) continue;

                if (bounds == null) bounds = r.bounds;
                else
                {
                    var b = bounds.Value;
                    b.Encapsulate(r.bounds);
                    bounds = b;
                }
            }

            if (bounds == null)
            {
                radius = 28f;
                return new Vector3(0f, 3f, 0f);
            }

            var bb = bounds.Value;
            radius = Mathf.Max(bb.extents.x, bb.extents.z, 18f);
            return new Vector3(bb.center.x, Mathf.Clamp(bb.center.y, 1f, 8f), bb.center.z);
        }

        static void FixPinkStickmen()
        {
            var grey = Resources.Load<Material>("Stickman_Grey");

            foreach (var sm in Object.FindObjectsByType<StickmanController>(FindObjectsInactive.Include))
            {
                foreach (var r in sm.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null) continue;
                    bool broken = r.sharedMaterial == null ||
                                  r.sharedMaterial.shader == null ||
                                  r.sharedMaterial.shader.name.Contains("InternalError");
                    if (!broken) continue;

                    if (grey != null)
                    {
                        r.sharedMaterial = grey;
                        continue;
                    }

                    var shader = Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Standard")
                                 ?? Shader.Find("Unlit/Color");
                    if (shader == null) continue;
                    var mat = new Material(shader);
                    var c = new Color(0.55f, 0.55f, 0.58f, 1f);
                    mat.color = c;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    r.sharedMaterial = mat;
                }
            }
        }
    }
}

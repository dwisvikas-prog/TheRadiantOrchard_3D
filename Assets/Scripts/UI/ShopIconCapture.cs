using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Renders a small real snapshot of each shop prefab (not a flat color
    /// swatch) so the player can see what they're actually buying — works in
    /// a device build (no Editor-only APIs), captured once and cached.
    ///
    /// Capture is async (one real render pass via an enabled camera, waited
    /// for with WaitForEndOfFrame) rather than a manual disabled-camera
    /// Render() call — the latter can silently produce a blank/black texture
    /// under URP if the pipeline hasn't set the camera up for a frame yet.
    /// </summary>
    public static class ShopIconCapture
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly HashSet<string> queuedIds = new HashSet<string>();
        static readonly Queue<(IslandShopCatalog.Entry entry, Action<Sprite> onReady)> queue =
            new Queue<(IslandShopCatalog.Entry, Action<Sprite>)>();
        static Camera rigCam;
        static Transform rigRoot;
        static Runner runner;
        static bool consuming;
        const float RigDistance = 6000f; // parked far below the play area
        const int IconLayer = 30;

        /// <summary>Returns a cached icon immediately, or null while a fresh capture is queued — call again (e.g. next frame) or use GetIconAsync.</summary>
        public static Sprite GetIcon(IslandShopCatalog.Entry entry)
        {
            if (cache.TryGetValue(entry.id, out var cached) && cached != null)
                return cached;
            if (string.IsNullOrEmpty(entry.resourcePath)) return null;

            Enqueue(entry, null);
            return null;
        }

        /// <summary>Same as GetIcon, but calls back once the real snapshot is ready.</summary>
        public static void GetIconAsync(IslandShopCatalog.Entry entry, Action<Sprite> onReady)
        {
            if (cache.TryGetValue(entry.id, out var cached) && cached != null)
            {
                onReady?.Invoke(cached);
                return;
            }
            if (string.IsNullOrEmpty(entry.resourcePath)) return;

            Enqueue(entry, onReady);
        }

        // A single shared rig camera does the capturing, so only one item is
        // ever rendered at a time — a queue instead of one coroutine per
        // request avoids two captures fighting over that same camera.
        static void Enqueue(IslandShopCatalog.Entry entry, Action<Sprite> onReady)
        {
            EnsureRunner();
            if (!queuedIds.Contains(entry.id))
            {
                queuedIds.Add(entry.id);
                queue.Enqueue((entry, onReady));
            }
            else if (onReady != null)
            {
                // Already queued for someone else — still deliver to this caller too.
                queue.Enqueue((entry, onReady));
            }
            if (!consuming) runner.StartCoroutine(ConsumeQueue());
        }

        static void EnsureRunner()
        {
            if (runner != null) return;
            var go = new GameObject("ShopIconCaptureRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }

        static IEnumerator ConsumeQueue()
        {
            consuming = true;
            while (queue.Count > 0)
            {
                var (entry, onReady) = queue.Dequeue();
                queuedIds.Remove(entry.id);

                if (cache.TryGetValue(entry.id, out var already) && already != null)
                {
                    onReady?.Invoke(already);
                    continue;
                }
                yield return CaptureRoutine(entry, onReady);
            }
            consuming = false;
        }

        static void EnsureRig()
        {
            if (rigCam != null) return;

            var rootGo = new GameObject("ShopIconRig");
            UnityEngine.Object.DontDestroyOnLoad(rootGo);
            rootGo.transform.position = new Vector3(0f, -RigDistance, 0f);
            rigRoot = rootGo.transform;

            var camGo = new GameObject("IconCam");
            camGo.transform.SetParent(rootGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -4f);
            camGo.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            rigCam = camGo.AddComponent<Camera>();
            rigCam.clearFlags = CameraClearFlags.SolidColor;
            rigCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            rigCam.cullingMask = 1 << IconLayer;
            rigCam.fieldOfView = 28f;
            rigCam.nearClipPlane = 0.05f;
            rigCam.farClipPlane = 20f;
            // Stays disabled between captures — it's only ever turned on
            // for the single frame it needs a targetTexture set, so an idle
            // rig camera never renders to the actual screen.
            rigCam.enabled = false;
            rigCam.targetTexture = null;

            var light = new GameObject("IconLight").AddComponent<Light>();
            light.transform.SetParent(rootGo.transform, false);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.1f;
            light.cullingMask = 1 << IconLayer;
        }

        static IEnumerator CaptureRoutine(IslandShopCatalog.Entry entry, Action<Sprite> onReady)
        {
            EnsureRig();

            var prefab = Resources.Load<GameObject>(entry.resourcePath);
            if (prefab == null) yield break;

            var instance = UnityEngine.Object.Instantiate(prefab, rigRoot);
            SetLayerRecursive(instance.transform, IconLayer);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);

            var bounds = ComputeBounds(instance.transform);
            if (bounds.size.sqrMagnitude < 0.0001f)
            {
                UnityEngine.Object.Destroy(instance);
                yield break;
            }

            float radius = Mathf.Max(bounds.extents.magnitude, 0.2f);
            var camT = rigCam.transform;
            camT.localPosition = new Vector3(0f, bounds.center.y - rigRoot.position.y, -radius * 2.4f);
            camT.LookAt(bounds.center);

            const int size = 128;
            var rt = RenderTexture.GetTemporary(size, size, 16, RenderTextureFormat.ARGB32);
            // targetTexture set BEFORE enabling — an enabled camera with an
            // explicit targetTexture renders to that RT via the normal per-
            // frame pipeline, never to the actual screen.
            rigCam.targetTexture = rt;
            rigCam.enabled = true;

            yield return null;
            yield return new WaitForEndOfFrame();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;

            rigCam.enabled = false;
            rigCam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(instance);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            cache[entry.id] = sprite;
            onReady?.Invoke(sprite);
        }

        static Bounds ComputeBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
        }

        class Runner : MonoBehaviour { }
    }
}

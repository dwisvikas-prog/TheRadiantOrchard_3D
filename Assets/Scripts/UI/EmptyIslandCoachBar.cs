using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RadiantOrchard
{
    /// <summary>
    /// Compact Girl3 + tiny readable speech bubble. Island stays clear.
    /// </summary>
    public class EmptyIslandCoachBar : MonoBehaviour
    {
        const string GirlPrefabPath = "Assets/Animate Character/Girl3/Prefabs/Character.prefab";
        const string GirlResourcesPath = "Guide/Girl3Character";

        static EmptyIslandCoachBar instance;
        static string pendingTip;
        static bool pendingWithOk;
        static Action pendingOnOk;
        static string pendingOkText = "OK";

        Canvas canvas;
        GameObject root;
        Text tipText;
        Text nameText;
        RectTransform bubbleRt;
        Button okBtn;
        Text okLabel;
        Action currentOnOk;

        GameObject girlWorld;
        Camera cam;
        Coroutine idleCo;
        Coroutine hideCo;

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (instance != null) return;
            var go = new GameObject("EmptyIslandCoachBar");
            instance = go.AddComponent<EmptyIslandCoachBar>();
        }

        public static void SetTip(string tip) => SetTip(tip, false, null);

        /// <summary>Bubble only for real new events — auto-hides; girl stays idle after.</summary>
        public static void SetNewEvent(string tip) => SetTip(tip, false, null);

        public static void SetTip(string tip, bool withOk, Action onOk, string okText = "OK")
        {
            Ensure();
            if (instance == null) return;
            if (string.IsNullOrEmpty(tip))
            {
                GoIdleQuiet();
                return;
            }
            if (!EmptyIslandGuideStack.AllowsCoachTip)
            {
                pendingTip = tip;
                pendingWithOk = withOk;
                pendingOnOk = onOk;
                pendingOkText = okText;
                return;
            }
            pendingTip = null;
            pendingWithOk = false;
            pendingOnOk = null;
            pendingOkText = "OK";
            instance.Show(tip, withOk, onOk, okText);
        }

        public static void Hide()
        {
            if (instance == null) return;
            instance.CloseAndResolvePending();
        }

        /// <summary>No speech bubble — girl just hangs out / does little actions.</summary>
        public static void GoIdleQuiet()
        {
            Ensure();
            if (instance == null) return;
            instance.CloseAndResolvePending();
        }

        /// <summary>
        /// Shared close path for Hide()/GoIdleQuiet(). A pending OK callback
        /// (e.g. a phase-toast's BlocksWorldInput-clearing callback) must never
        /// be silently dropped just because some other system closed the bar
        /// first — that was leaving BlocksWorldInput permanently true. Capture
        /// + null currentOnOk BEFORE invoking so a callback that itself calls
        /// Hide()/GoIdleQuiet() (e.g. Level2Intro's) can't re-enter and fire twice.
        /// </summary>
        void CloseAndResolvePending()
        {
            var cb = currentOnOk;
            currentOnOk = null;
            if (root != null) root.SetActive(false);
            // Keep girl around in quiet idle (no constant bubble)
            EnsureGirl();
            if (girlWorld != null) girlWorld.SetActive(true);
            StartIdleMotion();
            cb?.Invoke();
        }

        public static void FlushPending()
        {
            if (string.IsNullOrEmpty(pendingTip)) return;
            string t = pendingTip;
            bool ok = pendingWithOk;
            Action cb = pendingOnOk;
            string ot = pendingOkText;
            pendingTip = null;
            pendingWithOk = false;
            pendingOnOk = null;
            pendingOkText = "OK";
            SetTip(t, ok, cb, ot);
        }

        void Awake()
        {
            instance = this;
            cam = Camera.main;
            Build();
            EnsureGirl();
        }

        void OnDestroy()
        {
            if (girlWorld != null) Destroy(girlWorld);
            if (instance == this) instance = null;
        }

        void LateUpdate()
        {
            if (girlWorld == null || !girlWorld.activeSelf) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            // Tiny guide — bottom-left corner only
            Vector3 wp = cam.ViewportToWorldPoint(new Vector3(0.08f, 0.11f, 4.5f));
            girlWorld.transform.position = wp;
            girlWorld.transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
            girlWorld.transform.localScale = Vector3.one * 0.12f;
        }

        void Build()
        {
            canvas = OrchardUITheme.MakeCanvas(transform, "CoachCanvas", 460);

            root = new GameObject("GuideRoot", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            // Compact bottom band — island stays clear
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(1f, 0.30f);
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Animal Crossing-style bubble: white + black border + gold corners
            // Compact, beside girl (girl sits bottom-left in world)
            var shadow = OrchardUITheme.MakeImage(root.transform, "BubbleShadow",
                new Color(0f, 0f, 0f, 0.16f));
            OrchardUITheme.Place(shadow.rectTransform, 0.225f, 0.28f, 0.785f, 0.86f);

            var border = OrchardUITheme.MakeImage(root.transform, "BubbleBorder",
                new Color(0.12f, 0.12f, 0.14f, 1f));
            bubbleRt = border.rectTransform;
            OrchardUITheme.Place(bubbleRt, 0.22f, 0.32f, 0.78f, 0.90f);

            var fill = OrchardUITheme.MakeImage(bubbleRt, "Fill", Color.white);
            OrchardUITheme.Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(3.5f, 3.5f);
            fill.rectTransform.offsetMax = new Vector2(-3.5f, -3.5f);

            // Gold corner flourishes (AC style)
            AddCornerFlourish(fill.transform, "TL", new Vector2(0f, 1f), new Vector2(10f, -10f), 0f);
            AddCornerFlourish(fill.transform, "TR", new Vector2(1f, 1f), new Vector2(-10f, -10f), 90f);
            AddCornerFlourish(fill.transform, "BL", new Vector2(0f, 0f), new Vector2(10f, 10f), -90f);
            AddCornerFlourish(fill.transform, "BR", new Vector2(1f, 0f), new Vector2(-10f, 10f), 180f);

            // Tail pointing down-left toward girl
            var tailBorder = OrchardUITheme.MakeImage(bubbleRt, "TailBorder",
                new Color(0.12f, 0.12f, 0.14f, 1f));
            var tbrt = tailBorder.rectTransform;
            tbrt.anchorMin = tbrt.anchorMax = new Vector2(0.12f, 0f);
            tbrt.pivot = new Vector2(0.5f, 1f);
            tbrt.anchoredPosition = new Vector2(0f, 2f);
            tbrt.sizeDelta = new Vector2(18f, 18f);
            tbrt.localEulerAngles = new Vector3(0f, 0f, 45f);

            var tailFill = OrchardUITheme.MakeImage(bubbleRt, "TailFill", Color.white);
            var tfrt = tailFill.rectTransform;
            tfrt.anchorMin = tfrt.anchorMax = new Vector2(0.12f, 0f);
            tfrt.pivot = new Vector2(0.5f, 1f);
            tfrt.anchoredPosition = new Vector2(0f, 4f);
            tfrt.sizeDelta = new Vector2(12f, 12f);
            tfrt.localEulerAngles = new Vector3(0f, 0f, 45f);

            // No name tag — AC bubbles are just the line
            nameText = null;

            tipText = OrchardUITheme.MakeText(fill.transform, "Tip", 18, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.12f, 0.12f, 0.14f, 1f));
            OrchardUITheme.Place(tipText.rectTransform, 0.06f, 0.32f, 0.94f, 0.92f);
            tipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tipText.verticalOverflow = VerticalWrapMode.Truncate;
            tipText.resizeTextForBestFit = true;
            tipText.resizeTextMinSize = 12;
            tipText.resizeTextMaxSize = 18;
            tipText.lineSpacing = 1.05f;
            tipText.text = "";

            // OK / Find soil — INSIDE the bubble (AC style, one place)
            okBtn = OrchardUITheme.MakeGreenButton(fill.transform, "Ok", "OK", 14);
            OrchardUITheme.Place(okBtn.GetComponent<RectTransform>(), 0.28f, 0.06f, 0.72f, 0.28f);
            okLabel = okBtn.GetComponentInChildren<Text>();
            okBtn.onClick.AddListener(OnOkClicked);
            okBtn.gameObject.SetActive(false);

            root.SetActive(false);
        }

        static void AddCornerFlourish(Transform parent, string name, Vector2 anchor, Vector2 pos, float rotZ)
        {
            var gold = new Color(0.92f, 0.72f, 0.22f, 1f);
            // Outer L arm
            var a = OrchardUITheme.MakeImage(parent, name + "A", gold);
            var art = a.rectTransform;
            art.anchorMin = art.anchorMax = anchor;
            art.pivot = new Vector2(0.5f, 0.5f);
            art.anchoredPosition = pos;
            art.sizeDelta = new Vector2(14f, 2.2f);
            art.localEulerAngles = new Vector3(0f, 0f, rotZ);

            var b = OrchardUITheme.MakeImage(parent, name + "B", gold);
            var brt = b.rectTransform;
            brt.anchorMin = brt.anchorMax = anchor;
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = pos;
            brt.sizeDelta = new Vector2(2.2f, 14f);
            brt.localEulerAngles = new Vector3(0f, 0f, rotZ);
        }

        void EnsureGirl()
        {
            if (girlWorld != null) return;

            GameObject prefab = Resources.Load<GameObject>(GirlResourcesPath);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GirlPrefabPath);
#endif
            if (prefab != null)
            {
                girlWorld = Instantiate(prefab);
                girlWorld.name = "GuideGirl3";
                girlWorld.transform.SetParent(transform, true);
                SetLayerRecursively(girlWorld, 0);
                girlWorld.SetActive(false);
            }
            else
            {
                girlWorld = new GameObject("GuideGirlFallback");
                girlWorld.transform.SetParent(transform, true);
                var sr = girlWorld.AddComponent<SpriteRenderer>();
                sr.sprite = OrchardUITheme.LoadSprite(BestAssets.UiLeaf);
                sr.color = new Color(0.95f, 0.7f, 0.75f, 1f);
                girlWorld.SetActive(false);
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform c in go.transform)
                SetLayerRecursively(c.gameObject, layer);
        }

        void Show(string tip, bool withOk, Action onOk, string okText)
        {
            if (canvas == null || root == null) Build();
            EnsureGirl();

            // Keep messages short in compact bubble
            tipText.text = CompactMessage(tip);
            currentOnOk = onOk;
            if (okBtn != null)
            {
                okBtn.gameObject.SetActive(withOk);
                if (okLabel != null && withOk)
                    okLabel.text = string.IsNullOrEmpty(okText) ? "OK" : okText;
                // Text above button inside bubble
                OrchardUITheme.Place(tipText.rectTransform, 0.06f,
                    withOk ? 0.32f : 0.10f, 0.94f, 0.92f);
            }

            root.SetActive(true);
            if (girlWorld != null) girlWorld.SetActive(true);

            StopAllCoroutines();
            idleCo = null;
            hideCo = null;
            StartCoroutine(PopIn());
            if (!withOk)
                hideCo = StartCoroutine(AutoHideBubble(4.5f));
        }

        System.Collections.IEnumerator AutoHideBubble(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (root != null) root.SetActive(false);
            currentOnOk = null;
            StartIdleMotion();
            hideCo = null;
        }

        void StartIdleMotion()
        {
            if (idleCo != null) StopCoroutine(idleCo);
            idleCo = StartCoroutine(IdleGirlLoop());
        }

        System.Collections.IEnumerator IdleGirlLoop()
        {
            EnsureGirl();
            if (girlWorld == null) yield break;
            girlWorld.SetActive(true);
            var anim = girlWorld.GetComponentInChildren<Animator>();
            float nextAct = Time.unscaledTime + UnityEngine.Random.Range(4f, 9f);
            while (girlWorld != null && girlWorld.activeSelf && (root == null || !root.activeSelf))
            {
                var e = girlWorld.transform.localEulerAngles;
                e.z = Mathf.Sin(Time.unscaledTime * 1.6f) * 4f;
                girlWorld.transform.localEulerAngles = e;

                if (Time.unscaledTime >= nextAct)
                {
                    nextAct = Time.unscaledTime + UnityEngine.Random.Range(6f, 14f);
                    if (anim != null)
                    {
                        try { anim.SetTrigger("Wave"); } catch { /* ok */ }
                        try { anim.Play(0, 0, 0f); } catch { /* ok */ }
                    }
                    float t = 0f;
                    while (t < 0.6f && girlWorld != null)
                    {
                        t += Time.unscaledDeltaTime;
                        float w = Mathf.Sin(t * 20f) * 8f;
                        var ee = girlWorld.transform.localEulerAngles;
                        ee.z = w;
                        girlWorld.transform.localEulerAngles = ee;
                        yield return null;
                    }
                }
                yield return null;
            }
            idleCo = null;
        }

        static string CompactMessage(string tip)
        {
            if (string.IsNullOrEmpty(tip)) return tip;
            // Prefer first 2 lines max for compact bubble
            var lines = tip.Replace("\r\n", "\n").Split('\n');
            if (lines.Length <= 2) return tip.Trim();
            return (lines[0].Trim() + "\n" + lines[1].Trim()).Trim();
        }

        void OnOkClicked()
        {
            var cb = currentOnOk;
            currentOnOk = null;
            Hide();
            cb?.Invoke();
        }

        System.Collections.IEnumerator PopIn()
        {
            if (bubbleRt == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 8f;
                float s = Mathf.SmoothStep(0.9f, 1f, Mathf.Clamp01(t));
                bubbleRt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            bubbleRt.localScale = Vector3.one;
        }
    }
}

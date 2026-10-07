using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // Well wish — NO text typing. Tap well / button → Drop Coin → done.
    public class WellWishController : MonoBehaviour
    {
        public const string PrefsKey = "RO_WellWish_v1";
        const float TapMaxPixels = 140f;
        const float TapMaxSeconds = 0.8f;
        const float ScreenHitRadiusPx = 260f;

        Transform well;
        Camera cam;
        Canvas canvas;
        GameObject panelRoot;
        GameObject openWishBtnRoot;
        Text statusText;
        Button dropBtn;
        bool uiOpen;
        bool wishDone;
        bool pointerDown;
        Vector2 downPos;
        float downTime;

        public static event System.Action<string> OnWishCompleted;

        /// <summary>True while Drop Coin modal is up — camera must not pan.</summary>
        public static bool IsUiOpen { get; private set; }

        public static void Ensure()
        {
            if (FindFirstObjectByType<WellWishController>() != null) return;
            new GameObject("WellWish").AddComponent<WellWishController>();
        }

        void Start()
        {
            cam = Camera.main;
            well = StoneWellSetup.EnsureGoodWell();
            StoneWellSetup.SetTapHintVisible(well, false);
            DestroyWorldTextJunk();
            EnsureEventSystem();
            wishDone = PlayerPrefs.HasKey(PrefsKey);
            BuildUI();
            SetPanel(false);
            RefreshOpenWishCta();

            if (wishDone)
                EmptyIslandCoachBar.SetTip("Tap Make a Wish / Drop Coin to continue");
            else
                EmptyIslandCoachBar.SetTip("Tap the Stone Well — or Make a Wish");
        }

        void DestroyWorldTextJunk()
        {
            // Only strip temporary WELL CTAs — never wipe Wish Tree / fruit / track labels
            foreach (var tm in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if (tm == null) continue;
                if (tm.GetComponent<WishTreeLabelBillboard>() != null) continue;
                if (tm.GetComponent<TreeNameBillboard>() != null) continue;

                string t = tm.text ?? "";
                string u = t.ToUpperInvariant();
                if (u.Contains("WISH TREE") || u.Contains("PLACED") || u.Contains("TAP SOIL") ||
                    u.Contains("TAP HERE") || u.Contains("TAP TILE") || u.Contains("MOVE TILE") ||
                    u.Contains("APPLE") || u.Contains("STRAWBERRY") || u.Contains("TREE"))
                    continue;

                // Well-phase leftover junk only
                bool wellJunk =
                    u.Contains("DROP COIN") ||
                    u.Contains("STONE WELL") ||
                    u.Contains("TAP WELL") ||
                    u == "WISH" ||
                    u == "MAKE A WISH";
                if (wellJunk)
                    Destroy(tm.gameObject);
            }
        }

        void Update()
        {
            if (openWishBtnRoot != null && openWishBtnRoot.activeSelf)
            {
                var runner = EmptyIslandPhaseRunner.Instance;
                if (runner != null && runner.Current > EmptyIslandPhase.WellWish)
                    openWishBtnRoot.SetActive(false);
            }

            if (uiOpen) return;
            if (EmptyIslandPhaseRunner.BlocksWorldInput) return;
            if (cam == null) cam = Camera.main;
            if (well == null) well = StoneWellSetup.FindWell() ?? StoneWellSetup.EnsureGoodWell();
            if (well == null || cam == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                downPos = Input.mousePosition;
                downTime = Time.unscaledTime;
            }
            if (pointerDown && Input.GetMouseButtonUp(0))
            {
                pointerDown = false;
                TryTap(Input.mousePosition);
            }

            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    pointerDown = true;
                    downPos = t.position;
                    downTime = Time.unscaledTime;
                }
                else if (pointerDown && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
                {
                    pointerDown = false;
                    TryTap(t.position);
                }
            }
        }

        void TryTap(Vector2 upPos)
        {
            if (Time.unscaledTime - downTime > TapMaxSeconds) return;
            if ((upPos - downPos).magnitude > TapMaxPixels) return;
            if (well == null || cam == null) return;

            var ray = cam.ScreenPointToRay(upPos);
            if (Physics.Raycast(ray, out var hit, 300f) && IsWellHit(hit.transform))
            {
                OpenWish();
                return;
            }

            Vector3 tip = well.position + Vector3.up * 1.2f;
            Vector3 sp = cam.WorldToScreenPoint(tip);
            if (sp.z > 0f && Vector2.Distance(new Vector2(sp.x, sp.y), upPos) <= ScreenHitRadiusPx)
                OpenWish();
        }

        bool IsWellHit(Transform t)
        {
            while (t != null)
            {
                if (t == well || t.name == "StoneWell") return true;
                t = t.parent;
            }
            return false;
        }

        void OpenWish()
        {
            if (openWishBtnRoot != null) openWishBtnRoot.SetActive(false);
            SetPanel(true);
            if (dropBtn != null)
            {
                dropBtn.gameObject.SetActive(true);
                dropBtn.interactable = true;
            }
            if (statusText != null)
                statusText.text = wishDone
                    ? "Tap Drop Coin again to continue."
                    : "Drop a coin into the well — your wish is heard.";
        }

        void OnDropCoin()
        {
            // Direct — no typing required
            const string wish = "Orchard Blessing";
            PlayerPrefs.SetString(PrefsKey, wish);
            PlayerPrefs.Save();
            wishDone = true;

            // Close the modal FIRST — it was a full-screen dim+card sitting on
            // top of the 3D well, so the coin arc animated invisibly behind it.
            SetPanel(false);
            StartCoroutine(CoinDropRoutine(wish));
        }

        void OnClose()
        {
            SetPanel(false);
            RefreshOpenWishCta();
        }

        void RefreshOpenWishCta()
        {
            if (openWishBtnRoot == null) return;
            var runner = EmptyIslandPhaseRunner.Instance;
            if (runner != null && runner.Current > EmptyIslandPhase.WellWish)
            {
                openWishBtnRoot.SetActive(false);
                return;
            }
            openWishBtnRoot.SetActive(true);
            var label = openWishBtnRoot.GetComponentInChildren<Text>();
            if (label != null) label.text = wishDone ? "Continue →" : "Make a Wish";
        }

        IEnumerator CoinDropRoutine(string wish)
        {
            EmptyIslandCameraFocus.FocusWell();

            Vector3 mouth = well != null ? well.position + Vector3.up * 1.8f : Vector3.up * 1.8f;
            Vector3 water = well != null ? well.position + Vector3.up * 0.55f : Vector3.up * 0.55f;

            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "WishCoin";
            coin.transform.position = mouth;
            coin.transform.localScale = new Vector3(0.25f, 0.04f, 0.25f);
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Destroy(coin.GetComponent<Collider>());

            var mr = coin.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var gold = new Color(0.95f, 0.78f, 0.22f);
            mat.color = gold;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", gold);
            mr.sharedMaterial = mat;

            float t = 0f;
            Vector3 start = coin.transform.position;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.6f;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                Vector3 p = Vector3.Lerp(start, water, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.1f;
                coin.transform.position = p;
                coin.transform.Rotate(0f, 400f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }
            Destroy(coin);

            EmptyIslandCoachBar.SetNewEvent("The well heard your wish!");
            yield return new WaitForSeconds(0.5f);

            if (EmptyIslandPhaseRunner.Instance != null)
                EmptyIslandPhaseRunner.Instance.NotifyWishDone();
            else
                OnWishCompleted?.Invoke(wish);
        }

        void SetPanel(bool on)
        {
            uiOpen = on;
            IsUiOpen = on;
            if (panelRoot != null) panelRoot.SetActive(on);
        }

        void BuildUI()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            canvas = OrchardUITheme.MakeCanvas(transform, "WellWishCanvas", 560);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            panelRoot = new GameObject("WishPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvas.transform, false);
            OrchardUITheme.Stretch(panelRoot.GetComponent<RectTransform>());

            var dim = OrchardUITheme.MakeImage(panelRoot.transform, "Dim", new Color(0f, 0f, 0f, 0.22f));
            OrchardUITheme.Stretch(dim.rectTransform);

            var card = OrchardUITheme.MakeImage(panelRoot.transform, "Card", OrchardUITheme.PanelCream);
            var crt = card.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(420f, 280f);

            var title = OrchardUITheme.MakeText(card.transform, "Title", 26, FontStyle.Bold,
                TextAnchor.MiddleCenter, OrchardUITheme.InkBrown);
            OrchardUITheme.Place(title.rectTransform, 0.06f, 0.72f, 0.94f, 0.92f);
            title.text = "Stone Well";

            statusText = OrchardUITheme.MakeText(card.transform, "Status", 17, FontStyle.Normal,
                TextAnchor.MiddleCenter, OrchardUITheme.InkMuted);
            OrchardUITheme.Place(statusText.rectTransform, 0.08f, 0.42f, 0.92f, 0.70f);
            statusText.text = "";

            dropBtn = OrchardUITheme.MakeGreenButton(card.transform, "DropBtn", "Drop Coin", 22);
            dropBtn.GetComponent<Image>().color = new Color(0.90f, 0.55f, 0.12f);
            OrchardUITheme.Place(dropBtn.GetComponent<RectTransform>(), 0.15f, 0.08f, 0.55f, 0.26f);
            dropBtn.onClick.RemoveAllListeners();
            dropBtn.onClick.AddListener(OnDropCoin);

            var closeBtn = OrchardUITheme.MakeGreenButton(card.transform, "CloseBtn", "Close", 20);
            closeBtn.GetComponent<Image>().color = new Color(0.45f, 0.40f, 0.35f);
            OrchardUITheme.Place(closeBtn.GetComponent<RectTransform>(), 0.58f, 0.08f, 0.88f, 0.26f);
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(OnClose);

            openWishBtnRoot = OrchardUITheme.MakeGreenButton(canvas.transform, "OpenWishBtn",
                wishDone ? "Continue →" : "Make a Wish", 32).gameObject;
            var ort = openWishBtnRoot.GetComponent<RectTransform>();
            ort.anchorMin = new Vector2(0.5f, 0f);
            ort.anchorMax = new Vector2(0.5f, 0f);
            ort.pivot = new Vector2(0.5f, 0f);
            ort.sizeDelta = new Vector2(420f, 90f);
            ort.anchoredPosition = new Vector2(0f, 120f);
            openWishBtnRoot.GetComponent<Button>().onClick.RemoveAllListeners();
            openWishBtnRoot.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (wishDone) OnDropCoin();
                else OpenWish();
            });
            openWishBtnRoot.SetActive(true);

            panelRoot.SetActive(false);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }
}

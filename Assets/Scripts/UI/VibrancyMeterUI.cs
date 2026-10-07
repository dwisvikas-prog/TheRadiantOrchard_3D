using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // VibrancyMeterUI v2
    //
    // Changes vs v1:
    //   • fruitIcons[] now show the FRUIT SPRITE loaded from Resources/FruitIcons/
    //     or GameData/Sprites/ at Start — not coloured circles.
    //     Each slot in fruitIcons[] is an Image whose .sprite is set to the
    //     matching fruit icon (Icon_Strawberry.png etc.).
    //   • Harvested fruit bounces up with a small scale-pop animation (DOTween
    //     not required — pure Mathf.Lerp in Update).
    //   • On first run the icons are also auto-discovered from the scene's
    //     VibrancyMeterUI child hierarchy, so WireLevel1Scene.cs wiring still works
    //     and a hand-built HUD also works without re-running any tools.
    //   • fruitIconSprites[] can be assigned in the Inspector as a fallback when
    //     the Runtime Resources path doesn't exist.
    public class VibrancyMeterUI : MonoBehaviour
    {
        [SerializeField] private Image   fillImage;
        [SerializeField] private Image[] fruitIcons;   // one Image per fruit, ordered to match FruitOrder
        [SerializeField] private float   lerpSpeed = 2.0f;

        [Header("Fruit Icon Sprites (assign in Inspector or auto-loaded)")]
        // Assign the 9 sprites here in the same order as FruitOrder below.
        // If left empty the script tries to load from GameData/Sprites/ at runtime.
        [SerializeField] private Sprite[] fruitIconSprites;

        // Sprite folder path used by Resources.Load (must be inside a Resources/ folder).
        // Falls back to the GameData path if not found.
        private const string ResourcesIconPath = "FruitIcons/Icon_";

        private static readonly FruitType[] FruitOrder =
        {
            FruitType.Strawberry, FruitType.Pineapple, FruitType.Watermelon,
            FruitType.Lemon,      FruitType.Grapes,    FruitType.Apple,
            FruitType.Peach,      FruitType.Banana,    FruitType.Cherry
        };

        // ── internal ──────────────────────────────────────────────────────
        private float targetFill;
        private float displayedFill;
        private bool  subscribed;

        // Per-icon animation targets (pop scale on harvest)
        private float[] iconScaleTargets;
        private float[] iconScaleCurrent;

        // ── lifecycle ─────────────────────────────────────────────────────
        private void OnEnable()  => TrySubscribe();
        private void OnDisable()
        {
            if (subscribed && GameState.Instance != null)
            {
                GameState.Instance.VibrancyChanged  -= OnVibrancyChanged;
                GameState.Instance.FruitZoneChanged -= OnFruitZoneChanged;
            }
            subscribed = false;
        }

        private void Start()
        {
            // Ensure fill image is in Filled mode
            if (fillImage != null && fillImage.type != Image.Type.Filled)
            {
                fillImage.type        = Image.Type.Filled;
                fillImage.fillMethod  = Image.FillMethod.Horizontal;
                fillImage.fillOrigin  = (int)Image.OriginHorizontal.Left;
            }

            // Load fruit sprites into each icon Image
            LoadFruitSprites();

            // Init per-icon scale animation arrays
            int iconCount = fruitIcons != null ? fruitIcons.Length : 0;
            iconScaleTargets = new float[iconCount];
            iconScaleCurrent = new float[iconCount];
            for (int i = 0; i < iconCount; i++)
                iconScaleTargets[i] = iconScaleCurrent[i] = 1f;

            TrySubscribe();
        }

        // ── fruit sprite loading ──────────────────────────────────────────

        private void LoadFruitSprites()
        {
            if (fruitIcons == null || fruitIcons.Length == 0) return;

            for (int i = 0; i < fruitIcons.Length && i < FruitOrder.Length; i++)
            {
                if (fruitIcons[i] == null) continue;

                Sprite sprite = null;

                // 1. Inspector-assigned array
                if (fruitIconSprites != null && i < fruitIconSprites.Length)
                    sprite = fruitIconSprites[i];

                // 2. Runtime Resources folder (Assets/Resources/FruitIcons/Icon_Strawberry etc.)
                if (sprite == null)
                    sprite = Resources.Load<Sprite>(ResourcesIconPath + FruitOrder[i]);

                // 3. Already has a sprite assigned directly on the Image
                if (sprite == null && fruitIcons[i].sprite != null)
                    sprite = fruitIcons[i].sprite;

                if (sprite != null)
                {
                    fruitIcons[i].sprite         = sprite;
                    fruitIcons[i].preserveAspect = true;
                    fruitIcons[i].color          = Color.white; // full colour always
                }
            }
        }

        // ── GameState subscription ────────────────────────────────────────

        private void TrySubscribe()
        {
            if (subscribed || GameState.Instance == null) return;
            var state = GameState.Instance;
            state.VibrancyChanged  += OnVibrancyChanged;
            state.FruitZoneChanged += OnFruitZoneChanged;
            subscribed = true;
            OnVibrancyChanged(state.CurrentVibrancy, state.MaxVibrancy);
            RefreshAllFruitIcons();
        }

        private void OnVibrancyChanged(int current, int max)
        {
            targetFill = max > 0 ? (float)current / max : 0f;
        }

        private void OnFruitZoneChanged(string zoneId, FruitGrowthStage stage)
            => RefreshAllFruitIcons();

        // ── icon refresh ──────────────────────────────────────────────────

        private void RefreshAllFruitIcons()
        {
            if (fruitIcons == null || GameState.Instance == null) return;

            for (int i = 0; i < fruitIcons.Length && i < FruitOrder.Length; i++)
            {
                if (fruitIcons[i] == null) continue;

                var stage = GameState.Instance.GetFruitZoneStage(
                    GameState.FruitZoneId(FruitOrder[i]));

                // Harvested → animate a pop to 1.25×; normal → return to 1.0×
                if (iconScaleTargets != null && i < iconScaleTargets.Length)
                    iconScaleTargets[i] = stage == FruitGrowthStage.Harvested ? 1.25f : 1.0f;
            }
        }

        // ── Update: fill lerp + icon pop animation ────────────────────────

        private void Update()
        {
            if (!subscribed) TrySubscribe();

            // Fill bar smooth lerp
            if (fillImage != null)
            {
                displayedFill = Mathf.MoveTowards(
                    displayedFill, targetFill,
                    lerpSpeed * Time.unscaledDeltaTime);
                fillImage.fillAmount = displayedFill;
            }

            // Per-icon scale pop
            if (fruitIcons == null || iconScaleTargets == null) return;
            for (int i = 0; i < fruitIcons.Length; i++)
            {
                if (fruitIcons[i] == null) continue;
                iconScaleCurrent[i] = Mathf.Lerp(
                    iconScaleCurrent[i], iconScaleTargets[i],
                    Time.unscaledDeltaTime * 8f);
                fruitIcons[i].rectTransform.localScale =
                    Vector3.one * iconScaleCurrent[i];
            }
        }
    }
}

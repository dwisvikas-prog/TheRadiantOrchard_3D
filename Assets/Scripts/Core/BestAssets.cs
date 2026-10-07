namespace RadiantOrchard
{
    // Curated "best look" picks for Radiant Orchard Empty Island / CoC pad.
    // Only these packs drive the live village — rest stay in project as backups.
    public static class BestAssets
    {
        // ── World (Resources/CoC_HQ) ──────────────────────────────────────
        // Wish Tree picks — GREEN only (first that loads wins):
        public const string WishTree     = "CoC_HQ/WishTree";          // ALP oak (tinted green)
        public const string WishTreeKenney = "CoC_WishTree/WishTree"; // Kenney low-poly CoC oak
        public const string WishTreeMagic = "CoC_HQ/WishTree_Magic"; // spare (purple — not used for wish)
        public const string ForestPine   = "CoC_HQ/Forest_Pine";       // Polytope
        public const string ForestGreen  = "CoC_HQ/Forest_FruitGreen";
        public const string ForestApple  = "CoC_HQ/Forest_FruitApples";
        public const string ForestPear   = "CoC_HQ/Forest_FruitPears";
        public const string ForestPlum   = "CoC_HQ/Forest_FruitPlums";
        public const string Bush         = "CoC_HQ/Bush";
        public const string Water        = "CoC_HQ/Water";              // UNS water
        public const string Rock1        = "CoC_HQ/Rock1";
        public const string Rock2        = "CoC_HQ/Rock2";

        // ── Character / interactables ─────────────────────────────────────
        public const string Stickman     = "Stickman";
        public const string WellPrefab   = "AssetsStore/Weel/Assets/well"; // scene ref; runtime uses StoneWell
        public const string FruitStraw   = "FruitData_Strawberry";

        // ── UI sprites (Resources/UI + FruitIcons) ────────────────────────
        public const string UiLeaf       = "UI/leaf";
        public const string UiStar       = "UI/star";
        public const string UiCircle     = "UI/circle";
        public const string UiVibrancy   = "UI/vibrancy_gradient";
        public const string FruitIconRoot = "FruitIcons/Icon_";

        // Packs intentionally NOT used on Empty Island (kept in Assets as spare):
        // Blinktool rocks (too many variants), Fantasy Forest sample, Kevin Iglesias
        // human dummies (Stickman is the hero), PolyKebap potions (later tonic VFX),
        // stylized-mini-floating-island (mesh reserved for future island shell).
    }
}

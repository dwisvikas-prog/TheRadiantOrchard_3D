using UnityEngine;

namespace RadiantOrchard
{
    // Proposal Virtue-Health Matrix defaults (Love/Joy/Peace… + health stats).
    // FruitData assets can override; Stickman falls back here so all 9 fruits
    // show the correct symptom bubble + heal VFX without re-authoring every asset.
    public static class FruitHealthMatrix
    {
        public readonly struct Entry
        {
            public readonly HealthStat healthStat;
            public readonly string symptomLabel;   // e.g. "Frustrated" — shown in bubble
            public readonly string healthHint;     // e.g. "Heart"
            public readonly Color themeColor;      // bubble accent + heal VFX
            public readonly string virtueName;     // proposal virtue name

            public Entry(HealthStat healthStat, string symptomLabel, string healthHint,
                         Color themeColor, string virtueName)
            {
                this.healthStat = healthStat;
                this.symptomLabel = symptomLabel;
                this.healthHint = healthHint;
                this.themeColor = themeColor;
                this.virtueName = virtueName;
            }
        }

        public static Entry Get(FruitType type) => type switch
        {
            FruitType.Strawberry => new Entry(HealthStat.HeartCirculation, "Frustrated", "Heart",
                new Color(0.95f, 0.25f, 0.35f), "Love"),
            FruitType.Pineapple => new Entry(HealthStat.ImmunityEnergy, "Tired", "Energy",
                new Color(1f, 0.78f, 0.15f), "Joy"),
            FruitType.Watermelon => new Entry(HealthStat.NervousSystem, "Anxious", "Calm",
                new Color(0.25f, 0.75f, 0.65f), "Peace"),
            FruitType.Lemon => new Entry(HealthStat.DetoxDigestion, "Queasy", "Detox",
                new Color(0.95f, 0.9f, 0.2f), "Patience"),
            FruitType.Grapes => new Entry(HealthStat.RespiratoryBreath, "Breathless", "Breath",
                new Color(0.55f, 0.3f, 0.75f), "Meekness"),
            FruitType.Apple => new Entry(HealthStat.BrainFocus, "Scattered", "Focus",
                new Color(0.3f, 0.55f, 0.95f), "Self-Control"),
            FruitType.Peach => new Entry(HealthStat.SkinGlow, "Dull", "Glow",
                new Color(1f, 0.7f, 0.55f), "Kindness"),
            FruitType.Banana => new Entry(HealthStat.MuscleStrength, "Weak", "Strength",
                new Color(0.95f, 0.85f, 0.2f), "Goodness"),
            FruitType.Cherry => new Entry(HealthStat.BoneStability, "Unsteady", "Stability",
                new Color(0.85f, 0.15f, 0.25f), "Faithfulness"),
            _ => new Entry(HealthStat.HeartCirculation, "Needs help", "Heal",
                Color.white, "Virtue")
        };

        public static Entry Resolve(FruitData fruit)
        {
            if (fruit == null) return Get(FruitType.Strawberry);
            var fallback = Get(fruit.fruitType);

            // Asset overrides win when authored; empty strings keep matrix defaults.
            string label = !string.IsNullOrEmpty(fruit.symptomLabel) ? fruit.symptomLabel : fallback.symptomLabel;
            string hint = !string.IsNullOrEmpty(fruit.healthHint) ? fruit.healthHint : fallback.healthHint;
            HealthStat stat = fruit.useCustomHealthStat ? fruit.healthStat : fallback.healthStat;
            Color color = fruit.useCustomHealthStat ? fruit.healVfxColor : fallback.themeColor;
            if (color.a <= 0.01f) color = fallback.themeColor;

            return new Entry(stat, label, hint, color, fallback.virtueName);
        }
    }
}

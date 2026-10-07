using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// First-level teaching book: mood → fruit → gesture → meaning for all 9 fruits.
    /// </summary>
    public static class FruitLessonBook
    {
        public readonly struct Lesson
        {
            public readonly FruitType fruit;
            public readonly string fruitName;
            public readonly string mood;       // visitor feeling
            public readonly string heals;      // short body / need label
            public readonly string virtue;     // Love, Joy, Peace…
            public readonly GestureType gesture;
            public readonly string gestureHow;
            public readonly string tip;
            public readonly string meaning;   // what this fruit is / why
            public readonly string feelings;  // which feelings show up
            public readonly string healDetail;// what it heals in body/spirit

            public Lesson(FruitType fruit, string fruitName, string mood, string heals,
                string virtue, GestureType gesture, string gestureHow, string tip,
                string meaning, string feelings, string healDetail)
            {
                this.fruit = fruit;
                this.fruitName = fruitName;
                this.mood = mood;
                this.heals = heals;
                this.virtue = virtue;
                this.gesture = gesture;
                this.gestureHow = gestureHow;
                this.tip = tip;
                this.meaning = meaning;
                this.feelings = feelings;
                this.healDetail = healDetail;
            }

            public string FullMeaningCard() =>
                "WHAT: " + meaning + "\n" +
                "FEELING: " + feelings + "\n" +
                "HEALS: " + healDetail + "\n" +
                "VIRTUE: " + virtue + "  ·  BODY: " + heals + "\n" +
                "GESTURE: " + gestureHow;
        }

        public static readonly Lesson[] All =
        {
            new(FruitType.Strawberry, "Strawberry", "Frustrated", "Heart", "Love",
                GestureType.DoubleTap, "DOUBLE-TAP the strawberry",
                "Frustrated? Double-tap Strawberry for Love / Heart.",
                "Sweet red berry that carries Love tonic.",
                "Anger, irritation, closed heart, short temper.",
                "Heart & circulation — softens frustration into warmth."),

            new(FruitType.Pineapple, "Pineapple", "Tired", "Energy", "Joy",
                GestureType.LongPress, "HOLD (long-press) the pineapple",
                "Tired? Hold Pineapple for Joy / Energy.",
                "Bright tropical fruit that carries Joy tonic.",
                "Exhaustion, low spark, heavy body, no cheer.",
                "Immunity & energy — wakes tired visitors with Joy."),

            new(FruitType.Watermelon, "Watermelon", "Anxious", "Calm", "Peace",
                GestureType.FastSwipe, "SWIPE across the watermelon",
                "Anxious? Swipe Watermelon for Peace / Calm.",
                "Cool juicy fruit that carries Peace tonic.",
                "Worry, racing thoughts, restless nerves.",
                "Nervous system — cools anxiety into calm Peace."),

            new(FruitType.Lemon, "Lemon", "Queasy", "Detox", "Patience",
                GestureType.DoubleTap, "DOUBLE-TAP the lemon",
                "Queasy? Double-tap Lemon for Patience / Detox.",
                "Sharp citrus that carries Patience tonic.",
                "Sick stomach, impatience, toxic buildup feeling.",
                "Detox & digestion — clears queasy into clean Patience."),

            new(FruitType.Grapes, "Grapes", "Breathless", "Breath", "Meekness",
                GestureType.LongPress, "HOLD the grapes",
                "Breathless? Hold Grapes for Meekness / Breath.",
                "Cluster fruit that carries Meekness tonic.",
                "Short breath, tight chest, rushing / forcing.",
                "Breath & lungs — opens breathless into soft Meekness."),

            new(FruitType.Apple, "Apple", "Scattered", "Focus", "Self-Control",
                GestureType.FastSwipe, "SWIPE across the apple",
                "Scattered? Swipe Apple for Self-Control / Focus.",
                "Classic orchard fruit that carries Self-Control tonic.",
                "Distracted mind, no focus, jumping tasks.",
                "Brain & focus — gathers scattered into Self-Control."),

            new(FruitType.Peach, "Peach", "Dull", "Glow", "Kindness",
                GestureType.DoubleTap, "DOUBLE-TAP the peach",
                "Dull skin mood? Double-tap Peach for Kindness / Glow.",
                "Soft fuzzy fruit that carries Kindness tonic.",
                "Dull skin, low warmth toward self/others.",
                "Skin glow — restores dull into Kindness shine."),

            new(FruitType.Banana, "Banana", "Weak", "Strength", "Goodness",
                GestureType.LongPress, "HOLD the banana",
                "Weak? Hold Banana for Goodness / Strength.",
                "Power fruit that carries Goodness tonic.",
                "Weak muscles, drained will, can't stand strong.",
                "Muscle strength — builds weak into Goodness power."),

            new(FruitType.Cherry, "Cherry", "Unsteady", "Stability", "Faithfulness",
                GestureType.FastSwipe, "SWIPE across the cherry",
                "Unsteady? Swipe Cherry for Faithfulness / Stability.",
                "Tiny red fruit that carries Faithfulness tonic.",
                "Shaky balance, unstable mood, can't stay steady.",
                "Bones & stability — roots unsteady into Faithfulness."),
        };

        public static Lesson Get(FruitType type)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].fruit == type) return All[i];
            return All[0];
        }

        public static Lesson GetByMood(string mood)
        {
            if (string.IsNullOrEmpty(mood)) return All[0];
            for (int i = 0; i < All.Length; i++)
                if (string.Equals(All[i].mood, mood, System.StringComparison.OrdinalIgnoreCase))
                    return All[i];
            return All[0];
        }

        public static string GestureShort(GestureType g) => g switch
        {
            GestureType.DoubleTap => "Double-tap",
            GestureType.LongPress => "Hold",
            GestureType.FastSwipe => "Swipe",
            _ => "Tap"
        };
    }
}

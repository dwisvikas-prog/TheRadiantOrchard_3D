using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Tracks which fruits the player has already been taught (first-time only).
    /// After all 9 are taught, visitors use quiet alert mode.
    /// </summary>
    public static class FruitTeachProgress
    {
        const string PrefPrefix = "RO_FruitTaught_v1_";

        public static bool IsTaught(FruitType fruit) =>
            PlayerPrefs.GetInt(PrefPrefix + fruit, 0) == 1;

        public static void MarkTaught(FruitType fruit)
        {
            PlayerPrefs.SetInt(PrefPrefix + fruit, 1);
            PlayerPrefs.Save();
        }

        public static void ClearTaught(FruitType fruit)
        {
            PlayerPrefs.DeleteKey(PrefPrefix + fruit);
            PlayerPrefs.Save();
        }

        public static bool AllTaught()
        {
            var all = FruitLessonBook.All;
            for (int i = 0; i < all.Length; i++)
            {
                if (!IsTaught(all[i].fruit)) return false;
            }
            return true;
        }

        public static int TaughtCount()
        {
            int n = 0;
            var all = FruitLessonBook.All;
            for (int i = 0; i < all.Length; i++)
                if (IsTaught(all[i].fruit)) n++;
            return n;
        }
    }
}

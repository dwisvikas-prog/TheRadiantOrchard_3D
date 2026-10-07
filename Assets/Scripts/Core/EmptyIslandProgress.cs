using UnityEngine;

namespace RadiantOrchard
{
    // Single source for Empty Island onboarding prefs + reset.
    public static class EmptyIslandProgress
    {
        public const string PhaseKey = EmptyIslandPhaseRunner.PrefsKey;
        public const string WishKey = WellWishController.PrefsKey;
        public const string PlantKey = FirstPlantTutorial.PrefsKey;

        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(PhaseKey);
            PlayerPrefs.DeleteKey("RO_EmptyIslandPhase_v1"); // legacy
            PlayerPrefs.DeleteKey(WishKey);
            PlayerPrefs.DeleteKey(PlantKey);
            PlayerPrefs.DeleteKey(FirstWishTreePlant.PrefsKey);
            PlayerPrefs.DeleteKey(EmptyIslandNextGuide.PrefsKey);
            PlayerPrefs.Save();
            Debug.Log("[EmptyIsland] Progress cleared — next Play starts from Landmarks.");
        }

        public static bool WishDone => PlayerPrefs.HasKey(WishKey);
        public static bool PlantDone => PlayerPrefs.GetInt(PlantKey, 0) == 1;
        public static bool WishTreePlanted => FirstWishTreePlant.IsPlanted;

        public static void SeedWishForDebug(string wish = "(dev skip)")
        {
            PlayerPrefs.SetString(WishKey, wish);
            PlayerPrefs.Save();
        }

        public static void SeedPlantForDebug()
        {
            PlayerPrefs.SetInt(PlantKey, 1);
            PlayerPrefs.Save();
        }
    }
}

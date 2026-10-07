using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// After first heal — one CoachBar tip with OK (no mid-screen card covering the island).
    /// </summary>
    public class EmptyIslandNextGuide : MonoBehaviour
    {
        public const string PrefsKey = "RO_EmptyIslandNextGuide_v1";

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (PlayerPrefs.GetInt(PrefsKey, 0) == 1) return;
            if (FindFirstObjectByType<EmptyIslandNextGuide>() != null) return;
            new GameObject("EmptyIslandNextGuide").AddComponent<EmptyIslandNextGuide>();
        }

        void Start()
        {
            MoodFruitCoach.HideAll();
            // Bubble only — island stays visible
            EmptyIslandCoachBar.SetTip(
                "Orchard alive!\nNext visitor = next fruit lesson.\nGirl teaches one mood at a time.",
                true,
                () =>
                {
                    PlayerPrefs.SetInt(PrefsKey, 1);
                    PlayerPrefs.Save();
                    EmptyIslandCoachBar.SetTip("Wait for visitors — match their mood fruit");
                    Destroy(gameObject);
                },
                "Got it!");
        }
    }
}

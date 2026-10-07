using System;
using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Empty Island level progression: Level 1 = learn all 9 fruits (single
    /// visitor loop). Level 2 = multi-visitor + a build/decorate goal, CoC-style.
    /// Separate from LevelManager (the scripted-level/objective system used by
    /// the other scenes) — this only paces the Empty Island onboarding loop.
    /// </summary>
    public static class EmptyIslandLevelProgress
    {
        const string LevelKey = "RO_EmptyIsland_Level_v1";
        const string Level2DecoKey = "RO_EmptyIsland_L2DecoCount_v1";
        const string Level2DecoDoneKey = "RO_EmptyIsland_L2DecoDone_v1";

        public const int Level2DecorationTarget = 3;

        public static event Action OnLevel1Complete;
        public static event Action<int, int> OnDecorationProgress; // current, target
        public static event Action OnLevel2DecorationGoalComplete;

        static bool level1CompleteFiredThisSession;

        public static int CurrentLevel
        {
            get => PlayerPrefs.GetInt(LevelKey, 1);
            private set { PlayerPrefs.SetInt(LevelKey, value); PlayerPrefs.Save(); }
        }

        public static bool IsLevel2OrLater => CurrentLevel >= 2;

        public static int DecorationsPlacedThisLevel
        {
            get => PlayerPrefs.GetInt(Level2DecoKey, 0);
            private set { PlayerPrefs.SetInt(Level2DecoKey, value); PlayerPrefs.Save(); }
        }

        public static bool Level2DecorationGoalDone
        {
            get => PlayerPrefs.GetInt(Level2DecoDoneKey, 0) == 1;
            private set { PlayerPrefs.SetInt(Level2DecoDoneKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Call whenever a fruit is marked taught. Fires Level 1 complete once.</summary>
        public static void CheckLevel1Completion()
        {
            if (CurrentLevel != 1) return;
            if (level1CompleteFiredThisSession) return;
            if (!FruitTeachProgress.AllTaught()) return;

            level1CompleteFiredThisSession = true;
            OnLevel1Complete?.Invoke();
        }

        /// <summary>Player confirmed the Level 1 complete banner — advance to Level 2.</summary>
        public static void AdvanceToLevel2()
        {
            if (CurrentLevel >= 2) return;
            CurrentLevel = 2;
        }

        public static void RegisterDecorationPlaced()
        {
            if (CurrentLevel < 2 || Level2DecorationGoalDone) return;

            DecorationsPlacedThisLevel++;
            OnDecorationProgress?.Invoke(DecorationsPlacedThisLevel, Level2DecorationTarget);

            if (DecorationsPlacedThisLevel >= Level2DecorationTarget)
            {
                Level2DecorationGoalDone = true;
                OnLevel2DecorationGoalComplete?.Invoke();
            }
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.DeleteKey(Level2DecoKey);
            PlayerPrefs.DeleteKey(Level2DecoDoneKey);
            PlayerPrefs.Save();
            level1CompleteFiredThisSession = false;
        }
    }
}

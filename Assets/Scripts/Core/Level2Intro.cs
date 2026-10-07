using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Level 2 kickoff: tells the player about the new decoration goal via the
    /// girl guide, then listens for the goal being met to hand out a reward.
    /// </summary>
    public static class Level2Intro
    {
        static bool wired;

        public static void Wire()
        {
            if (wired) return;
            wired = true;
            EmptyIslandLevelProgress.OnDecorationProgress += OnProgress;
            EmptyIslandLevelProgress.OnLevel2DecorationGoalComplete += OnGoalComplete;
        }

        public static void Show()
        {
            EmptyIslandCoachBar.SetTip(
                "Level 2! Open the Shop and place " + EmptyIslandLevelProgress.Level2DecorationTarget +
                " decorations to brighten the island.",
                true, () => EmptyIslandCoachBar.GoIdleQuiet(), "Got it");
        }

        static void OnProgress(int current, int target)
        {
            if (current >= target) return;
            EmptyIslandCoachBar.SetNewEvent("Decoration placed! " + current + "/" + target);
        }

        static void OnGoalComplete()
        {
            EmptyIslandCoachBar.SetNewEvent("Island decorated! Vibrancy boosted.");
            if (GameState.Instance != null)
                GameState.Instance.AddVibrancy(15);
        }
    }
}

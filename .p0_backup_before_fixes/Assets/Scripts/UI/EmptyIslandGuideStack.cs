using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Guide layers — CoachBar bubble stays for Lesson / Gesture / tips.
    /// No full-screen mute (NextGuide is also a bubble now).
    /// </summary>
    public static class EmptyIslandGuideStack
    {
        public enum Layer
        {
            None = 0,
            Mood = 1,
            Lesson = 2,
            Gesture = 3,
            Reward = 4,
            NextGuide = 5
        }

        static Layer current = Layer.None;

        public static Layer Current => current;

        public static void Push(Layer layer)
        {
            current = layer;
            if (layer >= Layer.Lesson)
                MoodFruitCoach.HideAll();
            if (layer >= Layer.Gesture)
                FruitPlantLessonFlow.MinimizePanel();
            if (layer == Layer.Reward)
            {
                MoodFruitCoach.HideAll();
                GestureWhereGuide.HideAll();
            }
        }

        public static void Pop(Layer layer)
        {
            if (current == layer)
                current = Layer.None;
            EmptyIslandCoachBar.FlushPending();
        }

        // Always allow coach bubble — never mute with a full-screen card
        public static bool AllowsCoachTip => true;
    }
}

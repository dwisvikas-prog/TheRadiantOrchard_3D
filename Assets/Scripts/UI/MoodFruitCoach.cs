using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// One fruit tip at a time — ONLY for fruits not yet taught.
    /// Taught fruits use EmptyIslandVisitorAlert instead.
    /// </summary>
    public class MoodFruitCoach : MonoBehaviour
    {
        public const string PrefsKey = "RO_MoodFruitCoachSeen_v1";

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<MoodFruitCoach>() != null) return;
            new GameObject("MoodFruitCoach").AddComponent<MoodFruitCoach>();
        }

        public static void SuggestForMood(string mood, FruitType? forceFruit = null)
        {
            Ensure();
            var lesson = forceFruit.HasValue
                ? FruitLessonBook.Get(forceFruit.Value)
                : FruitLessonBook.GetByMood(mood);

            // Already taught — no bubble spam
            if (FruitTeachProgress.IsTaught(lesson.fruit))
                return;

            if (EmptyIslandGuideStack.Current >= EmptyIslandGuideStack.Layer.Lesson)
                return;

            EmptyIslandGuideStack.Push(EmptyIslandGuideStack.Layer.Mood);
            EmptyIslandCoachBar.SetTip(
                "Visitor is " + lesson.mood + "!\n" +
                "Plant " + lesson.fruitName + " → " + FruitLessonBook.GestureShort(lesson.gesture) + "\n" +
                "✓ on fruit   ✗ not ground");
        }

        public static void ShowFullFruitGuide()
        {
            EmptyIslandCoachBar.SetNewEvent("One fruit at a time — girl teaches each mood once");
        }

        public static void HideAll()
        {
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Mood);
        }
    }
}

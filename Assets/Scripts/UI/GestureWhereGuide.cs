using System.Collections;
using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// After tree plant — slowly track camera to the fruit where player must gesture.
    /// </summary>
    public class GestureWhereGuide : MonoBehaviour
    {
        Transform follow;
        FruitLessonBook.Lesson lesson;

        public static GestureWhereGuide Show(Transform fruitWorld, FruitLessonBook.Lesson lesson)
        {
            HideAllQuiet();

            var go = new GameObject("GestureWhereGuide");
            var g = go.AddComponent<GestureWhereGuide>();
            g.follow = fruitWorld;
            g.lesson = lesson;
            g.StartCoroutine(g.RevealSlowTrack());
            return g;
        }

        public static void HideAll()
        {
            HideAllQuiet();
            EmptyIslandGuideStack.Pop(EmptyIslandGuideStack.Layer.Gesture);
            IslandTapTrack.Hide();
        }

        static void HideAllQuiet()
        {
            IslandTapTrack.Hide();
            var all = FindObjectsByType<GestureWhereGuide>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null) Object.Destroy(all[i].gameObject);
            }
        }

        IEnumerator RevealSlowTrack()
        {
            EmptyIslandGuideStack.Push(EmptyIslandGuideStack.Layer.Gesture);

            string gName = FruitLessonBook.GestureShort(lesson.gesture).ToUpperInvariant();
            EmptyIslandCoachBar.SetNewEvent(
                "Tree planted!\nNow " + gName + " the " + lesson.fruitName + "…");

            // Beat 1: look at tree base briefly (plant settle)
            if (follow != null)
            {
                Vector3 treeBase = follow.position;
                treeBase.y = 0.3f;
                EmptyIslandCameraFocus.FocusSmooth(treeBase, 13f, 0.9f);
            }

            yield return new WaitForSeconds(0.85f);
            if (follow == null) yield break;

            // Beat 2: SLOW glide to the fruit — where to tap / gesture
            string tipGesture = gName;
            if (tipGesture.Contains("DOUBLE")) tipGesture = "DOUBLE-TAP";
            EmptyIslandCoachBar.SetNewEvent(
                tipGesture + " here on the " + lesson.fruitName + "!");

            IslandTapTrack.ShowGestureOnTree(follow, gName);
            EmptyIslandCameraFocus.FocusSmooth(follow.position + Vector3.up * 0.55f, 10f, 1.85f);
        }
    }
}

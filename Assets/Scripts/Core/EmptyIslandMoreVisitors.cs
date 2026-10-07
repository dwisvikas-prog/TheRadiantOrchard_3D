using System.Collections;
using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// After first heal (CoreLoop) — keeps sending occasional new visitors.
    /// First time per fruit = full teach. Later = top alert + info only.
    /// </summary>
    public class EmptyIslandMoreVisitors : MonoBehaviour
    {
        const float FirstDelay = 8f;
        const float IntervalMin = 22f;
        const float IntervalMax = 32f;
        int spawned;

        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<EmptyIslandMoreVisitors>() != null) return;
            new GameObject("EmptyIslandMoreVisitors").AddComponent<EmptyIslandMoreVisitors>();
        }

        void Start() => StartCoroutine(Loop());

        IEnumerator Loop()
        {
            while (PlayerPrefs.GetInt(EmptyIslandNextGuide.PrefsKey, 0) == 0 &&
                   FindFirstObjectByType<EmptyIslandNextGuide>() != null)
                yield return new WaitForSeconds(0.5f);

            EmptyIslandCoachBar.SetNewEvent("More friends will visit — learn each fruit once!");
            yield return new WaitForSeconds(FirstDelay);

            while (true)
            {
                // Strictly one visitor at a time — wait for the current one to
                // be fully done (healed/served) before the next even starts
                // walking in.
                while (FruitPlantLessonFlow.IsBusy)
                    yield return new WaitForSeconds(1.5f);
                while (FindFirstObjectByType<EmptyIslandNextGuide>() != null)
                    yield return new WaitForSeconds(0.5f);
                while (EmptyIslandGuideStack.Current == EmptyIslandGuideStack.Layer.Reward)
                    yield return new WaitForSeconds(0.5f);

                int lessonIndex = spawned;
                SpawnExtraVisitor(lessonIndex);
                spawned++;

                float wait = Random.Range(IntervalMin, IntervalMax);
                yield return new WaitForSeconds(wait);
            }
        }

        void SpawnExtraVisitor(int lessonIndex)
        {
            var prefab = Resources.Load<GameObject>(BestAssets.Stickman)
                         ?? Resources.Load<GameObject>("Stickman");
            Vector3 near = new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-8f, -3f));
            var well = GameObject.Find("StoneWell");
            if (well != null)
                near = well.transform.position + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-5f, -2f));
            near.y = 0f;
            near = StickmanSeparation.FindFreeSpot(near);

            // Every earlier visitor walked in from due south — always the same
            // approach felt repetitive/unreal. Pick a random compass direction
            // each time so arrivals come from different sides of the island.
            float entryAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 entryDir = new Vector3(Mathf.Sin(entryAngle), 0f, Mathf.Cos(entryAngle));
            Vector3 start = near + entryDir * 11f;
            start.y = 0f;

            GameObject go;
            if (prefab != null)
                go = Instantiate(prefab, start, Quaternion.identity);
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.position = start;
                go.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            }

            go.name = "Visitor_" + (lessonIndex + 2);
            var ctrl = go.GetComponent<StickmanController>();
            if (ctrl == null) ctrl = go.AddComponent<StickmanController>();

            var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            StartCoroutine(WalkIn(go.transform, near, ctrl, lessonIndex));
        }

        IEnumerator WalkIn(Transform t, Vector3 end, StickmanController ctrl, int lessonIndex)
        {
            if (ctrl != null) ctrl.enabled = false;

            int idx = lessonIndex % FruitLessonBook.All.Length;
            if (lessonIndex == 0 && FruitLessonBook.All[idx].fruit == FruitType.Strawberry)
                idx = 1 % FruitLessonBook.All.Length;
            var lesson = FruitLessonBook.All[idx];
            bool taught = FruitTeachProgress.IsTaught(lesson.fruit);

            // Camera always follows a new visitor's walk-in, taught or not —
            // otherwise a visitor arriving after the player already knows every
            // fruit shows up completely off the player's radar.
            StartCoroutine(EmptyIslandCameraFocus.TrackWalkingVisitor(t, end, 3.5f));

            yield return StickmanGentleWalk.WalkTo(t, end);

            if (t == null) yield break;
            EmptyIslandCameraFocus.FocusVisitor(t);

            if (ctrl != null)
            {
                ctrl.enabled = true;
                var fruit = FruitTreeCatalog.MakeFruitData(lesson);
                ctrl.Initialize(fruit);
                ctrl.SetPatienceTime(120f);

                var bubble = ctrl.GetComponentInChildren<SymptomBubble>(true);
                if (bubble != null)
                {
                    var entry = FruitHealthMatrix.Get(lesson.fruit);
                    bubble.SetSymptom(entry.symptomLabel, entry.healthHint, entry.themeColor);
                    bubble.SetVisible(true);
                }
            }

            if (taught)
            {
                // Quiet: top indicator only — no coach spam
                FruitPlantLessonFlow.StartFor(lesson, ctrl);
            }
            else
            {
                EmptyIslandCoachBar.SetNewEvent(
                    "New fruit to learn: " + lesson.fruitName + "!");
                MoodFruitCoach.SuggestForMood(lesson.mood, lesson.fruit);
                FruitPlantLessonFlow.StartFor(lesson, ctrl);
            }
        }
    }
}

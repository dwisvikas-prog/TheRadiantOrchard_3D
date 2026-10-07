using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace RadiantOrchard
{
    // First Visitor walk-in. After arrive → FirstPlant phase (heal).
    public class FirstVisitorArrival : MonoBehaviour
    {
        public const string VisitorName = "FirstVisitor";

        [SerializeField] float spawnDelay = 0.8f;
        [SerializeField] float walkDuration = 5.5f;

        StickmanController stickman;
        bool started;
        bool arrived;

        public static event System.Action OnArrived;

        public static void Ensure()
        {
            if (!Application.isPlaying) return;

            // If visitor already standing & ready — just signal arrived (don't kill them)
            var existingVisitor = GameObject.Find(VisitorName);
            if (existingVisitor != null)
            {
                var ctrl = existingVisitor.GetComponent<StickmanController>();
                if (ctrl != null && ctrl.enabled)
                {
                    var runner = FindFirstObjectByType<FirstVisitorArrival>();
                    if (runner != null && !runner.arrived)
                        runner.MarkArrived();
                    else if (EmptyIslandPhaseRunner.Instance != null &&
                             EmptyIslandPhaseRunner.Instance.Current == EmptyIslandPhase.FirstVisitor)
                        EmptyIslandPhaseRunner.Instance.NotifyVisitorArrived();
                    return;
                }
            }

            var existing = FindFirstObjectByType<FirstVisitorArrival>();
            if (existing != null)
            {
                existing.ForceRestart();
                return;
            }

            new GameObject("FirstVisitorArrival").AddComponent<FirstVisitorArrival>();
        }

        void ForceRestart()
        {
            StopAllCoroutines();
            started = true;
            arrived = false;
            stickman = null;

            var old = GameObject.Find(VisitorName);
            if (old != null)
            {
                old.name = "FirstVisitor_Old";
                Destroy(old);
            }

            StartCoroutine(ArrivalRoutine());
        }

        void Start()
        {
            if (started) return;
            started = true;
            StartCoroutine(ArrivalRoutine());
        }

        IEnumerator ArrivalRoutine()
        {
            float wait = spawnDelay;
            while (wait > 0f)
            {
                wait -= Time.deltaTime;
                yield return null;
            }

            // Clear stale
            var leftover = GameObject.Find(VisitorName);
            if (leftover != null)
            {
                leftover.name = "FirstVisitor_Old";
                Destroy(leftover);
                yield return null;
            }

            Vector3 end = ResolveStandPoint();
            Vector3 start = end + new Vector3(0f, 0f, -Mathf.Max(12f, CoCBlankGround.PadHalf * 0.5f));
            start.y = 0f;
            end.y = 0f;

            stickman = SpawnMainStickman(start);
            if (stickman == null)
            {
                Debug.LogError("[FirstVisitor] Stickman missing in Resources/Stickman");
                // Still unblock progression with a placeholder capsule
                stickman = SpawnPlaceholder(start);
                if (stickman == null) yield break;
            }

            stickman.name = VisitorName;
            var agent = stickman.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            stickman.enabled = false;

            EmptyIslandCoachBar.SetTip("Visitor walking in…");
            StartCoroutine(EmptyIslandCameraFocus.TrackWalkingVisitor(stickman.transform, end, 3.5f));
            yield return StickmanGentleWalk.WalkTo(stickman.transform, end, StickmanGentleWalk.GentleSpeed);

            if (stickman == null) yield break;
            stickman.enabled = true;
            var fruit = Resources.Load<FruitData>(BestAssets.FruitStraw)
                        ?? Resources.Load<FruitData>("FruitData_Strawberry");
            if (fruit != null) stickman.Initialize(fruit);
            stickman.SetPatienceTime(99999f);

            // Match fruit lesson: Strawberry → Frustrated (first-level teach)
            var lesson = FruitLessonBook.Get(FruitType.Strawberry);
            var bubble = stickman.GetComponentInChildren<SymptomBubble>(true);
            if (bubble != null)
            {
                var entry = FruitHealthMatrix.Get(FruitType.Strawberry);
                bubble.SetSymptom(entry.symptomLabel, entry.healthHint, entry.themeColor);
                bubble.SetVisible(true);
            }

            var anim = stickman.GetComponent<Animator>();
            if (anim != null)
            {
                anim.speed = 1f;
                anim.SetFloat("Speed", 0f);
                anim.Play("Idle_Sad", 0, 0f);
            }

            Debug.Log("[FirstVisitor] Arrived — mood " + lesson.mood);
            // First level: teach mood → fruit → gesture
            MoodFruitCoach.SuggestForMood(lesson.mood, FruitType.Strawberry);
            MarkArrived();
        }

        void MarkArrived()
        {
            if (arrived) return;
            arrived = true;
            if (stickman != null)
                EmptyIslandCameraFocus.FocusVisitor(stickman.transform);
            else
                EmptyIslandCameraFocus.FocusVisitor();
            EmptyIslandCoachBar.SetTip("Visitor arrived — check mood tip");
            if (EmptyIslandPhaseRunner.Instance != null)
                EmptyIslandPhaseRunner.Instance.NotifyVisitorArrived();
            else
                OnArrived?.Invoke();
        }

        static StickmanController SpawnMainStickman(Vector3 pos)
        {
            var prefab = Resources.Load<GameObject>(BestAssets.Stickman)
                         ?? Resources.Load<GameObject>("Stickman");
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab, pos, Quaternion.identity);
            go.name = VisitorName;
            var ctrl = go.GetComponent<StickmanController>();
            if (ctrl == null) ctrl = go.AddComponent<StickmanController>();

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
            var wandering = go.GetComponent<WanderingNPC>();
            if (wandering != null) wandering.enabled = false;
            return ctrl;
        }

        static StickmanController SpawnPlaceholder(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = VisitorName;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            var ctrl = go.AddComponent<StickmanController>();
            return ctrl;
        }

        static Vector3 ResolveStandPoint()
        {
            var well = GameObject.Find("StoneWell");
            if (well != null)
            {
                var p = well.transform.position + new Vector3(2.8f, 0f, -1.2f);
                p.y = 0f;
                return p;
            }
            var tree = GameObject.Find(FirstWishTreePlant.TreeName);
            if (tree != null)
            {
                var p = tree.transform.position + new Vector3(3f, 0f, -2f);
                p.y = 0f;
                return p;
            }
            return new Vector3(3f, 0f, -5f);
        }
    }
}

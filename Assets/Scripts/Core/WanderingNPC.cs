using UnityEngine;
using UnityEngine.AI;

namespace RadiantOrchard
{
    // Ambient background NPC: strolls the island, pausing to idle between legs,
    // forever. Deliberately separate from StickmanController's request/heal
    // state machine (Idle_Sad/Receiving/Celebrate/Leaving) — these are pure
    // scenery, never assigned a requiredFruit, so they should never show a
    // symptom bubble or react to tonic delivery.
    //
    // ROAMING AREA: by default a leg's destination is a uniformly random point
    // on the whole baked NavMesh (roamWholeIsland), not a point in a small circle
    // around the spawn position. Circling the spawn point is what made every NPC
    // loiter in the tree cluster it happened to start in — and because those legs
    // were short, a blocked or unreachable destination left them standing there.
    // Island-wide destinations are also validated before use (reachable path,
    // sensible leg length), with a stuck timer as a backstop, so an NPC that
    // can't make progress immediately picks somewhere else instead of freezing.
    //
    // Drives the Animator's "Speed" float (see StickmanState.controller's
    // Idle_Sad<->Walk transitions) so the real walk clip plays while moving.
    // The NavMeshAgent still owns translation (no root motion) — to avoid
    // foot-sliding, Animator.speed is scaled every frame so the clip's own
    // authored pace (walkClipSpeed, its baked-in forward travel speed) always
    // matches the agent's actual velocity, instead of playing at a fixed
    // rate that only matches translation at one specific agent speed.
    // NOTE: every Stickman instance in the main scene carries both this
    // component and StickmanController, which now owns the same
    // NavMeshAgent/Animator far more completely (rescue flow + the Spirit-driven
    // ambient life loop). StickmanController.Awake() disables this component on
    // any GameObject that has one, so the two never fight over destinations or
    // animation. WanderingNPC remains here, fully functional, for any future
    // NPC that should *only* wander ambiently with no rescue/Spirit behavior.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class WanderingNPC : MonoBehaviour
    {
        [Header("Roaming area")]
        // Whole-island roaming (the default) vs. the legacy tight circle below.
        [SerializeField] bool roamWholeIsland = true;
        // Shortest/longest leg to accept, in metres. The minimum stops NPCs
        // shuffling on the spot; the maximum stops one trekking the full
        // island every single leg.
        [SerializeField] float minTravelDistance = 8f;
        [SerializeField] float maxTravelDistance = 40f;
        [SerializeField] int destinationAttempts = 12;
        // Legacy fallback radius used only when roamWholeIsland is off.
        [SerializeField] float wanderRadius = 8f;

        [SerializeField] float minIdleTime = 2f;
        [SerializeField] float maxIdleTime = 5f;
        [SerializeField] float arriveThreshold = 0.3f;

        // No progress toward the destination for this long = re-pick. Covers a
        // path that became blocked after it was chosen, or a crowding agent.
        [SerializeField] float stuckTimeout = 2.5f;

        // Both male/female walk clips were authored at ~1.17 m/s forward
        // travel (AnimationClip.averageSpeed.z at import) — close enough to
        // share one calibration constant rather than needing to know which
        // gender variant this instance ended up with.
        [SerializeField] float walkClipSpeed = 1.17f;
        [SerializeField] float minAnimatorSpeed = 0.6f;
        [SerializeField] float maxAnimatorSpeed = 1.6f;

        NavMeshAgent agent;
        Animator animator;
        Vector3 homePosition;
        float idleTimer;
        float stuckTimer;
        bool idling;
        bool warnedAboutNavMesh;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            homePosition = transform.position;
        }

        void Start()
        {
            // Only start roaming if actually on NavMesh.
            // If not on NavMesh, Update() will warn once and skip movement.
            if (agent != null && agent.isOnNavMesh)
                PickNewDestination();
        }

        void Update()
        {
            if (agent == null) return;

            if (!agent.isOnNavMesh)
            {
                // Roaming needs a baked NavMesh under the NPC. Say so once instead
                // of silently standing still ("stickmen never move" with no clue
                // in the console) — re-bake with Tools ▸ Radiant Orchard ▸ Bake
                // NavMesh after the island changes.
                if (!warnedAboutNavMesh)
                {
                    warnedAboutNavMesh = true;
                    Debug.LogWarning($"{name}: WanderingNPC is off the NavMesh at {transform.position} — " +
                                     "re-bake the NavMesh (Tools ▸ Radiant Orchard ▸ Bake NavMesh).", this);
                }
                return;
            }

            float speed = agent.velocity.magnitude;
            UpdateAnimator(speed);

            if (idling)
            {
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f) PickNewDestination();
                return;
            }

            if (agent.pathPending) return;

            // Arrived — pause somewhere between min/maxIdleTime, then move on.
            if (agent.remainingDistance <= arriveThreshold)
            {
                idling = true;
                idleTimer = Random.Range(minIdleTime, maxIdleTime);
                return;
            }

            // Not arrived and not moving: the destination is probably blocked.
            stuckTimer = speed > 0.05f ? 0f : stuckTimer + Time.deltaTime;
            if (stuckTimer >= stuckTimeout) PickNewDestination();
        }

        void UpdateAnimator(float speed)
        {
            if (animator == null) return;

            animator.SetFloat("Speed", speed);

            if (speed > 0.05f)
                animator.speed = Mathf.Clamp(speed / walkClipSpeed, minAnimatorSpeed, maxAnimatorSpeed);
            else
                animator.speed = 1f; // normal playback rate for Idle_Sad once stopped
        }

        void PickNewDestination()
        {
            idling = false;
            stuckTimer = 0f;

            for (int attempt = 0; attempt < Mathf.Max(1, destinationAttempts); attempt++)
            {
                if (!TryPickCandidate(out var candidate)) break;

                // Leg length filter only applies to island-wide roaming; the
                // legacy circle already bounds the distance.
                if (roamWholeIsland)
                {
                    float travel = Vector3.Distance(agent.transform.position, candidate);
                    if (travel < minTravelDistance || travel > maxTravelDistance) continue;
                }

                if (!IsReachable(candidate)) continue;

                agent.SetDestination(candidate);
                return;
            }

            // Nothing usable this attempt (tiny island piece, everything too far,
            // paths blocked) — idle and retry next cycle rather than retrying
            // every frame.
            idling = true;
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
        }

        bool TryPickCandidate(out Vector3 point)
        {
            point = Vector3.zero;

            if (!roamWholeIsland)
            {
                Vector2 offset = Random.insideUnitCircle * wanderRadius;
                var near = homePosition + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(near, out var nearHit, 2.5f, NavMesh.AllAreas)) return false;
                point = nearHit.position;
                return true;
            }

            // Uniform random point over the whole baked NavMesh: pick a random
            // triangle, then a random barycentric point inside it. Sampling a
            // point in a bounding circle instead would land constantly off the
            // island (and in the sea), and picking triangle corners would bunch
            // every destination along mesh edges.
            var triangulation = NavMesh.CalculateTriangulation();
            int triangleCount = triangulation.indices.Length / 3;
            if (triangleCount <= 0) return false;

            int t = Random.Range(0, triangleCount) * 3;
            Vector3 a = triangulation.vertices[triangulation.indices[t]];
            Vector3 b = triangulation.vertices[triangulation.indices[t + 1]];
            Vector3 c = triangulation.vertices[triangulation.indices[t + 2]];

            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;
            Vector3 sampled = (1f - r1) * a + r1 * (1f - r2) * b + r1 * r2 * c;

            // Triangulation vertices sit on the surface, but re-sampling keeps
            // the agent's own position tolerance in play.
            if (!NavMesh.SamplePosition(sampled, out var hit, 1.5f, NavMesh.AllAreas)) return false;
            point = hit.position;
            return true;
        }

        // A destination the agent cannot actually path to is what turns into a
        // frozen NPC standing at the water's edge, so check the path first.
        //
        // Allocated on first use, never as a field initializer: Unity runs field
        // initializers inside the MonoBehaviour constructor, and NavMeshPath's
        // constructor calls the engine's InitializeNavMeshPath, which the engine
        // refuses to run from a constructor — "InitializeNavMeshPath is not
        // allowed to be called from a MonoBehaviour constructor (or instance
        // field initializer)", once per NPC, failing the build/play session.
        private NavMeshPath pathCheck;

        bool IsReachable(Vector3 destination)
        {
            // Guard: CalculatePath crashes if agent is not on NavMesh
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
                return false;
            if (pathCheck == null) pathCheck = new NavMeshPath();
            if (agent.CalculatePath(destination, pathCheck) == false) return false;
            return pathCheck.status == NavMeshPathStatus.PathComplete;
        }
    }
}

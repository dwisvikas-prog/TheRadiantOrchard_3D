using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RadiantOrchard
{
    public enum StickmanState
    {
        Idle_Sad,
        Receiving,
        Celebrate,
        Roaming,
        Leaving
    }

    [RequireComponent(typeof(Animator))]
    public class StickmanController : MonoBehaviour
    {
        [SerializeField] private FruitData requiredFruit;
        [SerializeField] private Material  greyMat;
        [SerializeField] private Material  colorMat;
        [SerializeField] private int       vibrancyReward = 10;

        [Header("Walk overrides")]
        [SerializeField] private RuntimeAnimatorController maleWalkOverride;
        [SerializeField] private RuntimeAnimatorController femaleWalkOverride;

        [Header("Patience")]
        [SerializeField] private float patienceTime  = 25f;
        [SerializeField] private float leaveSpeed    = 2.5f;
        [SerializeField] private float leaveDuration = 5f;

        [Header("Magic Tree Exclusion")]
        // Stickmen are NEVER allowed to stand inside this radius of (0,0,0).
        // 10 = just outside the fence ring of the central magic tree.
        [SerializeField] private float treeExclusionRadius = 10f;

        [Header("Happy Roaming (after healed)")]
        [SerializeField] private float roamWalkSpeed  = 0.75f;
        // Whole-island roaming (default): each waypoint is a uniformly random
        // point on the entire baked NavMesh, same technique as WanderingNPC, so
        // a healed Stickman explores everywhere instead of pacing back and
        // forth around the one tree it happened to be healed next to.
        [SerializeField] private bool roamWholeIsland = true;
        [SerializeField] private float minRoamLegDistance = 8f;
        [SerializeField] private float maxRoamLegDistance = 45f;
        [SerializeField] private int roamDestinationAttempts = 12;
        [SerializeField] private float roamRadius     = 18f;   // legacy small-circle fallback when roamWholeIsland is off
        [SerializeField] private float roamPause      = 5f;    // wait at each waypoint
        [SerializeField] private float roamDuration   = 90f;   // 0 = stay forever
        [SerializeField] private float celebratePause = 4.0f;  // celebrate before roaming
        [Header("Color restore")]
        [SerializeField] private float colorRestoreDuration = 0.75f;

        [Header("Ambient life (Spirit-driven self-care loop, after being healed)")]
        [Tooltip("How far this NPC will look for a fruit source once its Spirit runs low.")]
        [SerializeField] private float ambientSearchRadius = 14f;
        [SerializeField] private float ambientDecisionInterval = 0.5f;
        [SerializeField] private float observeDuration = 1.0f;
        [SerializeField] private float interactDuration = 0.6f;
        [SerializeField] private float reactDuration = 0.5f;
        [SerializeField] private float healBeatDuration = 0.4f;
        [SerializeField] private Vector2 ambientCelebrateDurationRange = new Vector2(1.0f, 1.8f);
        [Tooltip("Movement speed multiplier applied while roaming, by current Spirit condition.")]
        [SerializeField] private float lowSpiritSpeedMultiplier = 0.7f;
        [SerializeField] private float sickSpeedMultiplier = 0.5f;
        // Both walk clips are authored at ~1.17 m/s forward travel (same
        // calibration WanderingNPC uses) — needed to scale Animator.speed so
        // the clip's own pace always matches the agent's actual velocity
        // instead of sliding whenever the agent moves at any other speed.
        [SerializeField] private float walkClipAuthoredSpeed = 1.17f;
        [SerializeField] private float minAnimatorSpeed = 0.6f;
        [SerializeField] private float maxAnimatorSpeed = 1.6f;

        // ── private ───────────────────────────────────────────────────────
        private Renderer[]   renderers;
        private Animator     anim;
        private NavMeshAgent agent;
        private SymptomBubble symptomBubble;
        private NPCSpiritComponent spirit;
        private NPCLookAt lookAt;

        private float patienceTimer;
        private float leaveTimer;
        private float stateTimer;
        private bool  warnedNoColorMat;

        // ── ambient sub-state (only meaningful while CurrentState == Roaming) ──
        public NPCBehaviorState AmbientBehaviorState { get; private set; } = NPCBehaviorState.Idle;
        public Transform AmbientTarget { get; private set; }
        private WorldFruitSource ambientFruitTarget;
        private float ambientDecisionTimer;
        private float ambientPhaseTimer;
        private Coroutine colorRestoreRoutine;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock colorMpb;

        public FruitData     RequiredFruit  => requiredFruit;
        public StickmanState CurrentState   { get; private set; }
        public bool          CanReceiveTonic =>
            CurrentState == StickmanState.Idle_Sad ||
            CurrentState == StickmanState.Receiving;

        // ── lifecycle ─────────────────────────────────────────────────────
        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            anim      = GetComponent<Animator>();
            agent     = GetComponent<NavMeshAgent>();
            spirit    = GetComponent<NPCSpiritComponent>();
            if (spirit == null) spirit = gameObject.AddComponent<NPCSpiritComponent>();
            lookAt    = GetComponent<NPCLookAt>();

            // WanderingNPC drives the same Animator/NavMeshAgent for pure ambient
            // scenery NPCs that have no rescue flow — every Stickman here has one,
            // so make sure it yields ownership instead of fighting this controller
            // for destinations/animation every frame.
            var wandering = GetComponent<WanderingNPC>();
            if (wandering != null) wandering.enabled = false;

            var walk = Random.value < 0.5f ? maleWalkOverride : femaleWalkOverride;
            if (walk != null && anim != null)
                anim.runtimeAnimatorController = walk;

            symptomBubble = GetComponentInChildren<SymptomBubble>();
            if (symptomBubble == null)
            {
                var go = new GameObject("SymptomBubble");
                go.transform.SetParent(transform, false);
                symptomBubble = go.AddComponent<SymptomBubble>();
            }

            if (GetComponent<StickmanTapPrompt>() == null)
                gameObject.AddComponent<StickmanTapPrompt>();
        }

        private void Start()
        {
            EnterIdleSad();
        }

        private void Update()
        {
            stateTimer += Time.deltaTime;

            switch (CurrentState)
            {
                // ── Waiting for fruit ──────────────────────────────────────
                case StickmanState.Idle_Sad:
                    patienceTimer += Time.deltaTime;
                    if (patienceTimer >= patienceTime) GiveUp();
                    break;

                // ── Short celebrate pause then start roaming ──────────────
                case StickmanState.Celebrate:
                    if (stateTimer >= celebratePause)
                        EnterRoaming();
                    break;

                // ── Happy wandering ───────────────────────────────────────
                case StickmanState.Roaming:
                    if (roamDuration > 0f && stateTimer >= roamDuration)
                    { Despawn(); return; }

                    UpdateAmbientLife();
                    break;

                // ── Walking off (uses Walk clip — not Sad Idle) ───────────
                case StickmanState.Leaving:
                    leaveTimer += Time.deltaTime;
                    // If no agent, push manually toward island edge
                    if (agent == null || !agent.isOnNavMesh)
                    {
                        Vector3 away = new Vector3(
                            transform.position.x, 0f, transform.position.z).normalized;
                        if (away == Vector3.zero) away = Vector3.forward;

                        // Face the direction we're actually moving — without
                        // this the body kept whatever rotation it had before
                        // Leaving and just slid backward/sideways (moonwalk).
                        var wantRot = Quaternion.LookRotation(away, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, wantRot, 10f * Time.deltaTime);
                        transform.position += away * leaveSpeed * Time.deltaTime;

                        // No NavMeshAgent driving the walk blend here, so the
                        // Speed param never got set — the Walk clip would play
                        // at whatever blend value was last left, static-looking.
                        if (anim != null)
                        {
                            anim.SetFloat("Speed", 1f);
                            anim.speed = 1f;
                        }
                    }
                    else
                    {
                        SyncWalkAnimatorSpeed();
                    }
                    if (leaveTimer >= leaveDuration) Despawn();
                    break;
            }
        }

        // Scales Animator.speed so the currently-playing walk clip's own
        // authored pace matches the agent's actual velocity — the same fix
        // WanderingNPC already uses. Without this, any state that plays the
        // "Leaving" clip at a different agent.speed than the clip was authored
        // for (roamWalkSpeed=1.4 vs leaveSpeed=2.5 vs a Spirit-condition-slowed
        // roam, all sharing one clip) visibly slides/skates.
        private void SyncWalkAnimatorSpeed()
        {
            if (anim == null || agent == null) return;
            float speed = agent.velocity.magnitude;
            anim.SetFloat("Speed", speed);
            anim.speed = speed > 0.05f
                ? Mathf.Clamp(speed / walkClipAuthoredSpeed, minAnimatorSpeed, maxAnimatorSpeed)
                : 1f;
        }

        // ── Ambient life: Spirit-driven self-care loop ──────────────────────
        // Runs only while happily Roaming after being rescued. A healthy NPC
        // just roams (unchanged). Once its Spirit fades enough (NPCSpiritComponent,
        // decaying in the background — see EnterRoaming), it autonomously
        // Searches for a suitable WorldFruitSource, Observes it, Approaches,
        // Interacts, receives its effect, Heals and Celebrates, then returns to
        // normal roaming — all without any player input, driven purely by its
        // own condition and what fruit is actually nearby (no magic knowledge:
        // FindBestFor only ever sees fruit within ambientSearchRadius).
        private void UpdateAmbientLife()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            bool busyWithSequence = AmbientBehaviorState != NPCBehaviorState.Idle &&
                                     AmbientBehaviorState != NPCBehaviorState.Walking &&
                                     AmbientBehaviorState != NPCBehaviorState.Searching;

            // Apply the Spirit-condition speed multiplier only while just
            // roaming/searching — an NPC that has already committed to
            // approaching a fruit moves with purposeful (full) speed.
            if (!busyWithSequence && spirit != null)
            {
                float mult = spirit.Condition switch
                {
                    NPCCondition.Sick => sickSpeedMultiplier,
                    NPCCondition.Injured => sickSpeedMultiplier,
                    NPCCondition.LowSpirit => lowSpiritSpeedMultiplier,
                    _ => 1f
                };
                agent.speed = roamWalkSpeed * mult;
            }

            SyncWalkAnimatorSpeed();

            if (!busyWithSequence)
            {
                AmbientBehaviorState = (agent.velocity.magnitude > 0.05f) ? NPCBehaviorState.Walking : NPCBehaviorState.Idle;

                // Base roaming waypoint logic (unchanged behavior when healthy).
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    StartCoroutine(PauseRoam());

                // Look for a need-satisfying fruit on a throttled interval —
                // proximity-bounded (ambientSearchRadius) and only touches the
                // small static WorldFruitSource registry, never the whole scene.
                ambientDecisionTimer -= Time.deltaTime;
                if (ambientDecisionTimer <= 0f)
                {
                    ambientDecisionTimer = ambientDecisionInterval;
                    TryBeginAmbientNeedSequence();
                }
                return;
            }

            ambientPhaseTimer -= Time.deltaTime;

            switch (AmbientBehaviorState)
            {
                case NPCBehaviorState.Observing:
                    if (ambientPhaseTimer <= 0f) EvaluateAndApproachOrReject();
                    break;

                case NPCBehaviorState.Approaching:
                    if (ambientFruitTarget == null) { EndAmbientSequence(); return; }
                    if (!agent.pathPending && agent.remainingDistance <= ambientFruitTarget.InteractRadius + 0.1f)
                        BeginAmbientInteract();
                    break;

                case NPCBehaviorState.Interacting:
                    if (ambientPhaseTimer <= 0f) BeginAmbientReceive();
                    break;

                case NPCBehaviorState.ReceivingTonic:
                    if (ambientPhaseTimer <= 0f) BeginAmbientHealing();
                    break;

                case NPCBehaviorState.Healing:
                    if (ambientPhaseTimer <= 0f) BeginAmbientCelebrate();
                    break;

                case NPCBehaviorState.Celebrating:
                    if (ambientPhaseTimer <= 0f) EndAmbientSequence();
                    break;
            }
        }

        private void TryBeginAmbientNeedSequence()
        {
            if (spirit == null || spirit.CurrentNeed == NPCNeed.None) return;

            var found = WorldFruitSource.FindBestFor(transform.position, spirit.Condition, ambientSearchRadius);
            if (found == null)
            {
                AmbientBehaviorState = NPCBehaviorState.Searching; // still looking, none in range yet
                return;
            }

            // Noticed something — pause to look at it before deciding (Observing).
            ambientFruitTarget = found;
            AmbientTarget = found.transform;
            AmbientBehaviorState = NPCBehaviorState.Observing;
            ambientPhaseTimer = observeDuration;

            // This NPC is already happy/healed here — the only spare clip
            // ("Idle_Sad") reads as sad/head-down, which is the wrong mood for
            // an ambient pause-to-notice-something beat. Slow the walk way
            // down instead of hard-stopping into that clip: SyncWalkAnimatorSpeed
            // keeps blending the same walk clip's playback rate to the actual
            // (now tiny) velocity, so it reads as "slowing to look", never a
            // frozen walking-in-place pose and never the sad face.
            SettleForAmbientPause();
            if (lookAt != null) lookAt.SetForcedTarget(AmbientTarget);
        }

        [SerializeField] private float ambientPauseSpeed = 0.25f;

        private void SettleForAmbientPause()
        {
            if (!AgentOnNavMesh) return;
            agent.isStopped = false;
            agent.speed = ambientPauseSpeed;
        }

        private void EvaluateAndApproachOrReject()
        {
            bool suitable = ambientFruitTarget != null && ambientFruitTarget.IsAvailable &&
                            ambientFruitTarget.FruitData != null && ambientFruitTarget.FruitData.IsSuitableFor(spirit.Condition);

            if (!suitable)
            {
                // Reject and keep searching rather than approaching useless fruit.
                ambientFruitTarget = null;
                AmbientTarget = null;
                if (lookAt != null) lookAt.SetForcedTarget(null);
                SafeSetAgentStopped(false);
                if (AgentOnNavMesh) agent.speed = roamWalkSpeed;
                AmbientBehaviorState = NPCBehaviorState.Searching;
                return;
            }

            AmbientBehaviorState = NPCBehaviorState.Approaching;
            if (!AgentOnNavMesh) return;
            agent.isStopped = false;
            agent.stoppingDistance = ambientFruitTarget.InteractRadius;
            agent.speed = roamWalkSpeed;
            agent.SetDestination(ambientFruitTarget.transform.position);
            anim?.Play("Walk");
        }

        private void BeginAmbientInteract()
        {
            AmbientBehaviorState = NPCBehaviorState.Interacting;
            ambientPhaseTimer = interactDuration;
            agent.stoppingDistance = 0f;
            SettleForAmbientPause(); // same reasoning as Observing — no sad clip on a happy NPC

            // Face the fruit rather than freezing at whatever heading it arrived with.
            if (ambientFruitTarget != null)
            {
                Vector3 dir = ambientFruitTarget.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        private void BeginAmbientReceive()
        {
            AmbientBehaviorState = NPCBehaviorState.ReceivingTonic;
            ambientPhaseTimer = reactDuration;

            if (ambientFruitTarget != null)
            {
                ambientFruitTarget.Consume(spirit);
                SfxPlayer.Instance?.PlayHeal();
                SimpleVfx.Burst(transform.position + Vector3.up * 1.2f, new Color(0.6f, 1f, 0.7f), count: 16, speed: 3f, size: 0.16f, lifetime: 0.8f);
            }
        }

        private void BeginAmbientHealing()
        {
            AmbientBehaviorState = NPCBehaviorState.Healing;
            ambientPhaseTimer = healBeatDuration;
        }

        private void BeginAmbientCelebrate()
        {
            AmbientBehaviorState = NPCBehaviorState.Celebrating;
            // Controlled variation, not chaos: each NPC's celebration runs a
            // slightly different length so a group doesn't all finish in lockstep.
            ambientPhaseTimer = Random.Range(ambientCelebrateDurationRange.x, ambientCelebrateDurationRange.y);
            anim?.Play(PickCelebrateAnim());
        }

        private void EndAmbientSequence()
        {
            ambientFruitTarget = null;
            AmbientTarget = null;
            if (lookAt != null) lookAt.SetForcedTarget(null);
            SafeSetAgentStopped(false);
            if (AgentOnNavMesh)
            {
                agent.stoppingDistance = 0f;
                agent.speed = roamWalkSpeed;
            }
            AmbientBehaviorState = NPCBehaviorState.Walking;
            anim?.Play("Walk");
            PickRoamDest();
        }

        // ── Public API ────────────────────────────────────────────────────

        public void Initialize(FruitData fruit)
        {
            requiredFruit = fruit;
            patienceTimer = 0f;
            leaveTimer    = 0f;
            stateTimer    = 0f;
            warnedNoColorMat = false;

            SyncAgent();
            PushOutOfTree();   // ← ensure not spawned inside magic tree

            spirit?.ResetToFull();
            AmbientBehaviorState = NPCBehaviorState.Idle;
            AmbientTarget = null;
            ambientFruitTarget = null;
            ambientDecisionTimer = 0f;

            EnterIdleSad();
        }

        public void SetPatienceTime(float t) { if (t > 0f) patienceTime = t; }

        // Called by NPCSpawner only when far more healed NPCs are roaming than
        // the orchard was ever meant to hold at once (see its
        // maxRoamingStickmen) — a normal healed NPC never gets this call and
        // just roams forever.
        public void RetireFromRoaming()
        {
            if (CurrentState == StickmanState.Roaming) Despawn();
        }

        public void BeginReceiving()
        {
            if (!CanReceiveTonic) return;
            SetState(StickmanState.Receiving);
        }

        public void ReceiveTonic()
        {
            if (!CanReceiveTonic) return;

            if (colorMat == null && !warnedNoColorMat)
            {
                warnedNoColorMat = true;
                Debug.LogWarning($"{name}: colorMat not assigned — stickman stays grey.", this);
            }

            symptomBubble.SetVisible(false);
            stateTimer = 0f;
            SetState(StickmanState.Celebrate);
            SfxPlayer.Instance?.PlayHeal();
            EmptyIslandCameraFocus.Shake();
            FloatingNumberPopup.Show(transform.position + Vector3.up * 1.2f, "+" + vibrancyReward);

            // Proposal: regain color smoothly + per-fruit health VFX (heart /
            // breath / glow…), not one generic gold burst for every virtue.
            if (colorRestoreRoutine != null) StopCoroutine(colorRestoreRoutine);
            colorRestoreRoutine = StartCoroutine(ColorRestoreRoutine());
            HealVfx.Play(requiredFruit, transform.position);

            if (GameState.Instance != null)
            {
                GameState.Instance.AddVibrancy(vibrancyReward);
                GameState.Instance.NotifyStickmanHealed();
            }
        }

        // Instant grey→color swap looked cheap vs proposal "transformation".
        // Swap to color material, then lerp tint from grey up to full color.
        private System.Collections.IEnumerator ColorRestoreRoutine()
        {
            if (colorMat == null)
            {
                ApplyMat(greyMat);
                yield break;
            }

            ApplyMat(colorMat);
            ClearColorPropertyBlocks();

            Color from = greyMat != null && greyMat.HasProperty(BaseColorId)
                ? greyMat.GetColor(BaseColorId)
                : (greyMat != null && greyMat.HasProperty(ColorId) ? greyMat.GetColor(ColorId) : new Color(0.55f, 0.55f, 0.55f));
            Color to = colorMat.HasProperty(BaseColorId)
                ? colorMat.GetColor(BaseColorId)
                : (colorMat.HasProperty(ColorId) ? colorMat.GetColor(ColorId) : Color.white);

            float duration = Mathf.Max(0.05f, colorRestoreDuration);
            float t = 0f;
            if (colorMpb == null) colorMpb = new MaterialPropertyBlock();

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                Color c = Color.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                ApplyColorPropertyBlock(c);
                yield return null;
            }

            ClearColorPropertyBlocks();
            colorRestoreRoutine = null;
        }

        private void ApplyColorPropertyBlock(Color c)
        {
            if (renderers == null) return;
            colorMpb.Clear();
            colorMpb.SetColor(BaseColorId, c);
            colorMpb.SetColor(ColorId, c);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(colorMpb);
            }
        }

        private void ClearColorPropertyBlocks()
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(null);
            }
        }

        // ── Private state logic ───────────────────────────────────────────

        private void EnterIdleSad()
        {
            StopAllCoroutines();
            colorRestoreRoutine = null;
            patienceTimer = 0f;
            ClearColorPropertyBlocks();
            ApplyMat(greyMat);
            StopAgent();
            ResetLocomotionParams();
            SetState(StickmanState.Idle_Sad);
            RefreshBubble();
        }

        // Clears leftover Speed from a previous roam/leave so Idle_Sad↔Walk
        // animator transitions cannot yank a waiting Stickman into Walk.
        private void ResetLocomotionParams()
        {
            if (anim == null) return;
            anim.SetFloat("Speed", 0f);
            anim.speed = 1f;
        }

        private void GiveUp()
        {
            symptomBubble.SetVisible(false);
            leaveTimer = 0f;

            // Stakes: an unhealed visitor leaving costs the island some of its
            // vibrancy — without this, patience timers were cosmetic (nothing
            // ever went down, so there was no real reason to hurry).
            if (GameState.Instance != null)
            {
                const int penalty = 6;
                GameState.Instance.SetVibrancy(GameState.Instance.CurrentVibrancy - penalty);
                // Coach bubble only exists on the Empty Island scene — guard so
                // this doesn't spawn its UI in other scenes.
                if (EmptyIslandPhaseRunner.Instance != null)
                    EmptyIslandCoachBar.SetNewEvent("A visitor left unhealed — vibrancy dropped.");
            }

            // Critical: tear down any lesson/quiet-assist still waiting on THIS
            // visitor's harvest — otherwise FruitPlantLessonFlow.IsBusy never
            // clears, which permanently blocks every future visitor spawn
            // (and Shop/Edit, which also gate on IsBusy).
            FruitPlantLessonFlow.CancelFor(this);

            // Try to walk off via NavMesh
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed     = leaveSpeed;
                Vector3 edge    = transform.position.normalized * 80f;
                edge.y          = transform.position.y;
                if (NavMesh.SamplePosition(edge, out var h, 8f, NavMesh.AllAreas))
                    agent.SetDestination(h.position);
            }

            SetState(StickmanState.Leaving);
        }

        private bool AgentOnNavMesh =>
            agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

        private void SafeSetAgentStopped(bool stopped)
        {
            if (!AgentOnNavMesh) return;
            agent.isStopped = stopped;
        }

        private void EnterRoaming()
        {
            stateTimer = 0f;
            // Empty Island — stay forever and live on the pad (no despawn)
            if (FindFirstObjectByType<EmptyIslandBootstrap>() != null)
                roamDuration = 0f;

            if (AgentOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = roamWalkSpeed;
            }
            else if (agent != null)
            {
                agent.enabled = false;
            }
            SetState(StickmanState.Roaming);
            PickRoamDest();
            if (!AgentOnNavMesh)
            {
                // Don't play Walk until we face the path — prevents moonwalk after Celebrate
                if (anim != null)
                {
                    anim.SetFloat("Speed", 0f);
                    anim.Play("Idle", 0, 0f);
                }
                StartCoroutine(NoNavMeshWander());
            }

            spirit?.SetAmbientDecayEnabled(true);
        }

        // Empty Island happy life: gentle walk whole pad + talk/gesture beats
        // every 5–10s (not frozen in one spot).
        private IEnumerator NoNavMeshWander()
        {
            float walkSpeed = Mathf.Min(roamWalkSpeed, 0.7f);
            Vector3 center = Vector3.zero;
            var well = GameObject.Find("StoneWell");
            if (well != null) center = well.transform.position;

            while (CurrentState == StickmanState.Roaming)
            {
                if (AgentOnNavMesh) yield break;

                // ── Gentle stroll to a far random point on the island ──
                Vector3 dest = PickIslandWanderPoint(center);
                yield return GentleWalkIsland(dest, walkSpeed);
                if (CurrentState != StickmanState.Roaming) yield break;

                // ── Act: talk / thank / celebrate with another healed friend ──
                yield return DoSocialOrIdleBeat();
                if (CurrentState != StickmanState.Roaming) yield break;

                // Brief pause then next activity (5–10s total act time already)
                yield return new WaitForSeconds(Random.Range(0.4f, 1.2f));
            }
        }

        Vector3 PickIslandWanderPoint(Vector3 center)
        {
            float half = CoCBlankGround.PadHalf - 4f;
            for (int i = 0; i < 12; i++)
            {
                float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float rad = Random.Range(6f, half * 0.85f);
                Vector3 p = center + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                p.y = 0f;
                if (Vector3.Distance(p, transform.position) < 5f) continue;
                if (Vector3.Distance(p, center) < 4f) continue;
                return p;
            }
            return center + new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
        }

        IEnumerator GentleWalkIsland(Vector3 end, float speed)
        {
            end.y = 0f;
            Vector3 start = transform.position;
            start.y = 0f;
            transform.position = start;

            // Face travel direction NOW — slow Slerp after Celebrate caused moonwalk
            Vector3 travel = end - start;
            travel.y = 0f;
            if (travel.sqrMagnitude > 0.01f)
                transform.rotation = FaceAlong(travel.normalized);

            if (anim != null)
            {
                anim.applyRootMotion = false;
                anim.SetFloat("Speed", 1f);
                anim.Play("Walk", 0, 0f);
                anim.speed = Mathf.Clamp(speed / walkClipAuthoredSpeed, 0.65f, 1.1f);
            }

            float dist = Vector3.Distance(start, end);
            float travelDist = 0f;
            while (travelDist < dist - 0.05f && CurrentState == StickmanState.Roaming)
            {
                float rem = dist - travelDist;
                float ease = 1f;
                if (travelDist < 0.5f) ease = Mathf.SmoothStep(0.4f, 1f, travelDist / 0.5f);
                else if (rem < 0.6f) ease = Mathf.SmoothStep(0.45f, 1f, rem / 0.6f);

                travelDist = Mathf.Min(dist, travelDist + speed * ease * Time.deltaTime);
                float t = dist > 0.001f ? travelDist / dist : 1f;
                var p = Vector3.Lerp(start, end, t);
                p.y = 0f;

                // Always face where we're going (path), not tiny frame delta
                Vector3 face = end - transform.position;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, FaceAlong(face.normalized), 12f * Time.deltaTime);

                transform.position = p;
                yield return null;
            }
            transform.position = end;
            if (anim != null) anim.speed = 1f;
        }

        // Stickman walk clips face along transform.forward; keep a single helper
        // so social face + walk face stay consistent.
        static Quaternion FaceAlong(Vector3 flatDir)
        {
            if (flatDir.sqrMagnitude < 0.0001f) return Quaternion.identity;
            return Quaternion.LookRotation(flatDir.normalized, Vector3.up);
        }

        IEnumerator DoSocialOrIdleBeat()
        {
            // Prefer facing another healed / roaming stickman = "talking"
            StickmanController buddy = FindNearbyHappyBuddy();
            if (buddy != null)
            {
                Vector3 to = buddy.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = FaceAlong(to);

                // Walk a little closer if far
                float d = to.magnitude;
                if (d > 3.5f && d < 14f)
                {
                    Vector3 near = buddy.transform.position - to.normalized * 2.2f;
                    near.y = 0f;
                    yield return GentleWalkIsland(near, Mathf.Min(roamWalkSpeed, 0.65f));
                    to = buddy.transform.position - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.01f)
                        transform.rotation = FaceAlong(to);
                }

                anim?.SetFloat("Speed", 0f);
                // Talk / thank gestures
                string clip = Random.value < 0.5f ? "Celebrate_Thankful" : "Celebrate";
                anim?.Play(clip, 0, 0f);
                if (lookAt != null) lookAt.SetForcedTarget(buddy.transform);
                yield return new WaitForSeconds(Random.Range(5f, 9f));
                if (lookAt != null) lookAt.SetForcedTarget(null);
            }
            else
            {
                anim?.SetFloat("Speed", 0f);
                // Solo happy acts
                float roll = Random.value;
                if (roll < 0.4f) anim?.Play("Celebrate", 0, 0f);
                else if (roll < 0.75f) anim?.Play("Celebrate_Thankful", 0, 0f);
                else anim?.Play("Idle", 0, 0f);
                yield return new WaitForSeconds(Random.Range(5f, 10f));
            }

            if (anim != null && CurrentState == StickmanState.Roaming)
            {
                anim.Play("Idle", 0, 0f);
                anim.SetFloat("Speed", 0f);
            }
        }

        StickmanController FindNearbyHappyBuddy()
        {
            var all = FindObjectsByType<StickmanController>(FindObjectsSortMode.None);
            StickmanController best = null;
            float bestD = 16f;
            for (int i = 0; i < all.Length; i++)
            {
                var o = all[i];
                if (o == null || o == this) continue;
                if (o.CurrentState != StickmanState.Roaming &&
                    o.CurrentState != StickmanState.Celebrate) continue;
                float d = Vector3.Distance(transform.position, o.transform.position);
                if (d < bestD && d > 0.8f)
                {
                    bestD = d;
                    best = o;
                }
            }
            return best;
        }

        private void PickRoamDest()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            if (roamWholeIsland)
            {
                for (int i = 0; i < Mathf.Max(1, roamDestinationAttempts); i++)
                {
                    if (!TryPickWholeIslandPoint(out var candidate)) break;

                    float flat = new Vector2(candidate.x, candidate.z).magnitude;
                    if (flat < treeExclusionRadius) continue;

                    float travel = Vector3.Distance(transform.position, candidate);
                    if (travel < minRoamLegDistance || travel > maxRoamLegDistance) continue;

                    if (!IsPathReachable(candidate)) continue;

                    agent.SetDestination(candidate);
                    return;
                }
                // Fall through to the small-circle method below if the whole
                // island came up empty (tiny/unbaked mesh, everything filtered
                // out) rather than leaving the NPC standing still.
            }

            // Try 10 candidates, avoid magic tree centre
            for (int i = 0; i < 10; i++)
            {
                Vector2 r2   = Random.insideUnitCircle * roamRadius;
                Vector3 cand = transform.position + new Vector3(r2.x, 0f, r2.y);

                // reject if would place stickman inside tree exclusion zone
                float flat = new Vector2(cand.x, cand.z).magnitude;
                if (flat < treeExclusionRadius) continue;

                if (NavMesh.SamplePosition(cand, out var hit, 4f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }

            // Fallback — any valid point
            if (NavMesh.SamplePosition(
                    transform.position + (Vector3)(Random.insideUnitCircle * roamRadius),
                    out var fb, 6f, NavMesh.AllAreas))
                agent.SetDestination(fb.position);
        }

        // NavMesh.CalculateTriangulation() walks the whole baked mesh — expensive
        // enough that calling it per-attempt, per-Stickman, every roam-leg pick
        // was a real hitch source with several NPCs active. The navmesh doesn't
        // change during play, so every Stickman shares one cached triangulation
        // instead of each recomputing its own.
        private static NavMeshTriangulation cachedTriangulation;
        private static bool cachedTriangulationValid;

        private static NavMeshTriangulation GetSharedTriangulation()
        {
            if (!cachedTriangulationValid)
            {
                cachedTriangulation = NavMesh.CalculateTriangulation();
                cachedTriangulationValid = true;
            }
            return cachedTriangulation;
        }

        // Uniform random point over the whole baked NavMesh — same technique
        // WanderingNPC uses, so a healed Stickman's happy roaming actually
        // covers the island instead of a small circle around wherever it was
        // healed.
        private bool TryPickWholeIslandPoint(out Vector3 point)
        {
            point = Vector3.zero;
            var triangulation = GetSharedTriangulation();
            int triangleCount = triangulation.indices.Length / 3;
            if (triangleCount <= 0) return false;

            int t = Random.Range(0, triangleCount) * 3;
            Vector3 a = triangulation.vertices[triangulation.indices[t]];
            Vector3 b = triangulation.vertices[triangulation.indices[t + 1]];
            Vector3 c = triangulation.vertices[triangulation.indices[t + 2]];

            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;
            Vector3 sampled = (1f - r1) * a + r1 * (1f - r2) * b + r1 * r2 * c;

            if (!NavMesh.SamplePosition(sampled, out var hit, 1.5f, NavMesh.AllAreas)) return false;
            point = hit.position;
            return true;
        }

        private NavMeshPath roamPathCheck;

        private bool IsPathReachable(Vector3 destination)
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;
            if (roamPathCheck == null) roamPathCheck = new NavMeshPath();
            if (!agent.CalculatePath(destination, roamPathCheck)) return false;
            return roamPathCheck.status == NavMeshPathStatus.PathComplete;
        }

        private IEnumerator PauseRoam()
        {
            SafeSetAgentStopped(true);
            anim?.Play("Idle");
            yield return new WaitForSeconds(roamPause);
            if (CurrentState != StickmanState.Roaming) yield break;
            SafeSetAgentStopped(false);
            anim?.Play("Walk");
            PickRoamDest();
        }

        // Push stickman radially away from magic tree centre if too close
        private void PushOutOfTree()
        {
            if (treeExclusionRadius <= 0f) return;

            Vector2 flat2d = new Vector2(transform.position.x, transform.position.z);
            if (flat2d.magnitude >= treeExclusionRadius) return;

            // Direction away from tree + just past exclusion radius
            Vector2 push   = (flat2d.magnitude < 0.01f ? Vector2.right : flat2d.normalized)
                           * (treeExclusionRadius + 2f);
            Vector3 target = new Vector3(push.x, transform.position.y, push.y);

            if (NavMesh.SamplePosition(target, out var hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                if (agent != null && agent.isActiveAndEnabled)
                    agent.Warp(hit.position);
            }
        }

        private void SetState(StickmanState s)
        {
            CurrentState = s;
            // Animate by name — animator states must match StickmanState.controller.
            // Distinct clips per mood:
            //   Idle_Sad          → Idle.fbx (neutral wait — no sad-slouch clip)
            //   Receiving         → Quick Informal Bow.fbx
            //   Celebrate         → Happy.fbx  OR  Celebrate_Thankful → Thankful.fbx
            //   Idle (roam pause)  → Idle.fbx
            //   Walk / Leaving    → male walk.fbx (female override swaps walk)
            if (anim != null)
            {
                if (s == StickmanState.Idle_Sad ||
                    s == StickmanState.Receiving ||
                    s == StickmanState.Celebrate)
                    ResetLocomotionParams();

                string clip = s switch
                {
                    StickmanState.Idle_Sad  => "Idle_Sad",
                    StickmanState.Receiving => "Receiving",
                    StickmanState.Celebrate => PickCelebrateAnim(),
                    StickmanState.Roaming   => "Walk",
                    // Leaving used to share Sad Idle with Idle_Sad (moonwalk /
                    // frozen-sad while the agent moved). Controller now maps
                    // Leaving → walk clip; Play Walk so male/female overrides apply.
                    StickmanState.Leaving   => "Walk",
                    _                       => "Idle_Sad"
                };
                anim.Play(clip);
            }

            // Head look-at is suppressed during the two "hero pose" animations
            // where independent head rotation would fight the authored pose;
            // it's welcome everywhere else, including plain idling/roaming.
            if (lookAt != null)
            {
                lookAt.Suppressed = s == StickmanState.Receiving || s == StickmanState.Celebrate;
                // Correct walk tilt whenever a walk clip is playing (happy roam
                // or grey leave). Neutral idle needs no downward-glance correction.
                lookAt.CorrectWalkTilt =
                    s == StickmanState.Roaming || s == StickmanState.Leaving;
            }
        }

        // Randomize joy reaction so healed Stickmen don't all do the same dance.
        private static string PickCelebrateAnim() =>
            Random.value < 0.5f ? "Celebrate" : "Celebrate_Thankful";

        private void StopAgent()
        {
            if (agent == null || !agent.isActiveAndEnabled) return;
            if (!agent.isOnNavMesh) return;   // can't set isStopped when off-mesh
            agent.isStopped = true;
            agent.velocity  = Vector3.zero;
            agent.ResetPath();
        }

        private void SyncAgent()
        {
            if (agent == null || !agent.isActiveAndEnabled) return;
            if (!agent.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out var h, 2f, NavMesh.AllAreas))
                agent.Warp(h.position);
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        private void RefreshBubble()
        {
            if (symptomBubble == null) return;

            if (requiredFruit != null)
            {
                var entry = FruitHealthMatrix.Resolve(requiredFruit);
                symptomBubble.SetSymptom(entry.symptomLabel, entry.healthHint, entry.themeColor);
            }

            symptomBubble.SetVisible(CurrentState == StickmanState.Idle_Sad);
        }

        private void ApplyMat(Material mat)
        {
            if (renderers == null || mat == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var slots = r.sharedMaterials;
                if (slots.Length <= 1) { r.sharedMaterial = mat; continue; }
                for (int i = 0; i < slots.Length; i++) slots[i] = mat;
                r.sharedMaterials = slots;
            }
        }

        // ── Pool ──────────────────────────────────────────────────────────
        private static readonly Stack<StickmanController> pool =
            new Stack<StickmanController>();

        private void Despawn()
        {
            StopAllCoroutines();
            colorRestoreRoutine = null;
            ClearColorPropertyBlocks();
            StopAgent();
            spirit?.SetAmbientDecayEnabled(false);
            AmbientBehaviorState = NPCBehaviorState.Idle;
            AmbientTarget = null;
            ambientFruitTarget = null;
            if (lookAt != null) lookAt.SetForcedTarget(null);
            gameObject.SetActive(false);
            pool.Push(this);
        }

        public static StickmanController GetPooledOrNull()
        {
            while (pool.Count > 0)
            {
                var c = pool.Pop();
                if (c != null) return c;
            }
            return null;
        }
    }
}

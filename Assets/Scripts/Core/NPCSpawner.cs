using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RadiantOrchard
{
    // Periodically spawns a Stickman at a random spawn point with a random
    // FruitData assigned, plus the matching fruit object on a designated orchard
    // path spot away from that Stickman (FruitHarvester wired to that Stickman —
    // picking the fruit sends a tonic flying across to heal it). Each spawned Stickman then
    // owns its own patience timer, walk-away/despawn, and post-celebrate
    // despawn (StickmanController) — this only decides when/where to spawn,
    // cleans up an orphaned fruit if its Stickman leaves unhelped, and caps
    // how many pairs are active at once so new arrivals keep coming.
    public class NPCSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject stickmanPrefab;
        [SerializeField] private FruitData[] fruitPool;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 15f;
        [SerializeField] private int maxActiveStickmen = 3;
        [Tooltip("Healed Stickmen roam forever and don't count against maxActiveStickmen (see Update()), " +
                 "but a mobile session left running for a very long time would otherwise accumulate them " +
                 "without bound. Once this many are living happily in the orchard, the oldest is retired " +
                 "(pooled) to make room — high enough that a just-healed NPC is never affected by it.")]
        [SerializeField] private int maxRoamingStickmen = 15;
        [SerializeField] private Vector3 fruitOffset = new Vector3(1.2f, 0.6f, 0f);
        [SerializeField] private float fruitScale = 0.4f;

        [Header("Fruit placement (designated path areas)")]
        // Where harvested fruits appear. Wire the fenced orchard zones / path
        // markers here; left empty, the "Fruits/Orchard_<FruitType>" areas that
        // Tools ▸ Radiant Orchard ▸ Generate Orchard Fruit Zones places on the
        // path ring are discovered automatically. Fruits deliberately do NOT
        // spawn next to their Stickman any more — the player has to walk over to
        // the orchard area and pick the fruit there, which is what makes the
        // fruit a destination on the path instead of an icon floating beside the
        // NPC (and it stops the fruit being tapped by accident when the player
        // meant to interact with the Stickman).
        [SerializeField] private Transform[] fruitSpawnPoints;
        // Minimum gap between a fruit and the Stickman that needs it. If no spot
        // is that far away (small island, everything crowded), the furthest
        // available spot is used rather than dropping the fruit in the NPC's lap.
        [SerializeField] private float minFruitDistanceFromStickman = 12f;
        // Two fruits closer together than this count as sharing a spot — see
        // PickFruitPosition.
        [SerializeField] private float minFruitSpacing = 3f;
        // How many points to sample across the NavMesh when no spots are wired.
        [SerializeField] private int sampledFruitSpotCount = 12;
        // Applied in a spot marker's LOCAL space. An orchard zone root sits at the
        // centre of its fenced clearing with the crop plant on top of it, so
        // spawning exactly there would bury the fruit inside the bush — the zones
        // face inward, so a +Z nudge drops the fruit in front of the crop, inside
        // the fence, on the side the player walks past.
        [SerializeField] private Vector3 fruitSpotLocalOffset = new Vector3(0f, 0f, 2.2f);

        private class SpawnedPair
        {
            public StickmanController stickman;
            public FruitHarvester fruit;
        }

        private readonly List<SpawnedPair> active = new List<SpawnedPair>();
        private readonly List<StickmanController> roaming = new List<StickmanController>();
        private readonly List<Vector3> fruitSpots = new List<Vector3>();
        private float timer;
        private float patienceTime; // 0 = no override, spawned Stickman keeps its prefab default
        private bool loggedFirstSpawn;

        private void Start()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LevelChanged += ApplyPacing;
                ApplyPacing(LevelManager.Instance.CurrentLevel);
            }

            BuildFruitSpots();
            WarnAboutMissingWiring();
            QueueOrphanStickmen();

            // Without this the player stares at an empty island for a full
            // spawnInterval (15s by default) before the very first Stickman
            // appears — long enough for a mobile player to bounce. Every spawn
            // after the first still waits the full interval as before.
            timer = spawnInterval;
        }

        // Every guard below makes SpawnStickman() return without a word, so with
        // any of them unset the whole loop (visitor arrives → fruit on the path →
        // tonic → heal) never starts and the console explains nothing. These
        // warnings are what turn "the game does nothing" into a named cause.
        private void WarnAboutMissingWiring()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                Debug.LogWarning("NPCSpawner: no spawn points assigned — no Stickman will ever arrive, " +
                                 "so no fruit is ever placed. Assign Spawn Points (Tools ▸ Radiant Orchard wizards wire these).", this);
            else if (fruitPool == null || fruitPool.Length == 0)
                Debug.LogWarning("NPCSpawner: fruit pool is empty — a Stickman can arrive but never gets a fruit " +
                                 "to be healed with. Assign the GameData/FruitData assets.", this);
            else if (stickmanPrefab == null)
                Debug.LogWarning("NPCSpawner: no Stickman prefab assigned — the first spawn will be skipped. " +
                                 "Assign GameData/Prefabs/Stickman.prefab.", this);
        }

        // --- Fruit spots -----------------------------------------------------
        // Resolved once at level start, in priority order:
        //   1. spots wired in the Inspector,
        //   2. the orchard zones on the path ring ("Fruits/Orchard_<FruitType>"),
        //   3. uniformly sampled points across the baked NavMesh.
        // (3) is the safety net for an older scene: it still guarantees the fruit
        // is off the Stickman, which is the part the loop depends on.
        private void BuildFruitSpots()
        {
            fruitSpots.Clear();

            if (fruitSpawnPoints != null)
            {
                foreach (var spot in fruitSpawnPoints)
                    if (spot != null) fruitSpots.Add(spot.TransformPoint(fruitSpotLocalOffset));
            }

            if (fruitSpots.Count == 0)
            {
                var fruitsGroup = GameObject.Find("Fruits");
                if (fruitsGroup != null)
                {
                    foreach (Transform child in fruitsGroup.transform)
                    {
                        if (child != null && child.name.StartsWith("Orchard_"))
                            fruitSpots.Add(child.TransformPoint(fruitSpotLocalOffset));
                    }
                }
            }

            if (fruitSpots.Count == 0) SampleNavMeshSpots();

            if (fruitSpots.Count == 0)
                Debug.LogWarning("NPCSpawner: no fruit spots found and no NavMesh to sample — " +
                                 "fruits will fall back to sitting beside their Stickman.", this);
        }

        private void SampleNavMeshSpots()
        {
            var triangulation = NavMesh.CalculateTriangulation();
            int triangleCount = triangulation.indices.Length / 3;
            if (triangleCount <= 0) return;

            for (int i = 0; i < Mathf.Max(1, sampledFruitSpotCount); i++)
            {
                int t = Random.Range(0, triangleCount) * 3;
                Vector3 a = triangulation.vertices[triangulation.indices[t]];
                Vector3 b = triangulation.vertices[triangulation.indices[t + 1]];
                Vector3 c = triangulation.vertices[triangulation.indices[t + 2]];

                float r1 = Mathf.Sqrt(Random.value);
                float r2 = Random.value;
                Vector3 sampled = (1f - r1) * a + r1 * (1f - r2) * b + r1 * r2 * c;

                if (NavMesh.SamplePosition(sampled, out var hit, 2f, NavMesh.AllAreas))
                    fruitSpots.Add(hit.position);
            }
        }

        // Prefers a spot at least minFruitDistanceFromStickman away, and among
        // those the closest one, so the fruit is a trip but not a trek. Falls
        // back to the furthest spot when nothing clears the distance.
        private Vector3 PickFruitPosition(Vector3 stickmanPosition)
        {
            if (fruitSpots.Count == 0) return stickmanPosition + fruitOffset;

            // Pass 1 only considers spots no other live fruit is sitting on.
            // With up to maxActiveStickmen pairs on the path at once, two of them
            // picking the same spot put two fruits inside each other — one tap
            // then matched both (healing two Stickmen for one gesture) and the
            // second fruit was invisible under the first. Pass 2 falls back to
            // every spot so a crowded island still gets its fruit.
            if (TryPickSpot(stickmanPosition, freeOnly: true, out var spot)) return spot;
            if (TryPickSpot(stickmanPosition, freeOnly: false, out spot)) return spot;
            return fruitSpots[0];
        }

        private bool TryPickSpot(Vector3 stickmanPosition, bool freeOnly, out Vector3 result)
        {
            result = Vector3.zero;
            float bestDistance = 0f;
            bool bestClearsGap = false;
            bool found = false;

            for (int i = 0; i < fruitSpots.Count; i++)
            {
                var candidate = fruitSpots[i];
                if (freeOnly && IsSpotOccupied(candidate)) continue;

                float distance = Vector3.Distance(candidate, stickmanPosition);
                bool clearsGap = distance >= minFruitDistanceFromStickman;

                bool better = !found
                    || (clearsGap ? (!bestClearsGap || distance < bestDistance)
                                  : (!bestClearsGap && distance > bestDistance));

                if (better)
                {
                    result = candidate;
                    bestDistance = distance;
                    bestClearsGap = clearsGap;
                    found = true;
                }
            }

            return found;
        }

        // Is another live fruit already parked here? (A fruit waiting on its
        // post-harvest despawn still counts — it's visible to the player.)
        private bool IsSpotOccupied(Vector3 position)
        {
            for (int i = 0; i < active.Count; i++)
            {
                var fruit = active[i].fruit;
                if (fruit == null || !fruit.gameObject.activeSelf) continue;
                if (Vector3.Distance(fruit.transform.position, position) < minFruitSpacing) return true;
            }

            return false;
        }

        private void OnDestroy()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.LevelChanged -= ApplyPacing;
        }

        // Reads spawnInterval/maxActiveStickmen/patienceTime from the active
        // level instead of this component's own fixed inspector values, so
        // pacing actually tightens level to level (Issue Register: Per-level
        // pacing). A level with defaults untouched behaves exactly as before.
        private void ApplyPacing(LevelDefinition level)
        {
            if (level == null) return;
            spawnInterval = level.spawnInterval;
            maxActiveStickmen = level.maxActiveStickmen;
            patienceTime = level.patienceTime;
        }

        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                // A pooled Stickman is deactivated, not destroyed, when it's
                // done — check activeSelf too, not just null, or this list
                // would never notice a pooled Stickman has left.
                var sm = active[i].stickman;

                // Clear an unharvested fruit when its Stickman can no longer
                // receive a tonic — gave up or already healed.
                var fruit = active[i].fruit;
                if (fruit != null && !fruit.IsHarvestable && fruit.gameObject.activeSelf)
                    fruit.Despawn();

                // Once healed and happily Roaming, a Stickman lives on in the
                // scene indefinitely (StickmanController.roamDuration = 0 — see
                // its own ambient life loop) instead of being pooled/despawned.
                // It must still free its spawner slot here, or every visitor
                // that gets helped would permanently occupy one of
                // maxActiveStickmen and new needy visitors would stop arriving
                // after the very first batch.
                bool stillNeedsTracking = sm != null && sm.gameObject.activeSelf &&
                                          sm.CurrentState != StickmanState.Roaming;
                if (stillNeedsTracking) continue;

                if (sm != null && sm.gameObject.activeSelf && !roaming.Contains(sm))
                    roaming.Add(sm);

                active.RemoveAt(i);
            }

            // Retire the oldest happily-roaming NPC once far more of them are
            // alive than the orchard was ever meant to hold at once — see
            // maxRoamingStickmen's tooltip. A freshly healed NPC is nowhere
            // near the front of this queue, so it's never the one retired.
            for (int i = roaming.Count - 1; i >= 0; i--)
            {
                if (roaming[i] == null || !roaming[i].gameObject.activeSelf) roaming.RemoveAt(i);
            }
            while (roaming.Count > Mathf.Max(1, maxRoamingStickmen))
            {
                var oldest = roaming[0];
                roaming.RemoveAt(0);
                if (oldest != null) oldest.RetireFromRoaming();
            }

            if (active.Count >= maxActiveStickmen) return;

            timer += Time.deltaTime;
            if (timer < spawnInterval) return;

            timer = 0f;
            SpawnStickman();
        }

        // Any StickmanController already sitting in the scene at level start
        // (hand-placed, not spawned by this component) would otherwise wait in
        // Idle_Sad forever with no FruitHarvester ever created for it — this
        // spawner only ever pairs a fruit with a Stickman it spawns itself.
        // Queued here rather than adopted all at once: they come online one at
        // a time through the normal spawnInterval-paced cycle below (the very
        // first one still appears immediately, same as before), instead of
        // flooding the screen and instantly filling maxActiveStickmen so no
        // *new* visitor can arrive until the whole backlog clears.
        private readonly Queue<StickmanController> orphanQueue = new Queue<StickmanController>();

        private void QueueOrphanStickmen()
        {
            var existing = Object.FindObjectsByType<StickmanController>(FindObjectsSortMode.None);
            bool first = true;
            foreach (var stickman in existing)
            {
                if (stickman == null || !stickman.gameObject.activeSelf) continue;
                if (IsTracked(stickman)) continue;

                // Only the first pre-placed Stickman stays visible immediately —
                // every other one is hidden until SpawnStickman() dequeues and
                // activates it on the normal spawnInterval cadence below.
                // Previously all of them were left active here, so a scene with
                // several hand-placed NPCs dumped the whole cast on screen at
                // once instead of trickling them in one at a time.
                if (!first) stickman.gameObject.SetActive(false);
                first = false;

                // Its own patience countdown is already running (started in its
                // Start(), independent of this spawner) — freeze it so it can't
                // give up and vanish while merely waiting its turn in the queue.
                // SpawnStickman() restores the real patienceTime once it's
                // actually dequeued and given its fruit.
                stickman.SetPatienceTime(float.MaxValue);
                orphanQueue.Enqueue(stickman);
            }
        }

        private bool IsTracked(StickmanController stickman)
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i].stickman == stickman) return true;
            return false;
        }

        private void SpawnStickman()
        {
            // Bring one already-placed, never-paired Stickman online before
            // spawning/pooling a brand new one, so the pre-existing cast of
            // NPCs gradually joins the healable loop at the same one-at-a-time
            // pace as everything else, in their own original spot.
            StickmanController orphan;
            while (orphanQueue.Count > 0)
            {
                orphan = orphanQueue.Dequeue();
                // Deactivated in QueueOrphanStickmen() to stagger its entrance —
                // reactivate it here rather than skipping it, or every orphan
                // past the first would silently never come online.
                if (orphan == null || IsTracked(orphan)) continue;
                if (!orphan.gameObject.activeSelf) orphan.gameObject.SetActive(true);

                var orphanFruit = orphan.RequiredFruit != null
                    ? orphan.RequiredFruit
                    : (fruitPool != null && fruitPool.Length > 0 ? fruitPool[Random.Range(0, fruitPool.Length)] : null);
                if (orphanFruit == null) return;

                orphan.Initialize(orphanFruit);
                orphan.SetPatienceTime(patienceTime);
                var orphanHarvester = SpawnFruitFor(orphan, orphanFruit);
                active.Add(new SpawnedPair { stickman = orphan, fruit = orphanHarvester });
                EmptyIslandCameraFocus.FocusVisitor(orphan.transform);
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0) return;
            if (fruitPool == null || fruitPool.Length == 0) return;

            var point    = spawnPoints[Random.Range(0, spawnPoints.Length)];
            var fruitData = fruitPool[Random.Range(0, fruitPool.Length)];

            // FIX — Stickman spawning underground / floating:
            // Snap the spawn point's Y to the actual NavMesh surface so a
            // manually placed spawn point that drifted off the mesh still
            // lands the Stickman on solid ground.
            Vector3 spawnPos = point.position;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out var navHit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                spawnPos = navHit.position;

            var stickman = StickmanController.GetPooledOrNull();
            if (stickman != null)
            {
                stickman.transform.SetPositionAndRotation(spawnPos, point.rotation);
                stickman.gameObject.SetActive(true);
            }
            else
            {
                if (stickmanPrefab == null) return;
                var stickmanGO = Instantiate(stickmanPrefab, spawnPos, point.rotation);
                stickman = stickmanGO.GetComponent<StickmanController>();
                if (stickman == null) { Destroy(stickmanGO); return; }
            }

            stickman.Initialize(fruitData);
            stickman.SetPatienceTime(patienceTime);
            var fruitHarvester = SpawnFruitFor(stickman, fruitData);

            active.Add(new SpawnedPair { stickman = stickman, fruit = fruitHarvester });
            EmptyIslandCameraFocus.FocusVisitor(stickman.transform);

            // One line on the first pair only: confirms the loop's starting
            // condition (a fruit really did get placed away from the Stickman it
            // belongs to) without spamming a log three times a minute.
            if (!loggedFirstSpawn && fruitHarvester != null)
            {
                loggedFirstSpawn = true;
                float gap = Vector3.Distance(fruitHarvester.transform.position, stickman.transform.position);
                Debug.Log($"[NPCSpawner] First pair: {fruitData.displayName} ({fruitData.gestureType}) placed " +
                          $"{gap:F1}m from its Stickman. Fruit spots resolved: {fruitSpots.Count}.", this);
            }
        }

        private FruitHarvester SpawnFruitFor(StickmanController stickman, FruitData fruitData)
        {
            // ── Fruit spawns RIGHT NEXT TO the stickman ───────────────────
            // Old system put fruit on distant orchard spots — player had no idea
            // where to look. Now fruit appears beside the stickman who needs it
            // (1.5 units to the side, 0.8 units up = clearly visible floating
            // next to them). Player sees stickman → sees bubble → sees glowing
            // fruit right next to them → double-taps it.
            Vector3 position = stickman.transform.position
                              + stickman.transform.right * 1.5f
                              + Vector3.up * 0.8f;

            // Snap to NavMesh surface so it doesn't float underground
            if (UnityEngine.AI.NavMesh.SamplePosition(
                    stickman.transform.position + stickman.transform.right * 1.5f,
                    out var navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                position.y = navHit.position.y + 0.8f;
            }

            // Scale 1.5 → clearly visible from camera height
            var harvester = FruitHarvester.GetPooled(fruitData, position, 1.5f);
            harvester.name = "Fruit_" + fruitData.displayName;
            harvester.Initialize(fruitData, stickman);

            // Reset glow pulse base position after placement
            var pulse = harvester.GetComponent<FruitGlowPulse>();
            if (pulse != null) pulse.ResetBase();

            return harvester;
        }
    }
}

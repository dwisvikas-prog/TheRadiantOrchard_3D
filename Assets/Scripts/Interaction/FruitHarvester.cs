using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    // Per-fruit gesture target. GestureManager is scene-global and reports raw
    // screen positions, so this subscribes to all three of its events and, for
    // each, checks whether the gesture landed close enough to this fruit's
    // own screen position before reacting — a screen-space distance check,
    // not a 3D physics raycast, so it works the same whether or not the
    // fruit even has a collider and doesn't care what else is in front of it
    // in 3D space (occluding scenery shouldn't block a 2D tap target). On a
    // match against fruitData.gestureType it spawns a tonic, flies it to the
    // target Stickman (TonicFlight), and calls ReceiveTonic() on arrival.
    public class FruitHarvester : MonoBehaviour
    {
        [SerializeField] private FruitData fruitData;
        [SerializeField] private StickmanController targetStickman;
        [SerializeField] private GameObject tonicPrefab; // optional; placeholder sphere used if unassigned
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float tapRadiusPixels = 80f;
        [SerializeField] private float despawnAfterHarvestDelay = 1.5f; // 0 = never self-despawn

        private GestureManager gestureManager;
        private bool harvested;
        private bool pooled;
        private bool inPool;
        private FruitType poolFruitType;

        public FruitData FruitData => fruitData;
        public StickmanController TargetStickman => targetStickman;
        public bool Harvested => harvested;

        // Static event — OnboardingManager and anything else that needs to know
        // "any fruit was just harvested" without holding a reference to a specific
        // FruitHarvester instance subscribes here.
        public static event System.Action OnAnyHarvested;

        // A fruit is only worth tapping (or pointing a tutorial hint at) while
        // its Stickman is still waiting to be helped. Once that Stickman has
        // celebrated or given up and started walking off, the fruit is dead
        // weight: the gesture would launch a tonic at somebody who can no
        // longer react (ReceiveTonic early-returns), so the fruit would be
        // consumed for nothing and the player would see "the fruit did not
        // work". NPCSpawner/TutorialManager use this to clear/skip those.
        public bool IsHarvestable =>
            isActiveAndEnabled &&
            !harvested &&
            fruitData != null &&
            targetStickman != null &&
            targetStickman.CanReceiveTonic;

        // --- Live registry ----------------------------------------------------
        // Fruits register here while they are active so systems that need to
        // know where the harvest targets are on screen (the camera rig, which
        // must not pan a drag that is really a fruit gesture) can ask without a
        // per-frame FindObjectsByType. Pooled fruits drop out on deactivate.
        private static readonly List<FruitHarvester> live = new List<FruitHarvester>();

        private void OnEnable()
        {
            inPool = false;
            if (!live.Contains(this)) live.Add(this);
        }

        private void OnDisable()
        {
            live.Remove(this);
        }

        private void OnDestroy()
        {
            live.Remove(this);

            if (gestureManager == null) return;

            gestureManager.OnDoubleTap -= HandleDoubleTap;
            gestureManager.OnLongPress -= HandleLongPress;
            gestureManager.OnFastSwipe -= HandleFastSwipe;
        }

        // Screen-space query used by the camera rigs: is a press at this point
        // close enough to a harvestable fruit that it should be read as fruit
        // input rather than a camera drag? See CameraOrbitController's
        // fruitGesturePanRadiusPixels.
        public static bool IsPointerNearAnyFruit(Vector2 screenPos, float radiusPixels)
        {
            for (int i = 0; i < live.Count; i++)
            {
                var fruit = live[i];
                if (fruit == null || !fruit.IsHarvestable) continue;

                var camera = fruit.ResolvedCamera();
                if (camera == null) continue;

                Vector2 fruitScreenPos = camera.WorldToScreenPoint(fruit.transform.position);
                if (Vector2.Distance(fruitScreenPos, screenPos) <= radiusPixels) return true;
            }

            return false;
        }

        // A pickable fruit sits at a fixed world position set once when it's
        // spawned on its tree — it isn't parented to the tree, so dragging that
        // tree in Edit Mode (EditModeManager) left the fruit behind at the old
        // spot, effectively "missing" once the camera settled elsewhere after
        // Save. Called by EditModeManager right after it moves a tree root, so
        // any fruit that was sitting on it moves along by the same delta.
        public static void ShiftNearby(Vector3 oldCenter, Vector3 delta, float radius)
        {
            float sqrRadius = radius * radius;
            for (int i = 0; i < live.Count; i++)
            {
                var fruit = live[i];
                if (fruit == null) continue;
                if ((fruit.transform.position - oldCenter).sqrMagnitude <= sqrRadius)
                    fruit.transform.position += delta;
            }
        }

        // Camera.main is the intended reference, but a scene without the
        // MainCamera tag used to make every tap fail silently (IsPointOnThisFruit
        // just returned false), which is indistinguishable from "fruits can't be
        // harvested". Fall back to whatever camera the scene does have.
        private Camera ResolvedCamera()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();
            return targetCamera;
        }

        // Called by NPCSpawner right after AddComponent/pool-reuse (same frame,
        // before Start runs on a fresh instance) to wire up a fruit. Resets
        // harvested too, since a pooled reuse skips Start().
        public void Initialize(FruitData data, StickmanController target)
        {
            fruitData = data;
            targetStickman = target;
            harvested = false;
        }

        private void Start()
        {
            ResolvedCamera();
            BindGestures();
        }

        // Tutorial / late-spawned fruits call this if Start ran before GestureManager existed.
        public void BindGestures()
        {
            gestureManager = FindFirstObjectByType<GestureManager>();
            if (gestureManager == null)
            {
                Debug.LogWarning($"{name}: no GestureManager in the scene — this fruit can't be harvested.", this);
                return;
            }

            gestureManager.OnDoubleTap -= HandleDoubleTap;
            gestureManager.OnLongPress -= HandleLongPress;
            gestureManager.OnFastSwipe -= HandleFastSwipe;
            gestureManager.OnDoubleTap += HandleDoubleTap;
            gestureManager.OnLongPress += HandleLongPress;
            gestureManager.OnFastSwipe += HandleFastSwipe;
        }

        private void HandleDoubleTap(Vector2 screenPos) => TryHarvest(GestureType.DoubleTap, screenPos);
        private void HandleLongPress(Vector2 screenPos) => TryHarvest(GestureType.LongPress, screenPos);
        private void HandleFastSwipe(Vector2 start, Vector2 end) => TryHarvest(GestureType.FastSwipe, start);

        private void TryHarvest(GestureType gesture, Vector2 screenPos)
        {
            // A pooled instance keeps its GestureManager subscription alive even
            // while deactivated (SetActive doesn't sever C# event subscriptions),
            // so this guard is what actually stops an inactive/pooled fruit from
            // reacting to gestures aimed at whatever's currently on screen.
            if (!isActiveAndEnabled) return;
            // IsHarvestable also rejects a fruit whose Stickman has already
            // celebrated or walked off — see its comment: harvesting then would
            // spend the fruit on a heal that can no longer land.
            if (!IsHarvestable || gesture != fruitData.gestureType) return;
            if (!IsPointOnThisFruit(screenPos)) return;

            harvested = true;
            SfxPlayer.Instance?.PlayGestureSuccess();
            SimpleVfx.Burst(transform.position, fruitData != null ? fruitData.placeholderColor : Color.white);
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            OnAnyHarvested?.Invoke(); // notify OnboardingManager + any other listeners

            // VibrancyMeterUI's fruit icons key off this zoneId convention to
            // show per-fruit harvest feedback — used to be set by the now-removed
            // FruitZoneInteractable; this is the one harvest path now, so it's
            // the one place that reports it.
            if (GameState.Instance != null)
                GameState.Instance.SetFruitZoneStage(GameState.FruitZoneId(fruitData.fruitType), FruitGrowthStage.Harvested);

            SpawnAndSendTonic();

            // Its job is done once the tonic is on its way — a picked fruit
            // shouldn't sit around forever, especially with NPCSpawner
            // continuously adding fresh ones over time.
            if (despawnAfterHarvestDelay > 0f) StartCoroutine(DespawnAfter(despawnAfterHarvestDelay));
        }

        private IEnumerator DespawnAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Despawn();
        }

        // Called by NPCSpawner when this fruit's Stickman leaves unhelped —
        // the fruit is still unharvested, just orphaned.
        public void Despawn()
        {
            // Idempotent: NPCSpawner can clear an orphaned fruit in the same
            // frame its own post-harvest despawn runs. Pushing twice would put
            // one instance in the pool stack twice, and GetPooled would then
            // hand the same fruit object to two Stickmen at once.
            if (inPool) return;
            inPool = true;

            if (pooled)
            {
                gameObject.SetActive(false);
                Pool(poolFruitType).Push(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private bool IsPointOnThisFruit(Vector2 screenPos)
        {
            var camera = ResolvedCamera();
            if (camera == null) return false;
            Vector2 fruitScreenPos = camera.WorldToScreenPoint(transform.position);
            return Vector2.Distance(fruitScreenPos, screenPos) <= tapRadiusPixels;
        }

        private void SpawnAndSendTonic()
        {
            TonicFlight flight;
            if (tonicPrefab != null)
            {
                var tonicGO = Instantiate(tonicPrefab, transform.position, Quaternion.identity);
                flight = tonicGO.GetComponent<TonicFlight>();
                if (flight == null) flight = tonicGO.AddComponent<TonicFlight>();
            }
            else
            {
                // Pooled instead of Instantiate+Destroy — see TonicFlight.GetPooled.
                var spawnPos = transform.position + Vector3.up * 0.5f;
                var color = fruitData != null ? fruitData.placeholderColor : Color.white;
                flight = TonicFlight.GetPooled(spawnPos, color);
            }

            if (targetStickman != null) targetStickman.BeginReceiving();

            var destination = targetStickman != null
                ? targetStickman.transform.position + Vector3.up * 1.2f
                : transform.position;
            Transform stickTf = targetStickman != null ? targetStickman.transform : null;

            // Track tonic → stickman like a CoC arrow
            IslandTapTrack.FollowHealToStickman(flight.transform, stickTf);
            EmptyIslandCoachBar.SetTip("Tonic flying → visitor!\nWatch them heal");

            flight.FlyTo(destination, () =>
            {
                IslandTapTrack.Hide();
                if (targetStickman != null) targetStickman.ReceiveTonic();
            });
        }

        // --- Pooling ------------------------------------------------------
        // NPCSpawner used to CreatePrimitive + AddComponent a brand-new fruit
        // every spawn and Destroy() it on harvest/orphan (Issue Register: Object
        // pooling). GetPooled reuses a deactivated instance of the same shape
        // before creating a new one; color still varies via MaterialPropertyBlock
        // over one shared material per shape, so pooled fruit keep batching too.

        private static readonly Dictionary<FruitType, Stack<FruitHarvester>> pools =
            new Dictionary<FruitType, Stack<FruitHarvester>>();

        private static Stack<FruitHarvester> Pool(FruitType type)
        {
            if (!pools.TryGetValue(type, out var stack))
            {
                stack = new Stack<FruitHarvester>();
                pools[type] = stack;
            }
            return stack;
        }

        public static FruitHarvester GetPooled(FruitData data, Vector3 position, float scale)
        {
            var stack = Pool(data.fruitType);
            FruitHarvester harvester = null;
            while (stack.Count > 0 && harvester == null)
                harvester = stack.Pop();

            if (harvester == null)
            {
                var root = new GameObject("Fruit_" + data.fruitType);
                harvester = root.AddComponent<FruitHarvester>();
                harvester.pooled       = true;
                harvester.poolFruitType = data.fruitType;

                // Add pulsing glow/bob/spin component — works on any visual.
                root.AddComponent<FruitGlowPulse>();

                BuildVisual(root.transform, data);
            }

            // ── Size: bigger so gesture target is obvious on pad ──
            float finalScale = Mathf.Max(scale, 5.5f);
            harvester.transform.position   = position;
            harvester.transform.localScale = Vector3.one * finalScale;
            harvester.gameObject.SetActive(true);
            harvester.inPool    = false;
            harvester.harvested = false;

            return harvester;
        }

        // Builds the visible fruit model as a child of the pooled root: the
        // real 3D mesh (data.visualPrefab) when the fruit has one, otherwise
        // the old glowing primitive stand-in. Colliders are stripped either
        // way — harvesting uses a screen-space distance check, not physics.
        private static void BuildVisual(Transform root, FruitData data)
        {
            if (data.visualPrefab != null)
            {
                var visual = Instantiate(data.visualPrefab, root);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (var col in visual.GetComponentsInChildren<Collider>(true))
                    Destroy(col);
                return;
            }

            var prim = GameObject.CreatePrimitive(
                data.placeholderShape == PlaceholderShape.Cube ? PrimitiveType.Cube : PrimitiveType.Sphere);
            prim.name = "Visual";
            prim.transform.SetParent(root, false);

            var primCol = prim.GetComponent<Collider>();
            if (primCol != null) Destroy(primCol);

            bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            var shader  = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = data.placeholderColor };

            // Emission so it glows even without direct light
            mat.EnableKeyword("_EMISSION");
            Color emitColor = data.placeholderColor * 1.8f;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emitColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.8f);

            prim.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}

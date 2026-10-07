using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    // Every healsPerReward Stickmen healed, unlocks the next decoration —
    // cycling one per virtue — and places it at a free spot on the island.
    // GameState.AddDecoration both persists the unlock and fires an event, so
    // rebuilding previously-unlocked decorations on load and building a
    // freshly-unlocked one both go through the same BuildDecoration() call —
    // no separate "restore" path to keep in sync.
    public class RewardManager : MonoBehaviour
    {
        [SerializeField] private VirtueDefinition[] virtuePool; // 9 virtues; decorationIndex cycles through this
        [SerializeField] private EditModeManager editModeManager; // optional; registers each built decoration so it becomes draggable
        [SerializeField] private int healsPerReward = 3;
        [SerializeField] private float placementRadiusMin = 20f;
        [SerializeField] private float placementRadiusMax = 36f;
        [SerializeField] private float minSpacingFromOthers = 4f;
        [SerializeField] private int placementAttempts = 20;
        [SerializeField] private float groundRaycastHeight = 60f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private int healCount;
        private readonly List<Vector3> placedPositions = new List<Vector3>();

        private void Start()
        {
            if (GameState.Instance == null) return;

            foreach (var entry in GameState.Instance.UnlockedDecorations)
            {
                BuildDecoration(entry.decorationIndex, entry.position);
                placedPositions.Add(entry.position);
            }

            GameState.Instance.StickmanHealed += OnStickmanHealed;
            GameState.Instance.DecorationUnlocked += OnDecorationUnlocked;
        }

        private void OnDestroy()
        {
            if (GameState.Instance == null) return;
            GameState.Instance.StickmanHealed -= OnStickmanHealed;
            GameState.Instance.DecorationUnlocked -= OnDecorationUnlocked;
        }

        private void OnStickmanHealed()
        {
            healCount++;
            if (healCount % healsPerReward != 0) return;
            if (virtuePool == null || virtuePool.Length == 0 || GameState.Instance == null) return;

            int index = GameState.Instance.UnlockedDecorations.Count % virtuePool.Length;
            var position = FindOpenSpot();
            GameState.Instance.AddDecoration(index, position); // fires DecorationUnlocked -> OnDecorationUnlocked builds it
        }

        private void OnDecorationUnlocked(int index, Vector3 position)
        {
            BuildDecoration(index, position);
            placedPositions.Add(position);
        }

        private Vector3 FindOpenSpot()
        {
            for (int i = 0; i < placementAttempts; i++)
            {
                var candidate = RandomRingPoint();
                bool tooClose = false;
                foreach (var p in placedPositions)
                {
                    if (Vector3.Distance(candidate, p) < minSpacingFromOthers) { tooClose = true; break; }
                }
                if (!tooClose) return candidate;
            }

            return RandomRingPoint(); // fallback: a bit of overlap is fine, better than never placing it
        }

        private Vector3 RandomRingPoint()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(placementRadiusMin, placementRadiusMax);
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            // Raycast down to the actual ground instead of a fixed y=0.5 — this
            // used to only work by coincidence because the blockout island is
            // flat; it would float/clip the moment the final terrain isn't
            // (Issue Register: Decoration placement ignores terrain height).
            var origin = new Vector3(x, groundRaycastHeight, z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, groundRaycastHeight * 2f, groundLayers))
                return hit.point;

            return new Vector3(x, 0.5f, z); // fallback if nothing was hit
        }

        // Simple placeholder monument: stone base + a gem tinted with the
        // virtue's theme color. Swap for real art later — everything else
        // (unlock trigger, placement, save/load) stays the same.
        private void BuildDecoration(int index, Vector3 position)
        {
            var virtue = (virtuePool != null && index >= 0 && index < virtuePool.Length) ? virtuePool[index] : null;
            Color gemColor = virtue != null ? virtue.themeColor : Color.white;
            string label = virtue != null ? virtue.displayName : "Reward";

            var root = new GameObject("Decoration_" + label);
            root.transform.position = position;

            var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGO.name = "Base";
            baseGO.transform.SetParent(root.transform, false);
            baseGO.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            baseGO.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);
            TintAndStripCollider(baseGO, new Color(0.5f, 0.45f, 0.4f));

            var gemGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gemGO.name = "Gem";
            gemGO.transform.SetParent(root.transform, false);
            gemGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            gemGO.transform.localScale = Vector3.one * 0.6f;
            TintAndStripCollider(gemGO, gemColor);

            if (editModeManager != null) editModeManager.RegisterDecoration(root.transform);
            DecorationInfoTarget.Ensure(root, label,
                virtue != null ? "A reward for the virtue of " + label + "." : "A reward for helping visitors.");
            EmptyIslandLevelProgress.RegisterDecorationPlaced();
        }

        private static Material sharedDecorationMaterial;

        private static Material GetSharedDecorationMaterial()
        {
            if (sharedDecorationMaterial != null) return sharedDecorationMaterial;
            bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
            sharedDecorationMaterial = new Material(shader) { enableInstancing = true };
            return sharedDecorationMaterial;
        }

        private static void TintAndStripCollider(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = GetSharedDecorationMaterial();
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", color);
                mpb.SetColor("_Color", color);
                rend.SetPropertyBlock(mpb);
            }

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // decorative only
        }
    }
}

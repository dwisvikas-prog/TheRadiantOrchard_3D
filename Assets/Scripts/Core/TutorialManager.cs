using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // First-run onboarding: shows a one-time intro popup explaining the core
    // loop, then points TutorialPointerUI at the next unharvested fruit,
    // advancing it every time a Stickman is healed until none are left.
    public class TutorialManager : MonoBehaviour
    {
        // Internal, not private: GameState.ResetProgress() clears this key too,
        // so a full reset doesn't leave the tutorial permanently marked seen.
        internal const string SeenPrefKey = "RadiantOrchard_TutorialSeen";

        [SerializeField] private GameObject introPanel;
        [SerializeField] private Button continueButton;
        [SerializeField] private TutorialPointerUI pointer;
        // How often to re-check that the pointed fruit is still there to be
        // picked. Fruits are pooled and get recycled with their Stickman, so
        // the hinted fruit can vanish without any heal happening.
        [SerializeField] private float repointCheckInterval = 0.5f;

        private FruitHarvester pointedFruit;
        private float repointTimer;

        private void Start()
        {
            if (GameState.Instance != null)
                GameState.Instance.StickmanHealed += OnStickmanHealed;

            if (continueButton != null) continueButton.onClick.AddListener(DismissIntro);

            bool alreadySeen = PlayerPrefs.GetInt(SeenPrefKey, 0) == 1;
            if (introPanel != null) introPanel.SetActive(!alreadySeen);

            if (alreadySeen) PointAtNextFruit();
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.StickmanHealed -= OnStickmanHealed;
        }

        private void DismissIntro()
        {
            if (introPanel != null) introPanel.SetActive(false);
            PlayerPrefs.SetInt(SeenPrefKey, 1);
            PlayerPrefs.Save();
            PointAtNextFruit();
        }

        private void OnStickmanHealed() => PointAtNextFruit();

        // The hint used to be re-aimed only on a heal. If the pointed fruit's
        // Stickman gave up and walked off instead, NPCSpawner recycled that fruit
        // and the pointer kept hovering over an empty patch of path with a
        // gesture label on it — actively misleading the player about where to
        // pick. Re-check cheaply a few times a second.
        private void LateUpdate()
        {
            if (pointer == null) return;

            // While the intro popup is still up, point at nothing: the hint is
            // shown by DismissIntro, and a label floating behind the panel would
            // just look like clutter.
            if (introPanel != null && introPanel.activeSelf) return;

            repointTimer -= Time.deltaTime;
            if (repointTimer > 0f) return;
            repointTimer = Mathf.Max(0.1f, repointCheckInterval);

            if (pointedFruit == null || !pointedFruit.IsHarvestable) PointAtNextFruit();
        }

        private void PointAtNextFruit()
        {
            if (pointer == null) return;

            repointTimer = Mathf.Max(0.1f, repointCheckInterval);

            var next = SelectFruit();
            pointedFruit = next;

            if (next == null)
            {
                // No fruit to point at right now (between spawns) — hide rather
                // than leave a stale label floating on the path.
                pointer.Hide();
                return;
            }

            pointer.PointAt(next.transform, GestureLabel(next.FruitData.gestureType));
        }

        // Only fruits that can still be turned into a heal count (see
        // FruitHarvester.IsHarvestable), and among those the one nearest the
        // middle of the screen wins: fruits now sit out on the orchard paths
        // across the island, so the "next" fruit in list order may be behind the
        // camera, and pointing at something off-screen teaches nothing.
        private FruitHarvester SelectFruit()
        {
            var candidates = FindObjectsByType<FruitHarvester>(FindObjectsSortMode.None);
            var camera = Camera.main;

            if (camera == null)
                return candidates.FirstOrDefault(h => h != null && h.IsHarvestable);

            FruitHarvester best = null;
            float bestScore = float.MaxValue;
            var screenCenter = new Vector2(0.5f, 0.5f);

            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.IsHarvestable) continue;

                Vector3 viewport = camera.WorldToViewportPoint(candidate.transform.position);

                // 0..~0.8 for on-screen fruits, 1+ for off-screen ones, so an
                // on-screen fruit always beats one the player can't see.
                float score = viewport.z <= 0f
                    ? float.MaxValue
                    : Vector2.Distance(new Vector2(viewport.x, viewport.y), screenCenter)
                      + (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f ? 1f : 0f);

                if (best == null || score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static string GestureLabel(GestureType type)
        {
            switch (type)
            {
                case GestureType.DoubleTap: return "Double-tap!";
                case GestureType.LongPress: return "Hold!";
                case GestureType.FastSwipe: return "Swipe!";
                default: return "Tap!";
            }
        }
    }
}

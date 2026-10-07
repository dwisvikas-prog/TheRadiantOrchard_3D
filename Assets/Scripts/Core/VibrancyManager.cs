using UnityEngine;

namespace RadiantOrchard
{
    // Thin compatibility facade over GameState's vibrancy tracking. GameState
    // stays the only real state (currentVibrancy, VibrancyChanged, save/load) —
    // this just forwards to it, so code written against "VibrancyManager
    // .Instance" (a common tutorial/snippet naming) works without a second,
    // diverging source of truth.
    public class VibrancyManager : MonoBehaviour
    {
        public static VibrancyManager Instance { get; private set; }

        public int CurrentVibrancy => GameState.Instance != null ? GameState.Instance.CurrentVibrancy : 0;
        public int MaxVibrancy => GameState.Instance != null ? GameState.Instance.MaxVibrancy : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void AddVibrancy(int amount)
        {
            if (GameState.Instance != null) GameState.Instance.AddVibrancy(amount);
        }

        public void SetVibrancy(int value)
        {
            if (GameState.Instance != null) GameState.Instance.SetVibrancy(value);
        }
    }
}

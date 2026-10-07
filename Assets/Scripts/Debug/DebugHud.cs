#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace RadiantOrchard
{
    // Temporary Play-mode readout for verifying GameState/interactions before
    // the real Vibrancy Meter UI (Phase 13) exists. Safe to delete once that lands.
    // Editor/dev-build only — never compiled into a release build.
    public class DebugHud : MonoBehaviour
    {
        private string lastEvent = "(no events yet)";

        private void Start()
        {
            if (GameState.Instance == null) return;
            GameState.Instance.VibrancyChanged += OnVibrancyChanged;
            GameState.Instance.NpcEmotionChanged += OnNpcEmotionChanged;
            GameState.Instance.FruitZoneChanged += OnFruitZoneChanged;
        }

        private void OnDestroy()
        {
            if (GameState.Instance == null) return;
            GameState.Instance.VibrancyChanged -= OnVibrancyChanged;
            GameState.Instance.NpcEmotionChanged -= OnNpcEmotionChanged;
            GameState.Instance.FruitZoneChanged -= OnFruitZoneChanged;
        }

        private void OnVibrancyChanged(int current, int max) => lastEvent = $"Vibrancy -> {current}/{max}";
        private void OnNpcEmotionChanged(string npcId, EmotionState emotion) => lastEvent = $"NPC '{npcId}' -> {emotion}";
        private void OnFruitZoneChanged(string zoneId, FruitGrowthStage stage) => lastEvent = $"Fruit '{zoneId}' -> {stage}";

        private void OnGUI()
        {
            if (GameState.Instance == null) return;

            GUI.Box(new Rect(10, 10, 340, 60), GUIContent.none);
            GUI.Label(new Rect(20, 15, 320, 20), $"Vibrancy: {GameState.Instance.CurrentVibrancy} / {GameState.Instance.MaxVibrancy}");
            GUI.Label(new Rect(20, 35, 320, 20), $"Last event: {lastEvent}");
        }
    }
}
#endif

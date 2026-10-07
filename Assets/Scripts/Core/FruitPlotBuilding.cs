using UnityEngine;

namespace RadiantOrchard
{
    // CoC-style fruit plot building: level 1–3 upgrade, scales crops / fence feel.
    public class FruitPlotBuilding : MonoBehaviour
    {
        public FruitType FruitType { get; private set; }
        public int Level { get; private set; } = 1;
        public const int MaxLevel = 3;

        Color fruitColor = Color.red;

        public void Setup(FruitType type, Color color)
        {
            FruitType = type;
            fruitColor = color;
            Level = 1;
            ApplyVisual();
        }

        // Spend vibrancy (tonic progress) to upgrade — ties into existing resource loop.
        public bool TryUpgrade()
        {
            if (Level >= MaxLevel) return false;
            if (GameState.Instance == null) return false;
            int cost = 5 * Level;
            if (GameState.Instance.CurrentVibrancy < cost) return false;

            GameState.Instance.AddVibrancy(-cost);
            Level++;
            ApplyVisual();
            Debug.Log($"[FruitPlotBuilding] {FruitType} → Lv{Level} (cost {cost} vibrancy)");
            return true;
        }

        // Collect: small vibrancy reward (fruit → tonic vibe).
        public void Collect()
        {
            if (GameState.Instance == null) return;
            int gain = 2 + Level;
            GameState.Instance.AddVibrancy(gain);
            Debug.Log($"[FruitPlotBuilding] Collected {FruitType} +{gain} vibrancy");
        }

        void ApplyVisual()
        {
            float scale = 1f + (Level - 1) * 0.18f;
            transform.localScale = new Vector3(scale, 1f, scale);

            // Tint sign text with level
            var tm = GetComponentInChildren<TextMesh>();
            if (tm != null)
                tm.text = FruitType + " Lv" + Level;
        }
    }
}

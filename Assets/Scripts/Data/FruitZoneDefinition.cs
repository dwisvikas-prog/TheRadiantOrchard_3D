using UnityEngine;

namespace RadiantOrchard
{
    [CreateAssetMenu(fileName = "Fruit_", menuName = "Radiant Orchard/Fruit Zone Definition")]
    public class FruitZoneDefinition : ScriptableObject
    {
        public FruitType fruitType;
        public string displayName;
        public Color fruitColor = Color.white;
        public int vibrancyRewardOnHarvest = 5;
    }
}

using UnityEngine;

namespace RadiantOrchard
{
    [CreateAssetMenu(fileName = "Virtue_", menuName = "Radiant Orchard/Virtue Definition")]
    public class VirtueDefinition : ScriptableObject
    {
        public string virtueId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public Color themeColor = Color.white;
        public int vibrancyReward = 10;
    }
}

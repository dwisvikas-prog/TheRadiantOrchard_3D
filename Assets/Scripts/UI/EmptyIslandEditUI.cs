using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Edit lives on EmptyIslandHUD (next to Pause). This Ensure just makes sure HUD exists.
    /// </summary>
    public class EmptyIslandEditUI : MonoBehaviour
    {
        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            EmptyIslandHUD.Ensure();
            // Destroy leftover standalone Edit canvases from older builds
            var old = FindObjectsByType<EmptyIslandEditUI>(FindObjectsSortMode.None);
            for (int i = 0; i < old.Length; i++)
            {
                if (old[i] != null) Object.Destroy(old[i].gameObject);
            }
        }
    }
}

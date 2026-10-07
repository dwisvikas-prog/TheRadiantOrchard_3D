using UnityEngine;

namespace RadiantOrchard
{
    // Lets InteractionController "claim" a tap so GestureManager doesn't also
    // fire a harvest gesture for the exact same touch (Issue Register: No
    // touch-ownership arbitration). Time+radius based rather than a per-touch
    // flag, since InteractionController's raycast fires at touch-down and a
    // gesture like a long press only resolves much later — this way the check
    // works regardless of which script's Update() runs first in a given frame.
    // Deliberately tiny and stateless beyond this — not a general input manager.
    public static class TouchArbiter
    {
        private const float ClaimRadiusPixels = 50f;
        private const float ClaimWindowSeconds = 1f; // covers GestureManager's longPressThreshold

        private static float claimedAtTime = -999f;
        private static Vector2 claimedAtPosition;

        public static void Claim(Vector2 screenPos)
        {
            claimedAtTime = Time.unscaledTime;
            claimedAtPosition = screenPos;
        }

        public static bool WasRecentlyClaimedNear(Vector2 screenPos)
        {
            if (Time.unscaledTime - claimedAtTime > ClaimWindowSeconds) return false;
            return Vector2.Distance(claimedAtPosition, screenPos) <= ClaimRadiusPixels;
        }
    }
}

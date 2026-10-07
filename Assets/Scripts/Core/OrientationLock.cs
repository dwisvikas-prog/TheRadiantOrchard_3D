using UnityEngine;

namespace RadiantOrchard
{
    // Locks the game to landscape orientation on every platform.
    // Attach this to any persistent GameObject in the scene
    // (e.g. the same "GameManagers" object that holds GameState).
    //
    // No Inspector tweaks needed — just add the component and it works.
    public class OrientationLock : MonoBehaviour
    {
        private void Awake()
        {
            ApplyLandscapeLock();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyLandscapeLock();
        }

        private void ApplyLandscapeLock()
        {
            // LandscapeLeft  = home button on the right  (standard landscape)
            // LandscapeRight = home button on the left   (flipped landscape)
            Screen.autorotateToPortrait           = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft      = true;
            Screen.autorotateToLandscapeRight     = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}

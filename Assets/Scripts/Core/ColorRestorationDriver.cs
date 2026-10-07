using UnityEngine;

namespace RadiantOrchard
{
    // The one script that turns GameState's vibrancy ratio into the sweep
    // GreyToColor.shader reads. Every object using that shader reads these as
    // shader globals, not per-material properties, so they all stay in sync
    // automatically — this is the wire that was missing between the shader
    // and the rest of the game (Issue Register: Grey->Color shader).
    public class ColorRestorationDriver : MonoBehaviour
    {
        [SerializeField] private Transform sweepOrigin; // the Well/Tree; falls back to this transform
        [SerializeField] private float restorationRadius = 40f;
        [SerializeField] private float edgeSoftness = 6f;
        [SerializeField] private float smoothing = 0.6f; // seconds to ease toward a vibrancy change

        private static readonly int ProgressId = Shader.PropertyToID("_ColorRestorationProgress");
        private static readonly int OriginId = Shader.PropertyToID("_RestorationOrigin");
        private static readonly int RadiusId = Shader.PropertyToID("_RestorationRadius");
        private static readonly int SoftnessId = Shader.PropertyToID("_EdgeSoftness");

        private float current;
        private float target;

        private void OnEnable()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.VibrancyChanged += OnVibrancyChanged;
                OnVibrancyChanged(GameState.Instance.CurrentVibrancy, GameState.Instance.MaxVibrancy);
                current = target;
            }
            PushConstants();
        }

        private void OnDisable()
        {
            if (GameState.Instance != null)
                GameState.Instance.VibrancyChanged -= OnVibrancyChanged;
        }

        private void OnVibrancyChanged(int current_, int max) => target = max > 0 ? (float)current_ / max : 0f;

        private void Update()
        {
            current = Mathf.MoveTowards(current, target, Time.deltaTime / Mathf.Max(smoothing, 0.01f));
            Shader.SetGlobalFloat(ProgressId, current);
            PushConstants();
        }

        private void PushConstants()
        {
            Vector3 origin = sweepOrigin != null ? sweepOrigin.position : transform.position;
            Shader.SetGlobalVector(OriginId, origin);
            Shader.SetGlobalFloat(RadiusId, restorationRadius);
            Shader.SetGlobalFloat(SoftnessId, edgeSoftness);
        }
    }
}

using UnityEngine;

[ExecuteAlways]
public class TreeWindSway : MonoBehaviour
{
    [Header("Wind Settings")]
    public float swaySpeed = 1.5f;     // Hwa ki speed (kitni tezi se hilega)
    public float swayAmount = 1.5f;    // Hilne ka angle (degrees me, halki hwa ke liye kam rakhein)
    public bool swayX = true;
    public bool swayZ = true;

    [Header("Vibrancy Reactivity")]
    [Tooltip("Sway amount multiplier when the island is fully grey (0 = still).")]
    public float lowVibrancyMultiplier = 0.35f;

    private static readonly int ProgressId = Shader.PropertyToID("_ColorRestorationProgress");

    private Quaternion initialRotation;
    private bool hasInitialRotation;

    void OnEnable()
    {
        // Object ki original rotation save kar rahe hain
        initialRotation = transform.localRotation;
        hasInitialRotation = true;
    }

    void Update()
    {
        if (!hasInitialRotation) return;

        // Same wall-clock source everywhere (Time.time in Play, realtime in Edit) so the sway
        // doesn't speed up/slow down when toggling Play, same fix as the water flow.
        float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartupAsDouble;

        // Only swayAmount (amplitude) reacts to vibrancy, not swaySpeed inside
        // Sin/Cos — see LeafGentleBreeze.cs for why scaling the frequency term
        // itself would be wrong here.
        float liveliness = Mathf.Lerp(lowVibrancyMultiplier, 1f, Shader.GetGlobalFloat(ProgressId));

        // Sin/Cos waves ka use karke natural breeze rotation bana rahe hain
        float angleX = swayX ? Mathf.Sin(t * swaySpeed) * swayAmount * liveliness : 0f;
        float angleZ = swayZ ? Mathf.Cos(t * swaySpeed * 0.8f) * swayAmount * liveliness : 0f;

        // Initial rotation ke sath wind angle add kar rahe hain
        Quaternion targetRotation = initialRotation * Quaternion.Euler(angleX, 0f, angleZ);
        transform.localRotation = targetRotation;
    }
}

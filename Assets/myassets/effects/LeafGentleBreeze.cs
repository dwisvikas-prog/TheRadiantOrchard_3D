using UnityEngine;

public class MicroSmoothSway : MonoBehaviour
{
    [Header("Adjust These Values (Keep them very small)")]
    public float speed = 1.0f;       // Bahut slow speed
    public float intensity = 0.0005f;// Bilkul microscopic movement

    [Header("Vibrancy Reactivity")]
    [Tooltip("Sway intensity multiplier when the island is fully grey (0 = still, matches ColorRestorationDriver's _ColorRestorationProgress).")]
    public float lowVibrancyMultiplier = 0.35f;

    private static readonly int ProgressId = Shader.PropertyToID("_ColorRestorationProgress");

    private Vector3 originalPosition;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        // Only the amplitude (intensity) reacts to vibrancy, never the speed
        // inside Sin/Cos — scaling the frequency term itself would make this
        // sway's implied angular velocity spike whenever liveliness changes,
        // worse the longer the session has been running.
        float liveliness = Mathf.Lerp(lowVibrancyMultiplier, 1f, Shader.GetGlobalFloat(ProgressId));

        // Smooth sine wave for ultra-subtle wind breeze
        float xOffset = Mathf.Sin(Time.time * speed) * intensity * liveliness;
        float zOffset = Mathf.Cos(Time.time * speed * 0.7f) * intensity * liveliness;

        transform.localPosition = originalPosition + new Vector3(xOffset, 0f, zOffset);
    }
}
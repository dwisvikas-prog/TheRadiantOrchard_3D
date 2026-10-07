using UnityEngine;

// Circles a shared center point together with the rest of its flock (same center/radius/speed,
// only a small phase offset apart) so the birds visibly move as one group, not independently.
// Uses the same Time.time / realtimeSinceStartup split as the water and tree sway scripts so the
// flight speed doesn't change between Edit Mode (Scene view "Always Refresh") and Play mode.
[ExecuteAlways]
public class BirdFlight : MonoBehaviour
{
    [Header("Shared Flock Path")]
    public Vector3 center = Vector3.zero;
    public float radius = 45f;
    public float height = 42f;
    public float angularSpeedDegrees = 12f; // shared by the whole flock

    [Header("This Bird's Place In The Flock")]
    public float phaseOffsetDegrees = 0f;
    public float heightJitter = 0f;
    public float bobAmount = 0.6f;
    public float bobSpeed = 1.4f;

    [Header("Wings")]
    public Transform wingLeft;
    public Transform wingRight;
    public float flapSpeed = 9f;
    public float flapAngle = 45f;

    [Header("Vibrancy Reactivity")]
    [Tooltip("Bob/flap amplitude multiplier when the island is fully grey (0 = barely moving).")]
    public float lowVibrancyMultiplier = 0.4f;

    private static readonly int ProgressId = Shader.PropertyToID("_ColorRestorationProgress");

    void Update()
    {
        float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartupAsDouble;

        // Only bobAmount/flapAngle (amplitude) react to vibrancy — angularSpeedDegrees,
        // bobSpeed and flapSpeed all sit inside a Sin/Cos argument derived from
        // absolute time t, so scaling them by a value that changes over the
        // session would make the implied angular velocity spike (see
        // LeafGentleBreeze.cs); scaling the amplitude outside those functions
        // has no such issue.
        float liveliness = Mathf.Lerp(lowVibrancyMultiplier, 1f, Shader.GetGlobalFloat(ProgressId));

        float angleRad = (phaseOffsetDegrees + t * angularSpeedDegrees) * Mathf.Deg2Rad;
        float bob = Mathf.Sin(t * bobSpeed + phaseOffsetDegrees) * bobAmount * liveliness;

        Vector3 pos = center + new Vector3(Mathf.Cos(angleRad) * radius, height + heightJitter + bob, Mathf.Sin(angleRad) * radius);

        // Face the direction of travel (tangent to the circle) and bank slightly into the turn.
        Vector3 tangent = new Vector3(-Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
        Quaternion look = Quaternion.LookRotation(tangent, Vector3.up);
        Quaternion bank = Quaternion.Euler(0f, 0f, -12f);

        transform.position = pos;
        transform.rotation = look * bank;

        float flap = Mathf.Sin(t * flapSpeed + phaseOffsetDegrees) * flapAngle * liveliness;
        if (wingLeft != null) wingLeft.localRotation = Quaternion.Euler(0f, 0f, flap);
        if (wingRight != null) wingRight.localRotation = Quaternion.Euler(0f, 0f, -flap);
    }
}

using System.Collections;
using UnityEngine;

namespace RadiantOrchard
{
    // Clash-of-Clans-style opening: on scene start, the camera begins high and
    // wide above the orchard, sweeps in, and settles into the exact locked
    // isometric gameplay pose CameraOrbitController is authored to hold —
    // instead of just cutting straight to the gameplay camera.
    //
    // Reuses CameraOrbitController's own resting transform as the flyover's
    // landing pose (read directly off the Camera's authored position/rotation/
    // orthographicSize in the scene) rather than duplicating that framing here,
    // so the two can never drift apart. CameraOrbitController is disabled for
    // the flyover's duration (so player drag/zoom can't fight the animation)
    // and re-enabled once the camera lands exactly on its own captured resting
    // pose — [DefaultExecutionOrder] guarantees CameraOrbitController.Awake()
    // (which captures that resting pose) runs before this component touches
    // the transform at all.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public class IntroCameraFlyover : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float duration = 3.2f;
        [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Dramatic start pose (relative to the landing pose)")]
        [Tooltip("Degrees to sweep in from — the camera starts rotated this far around the pivot and eases back to the locked angle.")]
        [SerializeField] private float startYawOffsetDegrees = -80f;
        [Tooltip("Steeper starting pitch reads as a high drone/eagle view; eases down to the locked isometric pitch.")]
        [SerializeField] private float startPitchDegrees = 68f;
        [SerializeField] private float startDistanceMultiplier = 1.7f;
        [SerializeField] private float startOrthoSizeMultiplier = 2.3f;

        private CameraOrbitController orbitController;
        private Camera cam;

        private void Start()
        {
            cam = GetComponent<Camera>();
            orbitController = GetComponent<CameraOrbitController>();

            // Landing pose = exactly wherever the camera is authored to rest —
            // captured now, before this script moves anything.
            Vector3 landingPos = transform.position;
            Quaternion landingRot = transform.rotation;
            float landingOrtho = cam.orthographicSize;

            // Ground-plane pivot the camera is actually looking at, derived
            // generically from its own forward vector (no dependency on
            // CameraOrbitController's private pivot field).
            Vector3 pivot = landingPos;
            if (Mathf.Abs(transform.forward.y) > 0.0001f)
            {
                float t = -landingPos.y / transform.forward.y;
                if (t > 0f) pivot = landingPos + transform.forward * t;
            }

            float restDistance = Vector3.Distance(landingPos, pivot);
            float landingYaw = landingRot.eulerAngles.y;

            Quaternion startRot = Quaternion.Euler(startPitchDegrees, landingYaw + startYawOffsetDegrees, 0f);
            Vector3 startPos = pivot - (startRot * Vector3.forward) * (restDistance * startDistanceMultiplier);
            float startOrtho = landingOrtho * startOrthoSizeMultiplier;

            if (orbitController != null) orbitController.enabled = false;

            transform.SetPositionAndRotation(startPos, startRot);
            if (cam.orthographic) cam.orthographicSize = startOrtho;

            StartCoroutine(Flyover(startPos, startRot, startOrtho, landingPos, landingRot, landingOrtho));
        }

        private IEnumerator Flyover(Vector3 fromPos, Quaternion fromRot, float fromOrtho,
                                     Vector3 toPos, Quaternion toRot, float toOrtho)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / duration));

                transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPos, toPos, k),
                    Quaternion.Slerp(fromRot, toRot, k));
                if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(fromOrtho, toOrtho, k);

                yield return null;
            }

            // Land exactly on the authored resting pose — no drift from
            // accumulated float error over the animation.
            transform.SetPositionAndRotation(toPos, toRot);
            if (cam.orthographic) cam.orthographicSize = toOrtho;

            if (orbitController != null) orbitController.enabled = true;
        }
    }
}

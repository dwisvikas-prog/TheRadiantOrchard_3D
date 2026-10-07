using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    // Subtle procedural head-look so NPCs visibly notice the world instead of
    // walking with a permanently forward-locked head. No IK rig is set up on
    // the Stickman model, so this directly rotates the "Head" bone
    // (mixamorig:Head, found under Hips/Spine/Spine1/Spine2/Neck/Head on the
    // Stickman prefab) each frame by a small clamped offset — cheap, no
    // Animator.SetIKPosition/OnAnimatorIK needed, and it layers on top of
    // whatever clip is currently playing.
    //
    // Target selection runs on a shared decision interval (not every frame) and
    // reuses a small static registry of "interesting" transforms — other NPCs
    // and active WorldFruitSources — instead of scanning the whole scene.
    public class NPCLookAt : MonoBehaviour
    {
        [SerializeField] private string headBoneName = "Head";
        [SerializeField] private float decisionInterval = 1.2f;
        [SerializeField] private float noticeRadius = 10f;
        [SerializeField] private float maxYawDegrees = 55f;
        [SerializeField] private float maxPitchDegrees = 20f;
        [SerializeField] private float turnSpeedDegPerSec = 90f;
        [Tooltip("The walk clip itself has a baked-in downward head tilt (no better clip exists in the project). " +
                 "Applied as an immediate counter-pitch every frame — a slow blend can't catch up since the " +
                 "Animator re-applies the clip's full tilt every single frame.")]
        [SerializeField] private float walkTiltCorrectionDegrees = 25f;

        private static readonly List<Transform> interestingPoints = new List<Transform>();
        public static void RegisterPointOfInterest(Transform t) { if (t != null && !interestingPoints.Contains(t)) interestingPoints.Add(t); }
        public static void UnregisterPointOfInterest(Transform t) => interestingPoints.Remove(t);

        private Transform head;
        private Quaternion headRestLocalRotation;
        private Transform currentTarget;
        private float timer;

        // External systems (StickmanController's ambient brain) can force a
        // specific look target (e.g. the fruit it's approaching) and suppress
        // idle-glancing while doing so.
        private Transform forcedTarget;
        public void SetForcedTarget(Transform t) => forcedTarget = t;

        // Disabled during animations where head movement would look wrong
        // (Receiving/Celebrate hero poses) — StickmanController toggles this.
        public bool Suppressed { get; set; }

        // Only apply the walk clip's downward-tilt correction while actually
        // playing the happy Walk clip (Roaming) — Idle_Sad/Leaving intentionally
        // use the Sad Idle clip's downward glance, which should stay as-is.
        public bool CorrectWalkTilt { get; set; }

        private void Awake()
        {
            head = FindDeep(transform, headBoneName);
            if (head != null) headRestLocalRotation = head.localRotation;
            timer = Random.Range(0f, decisionInterval);
        }

        private void OnEnable() => RegisterPointOfInterest(transform);
        private void OnDisable() => UnregisterPointOfInterest(transform);

        // Must run in LateUpdate, not Update: Mecanim evaluates the Animator
        // and writes the clip's bone poses between Update and LateUpdate, so a
        // correction applied in Update() gets silently overwritten by the
        // animation the same frame and never actually shows up. Running here
        // instead lets this correct the head *after* the clip (e.g. the walk
        // clip's own baked-in downward head tilt) has already been applied.
        private void LateUpdate()
        {
            if (head == null) return;

            if (Suppressed)
            {
                head.localRotation = Quaternion.RotateTowards(head.localRotation, headRestLocalRotation, turnSpeedDegPerSec * Time.deltaTime);
                return;
            }

            // Counteract the clip's own downward tilt immediately, on top of
            // whatever pose the Animator just wrote this frame — a slow
            // RotateTowards blend would never catch up since the Animator
            // re-applies its full baked-in tilt again next frame regardless.
            Quaternion tiltCorrectedBase = CorrectWalkTilt
                ? head.localRotation * Quaternion.Euler(-walkTiltCorrectionDegrees, 0f, 0f)
                : head.localRotation;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = decisionInterval * Random.Range(0.8f, 1.3f);
                currentTarget = forcedTarget != null ? forcedTarget : PickAmbientTarget();
            }

            if (currentTarget != null)
            {
                Vector3 toTarget = currentTarget.position - head.position;
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);
                    float yaw = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -maxYawDegrees, maxYawDegrees);
                    float pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg, -maxPitchDegrees, maxPitchDegrees);
                    Quaternion desired = tiltCorrectedBase * Quaternion.Euler(pitch, yaw, 0f);
                    head.localRotation = Quaternion.RotateTowards(head.localRotation, desired, turnSpeedDegPerSec * Time.deltaTime);
                    return;
                }
            }

            // No look-at target this frame: just the tilt correction, applied
            // directly (not blended) so it's fully in effect every frame.
            head.localRotation = tiltCorrectedBase;
        }

        private Transform PickAmbientTarget()
        {
            // Occasionally look at nothing (natural idle sway back to forward).
            if (Random.value < 0.35f) return null;

            // Prefer a nearby fruit source (matches "notice fruit" requirement),
            // otherwise a nearby NPC.
            var fruit = WorldFruitSource.FindBestFor(transform.position, NPCCondition.LowSpirit, noticeRadius);
            if (fruit != null) return fruit.transform;

            Transform best = null;
            float bestDistSqr = noticeRadius * noticeRadius;
            for (int i = 0; i < interestingPoints.Count; i++)
            {
                var t = interestingPoints[i];
                if (t == null || t == transform) continue;
                float d = (t.position - transform.position).sqrMagnitude;
                if (d < bestDistSqr) { bestDistSqr = d; best = t; }
            }
            return best;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}

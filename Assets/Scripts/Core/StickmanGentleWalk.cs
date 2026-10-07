using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Gentle human walk-in (no zombie slide / moonwalk). Constant pace synced to Walk clip.
    /// </summary>
    public static class StickmanGentleWalk
    {
        public const float GentleSpeed = 0.95f;
        public const float TurnLerp = 12f;

        // Transforms currently driven step-by-step by WalkTo — StickmanSeparation
        // leaves these alone (their position is authoritative each frame here)
        // and only nudges apart stickmen that are standing/roaming.
        public static readonly HashSet<Transform> Walking = new HashSet<Transform>();

        public static IEnumerator WalkTo(Transform body, Vector3 end, float speed = GentleSpeed)
        {
            if (body == null) yield break;
            Walking.Add(body);
            try
            {
                yield return WalkToInner(body, end, speed);
            }
            finally
            {
                Walking.Remove(body);
            }
        }

        static IEnumerator WalkToInner(Transform body, Vector3 end, float speed)
        {
            end.y = 0f;
            var start = body.position;
            start.y = 0f;
            body.position = start;

            // Snap face to path first — prevents backwards walk
            Vector3 face = end - start;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f)
                body.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);

            var anim = body.GetComponent<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
                anim.SetFloat("Speed", 1f);
                anim.Play("Walk", 0, 0f);
                float authored = 1.17f;
                anim.speed = Mathf.Clamp(speed / authored, 0.7f, 1.15f);
            }

            float dist = Vector3.Distance(start, end);
            float travel = 0f;
            while (travel < dist - 0.02f && body != null)
            {
                float remaining = dist - travel;
                float ease = 1f;
                if (travel < 0.55f) ease = Mathf.SmoothStep(0.35f, 1f, travel / 0.55f);
                else if (remaining < 0.7f) ease = Mathf.SmoothStep(0.4f, 1f, remaining / 0.7f);

                float step = speed * ease * Time.deltaTime;
                travel = Mathf.Min(dist, travel + step);
                float t = dist > 0.001f ? travel / dist : 1f;
                var p = Vector3.Lerp(start, end, t);
                p.y = 0f;

                Vector3 look = end - body.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                {
                    var want = Quaternion.LookRotation(look.normalized, Vector3.up);
                    body.rotation = Quaternion.Slerp(body.rotation, want, TurnLerp * Time.deltaTime);
                }

                body.position = p;
                yield return null;
            }

            if (body == null) yield break;
            body.position = end;

            if (anim != null)
            {
                anim.speed = 1f;
                anim.SetFloat("Speed", 0f);
                if (HasState(anim, "Idle_Sad"))
                    anim.Play("Idle_Sad", 0, 0f);
                else if (HasState(anim, "Idle"))
                    anim.Play("Idle", 0, 0f);
            }
        }

        static bool HasState(Animator a, string name)
        {
            if (a == null || a.runtimeAnimatorController == null) return false;
            for (int i = 0; i < a.layerCount; i++)
            {
                if (a.HasState(i, Animator.StringToHash(name))) return true;
            }
            return true;
        }
    }
}

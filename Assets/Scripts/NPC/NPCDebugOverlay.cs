#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RadiantOrchard
{
    // Editor-only gizmo overlay for tuning NPC behavior — never compiled into
    // a player build, never touches runtime UI. Select an NPC in the Scene
    // view to see its live State/Condition/Need/Target/Distance/Emotion plus
    // its current target line and search radius.
    public class NPCDebugOverlay : MonoBehaviour
    {
        [SerializeField] private float searchRadiusGizmo = 10f;

        private StickmanController stickman;
        private NPCSpiritComponent spirit;

        private void Awake()
        {
            stickman = GetComponent<StickmanController>();
            spirit = GetComponent<NPCSpiritComponent>();
        }

        private void OnDrawGizmosSelected()
        {
            if (stickman == null) stickman = GetComponent<StickmanController>();
            if (spirit == null) spirit = GetComponent<NPCSpiritComponent>();

            Gizmos.color = new Color(1f, 1f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, searchRadiusGizmo);

            var target = stickman != null ? stickman.AmbientTarget : null;
            if (target != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position + Vector3.up, target.position);
            }

            string state = stickman != null ? stickman.CurrentState.ToString() : "n/a";
            string behavior = stickman != null ? stickman.AmbientBehaviorState.ToString() : "n/a";
            string condition = spirit != null ? spirit.Condition.ToString() : "n/a";
            string need = spirit != null ? spirit.CurrentNeed.ToString() : "n/a";
            float dist = target != null ? Vector3.Distance(transform.position, target.position) : -1f;

            string label = $"State: {state}\nBehavior: {behavior}\nCondition: {condition}\nNeed: {need}\n" +
                           (target != null ? $"Target: {target.name} ({dist:0.0}m)" : "Target: none");

            Handles.Label(transform.position + Vector3.up * 2.2f, label);
        }
    }
}
#endif

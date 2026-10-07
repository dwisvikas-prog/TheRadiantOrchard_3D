using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RadiantOrchard
{
    // Tap/click-to-interact: raycasts from the camera at the touch/mouse point
    // and forwards to whatever Interactable it hits. This is the MVP input path
    // (mouse in-editor, single touch on device via Unity's mouse simulation) —
    // full gesture handling (drag/pinch/camera vs. interaction conflicts) is a
    // later pass once this base flow is verified end to end.
    public class InteractionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private float maxRayDistance = 200f;

        private void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (worldCamera == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            var ray = worldCamera.ScreenPointToRay(mousePos);

            // Use RaycastAll instead of a single Raycast: a non-interactable collider
            // (ground, water, decoration) that happens to sit in front of a tree's
            // collider would otherwise "win" the ray and silently swallow the tap.
            var hits = Physics.RaycastAll(ray, maxRayDistance, interactableLayers);
            if (hits.Length == 0) return;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var interactable = hits[i].collider.GetComponentInParent<Interactable>();
                if (interactable == null) continue;
                if (interactable.TryInteract(gameObject))
                    TouchArbiter.Claim(mousePos);
                return;
            }
        }
    }
}

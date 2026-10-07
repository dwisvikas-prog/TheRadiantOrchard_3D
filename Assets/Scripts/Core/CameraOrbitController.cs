using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace RadiantOrchard
{
    // Pan/zoom rig for the fixed isometric island view (Clash of Clans style).
    // Drives an orthographic camera so it adapts cleanly to any viewport aspect
    // (an orthographic camera's world-space width is orthographicSize * aspect,
    // computed fresh from Screen size every frame by Unity itself — this script
    // never assumes a fixed resolution). Works with mouse (editor/desktop) and
    // single/two-finger touch (device) without extra setup.
    //
    // THE VIEW ANGLE IS LOCKED. Pitch and distance are captured once in Awake()
    // and never change, and yaw may not wander more than maxYawOffset degrees
    // from the angle the camera started at (default 0 = pinned exactly). The
    // framing therefore stays the same however the player drags — free 360°
    // orbit let the island rotate out from under the HUD and lose its readable
    // layout, which is what this lock exists to prevent.
    //
    //   one finger / left-drag ......  pan across the island
    //   two-finger drag .............  pan
    //   pinch / scroll wheel ........  zoom
    //   right / middle-drag .........  pan (desktop convenience)
    //
    // Orbit is opt-in: with allowOrbit on AND maxYawOffset > 0 the primary drag
    // swings the camera around the pivot within that ±limit instead of panning.
    // It can never make a full turn.
    public class CameraOrbitController : MonoBehaviour
    {
        [SerializeField] Transform pivot;
        [SerializeField] float rotateSpeed = 0.25f;   // degrees per pixel of drag
        [SerializeField] float buttonRotateStep = 45f; // degrees per rotate-button press
        [SerializeField] float panSpeed = 0.02f;      // world units per pixel of drag
        [SerializeField] float zoomSpeed = 1.5f;      // orthographic units per scroll notch
        [SerializeField] float pinchZoomSpeed = 0.05f; // orthographic units per pixel of pinch
        [SerializeField] float minZoom = 8f;
        [SerializeField] float maxZoom = 40f;
        [SerializeField] float followDamping = 8f;

        // Half-width (in world units) the view is allowed to show at most, on
        // any device. An orthographic camera's visible half-width is
        // orthographicSize * aspect, so on a wide phone screen (e.g. 20:9
        // landscape, aspect ~2.2) the same orthographicSize that frames the
        // island nicely on a 4:3 desktop test would show far past its edge
        // into empty space beyond the terrain. Clamping the effective max
        // zoom by aspect keeps the island filling the frame on every device.
        [SerializeField] float islandRadius = 38f;

        [Header("Isometric angle lock")]
        // Off = the primary drag pans and never rotates.
        [SerializeField] bool allowOrbit = false;
        // Max degrees the yaw may swing away from its starting angle even when
        // orbit is on. 0 = fully locked (the default). Because this is the value
        // that actually clamps the angle, a scene that still has `allowOrbit: 1`
        // serialized from before is locked too — the clamp is what decides.
        [SerializeField] float maxYawOffset = 0f;

        [Header("Fruit gestures vs. camera drag")]
        // A press/drag starting within this many screen pixels of a harvestable
        // fruit is treated as fruit input and does NOT pan the camera. Without
        // it the two systems fight: a fast-swipe harvest is a 100+ pixel drag,
        // and panning moves the island with the finger, so by release the fruit
        // had slid away from the swipe's start point (the point FruitHarvester
        // tests) and the harvest failed silently — the player saw the camera
        // pan instead of a fruit being picked. 0 disables the check.
        [SerializeField] float fruitGesturePanRadiusPixels = 90f;

        Camera cam;
        Vector3 pivotPosition;
        float yaw;
        float pitch;       // fixed after Awake — never changes from Orbit()
        float distance;    // fixed after Awake — never changes from Orbit()
        float initialYaw;  // the captured isometric angle the yaw is locked around
        float targetZoom;

        Vector3 targetPivot;
        float targetYaw;

        Vector2 lastDragPos;
        bool dragging;
        bool panning;
        float lastPinchDistance;

        // Rotation is only possible when it's both switched on *and* given a
        // non-zero allowance — this is what keeps the primary drag panning
        // instead of silently doing nothing on a stale allowOrbit flag.
        bool OrbitEnabled => allowOrbit && maxYawOffset > 0f;

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (pivot != null) pivotPosition = pivot.position;

            Vector3 offset = transform.position - pivotPosition;
            distance = offset.magnitude;
            targetZoom = cam.orthographic ? cam.orthographicSize : distance;

            // Derive yaw/pitch via LookRotation instead of raw atan2/asin —
            // the manual formula didn't actually invert ApplyTransform's
            // reconstruction for every offset (broke camera framing whenever
            // offset.x was 0, sending the camera under the island). LookRotation
            // + matching Vector3.forward in ApplyTransform is a verified round trip.
            Quaternion lookRot = Quaternion.LookRotation(-offset.normalized, Vector3.up);
            yaw = lookRot.eulerAngles.y;
            pitch = lookRot.eulerAngles.x;

            initialYaw = yaw;   // the angle ClampYaw() holds the view to
            targetYaw = yaw;
            targetPivot = pivotPosition;

            if (cam.orthographic)
                targetZoom = cam.orthographicSize = Mathf.Min(targetZoom, EffectiveMaxZoom());
        }

        // On a wide device aspect, orthographicSize * aspect can exceed
        // islandRadius even at the designer-tuned maxZoom, so the true zoom
        // ceiling is whichever is smaller for the current screen.
        float EffectiveMaxZoom() => Mathf.Min(maxZoom, islandRadius / Mathf.Max(cam.aspect, 1f));

        void Update()
        {
            if (EmptyIslandPhaseRunner.BlocksWorldInput)
            {
                dragging = false;
                panning = false;
                return;
            }

            HandleMouse();
            HandleTouch();

            // Smoothly settle toward the latest target so a released drag or
            // a rotate-button press doesn't snap; also what makes scroll-wheel
            // and pinch zoom feel eased rather than steppy.
            float t = 1f - Mathf.Exp(-followDamping * Time.deltaTime);
            yaw = Mathf.LerpAngle(yaw, targetYaw, t);
            pivotPosition = Vector3.Lerp(pivotPosition, targetPivot, t);
            if (cam.orthographic)
            {
                // Re-clamped every frame (not just on zoom input) so an
                // orientation change or window resize on device can't leave
                // the view showing past the island until the next pinch/scroll.
                targetZoom = Mathf.Min(targetZoom, EffectiveMaxZoom());
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, t);
            }
            else
                distance = Mathf.Lerp(distance, targetZoom, t);

            ApplyTransform();
        }

        void ApplyTransform()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 fwd = rot * Vector3.forward; // matches the LookRotation convention used in Awake
            transform.position = pivotPosition - fwd * distance;
            transform.rotation = rot;
        }

        void HandleMouse()
        {
            if (Touchscreen.current != null && EnhancedTouch.activeTouches.Count > 0) return; // touch takes priority when present
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mousePos = mouse.position.ReadValue();

            // The HUD guard is only consulted when a drag *starts*: a drag that
            // wanders over a UI panel afterwards keeps control of the camera
            // instead of stalling halfway through the gesture.
            if (mouse.leftButton.wasPressedThisFrame)
            {
                dragging = !PointerOverUI(-1) && !StartsOnFruit(mousePos);
                if (dragging) lastDragPos = mousePos;
            }
            if (mouse.leftButton.wasReleasedThisFrame) dragging = false;

            if (mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame)
            {
                if (!PointerOverUI(-1))
                {
                    panning = true;
                    lastDragPos = mousePos;
                }
            }
            if (mouse.rightButton.wasReleasedThisFrame || mouse.middleButton.wasReleasedThisFrame) panning = false;

            if (dragging || panning)
            {
                Vector2 delta = mousePos - lastDragPos;
                lastDragPos = mousePos;

                // Primary drag pans (the locked default) or orbits when the
                // designer explicitly opted in; secondary drag always pans.
                if (dragging && OrbitEnabled) Orbit(delta);
                else Pan(delta);
            }

            // Input System's scroll delta is in different units than the legacy
            // "Mouse ScrollWheel" axis (roughly x120 larger, one Windows wheel
            // notch) — divided down here so zoomSpeed keeps its old meaning and
            // doesn't need re-tuning in the Inspector.
            float scroll = mouse.scroll.ReadValue().y / 120f;
            if (Mathf.Abs(scroll) > 0.0001f) ZoomBy(-scroll * zoomSpeed);
        }

        void HandleTouch()
        {
            if (Touchscreen.current == null) return;
            var touches = EnhancedTouch.activeTouches;

            if (touches.Count == 1)
            {
                var touch = touches[0];

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // Starting on a HUD element must not pan the island behind
                    // it, and neither must starting on a fruit (see
                    // fruitGesturePanRadiusPixels).
                    dragging = !PointerOverUI(touch.finger.index) && !StartsOnFruit(touch.screenPosition);
                    lastDragPos = touch.screenPosition;
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && dragging)
                {
                    // One finger pans across the island — it never also rotates,
                    // so the map can't spin while the player is moving around it.
                    if (OrbitEnabled) Orbit(touch.delta);
                    else Pan(touch.delta);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    dragging = false;
                }
            }
            else if (touches.Count == 2)
            {
                var t0 = touches[0];
                var t1 = touches[1];

                // Two-finger drag pans; the change in finger separation zooms —
                // both derived every frame so a pinch-and-drag does both at once.
                Vector2 avgDelta = (t0.delta + t1.delta) * 0.5f;
                Pan(avgDelta);

                float currentPinch = Vector2.Distance(t0.screenPosition, t1.screenPosition);
                if (t0.phase != UnityEngine.InputSystem.TouchPhase.Began && t1.phase != UnityEngine.InputSystem.TouchPhase.Began && lastPinchDistance > 0f)
                    ZoomBy(-(currentPinch - lastPinchDistance) * pinchZoomSpeed); // spread = zoom in
                lastPinchDistance = currentPinch;
            }
            else
            {
                lastPinchDistance = 0f;
            }
        }

        // Yaw may never wander further than maxYawOffset from the angle the
        // camera started at, so the isometric framing can't be spun around the
        // island. DeltaAngle keeps the clamp correct across the ±180° wrap.
        float ClampYaw(float value)
        {
            float limit = OrbitEnabled ? maxYawOffset : 0f;
            return initialYaw + Mathf.Clamp(Mathf.DeltaAngle(initialYaw, value), -limit, limit);
        }

        void Orbit(Vector2 screenDelta)
        {
            if (!OrbitEnabled) return;
            targetYaw = ClampYaw(targetYaw + screenDelta.x * rotateSpeed);
        }

        void Pan(Vector2 screenDelta)
        {
            Vector3 right = transform.right;
            Vector3 fwd = Vector3.Cross(right, Vector3.up);
            float scale = panSpeed * (cam.orthographic ? cam.orthographicSize / 10f : distance / 10f);
            targetPivot -= (right * screenDelta.x + fwd * screenDelta.y) * scale;
        }

        // Amount is in orthographic-size units (world height of the view), which
        // is what makes zoom feel identical at every resolution.
        void ZoomBy(float units)
        {
            targetZoom = Mathf.Clamp(targetZoom + units, minZoom, EffectiveMaxZoom());
        }

        bool StartsOnFruit(Vector2 screenPos)
        {
            if (fruitGesturePanRadiusPixels <= 0f) return false;
            return FruitHarvester.IsPointerNearAnyFruit(screenPos, fruitGesturePanRadiusPixels);
        }

        static bool PointerOverUI(int fingerId)
        {
            var events = EventSystem.current;
            if (events == null) return false;
            return fingerId >= 0
                ? events.IsPointerOverGameObject(fingerId)
                : events.IsPointerOverGameObject();
        }

        public void SetPivot(Transform newPivot)
        {
            pivot = newPivot;
            if (pivot != null) targetPivot = pivot.position;
        }

        // --- On-screen rotate-button entry points ----------------------------
        // Wire a UI Button's OnClick to these for discrete step rotation instead
        // of (or alongside) drag-to-orbit. Both are clamped by ClampYaw(), so with
        // the default lock (maxYawOffset = 0) they intentionally do nothing.
        public void RotateLeft() => targetYaw = ClampYaw(targetYaw - buttonRotateStep);
        public void RotateRight() => targetYaw = ClampYaw(targetYaw + buttonRotateStep);
    }
}

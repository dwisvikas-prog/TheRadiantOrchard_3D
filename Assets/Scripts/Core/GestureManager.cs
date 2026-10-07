using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System;

namespace RadiantOrchard
{
    // Raw touch gesture recognition, on the Input System package (migrated off
    // the legacy Input Manager). Fires events other systems (fruit harvesting,
    // tonic brewing) subscribe to instead of polling input themselves.
    //
    // EDITOR / DESKTOP MOUSE FALLBACK:
    //   In the Unity Editor and on desktop builds Input.touchCount is always 0
    //   (no physical touch screen). The mouse fallback maps:
    //     Double-click (LMB)              → OnDoubleTap
    //     Hold LMB ≥ longPressThreshold   → OnLongPress
    //     Fast drag LMB                   → OnFastSwipe
    //     Middle-mouse single click       → OnDoubleTap  (quick test shortcut)
    //   This mirrors real touch behaviour 1-to-1 so Editor playtesting works
    //   without Unity Remote or a physical device.
    public class GestureManager : MonoBehaviour
    {
        public event Action<Vector2> OnDoubleTap;
        public event Action<Vector2> OnLongPress;
        public event Action<Vector2, Vector2> OnFastSwipe; // start, end

        [SerializeField] float doubleTapWindow     = 0.3f;
        [SerializeField] float longPressThreshold  = 0.6f;
        [SerializeField] float swipeMinDistance    = 100f;   // pixels
        [SerializeField] float swipeMaxDuration    = 0.4f;   // seconds

        // ── shared state (touch + mouse share the same variables) ──────────
        float   lastTapTime    = -1f;
        Vector2 lastTapPos;
        float   touchStartTime;
        Vector2 touchStartPos;
        bool    longPressFired;

        // ── mouse-specific ──────────────────────────────────────────────────
        bool mouseDown;

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            // ── TOUCH PATH (device / Unity Remote) ─────────────────────────
            if (Touchscreen.current != null && EnhancedTouch.activeTouches.Count > 0)
            {
                HandleTouch(EnhancedTouch.activeTouches[0]);
                return;
            }

            // ── MOUSE FALLBACK (Editor + desktop) ──────────────────────────
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            HandleMouse();
#endif
        }

        // ───────────────────────────────────────────────────────────────────
        // TOUCH HANDLER (unchanged from original)
        // ───────────────────────────────────────────────────────────────────
        void HandleTouch(EnhancedTouch touch)
        {
            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    touchStartTime = Time.time;
                    touchStartPos  = touch.screenPosition;
                    longPressFired = false;
                    break;

                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (!longPressFired && Time.time - touchStartTime >= longPressThreshold)
                    {
                        longPressFired = true;
                        if (!TouchArbiter.WasRecentlyClaimedNear(touch.screenPosition))
                            OnLongPress?.Invoke(touch.screenPosition);
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                    FireOnTouchEnded(touch.screenPosition, Time.time - touchStartTime,
                        Vector2.Distance(touchStartPos, touch.screenPosition));
                    break;
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // MOUSE FALLBACK HANDLER
        // ───────────────────────────────────────────────────────────────────
        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mousePos = mouse.position.ReadValue();

            // ── Button DOWN ────────────────────────────────────────────────
            if (mouse.leftButton.wasPressedThisFrame)
            {
                touchStartTime = Time.time;
                touchStartPos  = mousePos;
                longPressFired = false;
                mouseDown      = true;
            }

            // ── Held STATIONARY (long press) ───────────────────────────────
            if (mouseDown && mouse.leftButton.isPressed && !longPressFired)
            {
                float held     = Time.time - touchStartTime;
                float dragged  = Vector2.Distance(touchStartPos, mousePos);

                if (held >= longPressThreshold && dragged < swipeMinDistance * 0.4f)
                {
                    longPressFired = true;
                    if (!TouchArbiter.WasRecentlyClaimedNear(mousePos))
                        OnLongPress?.Invoke(mousePos);
                }
            }

            // ── Button UP ─────────────────────────────────────────────────
            if (mouse.leftButton.wasReleasedThisFrame && mouseDown)
            {
                mouseDown = false;
                float held = Time.time - touchStartTime;
                FireOnTouchEnded(mousePos, held,
                    Vector2.Distance(touchStartPos, mousePos));
            }

            // ── Middle mouse = instant DoubleTap shortcut for quick testing ─
            if (mouse.middleButton.wasPressedThisFrame)
            {
                OnDoubleTap?.Invoke(mousePos);
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // SHARED TOUCH-END LOGIC (same for touch and mouse)
        // ───────────────────────────────────────────────────────────────────
        void FireOnTouchEnded(Vector2 pos, float heldTime, float distance)
        {
            if (longPressFired) return; // already fired as long-press, don't also fire tap

            bool claimed = TouchArbiter.WasRecentlyClaimedNear(pos);

            if (distance >= swipeMinDistance && heldTime <= swipeMaxDuration)
            {
                // Fast swipe
                if (!claimed) OnFastSwipe?.Invoke(touchStartPos, pos);
            }
            else if (distance < swipeMinDistance && heldTime < longPressThreshold)
            {
                // Tap → check for double-tap
                if (lastTapTime > 0 && Time.time - lastTapTime <= doubleTapWindow
                    && Vector2.Distance(lastTapPos, pos) < 80f)
                {
                    if (!claimed) OnDoubleTap?.Invoke(pos);
                    lastTapTime = -1f;  // reset so triple tap doesn't chain
                }
                else
                {
                    lastTapTime = Time.time;
                    lastTapPos  = pos;
                }
            }
        }
    }
}

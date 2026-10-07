using UnityEngine;
using UnityEngine.EventSystems;
using RadiantOrchard;

// DioramaController — Clash of Clans style:
//   • Fixed isometric angle (no free rotate)
//   • Drag = smooth pan
//   • Pinch / scroll = smooth zoom in & out
//   • Soft pan bounds so the base stays on screen
public class DioramaController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Pitch (fixed)")]
    public float isometricPitch = 55f;
    public float fieldOfView = 42f;

    [Header("Zoom")]
    public float defaultZoom = 20f;
    public float minZoom = 10f;
    public float maxZoom = 42f;
    [Tooltip("Pinch sensitivity — keep low for CoC-smooth feel")]
    public float zoomSpeed = 0.022f;
    public float scrollZoomSpeed = 3.5f;

    [Header("Framing")]
    public float lookDownBias = 2.2f;
    public float portraitFieldOfView = 36f;

    [Header("Pan")]
    public float touchPanSpeed = 0.028f;
    public float mousePanSpeed = 0.10f;

    [Header("Pan Bounds")]
    public float islandRadius = 55f;
    [Range(0f, 1f)]
    public float panMarginFraction = 0.65f;

    [Header("Rotate")]
    public bool lockRotation = true;
    public float fixedYaw = 35f;
    public float twoFingerRotateMaxDeg = 0f;
    public float touchRotateSensitivity = 1f;
    public float mouseRotateSpeed = 3f;

    [Header("Smoothing (CoC feel)")]
    public float zoomSmooth = 0.22f;
    public float moveSmooth = 0.14f;

    [Header("Fruit gesture guard")]
    public float fruitGuardPixels = 48f;

    [Header("Idle zoom-out")]
    [Tooltip("Seconds of no player pan/zoom before the camera drifts out to show more of the island.")]
    public float idleZoomOutDelay = 6f;
    [Tooltip("How fast distTarget drifts toward the idle target once idle.")]
    public float idleZoomOutSpeed = 4f;
    [Tooltip("Extra distance (beyond the zoom the player left it at) the idle drift is allowed to add — keeps it from going all the way out to maxZoom.")]
    public float idleZoomOutMaxExtra = 8f;
    float lastInputTime;
    float idleZoomBase = -1f; // dist captured the moment idling begins; -1 = not idling

    float yaw, yaw0;
    float dist, distTarget, distVel;
    Vector3 pivotOffset;
    Vector3 pivotVel;

    bool gestureLocked;
    float prevTwistAngle;
    bool twistSeeded;
    Vector2 lastMousePan;
    bool panningActive;
    float ignoreMouseUntil; // after touch — synthesized mouse causes map jumps

    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.orthographic = false;
            ApplyAspectFov();
        }

        SyncZoomFromDefaults();
        if (lockRotation) yaw = yaw0 = fixedYaw;
        else yaw = yaw0 = transform.eulerAngles.y;

        lastInputTime = Time.unscaledTime;
        if (target) Snap();
    }

    void SyncZoomFromDefaults()
    {
        if (maxZoom < minZoom + 1f) maxZoom = minZoom + 20f;
        float start = defaultZoom > 0.1f ? defaultZoom : Mathf.Lerp(minZoom, maxZoom, 0.45f);
        start = Mathf.Clamp(start, minZoom, maxZoom);
        dist = distTarget = start;
    }

    // Framing scripts call this for CoC: locked yaw + usable zoom range.
    public void ApplyCoCLock(float yawDeg)
    {
        lockRotation = true;
        fixedYaw = yawDeg;
        yaw = yaw0 = yawDeg;
        twoFingerRotateMaxDeg = 0f;
        SyncZoomFromDefaults();
        if (cam == null) cam = GetComponent<Camera>();
        if (target) Snap();
    }

    // Convenience: set CoC zoom band (close ↔ whole base) then lock.
    public void ConfigureCoCView(float yawDeg, float closeZoom, float farZoom, float startZoom)
    {
        minZoom = Mathf.Max(8f, closeZoom);
        maxZoom = Mathf.Max(minZoom + 8f, farZoom);
        defaultZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);
        ApplyCoCLock(yawDeg);
    }

    /// <summary>
    /// Empty Island framing — whole pad visible, CoC free pan (no rubber snap-back).
    /// </summary>
    public void ApplyEmptyIslandCloseUp()
    {
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 0.56f;
        bool portrait = aspect < 1.05f;

        isometricPitch = portrait ? 48f : 50f;
        fieldOfView = portrait ? 38f : 40f;
        portraitFieldOfView = 36f;
        lookDownBias = 1.8f;
        // Full 44×44 pad — CoC free slide
        islandRadius = CoCBlankGround.PadHalf * 1.2f;
        panMarginFraction = 1f;

        // Speeds unused for Empty Island — pan is screen→ground 1:1 (CoC)
        touchPanSpeed = 1f;
        mousePanSpeed = 1f;
        zoomSpeed = 0.022f;
        scrollZoomSpeed = 2.8f;
        zoomSmooth = 0.14f;
        moveSmooth = 0.08f; // idle only; while dragging we snap
        fruitGuardPixels = 40f;

        float closeZ = 9f;
        float farZ = portrait ? 50f : 54f;
        float startZ = portrait ? 24f : 26f;
        ConfigureCoCView(35f, closeZ, farZ, startZ);
        pivotOffset = Vector3.zero;
        pivotVel = Vector3.zero;
        Snap();
    }

    /// <summary>One decent step out — for a manual Zoom Out HUD button (replaces auto idle drift).</summary>
    public void ManualZoomOut()
    {
        CancelFocus();
        float step = Mathf.Lerp(minZoom, maxZoom, 0.22f);
        distTarget = Mathf.Clamp(distTarget + step, minZoom, maxZoom);
        lastInputTime = Time.unscaledTime;
    }

    /// <summary>Cancel scripted focus so player pan wins (CoC feel).</summary>
    public void CancelFocus()
    {
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }
        StopFollow();
    }

    /// <summary>Keep camera locked on a moving transform (new visitor walk-in).</summary>
    public void StartFollow(Transform follow, float preferredZoom = 12f)
    {
        if (follow == null || target == null) return;
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }
        StopFollow();
        followRoutine = StartCoroutine(FollowLiveRoutine(follow, preferredZoom));
    }

    public void StopFollow()
    {
        if (followRoutine == null) return;
        StopCoroutine(followRoutine);
        followRoutine = null;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Never let Orbit fight this rig (causes map jump / idhar-udhar)
        var orbit = GetComponent<CameraOrbitController>();
        if (orbit != null && orbit.enabled) orbit.enabled = false;

        if (cam != null)
        {
            if (cam.rect.width < 0.99f || cam.rect.height < 0.99f)
                cam.rect = new Rect(0f, 0f, 1f, 1f);
            ApplyAspectFov();
        }

        if (lockRotation) yaw = yaw0;

        if (EmptyIslandPhaseRunner.BlocksWorldInput || WellWishController.IsUiOpen ||
            EditModeManager.IsDraggingObject)
        {
            panningActive = false;
            dist = Mathf.SmoothDamp(dist, distTarget, ref distVel, zoomSmooth);
            dist = Mathf.Clamp(dist, minZoom, maxZoom);
            ClampPivotToBounds();
            Reposition(false);
            return;
        }

        panningActive = false;
        bool focusing = focusRoutine != null || followRoutine != null;
        if (Input.touchCount > 0)
        {
            ignoreMouseUntil = Time.unscaledTime + 0.25f;
            HandleTouch();
        }
        else if (Time.unscaledTime >= ignoreMouseUntil)
        {
            HandleMouse();
        }

        if (!focusing)
        {
            // Idle auto zoom-out removed — it kept re-triggering on its own and
            // felt inconsistent. Zoom is now only player pinch/scroll or the
            // explicit Zoom Out HUD button (see ManualZoomOut).
            dist = Mathf.SmoothDamp(dist, distTarget, ref distVel, zoomSmooth);
            dist = Mathf.Clamp(dist, minZoom, maxZoom);
            ClampPivotToBounds();
            Reposition(panningActive);
        }
    }

    void ApplyAspectFov()
    {
        if (cam == null) return;
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.77f;
        cam.fieldOfView = aspect < 1.05f ? portraitFieldOfView : fieldOfView;
    }

    void HandleTouch()
    {
        int n = Input.touchCount;

        if (n == 1)
        {
            Touch t = Input.GetTouch(0);
            twistSeeded = false;

            if (t.phase == TouchPhase.Began)
                gestureLocked = StartsOnFruit(t.position);

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                gestureLocked = false;

            if (OverUI(t.fingerId)) return;

            // CoC: map sticks under finger (screen → ground plane)
            if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) &&
                !gestureLocked && t.deltaPosition.sqrMagnitude > 0.01f)
            {
                Vector2 prev = t.position - t.deltaPosition;
                PanByScreenPoints(prev, t.position);
            }
        }
        else if (n >= 2)
        {
            gestureLocked = false;
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 p0prev = t0.position - t0.deltaPosition;
            Vector2 p1prev = t1.position - t1.deltaPosition;
            float dPrev = (p0prev - p1prev).magnitude;
            float dCurr = (t0.position - t1.position).magnitude;
            float pinch = dCurr - dPrev;

            float norm = Mathf.Max(320f, Screen.height * 0.5f);
            float zoomDelta = (pinch / norm) * (zoomSpeed * 40f);
            zoomDelta = Mathf.Clamp(zoomDelta, -0.55f, 0.55f);
            if (Mathf.Abs(zoomDelta) > 0.001f) { CancelFocus(); lastInputTime = Time.unscaledTime; }
            distTarget = Mathf.Clamp(distTarget - zoomDelta, minZoom, maxZoom);

            Vector2 axis = t1.position - t0.position;
            float twistNow = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
            bool fresh = t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began;
            if (!twistSeeded || fresh)
            {
                prevTwistAngle = twistNow;
                twistSeeded = true;
            }
            else if (!lockRotation)
            {
                float delta = Mathf.DeltaAngle(prevTwistAngle, twistNow) * touchRotateSensitivity;
                yaw = ClampYaw(yaw - delta);
                prevTwistAngle = twistNow;
            }
            else prevTwistAngle = twistNow;

            // Two-finger slide (almost no pinch) = pan midpoint on ground
            if (Mathf.Abs(pinch) < 8f)
            {
                Vector2 midPrev = (p0prev + p1prev) * 0.5f;
                Vector2 midNow = (t0.position + t1.position) * 0.5f;
                PanByScreenPoints(midPrev, midNow);
            }
        }
        else twistSeeded = false;
    }

    void HandleMouse()
    {
        bool ui = OverUI(-1);

        if (Input.GetMouseButtonDown(0))
        {
            gestureLocked = StartsOnFruit(Input.mousePosition);
            lastMousePan = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0)) gestureLocked = false;

        // Left drag = CoC ground pan (same as finger)
        if (!ui && !gestureLocked && Input.GetMouseButton(0))
        {
            Vector2 now = Input.mousePosition;
            if ((now - lastMousePan).sqrMagnitude > 0.01f)
                PanByScreenPoints(lastMousePan, now);
            lastMousePan = now;
        }

        // Right drag = pan too
        if (!ui && Input.GetMouseButton(1))
        {
            Vector2 now = Input.mousePosition;
            // Mouse X/Y axes are frame-rate dependent — use absolute screen delta
            if (!Input.GetMouseButtonDown(1))
            {
                Vector2 prev = now - new Vector2(
                    Input.GetAxisRaw("Mouse X") * 20f,
                    Input.GetAxisRaw("Mouse Y") * 20f);
                PanByScreenPoints(prev, now);
            }
        }

        if (!ui && !lockRotation && Input.GetMouseButton(2))
            yaw = ClampYaw(yaw + Input.GetAxis("Mouse X") * mouseRotateSpeed);

        float s = Input.mouseScrollDelta.y;
        if (Mathf.Abs(s) < 0.01f) s = Input.GetAxis("Mouse ScrollWheel") * 10f;
        if (Mathf.Abs(s) > 0.01f)
        {
            CancelFocus();
            distTarget = Mathf.Clamp(distTarget - s * scrollZoomSpeed * 0.22f, minZoom, maxZoom);
            lastInputTime = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Clash of Clans pan: whatever is under the finger stays under the finger.
    /// Projects two screen points onto the ground plane and slides the pivot by that world delta.
    /// </summary>
    void PanByScreenPoints(Vector2 screenFrom, Vector2 screenTo)
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || target == null) return;

        float groundY = target.position.y;
        var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));

        Ray r0 = cam.ScreenPointToRay(screenFrom);
        Ray r1 = cam.ScreenPointToRay(screenTo);
        if (!plane.Raycast(r0, out float d0) || !plane.Raycast(r1, out float d1)) return;

        Vector3 w0 = r0.GetPoint(d0);
        Vector3 w1 = r1.GetPoint(d1);
        Vector3 delta = w0 - w1; // map follows finger
        delta.y = 0f;
        if (delta.sqrMagnitude < 1e-8f) return;

        CancelFocus();
        pivotOffset += delta;
        ClampPivotToBounds();
        panningActive = true;
        pivotVel = Vector3.zero; // kill SmoothDamp overshoot
        lastInputTime = Time.unscaledTime;
    }

    void GetPanBounds(out float bx, out float bz)
    {
        float halfFovRad = (cam != null ? cam.fieldOfView : fieldOfView) * 0.5f * Mathf.Deg2Rad;
        float frustumH = Mathf.Tan(halfFovRad) * dist;
        float aspect = Screen.width > 0 && Screen.height > 0
            ? (float)Screen.width / Screen.height
            : 16f / 9f;
        float frustumW = frustumH * aspect;

        // CoC: free slide across the pad. Never collapse to a pin-point
        // (that felt like rubber physics snapping you back to center).
        float roomX = islandRadius - frustumW * 0.35f;
        float roomZ = islandRadius - frustumH * 0.35f;
        float minPan = islandRadius * 0.45f;
        bx = Mathf.Max(minPan, roomX * panMarginFraction);
        bz = Mathf.Max(minPan, roomZ * panMarginFraction);
    }

    void ClampPivotToBounds()
    {
        GetPanBounds(out float bx, out float bz);
        pivotOffset.x = Mathf.Clamp(pivotOffset.x, -bx, bx);
        pivotOffset.z = Mathf.Clamp(pivotOffset.z, -bz, bz);
        pivotOffset.y = 0f;
    }

    float ClampYaw(float v)
    {
        if (lockRotation) return yaw0;
        float delta = Mathf.DeltaAngle(yaw0, v);
        return yaw0 + Mathf.Clamp(delta, -twoFingerRotateMaxDeg, twoFingerRotateMaxDeg);
    }

    bool StartsOnFruit(Vector2 pos) =>
        fruitGuardPixels > 0f &&
        FruitHarvester.IsPointerNearAnyFruit(pos, fruitGuardPixels);

    bool OverUI(int fingerId) =>
        EventSystem.current != null &&
        (fingerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(fingerId));

    Vector3 Pivot => target.position + pivotOffset;
    Vector3 LookPoint => Pivot + Vector3.down * lookDownBias;

    Coroutine focusRoutine;
    Coroutine followRoutine;

    /// <summary>Smooth CoC-style camera track to a world point (new visitor, plant, etc.).</summary>
    public void FocusOn(Vector3 worldPoint, float preferredZoom = -1f, float duration = 1.0f)
    {
        if (target == null) return;
        StopFollow();
        if (focusRoutine != null) StopCoroutine(focusRoutine);
        focusRoutine = StartCoroutine(FocusRoutine(worldPoint, preferredZoom, duration));
    }

    System.Collections.IEnumerator FollowLiveRoutine(Transform follow, float preferredZoom)
    {
        float toZoom = preferredZoom > 0f
            ? Mathf.Clamp(preferredZoom, minZoom, maxZoom)
            : distTarget;
        pivotVel = Vector3.zero;
        distVel = 0f;

        while (follow != null)
        {
            Vector3 want = follow.position + Vector3.up * 0.85f - target.position;
            want.y = 0f;
            // Gentle exp smooth chase — glides onto the visitor instead of
            // snapping there, then stays glued while they walk.
            float k = 1f - Mathf.Exp(-3.0f * Time.deltaTime);
            pivotOffset = Vector3.Lerp(pivotOffset, want, k);
            ClampPivotToBounds();
            dist = distTarget = Mathf.Lerp(dist, toZoom, 1f - Mathf.Exp(-2.2f * Time.deltaTime));
            transform.position = Pivot + Quaternion.Euler(isometricPitch, yaw, 0f) * new Vector3(0f, 0f, -dist);
            transform.LookAt(LookPoint);
            yield return null;
        }

        followRoutine = null;
    }

    /// <summary>Hard track — snap camera to action target NOW (gesture / soil / stickman).</summary>
    public void SnapFocus(Vector3 worldPoint, float preferredZoom = -1f)
    {
        if (target == null) return;
        CancelFocus();
        Vector3 want = worldPoint - target.position;
        want.y = 0f;
        pivotOffset = want;
        ClampPivotToBounds();
        if (preferredZoom > 0f)
            dist = distTarget = Mathf.Clamp(preferredZoom, minZoom, maxZoom);
        pivotVel = Vector3.zero;
        Snap();
    }

    System.Collections.IEnumerator FocusRoutine(Vector3 worldPoint, float preferredZoom, float duration)
    {
        Vector3 want = worldPoint - target.position;
        want.y = 0f;
        Vector3 fromPivot = pivotOffset;
        float fromZoom = dist;
        float toZoom = preferredZoom > 0f ? Mathf.Clamp(preferredZoom, minZoom, maxZoom) : distTarget;
        float t = 0f;
        duration = Mathf.Max(0.55f, duration);
        pivotVel = Vector3.zero;
        distVel = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);
            // Ease in-out cubic — smooth CoC glide, no snap
            u = u * u * (3f - 2f * u);
            pivotOffset = Vector3.Lerp(fromPivot, want, u);
            ClampPivotToBounds();
            dist = distTarget = Mathf.Lerp(fromZoom, toZoom, u);
            // Direct camera follow during focus (skip SmoothDamp lag)
            transform.position = Pivot + Quaternion.Euler(isometricPitch, yaw, 0f) * new Vector3(0f, 0f, -dist);
            transform.LookAt(LookPoint);
            yield return null;
        }
        pivotOffset = want;
        ClampPivotToBounds();
        dist = distTarget = toZoom;
        Snap();
        focusRoutine = null;
    }

    void Snap()
    {
        pivotVel = Vector3.zero;
        transform.position = Pivot + Quaternion.Euler(isometricPitch, yaw, 0f) * new Vector3(0f, 0f, -dist);
        transform.LookAt(LookPoint);
    }

    void Reposition(bool snapNow)
    {
        Vector3 desired = Pivot + Quaternion.Euler(isometricPitch, yaw, 0f) * new Vector3(0f, 0f, -dist);
        desired += ConsumeShakeOffset();
        if (snapNow)
        {
            pivotVel = Vector3.zero;
            transform.position = desired;
        }
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref pivotVel, moveSmooth);
        transform.LookAt(LookPoint);
    }

    float shakeTimeLeft;
    float shakeDuration = 1f;
    float shakeMagnitude;

    /// <summary>Quick punch on an impact moment (heal complete, etc.) — small, brief, never disorienting.</summary>
    public void Shake(float duration = 0.15f, float magnitude = 0.18f)
    {
        shakeDuration = Mathf.Max(0.05f, duration);
        shakeTimeLeft = shakeDuration;
        shakeMagnitude = magnitude;
    }

    Vector3 ConsumeShakeOffset()
    {
        if (shakeTimeLeft <= 0f) return Vector3.zero;
        shakeTimeLeft -= Time.deltaTime;
        float t = Mathf.Clamp01(shakeTimeLeft / shakeDuration);
        Vector3 offset = Random.insideUnitSphere * shakeMagnitude * t;
        offset.y *= 0.3f;
        return offset;
    }
}

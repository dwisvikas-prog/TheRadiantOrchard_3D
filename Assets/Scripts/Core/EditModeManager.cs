using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace RadiantOrchard
{
    /// <summary>
    /// CoC-style edit: drag ALL island objects (trees, shore rocks/bushes, decorations)
    /// on the 44×44 grid. No overlap — red ghost refuses commit.
    /// Camera pan stays ON (CoC village edit).
    /// </summary>
    public class EditModeManager : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float pickRadiusPixels = 140f;
        [SerializeField] private float groundHeight = 0f;

        readonly List<Transform> decorations = new List<Transform>(); // reward props — saved
        readonly HashSet<Transform> treeRoots = new HashSet<Transform>(); // fruit + HQ scenery — movable
        Transform dragging;
        Vector3 dragOrigin;
        Vector3 lastValidPos;
        GameObject dragGhostTile;

        float gridSize = 1f;
        float gridClampHalf = 20f;

        GestureManager gestureManager;
        InteractionController interactionController;

        public bool EditModeActive { get; private set; }
        public static bool IsDraggingObject { get; private set; }

        public static EditModeManager Ensure()
        {
            var em = FindFirstObjectByType<EditModeManager>();
            if (em == null)
            {
                var go = new GameObject("EditModeManager");
                em = go.AddComponent<EditModeManager>();
            }
            em.SetGridSnap(1f, CoCBlankGround.PadHalf - 2f);
            em.groundHeight = 0f;
            return em;
        }

        public void SetGridSnap(float size, float clampHalf)
        {
            gridSize = Mathf.Max(0f, size);
            gridClampHalf = Mathf.Max(1f, clampHalf);
        }

        public static Vector3 SnapToCoCTile(Vector3 p, float grid = 1f, float clampHalf = -1f)
        {
            if (clampHalf < 0f) clampHalf = CoCBlankGround.PadHalf - 2f;
            if (grid < 0.01f) grid = 1f;
            p.x = Mathf.Round(p.x / grid) * grid;
            p.z = Mathf.Round(p.z / grid) * grid;
            p.x = Mathf.Clamp(p.x, -clampHalf, clampHalf);
            p.z = Mathf.Clamp(p.z, -clampHalf, clampHalf);
            p.y = 0f;
            return p;
        }

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            gestureManager = FindFirstObjectByType<GestureManager>();
            interactionController = FindFirstObjectByType<InteractionController>();
            if (gridSize < 0.01f) SetGridSnap(1f, CoCBlankGround.PadHalf - 2f);
        }

        void Update()
        {
            if (!EditModeActive) return;

            if (Touchscreen.current != null && EnhancedTouch.activeTouches.Count > 0)
                HandleTouch();
            else
                HandleMouseOrLegacy();
        }

        public void RegisterDecoration(Transform t)
        {
            if (t == null) return;
            treeRoots.Remove(t);
            if (!decorations.Contains(t)) decorations.Add(t);
        }

        /// <summary>Fruit trees + HQ scenery — movable on grid, never written into decoration save list.</summary>
        public void RegisterTree(Transform t)
        {
            if (t == null) return;
            decorations.Remove(t);
            treeRoots.Add(t);
        }

        public void EnterEditMode()
        {
            EditModeActive = true;
            RegisterAllIslandObjects();
            SetOtherInputEnabled(false);
            EmptyIslandCoachBar.SetTip("EDIT — drag any tree, rock or bush (no overlap)");
            IslandTapTrack.Hide();
        }

        public void ExitEditMode()
        {
            EditModeActive = false;
            dragging = null;
            IsDraggingObject = false;
            if (dragGhostTile != null) dragGhostTile.SetActive(false);
            SetOtherInputEnabled(true);
            SaveLayout();
            EmptyIslandCoachBar.SetTip("Layout saved");
        }

        public void RefreshRegisteredTrees()
        {
            var tracker = FindFirstObjectByType<OrchardTreeTracker>();
            if (tracker == null) return;
            foreach (var pt in tracker.All)
            {
                if (pt.root != null) RegisterTree(pt.root);
            }
        }

        /// <summary>
        /// Pick up every movable on the pad: fruit trees, HQ trees/bushes, shore rocks, reward deco.
        /// Never touch well, wish tree, stickmen, fruit pickables, water, cameras.
        /// </summary>
        public void RegisterAllIslandObjects()
        {
            RefreshRegisteredTrees();

            var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null || t.parent == null) continue; // skip scene roots
                if (!IsNamedRoot(t)) continue;
                if (t.name.StartsWith("Decoration_"))
                    RegisterDecoration(t);
                else
                    RegisterTree(t);
            }

            for (int i = decorations.Count - 1; i >= 0; i--)
            {
                if (decorations[i] == null) decorations.RemoveAt(i);
            }
        }

        static bool IsNamedRoot(Transform t)
        {
            string n = t.name;
            return n.StartsWith("Shore_") ||
                   n.StartsWith("HQTree_") ||
                   n.StartsWith("Decoration_") ||
                   n.StartsWith("FruitPlantSpot_");
        }

        static bool IsLockedLandmark(Transform t)
        {
            if (t == null) return true;
            string n = t.name;
            if (n == "StoneWell" || n.StartsWith("StoneWell")) return true;
            if (n == "WisdomTree" || n == "WishTree" || n == "WishTree_Green" ||
                n.StartsWith("WishTree")) return true;
            if (n.StartsWith("Stickman") || n.Contains("FirstVisitor")) return true;
            if (n.StartsWith("Fruit_") || n.StartsWith("Water") || n.StartsWith("River")) return true;
            if (n.Contains("Camera") || n == "EditDragTile") return true;
            return false;
        }

        void SetOtherInputEnabled(bool value)
        {
            // Gestures/harvest off — but KEEP Diorama pan so edit feels like CoC village editor
            if (gestureManager != null) gestureManager.enabled = value;
            if (interactionController != null) interactionController.enabled = value;
        }

        void HandleMouseOrLegacy()
        {
            Vector2 mousePos = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;

            bool down = Mouse.current != null
                ? Mouse.current.leftButton.wasPressedThisFrame
                : Input.GetMouseButtonDown(0);
            bool held = Mouse.current != null
                ? Mouse.current.leftButton.isPressed
                : Input.GetMouseButton(0);
            bool up = Mouse.current != null
                ? Mouse.current.leftButton.wasReleasedThisFrame
                : Input.GetMouseButtonUp(0);

            if (down && !IsOverUI()) TryStartDrag(mousePos);
            else if (held && dragging != null) DragTo(mousePos);
            else if (up) EndDrag();
        }

        void HandleTouch()
        {
            var touch = EnhancedTouch.activeTouches[0];
            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (!IsOverUI()) TryStartDrag(touch.screenPosition);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (dragging != null) DragTo(touch.screenPosition);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    EndDrag();
                    break;
            }
        }

        static bool IsOverUI()
        {
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }

        void TryStartDrag(Vector2 screenPos)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            Transform closest = null;
            float closestDist = pickRadiusPixels;
            foreach (var d in Movables())
            {
                if (IsLockedLandmark(d)) continue;
                Vector3 wp = d.position + Vector3.up * 0.5f;
                Vector3 sp3 = targetCamera.WorldToScreenPoint(wp);
                if (sp3.z < 0.1f) continue;
                float dist = Vector2.Distance(new Vector2(sp3.x, sp3.y), screenPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = d;
                }
            }
            dragging = closest;
            IsDraggingObject = dragging != null;
            if (dragging != null)
            {
                dragOrigin = dragging.position;
                lastValidPos = SnapToCoCTile(dragging.position, gridSize, gridClampHalf);
                EnsureDragGhost();
                dragGhostTile.SetActive(true);
                float foot = Footprint(dragging);
                if (dragGhostTile != null)
                    dragGhostTile.transform.localScale = new Vector3(foot * 2f, foot * 2f, 1f);
                IslandTapTrack.ShowTapHere(dragging, "MOVE TILE");
                EmptyIslandCoachBar.SetTip("Green = free · Red = overlap — release on green");
            }
        }

        void DragTo(Vector2 screenPos)
        {
            if (dragging == null) return;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            var ray = targetCamera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            if (!plane.Raycast(ray, out float distance)) return;

            Vector3 p = SnapToCoCTile(ray.GetPoint(distance), gridSize, gridClampHalf);
            p.y = groundHeight;
            bool ok = IsTileFree(p, dragging);

            // Visual follows finger; commit only on free tiles (CoC red/green)
            if (ok)
            {
                Vector3 oldPos = dragging.position;
                dragging.position = p;
                lastValidPos = p;
                // A fruit tree's pickable fruit isn't parented to it — drag it along too.
                if (treeRoots.Contains(dragging))
                    FruitHarvester.ShiftNearby(oldPos, p - oldPos, 3f);
            }
            else
            {
                // Keep object on last valid — ghost shows intended (red) tile
                dragging.position = lastValidPos;
            }

            if (dragGhostTile != null)
            {
                dragGhostTile.transform.position = p + Vector3.up * 0.05f;
                SetGhostColor(ok ? new Color(0.3f, 1f, 0.4f, 0.55f) : new Color(1f, 0.25f, 0.2f, 0.55f));
            }
        }

        void EndDrag()
        {
            if (dragging != null)
            {
                Vector3 p = lastValidPos;
                if (!IsTileFree(p, dragging))
                    p = FindNearestFree(dragOrigin, dragging);
                p.y = groundHeight;
                dragging.position = p;
                OrchardTreeTracker.Ensure().NotifyMoved(dragging, p);
                string label = dragging.name
                    .Replace("Decoration_", "")
                    .Replace("FruitPlantSpot_", "")
                    .Replace("HQTree_", "Tree ")
                    .Replace("Shore_", "Rock ");
                IslandTapTrack.ShowPlaced(p, label);
            }
            dragging = null;
            IsDraggingObject = false;
            if (dragGhostTile != null) dragGhostTile.SetActive(false);
        }

        float Footprint(Transform t)
        {
            if (t == null) return 0.9f;
            string n = t.name;
            // Trees need wide clearance from each other
            if (n.StartsWith("FruitPlantSpot_") || n.StartsWith("HQTree_") ||
                n.StartsWith("FruitTree_") ||
                n.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return 5.5f;
            if (n.StartsWith("Decoration_")) return 1.2f;
            return 0.95f; // Shore rocks/bushes
        }

        bool IsTileFree(Vector3 p, Transform self)
        {
            var well = GameObject.Find("StoneWell");
            if (well != null && Vector3.Distance(p, well.transform.position) < 3.2f) return false;

            var wish = GameObject.Find("WisdomTree") ?? GameObject.Find("WishTree")
                       ?? GameObject.Find("WishTree_Green");
            if (wish != null && Vector3.Distance(p, wish.transform.position) < 2.5f) return false;

            float selfFoot = Footprint(self);
            foreach (var d in Movables())
            {
                if (d == self || d == null) continue;
                float need = Mathf.Max(selfFoot, Footprint(d));
                if (Vector3.Distance(p, d.position) < need) return false;
            }
            return true;
        }

        IEnumerable<Transform> Movables()
        {
            for (int i = 0; i < decorations.Count; i++)
                if (decorations[i] != null) yield return decorations[i];
            foreach (var t in treeRoots)
                if (t != null) yield return t;
        }

        void SaveLayout()
        {
            // ONLY reward decorations — never mix fruit trees / HQ scenery into GameState deco list
            if (GameState.Instance == null) return;
            var positions = new List<Vector3>();
            for (int i = 0; i < decorations.Count; i++)
            {
                if (decorations[i] == null) continue;
                if (treeRoots.Contains(decorations[i])) continue;
                if (!decorations[i].name.StartsWith("Decoration_")) continue;
                positions.Add(decorations[i].position);
            }
            GameState.Instance.UpdateDecorationPositions(positions);
        }

        Vector3 FindNearestFree(Vector3 from, Transform self)
        {
            for (int r = 0; r <= 10; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                for (int dz = -r; dz <= r; dz++)
                {
                    if (r > 0 && Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;
                    var c = SnapToCoCTile(from + new Vector3(dx, 0f, dz), gridSize, gridClampHalf);
                    if (IsTileFree(c, self)) return c;
                }
            }
            return from;
        }

        void EnsureDragGhost()
        {
            if (dragGhostTile != null) return;
            dragGhostTile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dragGhostTile.name = "EditDragTile";
            dragGhostTile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            dragGhostTile.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            Object.Destroy(dragGhostTile.GetComponent<Collider>());
            SetGhostColor(new Color(0.3f, 1f, 0.4f, 0.5f));
            dragGhostTile.SetActive(false);
        }

        void SetGhostColor(Color c)
        {
            if (dragGhostTile == null) return;
            var mr = dragGhostTile.GetComponent<MeshRenderer>();
            if (mr == null) return;
            if (mr.material == null)
                mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mr.material.color = c;
            if (mr.material.HasProperty("_BaseColor")) mr.material.SetColor("_BaseColor", c);
            mr.material.EnableKeyword("_EMISSION");
            if (mr.material.HasProperty("_EmissionColor")) mr.material.SetColor("_EmissionColor", c * 0.7f);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace RadiantOrchard
{
    /// <summary>
    /// Tap a tree trunk (not mid-lesson, not mid-gesture, not in Edit mode) to
    /// pop the CoC-style info card for that fruit.
    /// </summary>
    public class TreeInfoTapController : MonoBehaviour
    {
        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<TreeInfoTapController>() != null) return;
            new GameObject("TreeInfoTapController").AddComponent<TreeInfoTapController>();
        }

        void OnEnable() => EnhancedTouchSupport.Enable();

        void Update()
        {
            Vector2 pos;
            if (Touchscreen.current != null && EnhancedTouch.activeTouches.Count > 0)
            {
                var t = EnhancedTouch.activeTouches[0];
                if (t.phase != UnityEngine.InputSystem.TouchPhase.Began) return;
                pos = t.screenPosition;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pos = Mouse.current.position.ReadValue();
            }
            else return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // A tree's tap collider (trunk) can geometrically overlap the fruit
            // sitting on it — if the tap actually landed on/near the fruit, let
            // the harvest gesture have it and don't pop the info card over it.
            if (FruitHarvester.IsPointerNearAnyFruit(pos, 90f)) return;

            if (!CanOpenInfoCard()) return;

            var cam = Camera.main;
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(pos);
            if (!Physics.Raycast(ray, out var hit, 200f)) return;

            var treeTarget = hit.collider.GetComponentInParent<TreeInfoTarget>();
            if (treeTarget != null)
            {
                EmptyIslandCameraFocus.FocusSmooth(
                    treeTarget.transform.position + Vector3.up * 0.8f, 11f, 1.2f);
                TreeInfoCardUI.Show(treeTarget.fruit);
                return;
            }

            var decoTarget = hit.collider.GetComponentInParent<DecorationInfoTarget>();
            if (decoTarget != null)
            {
                EmptyIslandCameraFocus.FocusSmooth(
                    decoTarget.transform.position + Vector3.up * 0.6f, 11f, 1.2f);
                DecorationInfoCardUI.Show(decoTarget.title, decoTarget.description);
            }
        }

        static bool CanOpenInfoCard()
        {
            if (EditModeManager.IsDraggingObject) return false;
            var edit = FindFirstObjectByType<EditModeManager>();
            if (edit != null && edit.EditModeActive) return false;
            if (FruitPlantLessonFlow.IsBusy) return false;
            if (EmptyIslandPhaseRunner.BlocksWorldInput) return false;
            // Don't stack this on top of any other full panel already open —
            // that's what was making the screen feel cluttered with cards.
            if (WellWishController.IsUiOpen) return false;
            if (EmptyIslandShopUI.IsOpen) return false;
            return true;
        }
    }
}

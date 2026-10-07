using UnityEngine;
using UnityEngine.Events;

namespace RadiantOrchard
{
    // One reusable interaction entry point for every tappable world object
    // (NPCs, fruit zones, benches, the well, the bridge...). Concrete behavior
    // lives in small subclasses that override CanInteract/OnInteracted instead
    // of a giant switch over object type.
    public class Interactable : MonoBehaviour
    {
        public string interactableId;
        public string requiredVirtueId;
        public int vibrancyReward = 5;
        public UnityEvent onInteractSuccess;
        public UnityEvent onInteractFailure;

        public bool TryInteract(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                onInteractFailure?.Invoke();
                return false;
            }

            bool awardVibrancy = OnInteracted(interactor);
            onInteractSuccess?.Invoke();

            if (awardVibrancy && vibrancyReward != 0 && GameState.Instance != null)
                GameState.Instance.AddVibrancy(vibrancyReward);

            return true;
        }

        protected virtual bool CanInteract(GameObject interactor) => true;

        // Returns whether TryInteract should award vibrancyReward. Subclasses
        // that grant their own reward at a specific moment (e.g. fruit harvest)
        // return false here and call GameState.AddVibrancy themselves.
        protected virtual bool OnInteracted(GameObject interactor) => true;
    }
}

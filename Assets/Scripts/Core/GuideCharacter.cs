using UnityEngine;
using UnityEngine.AI;

namespace RadiantOrchard
{
    // Lightweight wrapper around a Stickman-style character used purely as the
    // onboarding mascot — never a rescuable NPC. Strips every rescue/AI script
    // the source prefab came with so it just stands there and plays clips.
    public class GuideCharacter : MonoBehaviour
    {
        [SerializeField] private Animator anim;

        private void Awake()
        {
            if (anim == null) anim = GetComponentInChildren<Animator>();
            StripRescueBehaviour();
        }

        private void StripRescueBehaviour()
        {
            Strip<StickmanController>();
            Strip<WanderingNPC>();
            Strip<NavMeshAgent>();
            Strip<NPCSpiritComponent>();
            Strip<NPCLookAt>();
            Strip<SymptomBubble>();
        }

        private void Strip<T>() where T : Behaviour
        {
            foreach (var c in GetComponentsInChildren<T>(true))
            {
                c.enabled = false;
                Destroy(c);
            }
        }

        public void PlayIdle()  => Play("Idle");
        public void PlayTalk()  => Play("Talk");
        public void PlayPoint() => Play("Point");

        private void Play(string clip)
        {
            if (anim == null) return;
            anim.Play(clip);
        }
    }
}

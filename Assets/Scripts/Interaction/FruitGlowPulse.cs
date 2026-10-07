using UnityEngine;

namespace RadiantOrchard
{
    // Attaches to every spawned placeholder fruit.
    // Does 3 things:
    //   1. Pulses the scale so the fruit visibly "breathes" → player sees it
    //   2. Bobs up and down slightly
    //   3. Spins slowly so shiny highlight moves around → feels alive
    public class FruitGlowPulse : MonoBehaviour
    {
        [SerializeField] private float pulseSpeed  = 2.2f;
        [SerializeField] private float pulseAmount = 0.18f;  // ±18% size change
        [SerializeField] private float bobSpeed    = 1.4f;
        [SerializeField] private float bobAmount   = 0.12f;  // world units up/down
        [SerializeField] private float spinSpeed   = 55f;    // degrees per second

        private Vector3 baseScale;
        private Vector3 basePos;

        private void OnEnable()
        {
            baseScale = transform.localScale;
            basePos   = transform.position;
        }

        private void Update()
        {
            float t = Time.time;

            // Pulse scale
            float pulse = 1f + Mathf.Sin(t * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * pulse;

            // Bob position
            float bob = Mathf.Sin(t * bobSpeed) * bobAmount;
            transform.position = basePos + Vector3.up * bob;

            // Spin
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        // When NPCSpawner repositions a pooled fruit, reset base values
        public void ResetBase()
        {
            baseScale = transform.localScale;
            basePos   = transform.position;
        }
    }
}

using UnityEngine;

namespace RadiantOrchard
{
    // Lightweight "Fantasy Farm Island" color grade — warm tint + saturation
    // + mild contrast boost — via a plain OnRenderImage blit. No package
    // dependency: com.unity.postprocessing 3.4.0 doesn't compile against
    // Unity 6000.5's Editor API (confirmed live, reverted immediately), and
    // this project needs Built-in RP for the Oak Tree's custom shaders, so
    // URP's Volume system isn't an option either.
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class SimpleColorGradingEffect : MonoBehaviour
    {
        // FIX: Saturation pulled back from 1.25 → 1.05 so the post-process
        // colour grade isn't amplifying the ground green on top of the already-
        // corrected material colours.  Contrast and warm tint unchanged — they
        // help the scene feel sunny without making greens electric.
        public float saturation = 1.05f;
        public float contrast = 1.08f;
        public Color warmTint = new Color(1.06f, 1.0f, 0.9f);

        private Material material;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var shader = Shader.Find("Hidden/SimpleColorGrade");
            if (shader == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (material == null || material.shader != shader)
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_Contrast", contrast);
            material.SetColor("_WarmTint", warmTint);
            Graphics.Blit(source, destination, material);
        }

        private void OnDisable()
        {
            if (material != null) DestroyImmediate(material);
        }
    }
}

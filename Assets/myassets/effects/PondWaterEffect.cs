using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class ConcentricRippleEffect : MonoBehaviour
{
    [Header("Ripple Settings")]
    public float rippleSpeed = 4.0f;
    public float frequency = 20.0f;
    public float amplitude = 0.02f;

    [Header("Vibrancy Reactivity")]
    [Tooltip("Ripple amplitude multiplier when the island is fully grey (0 = still water).")]
    public float lowVibrancyMultiplier = 0.25f;

    private static readonly int ProgressId = Shader.PropertyToID("_ColorRestorationProgress");

    private Renderer rend;
    private MaterialPropertyBlock block;

    void OnEnable()
    {
        rend = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (rend == null) return;
        var mat = rend.sharedMaterial;
        if (mat == null || !mat.HasProperty("_MainTex")) return;

        float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartupAsDouble;

        // Only amplitude reacts to vibrancy, never rippleSpeed inside Sin — see
        // LeafGentleBreeze.cs for why scaling the frequency term is unsafe.
        float liveliness = Mathf.Lerp(lowVibrancyMultiplier, 1f, Shader.GetGlobalFloat(ProgressId));

        // Center-based procedural ripple calculation via material properties
        float wave = Mathf.Sin(t * rippleSpeed) * amplitude * liveliness;

        // Texture offset ko radial/sinusoidal wave shift dena
        float offsetX = Mathf.Cos(t * frequency) * wave;
        float offsetY = Mathf.Sin(t * frequency) * wave;

        rend.GetPropertyBlock(block);
        Vector2 tiling = mat.GetTextureScale("_MainTex");
        block.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, 0.5f + offsetX, 0.5f + offsetY));
        // Same script-driven clock as WaterScroll, so the pond's dual-normal flow/bob stays in sync
        // with the rest of the water instead of drifting on the engine's edit/play-inconsistent _Time.
        if (mat.HasProperty("_FlowTime")) block.SetFloat("_FlowTime", t);
        rend.SetPropertyBlock(block);
    }
}

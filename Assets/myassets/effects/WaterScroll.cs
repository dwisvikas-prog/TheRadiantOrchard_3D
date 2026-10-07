using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class WaterScroll : MonoBehaviour
{
    public float scrollSpeedX = 0.05f;
    public float scrollSpeedY = 0.05f;

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
        if (mat == null) return;

        float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartupAsDouble;
        Vector2 offset = new Vector2(t * scrollSpeedX, t * scrollSpeedY);

        rend.GetPropertyBlock(block);
        if (mat.HasProperty("_MainTex"))
        {
            Vector2 tiling = mat.GetTextureScale("_MainTex");
            block.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, offset.x, offset.y));
        }
        if (mat.HasProperty("_BumpMap"))
        {
            Vector2 tiling = mat.GetTextureScale("_BumpMap");
            block.SetVector("_BumpMap_ST", new Vector4(tiling.x, tiling.y, offset.x, offset.y));
        }
        // Drives the StylizedWater shader's internal dual-normal flow and vertex bob using this
        // script's own clock instead of the engine's _Time, which runs at a different rate between
        // Scene-view "Always Refresh" and actual Play mode — that mismatch was why the water sped up
        // on pressing Play even though this script's own offset math was already consistent.
        if (mat.HasProperty("_FlowTime")) block.SetFloat("_FlowTime", t);
        rend.SetPropertyBlock(block);
    }
}

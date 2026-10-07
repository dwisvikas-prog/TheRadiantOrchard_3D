using UnityEngine;

[ExecuteAlways]
public class WaterFixer : MonoBehaviour
{
    [Header("Water Look")]
    [SerializeField] private Color waterColor = new Color(0.05f, 0.35f, 0.55f, 0.75f);
    [SerializeField, Range(0f, 1f)] private float smoothness = 0.9f;
    [SerializeField, Range(0f, 1f)] private float metallic = 0.0f;

    private const string GeneratedMaterialName = "WaterFixer_Generated_URPLit";

    void OnEnable() => FixMaterials();
    void Start() => FixMaterials();

    void FixMaterials()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return;

        Material[] materials = meshRenderer.sharedMaterials;
        bool changed = false;

        for (int i = 0; i < materials.Length; i++)
        {
            if (!IsBroken(materials[i])) continue;
            materials[i] = CreateWaterMaterial();
            changed = true;
        }

        if (changed)
        {
            meshRenderer.sharedMaterials = materials;
            Debug.Log($"WaterFixer: replaced broken material(s) on '{name}' with a clean URP/Lit water material.");
        }
    }

    // Anything null, actually erroring, or still pointing at the broken
    // Bitgem WaterVolume shader graph counts as broken — even if Unity
    // hasn't swapped its Shader reference to the error shader.
    bool IsBroken(Material mat)
    {
        if (mat == null) return true;
        if (mat.name == GeneratedMaterialName) return false; // already fixed, don't touch it again

        if (mat.shader == null) return true;

        string shaderName = mat.shader.name;
        return shaderName == "Hidden/InternalErrorShader"
            || shaderName.Contains("WaterVolume")
            || !mat.shader.isSupported;
    }

    Material CreateWaterMaterial()
    {
        // This project has moved between URP and Built-in RP more than once
        // (RevertToBuiltIn.cs exists for exactly that reason) — hardcoding
        // "Universal Render Pipeline/Lit" meant this silently fell back to a
        // Standard material with none of the transparency setup applied
        // whenever Built-in RP was active, which it currently is (confirmed:
        // GrassGround's material is shader "Standard"). Detect at runtime
        // instead, matching the same isURP check GenerateOrchardZones.cs uses.
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        Shader shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        if (shader == null)
        {
            Debug.LogWarning("WaterFixer: could not find a Lit shader for the active pipeline.");
            return new Material(Shader.Find("Diffuse"));
        }

        Material mat = new Material(shader) { name = GeneratedMaterialName };

        if (isURP)
        {
            // Transparent surface so waterColor's alpha actually shows as translucent water.
            mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);   // 0 = Alpha blend
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            // Built-in Standard shader's transparency: _Mode 3 = Transparent.
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        mat.color = waterColor;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", metallic);

        return mat;
    }
}

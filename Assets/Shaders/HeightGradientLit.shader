// URP shader replicating the dominant visual driver of the Polytope Studio
// Amplify shaders (PT_Vegetation_*, PT_Rock_Shader, PT_Buildings_Shader_PBR):
// a world-space-height gradient between _GroundColor and _TopColor. Skips
// their triplanar/snow/decal/wind extras, but keeps the actual color signal
// that was reading as flat white after the URP switch (their shaders aren't
// URP-compatible; this is a correctness-preserving replacement, not a
// simplification of what was actually visible before).
Shader "RadiantOrchard/HeightGradientLit"
{
    Properties
    {
        _GroundColor ("Ground Color", Color) = (0.2,0.2,0.2,1)
        _TopColor ("Top Color", Color) = (0.6,0.6,0.6,1)
        _Gradient ("Gradient", Range(0,10)) = 1.4
        _GradientPower ("Gradient Power", Range(0,10)) = 1
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _GroundColor;
            float4 _TopColor;
            float _Gradient;
            float _GradientPower;
            float _Smoothness;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
            OUT.positionCS = vp.positionCS;
            OUT.positionWS = vp.positionWS;
            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            float grad = saturate(pow(max(IN.positionWS.y * _Gradient, 0.0001), max(_GradientPower, 0.0001)));
            half3 albedo = lerp(_GroundColor.rgb, _TopColor.rgb, grad);

            Light mainLight = GetMainLight();
            float3 normalWS = normalize(IN.normalWS);
            half nDotL = saturate(dot(normalWS, mainLight.direction));
            half3 ambient = SampleSH(normalWS);
            half3 lit = albedo * (ambient + mainLight.color * nDotL);

            return half4(lit, 1.0);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}

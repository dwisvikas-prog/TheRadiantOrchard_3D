// URP shader for vertex-colored, texture-less low-poly assets (Polytope Studio
// foliage/rocks, ALP ground) whose original Built-in shaders read per-vertex
// mesh color for their actual appearance — Universal Render Pipeline/Lit
// ignores vertex color entirely, which is why these rendered flat white after
// switching the project to URP.
Shader "RadiantOrchard/VertexColorLit"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float _Glossiness;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 color      : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS   : TEXCOORD0;
            float4 color      : COLOR;
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
            OUT.color = IN.color;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            half3 albedo = IN.color.rgb * _Tint.rgb;

            Light mainLight = GetMainLight();
            float3 normalWS = normalize(IN.normalWS);
            half nDotL = saturate(dot(normalWS, mainLight.direction));
            half3 ambient = SampleSH(normalWS);
            half3 lit = albedo * (ambient + mainLight.color * nDotL);

            return half4(lit, IN.color.a * _Tint.a);
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

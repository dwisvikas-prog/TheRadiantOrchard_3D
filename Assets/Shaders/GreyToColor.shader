// URP-compatible rewrite (was a Built-in surface shader). Desaturates toward a
// grey/stone look, then restores full color outward from a world-space origin
// (the Well/Tree) as _ColorRestorationProgress rises — driven globally by
// ColorRestorationDriver.cs via Shader.SetGlobalFloat/Vector, so every object
// using this shader reacts in sync without per-material wiring.
Shader "RadiantOrchard/GreyToColor"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Restored Color Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.3
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _GreySaturation ("Grey Saturation (0 = fully desaturated stone)", Range(0,1)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _Glossiness;
            float _Metallic;
            float _GreySaturation;
        CBUFFER_END

        // Set globally by ColorRestorationDriver, not exposed in Properties.
        float _ColorRestorationProgress;
        float3 _RestorationOrigin;
        float _RestorationRadius;
        float _EdgeSoftness;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float3 normalWS   : TEXCOORD2;
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
            OUT.positionCS = vp.positionCS;
            OUT.positionWS = vp.positionWS;
            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

            float dist = distance(IN.positionWS.xz, _RestorationOrigin.xz);
            float sweepEdge = _ColorRestorationProgress * _RestorationRadius;
            float localProgress = saturate((sweepEdge - dist) / max(_EdgeSoftness, 0.001));

            float luminance = dot(albedo.rgb, float3(0.299, 0.587, 0.114));
            half3 greyColor = lerp(half3(luminance, luminance, luminance), albedo.rgb, _GreySaturation);
            half3 finalAlbedo = lerp(greyColor, albedo.rgb, localProgress);

            Light mainLight = GetMainLight();
            float3 normalWS = normalize(IN.normalWS);
            half nDotL = saturate(dot(normalWS, mainLight.direction));
            half3 ambient = SampleSH(normalWS);
            half3 lit = finalAlbedo * (ambient + mainLight.color * nDotL);

            return half4(lit, albedo.a);
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

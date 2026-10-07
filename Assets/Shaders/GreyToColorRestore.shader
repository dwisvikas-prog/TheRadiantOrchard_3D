// URP-compatible rewrite (was a Built-in surface shader). Plain uniform lerp
// between a desaturated and full-color sample of the base texture, driven by
// a per-material _ColorRestorationProgress (0 = grey, 1 = restored) — set
// directly by ColorRestorationController.cs / IslandGrowthController.cs.
Shader "RadiantOrchard/GreyToColorRestore"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Restored Color Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.3
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _ColorRestorationProgress ("Color Restoration Progress", Range(0,1)) = 0
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
            float _ColorRestorationProgress;
        CBUFFER_END

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
            float3 normalWS   : TEXCOORD1;
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

            float luminance = dot(albedo.rgb, float3(0.299, 0.587, 0.114));
            half3 greyColor = half3(luminance, luminance, luminance);
            half3 finalAlbedo = lerp(greyColor, albedo.rgb, saturate(_ColorRestorationProgress));

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

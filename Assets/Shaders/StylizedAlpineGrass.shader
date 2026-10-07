// URP-compatible rewrite (was a Built-in surface shader). Blends two warm
// stylized greens by large-scale noise, breaks up the surface with a detail
// texture + normal map, so the island top reads as a lush meadow instead of a
// flat tiled photo (see GenerateGroundAndPaths.cs's note on the original texture).
Shader "RadiantOrchard/StylizedAlpineGrass"
{
    Properties
    {
        _DetailTex ("Detail Albedo (surface breakup only)", 2D) = "white" {}
        _BumpMap ("Detail Normal", 2D) = "bump" {}
        _NoiseTex ("Large-Scale Color Variation Noise", 2D) = "grey" {}
        _ColorA ("Warm Green A (sun-kissed highlight)", Color) = (0.29, 0.478, 0.208, 1)
        _ColorB ("Warm Green B (soft shade)", Color) = (0.176, 0.353, 0.153, 1)
        _DetailStrength ("Detail Breakup Strength", Range(0,1)) = 0.35
        _NoiseTiling ("Noise Tiling (world units per tile)", Float) = 40
        _DetailTiling ("Detail Texture Tiling", Float) = 10
        _Glossiness ("Smoothness", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_DetailTex); SAMPLER(sampler_DetailTex);
        TEXTURE2D(_BumpMap);   SAMPLER(sampler_BumpMap);
        TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _ColorA;
            float4 _ColorB;
            float _DetailStrength;
            float _Glossiness;
            float _NoiseTiling;
            float _DetailTiling;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 tangentOS  : TANGENT;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
            float4 tangentWS  : TEXCOORD2;
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
            OUT.positionCS = vp.positionCS;
            OUT.positionWS = vp.positionWS;
            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
            float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
            OUT.tangentWS = float4(tangentWS, IN.tangentOS.w);
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            float2 noiseUV = IN.positionWS.xz / max(_NoiseTiling, 0.01);
            half noiseSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

            float2 detailUV = IN.positionWS.xz / max(_DetailTiling, 0.01);
            half3 detailTex = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, detailUV).rgb;
            half detailLum = dot(detailTex, half3(0.299, 0.587, 0.114));
            half detailFactor = lerp(1.0 - _DetailStrength * 0.5, 1.0 + _DetailStrength * 0.5, detailLum);

            half3 baseColor = lerp(_ColorB.rgb, _ColorA.rgb, noiseSample);
            half3 albedo = saturate(baseColor * detailFactor);

            half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, detailUV));
            float3 normalWS = normalize(IN.normalWS);
            float3 tangentWS = normalize(IN.tangentWS.xyz);
            float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentWS.w;
            float3 bumpedNormalWS = normalize(
                normalTS.x * tangentWS + normalTS.y * bitangentWS + normalTS.z * normalWS);

            Light mainLight = GetMainLight();
            half nDotL = saturate(dot(bumpedNormalWS, mainLight.direction));
            half3 ambient = SampleSH(bumpedNormalWS);
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

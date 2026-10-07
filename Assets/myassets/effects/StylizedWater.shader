Shader "Custom/StylizedWater"
{
    Properties
    {
        _Color ("Deep Water Color", Color) = (0.03, 0.22, 0.35, 0.88)
        _ShallowColor ("Shallow/Edge Color", Color) = (0.45, 0.85, 0.8, 0.55)
        _MainTex ("Water Albedo", 2D) = "white" {}
        _BumpMapA ("Normal A", 2D) = "bump" {}
        _BumpMapB ("Normal B", 2D) = "bump" {}
        _BumpStrength ("Normal Strength", Range(0,2)) = 0.8
        _Glossiness ("Smoothness", Range(0,1)) = 0.85
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _TilingA ("Tiling A", Vector) = (2,2,0,0)
        _TilingB ("Tiling B", Vector) = (3,3,0,0)
        _FlowA ("Flow Speed A", Vector) = (0.04,0.025,0,0)
        _FlowB ("Flow Speed B", Vector) = (-0.02,0.035,0,0)
        _FresnelPower ("Fresnel Power", Range(0.2,8)) = 3.5
        _FresnelIntensity ("Fresnel Intensity", Range(0,2)) = 1.0
        _FlowTime ("Flow Time (driven by script, not engine clock)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 250
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMapA;
        sampler2D _BumpMapB;
        fixed4 _Color;
        fixed4 _ShallowColor;
        half _Glossiness;
        half _Metallic;
        half _BumpStrength;
        float4 _TilingA;
        float4 _TilingB;
        float4 _FlowA;
        float4 _FlowB;
        half _FresnelPower;
        half _FresnelIntensity;
        float _FlowTime;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float facing : VFACE;
        };

        void vert(inout appdata_full v)
        {
            // slight vertical bob for a subtle "alive" surface without needing tessellation
            // Driven by _FlowTime (set from script, identical rate in Edit and Play mode) instead of
            // the engine's _Time, which visibly runs at different rates between Scene-view "Always
            // Refresh" preview and actual Play mode.
            float wave = sin(_FlowTime * 1.3 + v.vertex.x * 0.6 + v.vertex.z * 0.6) * 0.015;
            v.vertex.y += wave;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uvA = IN.uv_MainTex * _TilingA.xy + _FlowTime * _FlowA.xy;
            float2 uvB = IN.uv_MainTex * _TilingB.xy + _FlowTime * _FlowB.xy;

            fixed4 albedoA = tex2D(_MainTex, uvA);
            fixed4 albedoB = tex2D(_MainTex, uvB);
            fixed4 albedo = lerp(albedoA, albedoB, 0.5) * lerp(_Color, _ShallowColor, 0.25);

            fixed3 nA = UnpackNormal(tex2D(_BumpMapA, uvA));
            fixed3 nB = UnpackNormal(tex2D(_BumpMapB, uvB));
            fixed3 blendedNormal = normalize(fixed3(nA.xy * _BumpStrength + nB.xy * _BumpStrength, nA.z * nB.z));

            half fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), blendedNormal)), _FresnelPower) * _FresnelIntensity;

            o.Albedo = lerp(albedo.rgb, _ShallowColor.rgb, saturate(fresnel));
            o.Normal = blendedNormal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = saturate(lerp(_Color.a, 1.0, fresnel * 0.6));
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}

Shader "Hidden/SimpleColorGrade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0,3)) = 1.25
        _WarmTint ("Warm Tint", Color) = (1.06, 1.0, 0.9, 1)
        _Contrast ("Contrast", Range(0.5,2)) = 1.08
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Saturation;
            float4 _WarmTint;
            float _Contrast;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= _WarmTint.rgb;

                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(float3(luminance, luminance, luminance), col.rgb, _Saturation);

                col.rgb = saturate((col.rgb - 0.5) * _Contrast + 0.5);
                return col;
            }
            ENDCG
        }
    }
}

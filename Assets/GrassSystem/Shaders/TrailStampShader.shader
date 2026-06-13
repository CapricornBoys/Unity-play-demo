Shader "Custom/TrailStamp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _StampPos ("Stamp Position (World XZ)", Vector) = (0, 0, 0, 0)
        _StampRadius ("Stamp Radius", Float) = 1.0
        _StampStrength ("Stamp Strength", Range(0, 10)) = 3.0
        _WorldOrigin ("World Origin", Vector) = (0, 0, -9, 0)
        _WorldSize ("World Size (X, Z)", Vector) = (18, 0, 18, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _StampPos;
                float _StampRadius;
                float _StampStrength;
                float4 _WorldOrigin;
                float4 _WorldSize;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Convert UV to world-space XZ
                float2 worldXZ = _WorldOrigin.xz + i.uv * _WorldSize.xz;

                // Distance from stamp center in world space
                float2 delta = worldXZ - _StampPos.xz;
                float dist = length(delta);

                // Exponential radial falloff: mimics how grass blades get pressed down —
                // deepest at the center, recovering exponentially toward the edge.
                // Sigma = _StampRadius * 0.6 gives trail visibility to ~1.5x radius.
                float alpha = exp(-dist / (_StampRadius * 0.6)) * _StampStrength;
                alpha = saturate(alpha);

                // Max stamp — each pixel keeps the deepest impression from any single frame.
                // Avoids multi-frame accumulation flattening the radial gradient.
                half3 impression = alpha;
                col.rgb = max(col.rgb, impression);

                return col;
            }
            ENDHLSL
        }
    }
}

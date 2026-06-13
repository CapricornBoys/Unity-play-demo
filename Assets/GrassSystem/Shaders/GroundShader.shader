// GroundShader.shader - Ground with grass trail marks
Shader "Custom/GroundShader"
{
    Properties
    {
        _MainColor ("Ground Color", Color) = (0.35, 0.25, 0.15, 1)
        _GrassColor ("Grass Base Color", Color) = (0.15, 0.4, 0.1, 1)
        _TrailColor ("Trail / Pressed Color", Color) = (0.25, 0.18, 0.08, 1)
        _TrailTex   ("Trail Render Texture", 2D) = "black" {}
        _TrailStrength ("Trail Visibility", Range(0,1)) = 0.8
        _TrailWorldOrigin("Trail Origin", Vector) = (-10, 0, -10, 0)
        _TrailWorldSize  ("Trail Size", Vector) = (20, 0, 20, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _GrassColor;
                float4 _TrailColor;
                float4 _TrailTex_ST;
                float  _TrailStrength;
                float4 _TrailWorldOrigin;
                float4 _TrailWorldSize;
            CBUFFER_END

            TEXTURE2D(_TrailTex); SAMPLER(sampler_TrailTex);

            struct Attrs   { float4 pos : POSITION; float2 uv : TEXCOORD0; float3 norm : NORMAL; };
            struct Varyings{ float4 cs  : SV_POSITION; float2 uv : TEXCOORD0; float3 ws : TEXCOORD1; float3 nw : TEXCOORD2; };

            // Carries object UVs for base rendering and world position for trail-map sampling.
            Varyings vert(Attrs IN)
            {
                Varyings o;
                o.cs = TransformObjectToHClip(IN.pos.xyz);
                o.uv = TRANSFORM_TEX(IN.uv, _TrailTex);
                o.ws = TransformObjectToWorld(IN.pos.xyz);
                o.nw = TransformObjectToWorldNormal(IN.norm);
                return o;
            }

            // Samples the trail texture in world XZ space so mesh UV seams cannot create diagonal artifacts.
            half4 frag(Varyings IN) : SV_Target
            {
                float2 trailUV = saturate((IN.ws.xz - _TrailWorldOrigin.xz) / _TrailWorldSize.xz);
                float trail = SAMPLE_TEXTURE2D(_TrailTex, sampler_TrailTex, trailUV).r;
                float4 col = lerp(_GrassColor, _TrailColor, saturate(trail * _TrailStrength));
                Light l = GetMainLight();
                float ndl = saturate(dot(normalize(IN.nw), l.direction));
                col.rgb *= (ndl * 0.7 + 0.3) * l.color;
                return col;
            }
            ENDHLSL
        }
    }
}

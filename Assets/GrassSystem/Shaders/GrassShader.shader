// GrassShader.shader - URP Interactive Grass with Wind + Trampling + Trail
// DEBUG: BaseColor set to red to verify shader recompilation

Shader "Custom/GrassShader"
{
    Properties
    {
        _BaseColor ("Base Color (Bottom)", Color) = (1, 0, 0, 1)
        _TipColor  ("Tip Color (Top)",    Color) = (1, 0, 0, 1)
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.3
        _WindSpeed    ("Wind Speed",    Range(0, 5)) = 1.0
        _WindFrequency("Wind Frequency",Range(0.1, 5)) = 1.0
        _WindDirection("Wind Direction (XZ)", Vector) = (1, 0, 0, 0)
        _BladeWidth   ("Blade Width",  Range(0.01, 0.2)) = 0.05
        _BladeHeight  ("Blade Height", Range(0.1, 2.0)) = 0.5
        _BladeBend    ("Blade Bend",   Range(0, 1))     = 0.3
        _InteractionStrength ("Interaction Strength", Range(0, 2)) = 1.0
        _TrailTex       ("Trail RT", 2D) = "black" {}
        _TrailColor     ("Trail Color", Color) = (0.25, 0.15, 0.05, 1)
        _TrailStrength  ("Trail Strength", Range(0, 5)) = 1.5
        _TrailWorldOrigin("Trail World Origin", Vector) = (-10, 0, -10, 0)
        _TrailWorldSize  ("Trail World Size", Vector) = (20, 0, 20, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue"      = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float  _WindStrength;
                float  _WindSpeed;
                float  _WindFrequency;
                float4 _WindDirection;
                float  _BladeWidth;
                float  _BladeHeight;
                float  _BladeBend;
                float  _InteractionStrength;
                float4 _TrailColor;
                float  _TrailStrength;
                float4 _TrailWorldOrigin;
                float4 _TrailWorldSize;
                float4 _InteractorPositions[8];
                int    _InteractorCount;
            CBUFFER_END

            TEXTURE2D(_TrailTex); SAMPLER(sampler_TrailTex);

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct VertexOutput {
                float4 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            struct GeomToFrag {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  colorLerp  : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            float rand(float2 co) {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            VertexOutput vert(Attributes IN) {
                VertexOutput OUT;
                OUT.positionWS = float4(TransformObjectToWorld(IN.positionOS.xyz), 1);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = IN.uv;
                return OUT;
            }

            GeomToFrag MakeVert(float3 wsPos, float3 normal, float t) {
                GeomToFrag o;
                o.positionCS = TransformWorldToHClip(wsPos);
                o.positionWS = wsPos;
                o.normalWS   = normal;
                o.colorLerp  = t;
                o.fogFactor  = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            [maxvertexcount(12)]
            void geom(point VertexOutput IN[1], inout TriangleStream<GeomToFrag> stream)
            {
                float3 root = IN[0].positionWS.xyz;
                float r = rand(root.xz);
                float h = _BladeHeight * (0.7 + 0.6 * r);
                float w = _BladeWidth  * (0.8 + 0.4 * rand(root.xz + 0.5));
                float facing = r * 6.2832;
                float3 widthDir = normalize(float3(sin(facing), 0, cos(facing)));

                // Wind
                float windNoise = sin(_Time.y * _WindSpeed * _WindFrequency + root.x * 0.5 + root.z * 0.3);
                float windNoise2 = cos(_Time.y * _WindSpeed * 0.7 + root.x * 0.3 + root.z * 0.5);
                float2 wDir = normalize(_WindDirection.xz + float2(0.0001, 0));
                float3 windBend = float3(wDir.x, 0, wDir.y) * (windNoise * 0.7 + windNoise2 * 0.3) * _WindStrength;

                // Interaction
                float3 interactBend = float3(0, 0, 0);
                for (int i = 0; i < _InteractorCount && i < 8; i++)
                {
                    float3 delta = root - _InteractorPositions[i].xyz;
                    delta.y = 0;
                    float dist = length(delta);
                    float rad  = _InteractorPositions[i].w;
                    if (dist < rad)
                    {
                        float falloff = pow(1.0 - saturate(dist / rad), 2.0);
                        float3 pushDir = dist > 0.001 ? normalize(delta) : widthDir;
                        interactBend += pushDir * falloff * _InteractionStrength;
                    }
                }

                float3 bend = windBend + interactBend;

                // 3 segments
                float stepT = 1.0 / 3.0;
                for (int s = 0; s < 3; s++)
                {
                    float t0 = s * stepT;
                    float t1 = (s + 1) * stepT;
                    float3 off0 = float3(0,1,0) * h * t0 + bend * (t0 * t0);
                    float3 off1 = float3(0,1,0) * h * t1 + bend * (t1 * t1);
                    float hw0 = w * 0.5 * (1.0 - t0);
                    float hw1 = w * 0.5 * (1.0 - t1);
                    float3 tang = normalize(off1 - off0 + float3(0.001,0,0));
                    float3 norm = normalize(cross(widthDir, tang));

                    stream.Append(MakeVert(root + off0 - widthDir * hw0, norm, t0));
                    stream.Append(MakeVert(root + off0 + widthDir * hw0, norm, t0));
                    stream.Append(MakeVert(root + off1 - widthDir * hw1, norm, t1));
                    stream.Append(MakeVert(root + off1 + widthDir * hw1, norm, t1));
                    stream.RestartStrip();
                }

                // Tip
                float ts = 1.0 - stepT;
                float3 offTs = float3(0,1,0) * h * ts + bend * (ts * ts);
                float hwTs   = w * 0.5 * (1.0 - ts);
                float3 tip   = root + float3(0,1,0) * h + bend;
                float3 normT = normalize(cross(widthDir, float3(0,1,0)));
                stream.Append(MakeVert(root + offTs - widthDir * hwTs, normT, ts));
                stream.Append(MakeVert(root + offTs + widthDir * hwTs, normT, ts));
                stream.Append(MakeVert(tip, normT, 1.0));
                stream.RestartStrip();
            }

            half4 frag(GeomToFrag IN) : SV_Target
            {
                float4 col = lerp(_BaseColor, _TipColor, saturate(IN.colorLerp));

                // Trail — sample RT at blade world position, darken blade on trampled areas
                float2 trailUV = (IN.positionWS.xz - _TrailWorldOrigin.xz) / _TrailWorldSize.xz;
                float trail = SAMPLE_TEXTURE2D(_TrailTex, sampler_TrailTex, trailUV).r;
                trail = saturate(trail * _TrailStrength);
                col.rgb = lerp(col.rgb, col.rgb * _TrailColor.rgb, trail);

                // Simple diffuse
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                float3 lighting = mainLight.color * (NdotL * 0.8 + 0.2);
                float3 final = col.rgb * lighting;
                final = MixFog(final, IN.fogFactor);
                return half4(final, col.a);
            }
            ENDHLSL
        }
    }
}

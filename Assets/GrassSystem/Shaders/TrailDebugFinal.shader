Shader "Custom/TrailDebugFinal"
{
    Properties
    {
        _BladeWidth   ("Blade Width",  Range(0.01, 0.2)) = 0.05
        _BladeHeight  ("Blade Height", Range(0.1, 2.0)) = 0.5
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.3
        _WindSpeed    ("Wind Speed",    Range(0, 5)) = 1.0
        _InteractionStrength ("Interaction Force", Range(0, 2)) = 1.0
        _TrailTex       ("Trail RT", 2D) = "black" {}
        _TrailStrength  ("Trail Strength", Range(0, 5)) = 1.5
        _TrailWorldOrigin("Trail Origin", Vector) = (-10, 0, -10, 0)
        _TrailWorldSize  ("Trail Size", Vector) = (20, 0, 20, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex v
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float _WindStrength; float _WindSpeed;
                float _BladeWidth; float _BladeHeight; float _InteractionStrength;
                float4 _TrailWorldOrigin;
                float4 _TrailWorldSize;
                float  _TrailStrength;
                float4 _InteractorPositions[8];
                int    _InteractorCount;
            CBUFFER_END

            TEXTURE2D(_TrailTex); SAMPLER(sampler_TrailTex);

            struct A { float4 p:POSITION; float3 n:NORMAL; float2 u:TEXCOORD0; };
            struct V { float4 w:TEXCOORD0; float3 n:TEXCOORD1; float2 u:TEXCOORD2; };
            struct G { float4 c:SV_POSITION; float3 w:TEXCOORD0; float3 n:TEXCOORD1; float t:TEXCOORD2; float f:TEXCOORD3; };

            float rnd(float2 c) { return frac(sin(dot(c,float2(12.9898,78.233)))*43758.5453); }
            V v(A i) { V o; o.w=float4(TransformObjectToWorld(i.p.xyz),1); o.n=TransformObjectToWorldNormal(i.n); o.u=i.u; return o; }
            G mv(float3 p,float3 n,float t) { G o; o.c=TransformWorldToHClip(p); o.w=p; o.n=n; o.t=t; o.f=ComputeFogFactor(o.c.z); return o; }

            [maxvertexcount(12)]
            void geom(point V I[1], inout TriangleStream<G> s)
            {
                float3 rt=I[0].w.xyz; float ra=rnd(rt.xz);
                float h=_BladeHeight*(0.7+0.6*ra); float w=_BladeWidth*(0.8+0.4*rnd(rt.xz+0.5));
                float fa=ra*6.2832; float3 wd=normalize(float3(sin(fa),0,cos(fa)));
                float wn0=sin(_Time.y*_WindSpeed+rt.x*0.5+rt.z*0.3);
                float wn1=cos(_Time.y*_WindSpeed*0.7+rt.x*0.3+rt.z*0.5);
                float3 wb=float3(wn0*0.7+wn1*0.3,0,0)*_WindStrength;
                float3 ib=float3(0,0,0);
                for(int ii=0;ii<_InteractorCount&&ii<8;ii++){
                    float3 dl=rt-_InteractorPositions[ii].xyz; dl.y=0; float ds=length(dl);
                    float rn=_InteractorPositions[ii].w;
                    if(ds<rn){
                        float ff=pow(1-saturate(ds/rn),2);
                        float3 pd=ds>1e-3?normalize(dl):wd;
                        ib+=pd*ff*_InteractionStrength;
                    }
                }
                float3 bd=wb+ib; float st=1.0/3;
                for(int j=0;j<3;j++){
                    float t0=j*st,t1=(j+1)*st;
                    float3 o0=float3(0,1,0)*h*t0+bd*(t0*t0); float3 o1=float3(0,1,0)*h*t1+bd*(t1*t1);
                    float h0=w*0.5*(1-t0); float h1=w*0.5*(1-t1);
                    float3 tg=normalize(o1-o0+float3(1e-3,0,0)); float3 nm=normalize(cross(wd,tg));
                    s.Append(mv(rt+o0-wd*h0,nm,t0)); s.Append(mv(rt+o0+wd*h0,nm,t0));
                    s.Append(mv(rt+o1-wd*h1,nm,t1)); s.Append(mv(rt+o1+wd*h1,nm,t1)); s.RestartStrip();
                }
                float ts=1-st; float3 ot=float3(0,1,0)*h*ts+bd*(ts*ts); float ht=w*0.5*(1-ts);
                float3 tp=rt+float3(0,1,0)*h+bd; float3 nt=normalize(cross(wd,float3(0,1,0)));
                s.Append(mv(rt+ot-wd*ht,nt,ts)); s.Append(mv(rt+ot+wd*ht,nt,ts)); s.Append(mv(tp,nt,1)); s.RestartStrip();
            }

            half4 frag(G i) : SV_Target
            {
                // Sample trail RT using world-space UV
                float2 tuv = (i.w.xz - _TrailWorldOrigin.xz) / _TrailWorldSize.xz;
                float tr = SAMPLE_TEXTURE2D(_TrailTex, sampler_TrailTex, tuv).r;
                tr = saturate(tr * _TrailStrength);

                // Grass base color
                half3 grassCol = half3(0.12, 0.45, 0.08);
                half3 col = grassCol;

                // Trail effect: darken the grass toward a dark olive-brown.
                // Using lerp to a very dark neutral tone avoids any color cast.
                if (tr > 0.001)
                {
                    // Dark target color — neutral dark brown (no red bias)
                    half3 darkTone = half3(0.06, 0.05, 0.02);
                    col = lerp(grassCol, darkTone, tr);
                }

                // Simple hemisphere lighting
                half3 N = normalize(i.n);
                half NdotL = max(dot(N, half3(0.3, 0.8, 0.5)), 0.0);
                col *= (0.35 + 0.65 * NdotL);

                // Fog
                col = MixFog(col, i.f);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
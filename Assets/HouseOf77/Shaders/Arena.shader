Shader "HouseOf77/Arena"
{
    Properties { _Color("Stone", Color) = (0.065,0.075,0.11,1) _Luck("Luck", Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _Luck;
            CBUFFER_END
            Varyings vert(Attributes a) { Varyings o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz); o.world=TransformObjectToWorld(a.positionOS.xyz).xy; o.uv=a.uv; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            half4 frag(Varyings i):SV_Target
            {
                float2 w=i.world;
                float row=floor(w.y/1.4);
                float2 stone=float2(w.x+fmod(abs(row),2)*1.3,w.y)/float2(2.6,1.4);
                float2 cell=frac(stone);
                float grout=1-smoothstep(.015,.045,min(min(cell.x,1-cell.x),min(cell.y,1-cell.y)));
                float grain=hash(floor(w*20))*.008;
                float shade=hash(floor(stone))*.018;
                float3 c=_Color.rgb+shade+grain;
                c=lerp(c,c*.40,grout);
                float d=length(w*float2(1,1.07));
                float rings=1-smoothstep(.022,.055,min(abs(d-3.1),abs(d-3.35)));
                float outer=1-smoothstep(.015,.04,abs(d-5.75));
                float angle=atan2(w.y,w.x);
                float markings=step(.88,cos(angle*28))*step(5.40,d)*step(d,5.68);
                float3 gold=lerp(float3(.19,.15,.075),float3(.55,.33,.08),_Luck);
                c+=gold*(rings*.75+outer*.4+markings*.65);
                float border=max(abs(w.x)/15.4,abs(w.y)/7.7);
                c*=1-smoothstep(.65,1.08,border)*.6;
                c+=float3(.07,.035,.12)*exp(-abs(abs(w.x)-15.1)*4)*(.75+.25*sin(_Time.y));
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}

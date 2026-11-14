Shader "HouseOf77/Ring"
{
    Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} _Fill("Fill",Range(0,1))=0 _Width("Width",Range(.001,.5))=.03 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct A {float3 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            CBUFFER_START(UnityPerMaterial)
            float _Fill,_Width;
            CBUFFER_END
            V vert(A a){UNITY_SETUP_INSTANCE_ID(a);SetUpSpriteInstanceProperties();V o;o.positionCS=TransformObjectToHClip(UnityFlipSprite(a.positionOS,unity_SpriteProps.xy));o.uv=a.uv;o.color=a.color*unity_SpriteColor;return o;}
            half4 frag(V i):SV_Target {float d=length(i.uv-.5)*2;float aa=max(fwidth(d),.005);float edge=1-smoothstep(1-aa,1,d);float ring=smoothstep(1-_Width-aa,1-_Width,d)*edge;return half4(i.color.rgb,i.color.a*max(ring,edge*_Fill));}
            ENDHLSL
        }
    }
}

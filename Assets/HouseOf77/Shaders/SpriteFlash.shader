Shader "HouseOf77/SpriteFlash"
{
    Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Flash("Flash",Range(0,1))=0 _Gold("Gold",Range(0,1))=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float _Flash, _Gold;
            CBUFFER_END
            Varyings vert(Attributes a) { UNITY_SETUP_INSTANCE_ID(a); SetUpSpriteInstanceProperties(); Varyings o; o.positionCS=TransformObjectToHClip(UnityFlipSprite(a.positionOS,unity_SpriteProps.xy)); o.uv=a.uv; o.color=a.color*unity_SpriteColor; return o; }
            half4 frag(Varyings i):SV_Target { half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color; c.rgb=lerp(c.rgb,c.rgb*float3(1.4,1.08,.55)+float3(.08,.035,0),_Gold); c.rgb=lerp(c.rgb,float3(1,1,.92),_Flash); return c; }
            ENDHLSL
        }
    }
}

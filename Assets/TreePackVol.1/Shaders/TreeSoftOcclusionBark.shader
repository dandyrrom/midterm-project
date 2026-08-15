Shader "Nature/Tree Soft Occlusion Bark"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _BaseLight ("Base Light", Range(0, 1)) = 0.25
        _AO ("Amb. Occlusion", Range(0, 10)) = 2.4
        [HideInInspector] _TreeInstanceColor ("TreeInstanceColor", Vector) = (1,1,1,1)
        [HideInInspector] _TreeInstanceScale ("TreeInstanceScale", Vector) = (1,1,1,1)
        [HideInInspector] _SquashAmount ("Squash", Float) = 1
    }

    Dependency "BillboardShader" = "Hidden/Nature/Tree Soft Occlusion Bark Rendertex"

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex TreeVert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "UrpTreeCreatorCommon.hlsl"
            half4 Frag(Varyings input) : SV_Target { return TreeFrag(input, false); }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex TreeShadowVert
            #pragma fragment Frag
            #include "UrpTreeCreatorCommon.hlsl"
            half4 Frag(Varyings input) : SV_Target { return TreeShadowFrag(input, false); }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex TreeShadowVert
            #pragma fragment Frag
            #include "UrpTreeCreatorCommon.hlsl"
            half4 Frag(Varyings input) : SV_Target { return TreeShadowFrag(input, false); }
            ENDHLSL
        }
    }
    FallBack Off
}

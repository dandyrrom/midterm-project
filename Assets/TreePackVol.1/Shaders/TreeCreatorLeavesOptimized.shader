Shader "Hidden/Nature/Tree Creator Leaves Optimized"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _ShadowTex ("Shadow (RGB)", 2D) = "white" {}
        _BumpSpecMap ("Normalmap (GA) Spec (R) Shadow Offset (B)", 2D) = "bump" {}
        _TranslucencyMap ("Trans (B) Gloss(A)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
        [HideInInspector] _TreeInstanceColor ("TreeInstanceColor", Vector) = (1,1,1,1)
        [HideInInspector] _TreeInstanceScale ("TreeInstanceScale", Vector) = (1,1,1,1)
        [HideInInspector] _SquashAmount ("Squash", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
        }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex TreeVert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "UrpTreeCreatorCommon.hlsl"
            half4 Frag(Varyings input) : SV_Target { return TreeFrag(input, true); }
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
            half4 Frag(Varyings input) : SV_Target { return TreeShadowFrag(input, true); }
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
            half4 Frag(Varyings input) : SV_Target { return TreeShadowFrag(input, true); }
            ENDHLSL
        }
    }
    FallBack Off
}

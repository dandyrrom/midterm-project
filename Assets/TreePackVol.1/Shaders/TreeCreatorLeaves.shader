Shader "Nature/Tree Creator Leaves"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}
        _GlossMap ("Gloss (A)", 2D) = "black" {}
        _TranslucencyMap ("Translucency (A)", 2D) = "white" {}
        _ShadowOffset ("Shadow Offset (A)", 2D) = "black" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
        [HideInInspector] _TreeInstanceColor ("TreeInstanceColor", Vector) = (1,1,1,1)
        [HideInInspector] _TreeInstanceScale ("TreeInstanceScale", Vector) = (1,1,1,1)
        [HideInInspector] _SquashAmount ("Squash", Float) = 1
    }

    Dependency "OptimizedShader" = "Hidden/Nature/Tree Creator Leaves Optimized"

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

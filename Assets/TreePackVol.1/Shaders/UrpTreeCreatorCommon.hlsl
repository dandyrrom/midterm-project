#ifndef URP_TREE_CREATOR_COMMON_INCLUDED
#define URP_TREE_CREATOR_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
half4 _Color;
half _Cutoff;
CBUFFER_END

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 color : COLOR;
};

Varyings TreeVert(Attributes input)
{
    Varyings output;
    VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
    output.positionCS = posInputs.positionCS;
    output.positionWS = posInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    output.color = input.color;
    return output;
}

half4 TreeFrag(Varyings input, bool alphaClip)
{
    half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
    half ao = max(input.color.a, 0.35h);
    albedo.rgb *= input.color.rgb * ao;

    if (alphaClip)
        clip(albedo.a - _Cutoff);

    Light light = GetMainLight();
    half3 normalWS = normalize(input.normalWS);
    half ndotl = saturate(dot(normalWS, light.direction));
    half3 lighting = light.color * (ndotl * 0.65h + 0.35h);
    lighting += SampleSH(normalWS);
    return half4(albedo.rgb * lighting, 1);
}

Varyings TreeShadowVert(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    output.color = input.color;
    return output;
}

half4 TreeShadowFrag(Varyings input, bool alphaClip)
{
    if (alphaClip)
    {
        half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _Color.a;
        clip(alpha - _Cutoff);
    }
    return 0;
}

#endif

Shader "Custom/WorldSpaceWall"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _TileScale ("Texture Scale", Float) = 0.1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct Attributes
        {
            float4 positionOS   : POSITION;
            float3 normalOS     : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS   : SV_POSITION;
            float3 positionWS   : TEXCOORD0;
            float3 normalWS     : TEXCOORD1;
        };

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float _TileScale;
            float _Smoothness;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(0,0,0,0));

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 absNormal = abs(normalWS);

                // Triplanar Weights
                float3 weights = absNormal / (absNormal.x + absNormal.y + absNormal.z);

                // World Coordinates Tiling UVs
                float2 uvX = input.positionWS.zy * _TileScale;
                float2 uvY = input.positionWS.xz * _TileScale;
                float2 uvZ = input.positionWS.xy * _TileScale;

                // Sample Base Map
                half4 colX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX);
                half4 colY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY);
                half4 colZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ);

                half4 finalColor = colX * weights.x + colY * weights.y + colZ * weights.z;

                // Basic Lighting
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half NdotL = saturate(dot(normalWS, lightDir));
                half3 ambient = half3(0.2, 0.2, 0.2);

                half3 finalRGB = finalColor.rgb * (mainLight.color * NdotL + ambient);

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
}

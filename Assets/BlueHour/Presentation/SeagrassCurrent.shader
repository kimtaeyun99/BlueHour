Shader "BlueHour/Prototype Seagrass"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.15, 0.32, 0.17, 1)
        _LightFactor("Light Factor", Range(0, 1)) = 1
        _WaterCurrent("Water Current", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _LightFactor;
                float4 _WaterCurrent;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; uint vertexId : SV_VertexID; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float height : TEXCOORD0;
                float fog : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float breeze = sin(_Time.y * 1.4 + world.x * 0.19 + world.z * 0.13);
                uint bladeVertex = input.vertexId % 5;
                output.height = bladeVertex < 2 ? 0.0 : bladeVertex == 4 ? 1.0 : 0.58;
                world.xz += (_WaterCurrent.xy * 0.68 + float2(breeze * 0.10, breeze * 0.07))
                    * (output.height * output.height);
                output.positionCS = TransformWorldToHClip(world);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half3 color = _BaseColor.rgb * lerp(0.55, 1.25, input.height)
                    * lerp(0.24, 1.0, sqrt(saturate(_LightFactor)));
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}

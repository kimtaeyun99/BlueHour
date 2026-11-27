Shader "BlueHour/Prototype Water Surface"
{
    Properties
    {
        _LightFactor("Light Factor", Range(0, 1)) = 1
        _Glints("Glints", Range(0, 1)) = 1
        _WaterCurrent("Water Current", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _LightFactor;
                float _Glints;
                float4 _WaterCurrent;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fog : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.positionWS.xz * 0.82 + _WaterCurrent.xy * (_Time.y * 0.26);
                float wave = sin(uv.x + 0.85 * sin(uv.y - _Time.y * 0.54))
                    + cos(uv.y * 0.86 + 0.72 * sin(uv.x - _Time.y * 0.42));
                float caustics = pow(saturate(1.0 - abs(wave) * 1.7), 5.0);
                float light = sqrt(saturate(_LightFactor));
                half3 color = lerp(half3(0.012, 0.035, 0.062), half3(0.11, 0.46, 0.52), light);
                color += half3(0.18, 0.31, 0.30) * (caustics * _Glints * light);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}

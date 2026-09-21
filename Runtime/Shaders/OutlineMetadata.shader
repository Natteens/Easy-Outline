Shader "Hidden/Natteens/Outline/Metadata"
{
    Properties
    {
        _OutlineTargetColor("Target Color", Color) = (1,1,1,1)
        _OutlineUseTargetColor("Use Target Color", Float) = 0
        _OutlineObjectId("Object ID", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off
        ZTest Equal

        Pass
        {
            Name "OutlineMetadata"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineTargetColor;
            float _OutlineUseTargetColor;
            float4 _OutlineObjectId;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation float3 objectId : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct FragmentOutput
            {
                float4 metadata : SV_Target0;
                float4 normal : SV_Target1;
                float4 targetColor : SV_Target2;
            };

            float3 HashObject(float3 originWS, float3 axisX, float3 axisY, float3 axisZ)
            {
                float3 seed = originWS * 0.173 + axisX * 1.137 + axisY * 2.311 + axisZ * 3.719;
                return 0.002 + frac(sin(float3(
                    dot(seed, float3(12.9898, 78.233, 37.719)),
                    dot(seed, float3(39.3468, 11.135, 83.155)),
                    dot(seed, float3(73.156, 52.235, 9.151)))) * 43758.5453) * 0.996;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                if (_OutlineObjectId.a > 0.5)
                {
                    output.objectId = _OutlineObjectId.rgb;
                }
                else
                {
                    float3 origin = TransformObjectToWorld(0.0.xxx);
                    float3 axisX = TransformObjectToWorld(float3(1, 0, 0)) - origin;
                    float3 axisY = TransformObjectToWorld(float3(0, 1, 0)) - origin;
                    float3 axisZ = TransformObjectToWorld(float3(0, 0, 1)) - origin;
                    output.objectId = HashObject(origin, axisX, axisY, axisZ);
                }
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            FragmentOutput Frag(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                FragmentOutput output;
                output.metadata = float4(input.objectId, 1);
                output.normal = float4(normalize(input.normalWS) * 0.5 + 0.5, 1);
                output.targetColor = float4(_OutlineTargetColor.rgb, saturate(_OutlineUseTargetColor));
                return output;
            }
            ENDHLSL
        }
    }
}

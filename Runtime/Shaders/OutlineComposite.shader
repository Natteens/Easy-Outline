Shader "Hidden/Natteens/Outline/Composite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_OutlineMetadata);
        TEXTURE2D_X(_OutlineTargetColors);
        TEXTURE2D_X_FLOAT(_OutlineSelectedDepth);
        TEXTURE2D_X_FLOAT(_OutlineCameraDepth);
        TEXTURE2D_X(_OutlineVisibleIds);
        TEXTURE2D_X(_OutlineHorizontalIds);
        TEXTURE2D_X(_OutlineHorizontalColors);

        float4 _OutlineFixedColor;
        float4 _OutlineSettings;
        int _OutlineDebugMode;
        int _OutlineOcclusionEdges;

        int2 TextureSize()
        {
            return max(int2(_BlitTexture_TexelSize.zw + 0.5), int2(1, 1));
        }

        int2 ClampCoord(int2 coord)
        {
            return clamp(coord, 0, TextureSize() - 1);
        }

        int2 PixelCoord(float2 uv)
        {
            return ClampCoord(int2(floor(saturate(uv) * _BlitTexture_TexelSize.zw)));
        }

        float4 MetadataAt(int2 coord)
        {
            return LOAD_TEXTURE2D_X(_OutlineMetadata, ClampCoord(coord));
        }

        float4 VisibleIdAt(int2 coord)
        {
            return LOAD_TEXTURE2D_X(_OutlineVisibleIds, ClampCoord(coord));
        }

        float EyeDepth(float rawDepth)
        {
            return unity_OrthoParams.w > 0.5 ? LinearDepthToEyeDepth(rawDepth) : LinearEyeDepth(rawDepth, _ZBufferParams);
        }

        float DepthTolerance(float a, float b)
        {
            return max(0.0005, min(a, b) * 0.0001);
        }

        bool SameObject(float3 a, float3 b)
        {
            return max(abs(a.r - b.r), max(abs(a.g - b.g), abs(a.b - b.b))) < 0.0015;
        }

        bool StableIdLess(float3 a, float3 b)
        {
            if (abs(a.r - b.r) > 0.0015) return a.r < b.r;
            if (abs(a.g - b.g) > 0.0015) return a.g < b.g;
            return a.b < b.b;
        }

        bool ProjectedBoundary(int2 center, float3 centerId)
        {
            bool boundary = false;
            [unroll] for (int y = -1; y <= 1; y++)
            [unroll] for (int x = -1; x <= 1; x++)
            {
                if (x != 0 || y != 0)
                {
                    float4 neighbor = MetadataAt(center + int2(x, y));
                    if (neighbor.a < 0.5 || !SameObject(centerId, neighbor.rgb))
                        boundary = true;
                }
            }
            return boundary;
        }

        float3 SourceColorAt(int2 coord)
        {
            float2 uv = (float2(ClampCoord(coord)) + 0.5) * _BlitTexture_TexelSize.xy;
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0).rgb;
        }

        float3 LineColorAt(int2 owner)
        {
            float4 custom = LOAD_TEXTURE2D_X(_OutlineTargetColors, ClampCoord(owner));
            if (custom.a > 0.5)
                return custom.rgb;
            if (_OutlineSettings.w > 0.5)
                return _OutlineFixedColor.rgb;
            return SourceColorAt(owner) * saturate(1.0 - _OutlineSettings.y);
        }

        bool HasForeignNeighbor(int2 center, float3 centerId)
        {
            bool foreign = false;
            [unroll] for (int y = -1; y <= 1; y++)
            [unroll] for (int x = -1; x <= 1; x++)
            {
                if (x != 0 || y != 0)
                {
                    float4 neighbor = VisibleIdAt(center + int2(x, y));
                    if (neighbor.a > 0.25 && !SameObject(centerId, neighbor.rgb))
                        foreign = true;
                }
            }
            return foreign;
        }

        float3 ResolveDirect(int2 center)
        {
            float4 current = VisibleIdAt(center);
            float3 result = float3(0, center);
            if (current.a > 0.25)
                result.x = HasForeignNeighbor(center, current.rgb) ? 1.0 : 0.0;
            else
            {
                int2 owner = center;
                float3 ownerId = 0;
                int bestDistance = 100;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    int2 neighborCoord = ClampCoord(center + int2(x, y));
                    float4 neighbor = VisibleIdAt(neighborCoord);
                    int distance = x * x + y * y;
                    if (neighbor.a > 0.75 &&
                        (distance < bestDistance || (distance == bestDistance && StableIdLess(neighbor.rgb, ownerId))))
                    {
                        owner = neighborCoord;
                        ownerId = neighbor.rgb;
                        bestDistance = distance;
                    }
                }
                result = float3(bestDistance < 100 ? 1.0 : 0.0, owner);
            }
            return result;
        }

        float4 DebugOutput(int2 center, bool outline)
        {
            float4 visible = VisibleIdAt(center);
            float3 value = 0;
            if (_OutlineDebugMode == 1) value = (MetadataAt(center).a > 0.5 ? 1.0 : 0.0).xxx;
            if (_OutlineDebugMode == 2) value = visible.a > 0.25 ? visible.rgb : 0.0.xxx;
            if (_OutlineDebugMode == 3) value = (outline ? 1.0 : 0.0).xxx;
            if (_OutlineDebugMode == 4) value = (visible.a > 0.25 ? 1.0 : 0.0).xxx;
            return float4(value, 1);
        }

        float4 ResolveVisibility(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            float4 metadata = MetadataAt(center);
            if (metadata.a < 0.5)
                return 0;
            float selectedDepth = EyeDepth(LOAD_TEXTURE2D_X(_OutlineSelectedDepth, center).r);
            float sceneDepth = EyeDepth(LOAD_TEXTURE2D_X(_OutlineCameraDepth, center).r);
            if (selectedDepth > sceneDepth + DepthTolerance(selectedDepth, sceneDepth))
                return 0;
            metadata.a = _OutlineOcclusionEdges != 0 || ProjectedBoundary(center, metadata.rgb) ? 1.0 : 0.5;
            return metadata;
        }

        float4 CompositeDirect(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            float3 silhouette = ResolveDirect(center);
            bool outline = silhouette.x > 0.5 &&
                (_OutlineOcclusionEdges != 0 || MetadataAt(center).a < 0.5 || VisibleIdAt(center).a > 0.25);
            if (_OutlineDebugMode != 0)
                return DebugOutput(center, outline);
            float4 baseColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, 0);
            if (outline)
                baseColor.rgb = lerp(baseColor.rgb, LineColorAt(int2(silhouette.yz)), saturate(_OutlineSettings.x));
            return baseColor;
        }

        struct DilationOutput
        {
            float4 owner : SV_Target0;
            float4 color : SV_Target1;
        };

        DilationOutput HorizontalDilate(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            int radius = clamp((int)_OutlineSettings.z, 1, 8);
            int bestDistance = 100;
            int2 bestCoord = center;
            float3 bestId = 0;

            [unroll] for (int offset = -8; offset <= 8; offset++)
            {
                int distance = abs(offset);
                if (distance <= radius)
                {
                    int2 candidateCoord = ClampCoord(center + int2(offset, 0));
                    float4 candidate = VisibleIdAt(candidateCoord);
                    if (candidate.a > 0.75 &&
                        (distance < bestDistance || (distance == bestDistance && StableIdLess(candidate.rgb, bestId))))
                    {
                        bestCoord = candidateCoord;
                        bestId = candidate.rgb;
                        bestDistance = distance;
                    }
                }
            }

            DilationOutput output;
            output.owner = bestDistance < 100 ? float4(bestId, (bestDistance + 1) / 255.0) : 0;
            output.color = bestDistance < 100 ? float4(LineColorAt(bestCoord), 1) : 0;
            return output;
        }

        float4 DilatedComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            float4 current = VisibleIdAt(center);
            bool outline = false;
            float3 lineColor = 0;

            if (current.a > 0.25)
            {
                outline = HasForeignNeighbor(center, current.rgb);
                if (outline && _OutlineDebugMode == 0)
                    lineColor = LineColorAt(center);
            }
            else
            {
                int radius = clamp((int)_OutlineSettings.z, 1, 8);
                float maxDistance = (radius + 0.5) * (radius + 0.5);
                int bestDistance = 1000;
                float3 bestId = 0;

                [unroll] for (int offset = -8; offset <= 8; offset++)
                {
                    if (abs(offset) <= radius)
                    {
                        int2 rowCoord = ClampCoord(center + int2(0, offset));
                        float4 candidate = LOAD_TEXTURE2D_X(_OutlineHorizontalIds, rowCoord);
                        int xDistance = (int)round(candidate.a * 255.0) - 1;
                        int distance = xDistance * xDistance + offset * offset;
                        if (xDistance >= 0 && distance <= maxDistance &&
                            (distance < bestDistance || (distance == bestDistance && StableIdLess(candidate.rgb, bestId))))
                        {
                            bestDistance = distance;
                            bestId = candidate.rgb;
                            lineColor = LOAD_TEXTURE2D_X(_OutlineHorizontalColors, rowCoord).rgb;
                        }
                    }
                }
                outline = bestDistance < 1000 && (_OutlineOcclusionEdges != 0 || MetadataAt(center).a < 0.5);
            }

            if (_OutlineDebugMode != 0)
                return DebugOutput(center, outline);
            float4 baseColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, 0);
            if (outline)
                baseColor.rgb = lerp(baseColor.rgb, lineColor, saturate(_OutlineSettings.x));
            return baseColor;
        }
        ENDHLSL

        Pass
        {
            Name "ResolveVisibility"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment ResolveVisibility
            ENDHLSL
        }

        Pass
        {
            Name "DirectComposite"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment CompositeDirect
            ENDHLSL
        }

        Pass
        {
            Name "HorizontalDilation"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment HorizontalDilate
            ENDHLSL
        }

        Pass
        {
            Name "DilatedComposite"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment DilatedComposite
            ENDHLSL
        }
    }
}

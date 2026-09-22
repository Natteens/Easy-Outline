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
        TEXTURE2D_X(_OutlineOwner);
        TEXTURE2D_X(_OutlineDilated);
        TEXTURE2D_X(_OutlineDilatedOwner);

        float4 _OutlineFixedColor;
        float4 _OutlineSettings;
        int _OutlineDebugMode;

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

        float4 TargetColorAt(int2 coord)
        {
            return LOAD_TEXTURE2D_X(_OutlineTargetColors, ClampCoord(coord));
        }

        float EyeDepth(float rawDepth)
        {
            return unity_OrthoParams.w > 0.5 ? LinearDepthToEyeDepth(rawDepth) : LinearEyeDepth(rawDepth, _ZBufferParams);
        }

        float SelectedEyeDepth(int2 coord)
        {
            return EyeDepth(LOAD_TEXTURE2D_X(_OutlineSelectedDepth, ClampCoord(coord)).r);
        }

        float CameraEyeDepth(int2 coord)
        {
            return EyeDepth(LOAD_TEXTURE2D_X(_OutlineCameraDepth, ClampCoord(coord)).r);
        }

        float DepthTolerance(float a, float b)
        {
            return max(0.0005, min(a, b) * 0.0001);
        }

        bool SelectedAt(int2 coord)
        {
            return MetadataAt(coord).a > 0.5;
        }

        bool VisibleAt(int2 selectedCoord, int2 sceneCoord)
        {
            if (!SelectedAt(selectedCoord))
                return false;
            float selectedDepth = SelectedEyeDepth(selectedCoord);
            float sceneDepth = CameraEyeDepth(sceneCoord);
            return selectedDepth <= sceneDepth + DepthTolerance(selectedDepth, sceneDepth);
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

        float3 SourceColorAt(int2 coord)
        {
            float2 uv = (float2(ClampCoord(coord)) + 0.5) * _BlitTexture_TexelSize.xy;
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0).rgb;
        }

        float3 LineColorAt(int2 owner)
        {
            float4 custom = TargetColorAt(owner);
            if (custom.a > 0.5)
                return custom.rgb;
            if (_OutlineSettings.w > 0.5)
                return _OutlineFixedColor.rgb;
            return SourceColorAt(owner) * saturate(1.0 - _OutlineSettings.y);
        }

        float3 ResolveSilhouette(int2 center)
        {
            int2 bestOwner = center;
            float4 centerMetadata = MetadataAt(center);
            bool centerVisible = centerMetadata.a > 0.5 && VisibleAt(center, center);
            bool objectBoundary = false;
            float bestDepth = 1e30;

            [unroll] for (int y = -1; y <= 1; y++)
            [unroll] for (int x = -1; x <= 1; x++)
            {
                if (x != 0 || y != 0)
                {
                    int2 neighbor = ClampCoord(center + int2(x, y));
                    float4 neighborMetadata = MetadataAt(neighbor);
                    bool neighborVisible = neighborMetadata.a > 0.5 && VisibleAt(neighbor, neighbor);

                    if (!centerVisible && neighborVisible && VisibleAt(neighbor, center))
                    {
                        float neighborDepth = SelectedEyeDepth(neighbor);
                        if (neighborDepth < bestDepth ||
                            (abs(neighborDepth - bestDepth) <= DepthTolerance(neighborDepth, bestDepth) &&
                             StableIdLess(neighborMetadata.rgb, MetadataAt(bestOwner).rgb)))
                        {
                            bestOwner = neighbor;
                            bestDepth = neighborDepth;
                        }
                    }
                    else if (centerVisible && neighborVisible && !SameObject(centerMetadata.rgb, neighborMetadata.rgb))
                    {
                        objectBoundary = true;
                    }
                }
            }

            return float3(objectBoundary || bestDepth < 1e29 ? 1.0 : 0.0, objectBoundary ? center : bestOwner);
        }

        float4 DebugOutput(int2 center, float outlineMask)
        {
            float4 metadata = MetadataAt(center);
            bool selected = metadata.a > 0.5;
            bool visible = selected && VisibleAt(center, center);
            if (_OutlineDebugMode == 1) return float4(selected ? 1.0.xxx : 0.0.xxx, 1);
            if (_OutlineDebugMode == 2) return float4(selected ? metadata.rgb : 0.0.xxx, 1);
            if (_OutlineDebugMode == 3) return float4((outlineMask > 0.5 ? 1.0 : 0.0).xxx, 1);
            if (_OutlineDebugMode == 4) return float4(selected ? (visible ? float3(0.15, 1, 0.25) : float3(1, 0.1, 0.1)) : 0.0.xxx, 1);
            return 0;
        }

        float4 CompositeDirect(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            float4 baseColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, 0);
            float3 silhouette = ResolveSilhouette(center);
            if (_OutlineDebugMode != 0)
                return DebugOutput(center, silhouette.x);
            if (silhouette.x > 0.5)
                baseColor.rgb = lerp(baseColor.rgb, LineColorAt(int2(silhouette.yz)), saturate(_OutlineSettings.x));
            return baseColor;
        }

        struct EdgeOutput
        {
            float4 edge : SV_Target0;
            float4 owner : SV_Target1;
        };

        EdgeOutput ResolveEdge(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            float3 silhouette = ResolveSilhouette(center);
            int2 ownerCoord = int2(silhouette.yz);
            EdgeOutput output;
            output.edge = silhouette.x > 0.5 ? float4(LineColorAt(ownerCoord), SelectedEyeDepth(ownerCoord)) : 0;
            output.owner = silhouette.x > 0.5 ? float4(MetadataAt(ownerCoord).rgb, 1) : 0;
            return output;
        }

        EdgeOutput HorizontalDilate(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            int radius = clamp((int)_OutlineSettings.z, 0, 7);
            float4 bestEdge = 0;
            float4 bestOwner = 0;
            int bestDistance = 100;

            [unroll] for (int offset = -7; offset <= 7; offset++)
            {
                int distance = abs(offset);
                if (distance <= radius)
                {
                    int2 candidateCoord = ClampCoord(center + int2(offset, 0));
                    float4 candidateOwner = LOAD_TEXTURE2D_X(_OutlineOwner, candidateCoord);
                    float4 candidateEdge = LOAD_TEXTURE2D_X(_BlitTexture, candidateCoord);
                    bool nearer = distance < bestDistance;
                    bool stableTie = distance == bestDistance &&
                        (candidateEdge.a < bestEdge.a ||
                         (abs(candidateEdge.a - bestEdge.a) <= DepthTolerance(candidateEdge.a, bestEdge.a) &&
                          StableIdLess(candidateOwner.rgb, bestOwner.rgb)));
                    if (candidateOwner.a > 0.5 && (nearer || stableTie))
                    {
                        bestEdge = candidateEdge;
                        bestOwner = candidateOwner;
                        bestDistance = distance;
                    }
                }
            }

            EdgeOutput output;
            output.edge = bestEdge;
            output.owner = bestOwner;
            return output;
        }

        float4 DilatedComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            int radius = clamp((int)_OutlineSettings.z, 0, 7);
            float4 bestEdge = 0;
            float4 bestOwner = 0;
            int bestDistance = 100;

            [unroll] for (int offset = -7; offset <= 7; offset++)
            {
                int distance = abs(offset);
                if (distance <= radius)
                {
                    int2 candidateCoord = ClampCoord(center + int2(0, offset));
                    float4 candidateOwner = LOAD_TEXTURE2D_X(_OutlineDilatedOwner, candidateCoord);
                    float4 candidateEdge = LOAD_TEXTURE2D_X(_OutlineDilated, candidateCoord);
                    bool nearer = distance < bestDistance;
                    bool stableTie = distance == bestDistance &&
                        (candidateEdge.a < bestEdge.a ||
                         (abs(candidateEdge.a - bestEdge.a) <= DepthTolerance(candidateEdge.a, bestEdge.a) &&
                          StableIdLess(candidateOwner.rgb, bestOwner.rgb)));
                    if (candidateOwner.a > 0.5 && (nearer || stableTie))
                    {
                        bestEdge = candidateEdge;
                        bestOwner = candidateOwner;
                        bestDistance = distance;
                    }
                }
            }

            float4 centerMetadata = MetadataAt(center);
            bool centerVisible = centerMetadata.a > 0.5 && VisibleAt(center, center);
            float3 direct = ResolveSilhouette(center);
            bool directBoundary = centerVisible && direct.x > 0.5;
            bool outline = false;
            float3 lineColor = bestEdge.rgb;

            if (centerVisible)
            {
                outline = directBoundary || (bestOwner.a > 0.5 && !SameObject(centerMetadata.rgb, bestOwner.rgb));
                lineColor = LineColorAt(center);
            }
            else if (bestOwner.a > 0.5)
            {
                float sceneDepth = CameraEyeDepth(center);
                outline = bestEdge.a <= sceneDepth + DepthTolerance(bestEdge.a, sceneDepth);
            }

            if (_OutlineDebugMode != 0)
                return DebugOutput(center, outline ? 1.0 : 0.0);

            float4 baseColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, 0);
            if (outline)
                baseColor.rgb = lerp(baseColor.rgb, lineColor, saturate(_OutlineSettings.x));
            return baseColor;
        }
        ENDHLSL

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
            Name "EdgeResolve"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment ResolveEdge
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

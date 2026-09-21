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
        TEXTURE2D_X(_OutlineNormals);
        TEXTURE2D_X(_OutlineTargetColors);
        TEXTURE2D_X_FLOAT(_OutlineSelectedDepth);
        TEXTURE2D_X_FLOAT(_OutlineCameraDepth);
        TEXTURE2D_X(_OutlineDilated);

        float4 _OutlineFixedColor;
        float4 _OutlineSettings;
        float4 _OutlineDetail;
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

        float3 NormalAt(int2 coord)
        {
            float3 normal = LOAD_TEXTURE2D_X(_OutlineNormals, ClampCoord(coord)).rgb * 2.0 - 1.0;
            float lengthSquared = dot(normal, normal);
            return lengthSquared > 1e-6 ? normal * rsqrt(lengthSquared) : float3(0, 0, 1);
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
            float tolerance = max(0.0005, min(selectedDepth, sceneDepth) * 0.0001);
            return selectedDepth <= sceneDepth + tolerance;
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

        float RelativeDepthDelta(float a, float b)
        {
            return abs(a - b) / max(min(a, b), 0.001);
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
            bool centerSelected = centerMetadata.a > 0.5;
            bool centerVisible = centerSelected && VisibleAt(center, center);
            float bestDepth = 1e30;

            [unroll] for (int y = -1; y <= 1; y++)
            [unroll] for (int x = -1; x <= 1; x++)
            {
                if (x != 0 || y != 0)
                {
                    int2 neighbor = ClampCoord(center + int2(x, y));
                    float4 neighborMetadata = MetadataAt(neighbor);
                    bool neighborVisible = neighborMetadata.a > 0.5 && VisibleAt(neighbor, neighbor);

                    if (!centerSelected && neighborVisible && VisibleAt(neighbor, center))
                    {
                        float neighborDepth = SelectedEyeDepth(neighbor);
                        if (neighborDepth < bestDepth ||
                            (abs(neighborDepth - bestDepth) <= max(neighborDepth * 0.0001, 0.0005) && StableIdLess(neighborMetadata.rgb, MetadataAt(bestOwner).rgb)))
                        {
                            bestOwner = neighbor;
                            bestDepth = neighborDepth;
                        }
                    }
                    else if (centerVisible && neighborVisible && !SameObject(centerMetadata.rgb, neighborMetadata.rgb))
                    {
                        float centerDepth = SelectedEyeDepth(center);
                        float neighborDepth = SelectedEyeDepth(neighbor);
                        float tolerance = max(min(centerDepth, neighborDepth) * 0.0001, 0.0005);
                        if (centerDepth + tolerance < neighborDepth ||
                            (abs(centerDepth - neighborDepth) <= tolerance && StableIdLess(centerMetadata.rgb, neighborMetadata.rgb)))
                            return float3(1.0, center);
                    }
                }
            }

            return float3(bestDepth < 1e29 ? 1.0 : 0.0, bestOwner);
        }

        float ResolveInternalDetail(int2 center)
        {
            if (_OutlineDetail.x < 0.5 || !VisibleAt(center, center))
                return 0.0;

            int2 left = ClampCoord(center + int2(-1, 0));
            int2 right = ClampCoord(center + int2(1, 0));
            int2 top = ClampCoord(center + int2(0, -1));
            int2 bottom = ClampCoord(center + int2(0, 1));
            float3 id = MetadataAt(center).rgb;
            if (!VisibleAt(left, left) || !VisibleAt(right, right) || !VisibleAt(top, top) || !VisibleAt(bottom, bottom) ||
                !SameObject(id, MetadataAt(left).rgb) || !SameObject(id, MetadataAt(right).rgb) ||
                !SameObject(id, MetadataAt(top).rgb) || !SameObject(id, MetadataAt(bottom).rgb))
                return 0.0;

            float3 normal = NormalAt(center);
            float normalLeft = 1.0 - saturate(dot(normal, NormalAt(left)));
            float normalRight = 1.0 - saturate(dot(normal, NormalAt(right)));
            float normalTop = 1.0 - saturate(dot(normal, NormalAt(top)));
            float normalBottom = 1.0 - saturate(dot(normal, NormalAt(bottom)));
            float normalContrast = max(abs(normalLeft - normalRight), abs(normalTop - normalBottom));
            float normalPeak = max(max(normalLeft, normalRight), max(normalTop, normalBottom));
            float normalSignal = smoothstep(_OutlineDetail.w * 0.35, _OutlineDetail.w, normalContrast) *
                smoothstep(_OutlineDetail.w * 0.3, _OutlineDetail.w * 1.15, normalPeak);

            float centerDepth = SelectedEyeDepth(center);
            float depthLeft = RelativeDepthDelta(centerDepth, SelectedEyeDepth(left));
            float depthRight = RelativeDepthDelta(centerDepth, SelectedEyeDepth(right));
            float depthTop = RelativeDepthDelta(centerDepth, SelectedEyeDepth(top));
            float depthBottom = RelativeDepthDelta(centerDepth, SelectedEyeDepth(bottom));
            float depthContrast = max(abs(depthLeft - depthRight), abs(depthTop - depthBottom));
            float depthSignal = smoothstep(_OutlineDetail.z, _OutlineDetail.z * 2.0, depthContrast);
            return saturate(max(normalSignal, depthSignal) * _OutlineDetail.y);
        }

        float4 DebugOutput(int2 center, float silhouette, float detail)
        {
            float4 metadata = MetadataAt(center);
            bool selected = metadata.a > 0.5;
            bool visible = selected && VisibleAt(center, center);
            if (_OutlineDebugMode == 1) return float4(visible ? 1.0.xxx : 0.0.xxx, 1);
            if (_OutlineDebugMode == 2) return float4(selected ? metadata.rgb : 0.0.xxx, 1);
            if (_OutlineDebugMode == 3) return float4(silhouette.xxx, 1);
            if (_OutlineDebugMode == 4) return float4(detail.xxx, 1);
            if (_OutlineDebugMode == 5) return float4(selected ? (visible ? float3(0.15, 1, 0.25) : float3(1, 0.1, 0.1)) : 0.0.xxx, 1);
            return 0;
        }

        float4 CompositeDirect(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            float4 baseColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, 0);
            float3 silhouette = ResolveSilhouette(center);
            int2 owner = int2(silhouette.yz);
            float detail = ResolveInternalDetail(center);
            if (_OutlineDebugMode != 0)
                return DebugOutput(center, silhouette.x, detail);
            if (silhouette.x > 0.0)
                baseColor.rgb = lerp(baseColor.rgb, LineColorAt(owner), saturate(_OutlineSettings.x * silhouette.x));
            else if (detail > 0.0)
                baseColor.rgb = lerp(baseColor.rgb, LineColorAt(center), saturate(_OutlineSettings.x * detail));
            return baseColor;
        }

        float4 ResolveEdge(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            float3 silhouette = ResolveSilhouette(center);
            int2 owner = int2(silhouette.yz);
            return float4(LineColorAt(owner), silhouette.x > 0.0 ? SelectedEyeDepth(owner) : 0.0);
        }

        float4 HorizontalDilate(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            int radius = clamp((int)_OutlineSettings.z, 0, 7);
            float4 best = 0;
            int bestDistance = 100;
            [unroll] for (int offset = -7; offset <= 7; offset++)
            {
                int distance = abs(offset);
                if (distance > radius)
                    continue;
                float4 candidate = LOAD_TEXTURE2D_X(_BlitTexture, ClampCoord(center + int2(offset, 0)));
                if (candidate.a > 0.0 && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        float4 DilatedComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 center = PixelCoord(input.texcoord);
            int radius = clamp((int)_OutlineSettings.z, 0, 7);
            float4 edge = 0;
            int bestDistance = 100;
            [unroll] for (int offset = -7; offset <= 7; offset++)
            {
                int distance = abs(offset);
                if (distance > radius)
                    continue;
                float4 candidate = LOAD_TEXTURE2D_X(_OutlineDilated, ClampCoord(center + int2(0, offset)));
                if (candidate.a > 0.0 && distance < bestDistance)
                {
                    edge = candidate;
                    bestDistance = distance;
                }
            }

            float sceneDepth = CameraEyeDepth(center);
            float tolerance = max(0.0005, min(edge.a, sceneDepth) * 0.0001);
            bool edgeVisible = edge.a > 0.0 && edge.a <= sceneDepth + tolerance;
            float detail = ResolveInternalDetail(center);
            if (_OutlineDebugMode != 0)
                return DebugOutput(center, edgeVisible ? 1.0 : 0.0, detail);

            float4 baseColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, 0);
            if (edgeVisible)
                baseColor.rgb = lerp(baseColor.rgb, edge.rgb, saturate(_OutlineSettings.x));
            else if (detail > 0.0)
                baseColor.rgb = lerp(baseColor.rgb, LineColorAt(center), saturate(_OutlineSettings.x * detail));
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

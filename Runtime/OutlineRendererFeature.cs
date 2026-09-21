using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Natteens.Outline
{
    public sealed class OutlineRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private OutlineProfile _profile;
        [SerializeField, HideInInspector] private Shader _metadataShader;
        [SerializeField, HideInInspector] private Shader _compositeShader;

        private Material _metadataMaterial;
        private Material _compositeMaterial;
        private OutlineRenderPass _pass;
        private bool _reportedMissingResources;

        public OutlineProfile Profile
        {
            get => _profile;
            set
            {
                _profile = value;
                if (_profile != null)
                    OutlineTarget.ConfigureRenderingLayerBit(_profile.TargetRenderingLayerBit);
            }
        }

        public bool ResourcesReady => _metadataMaterial != null && _compositeMaterial != null;

        public override void Create()
        {
            _metadataShader ??= Shader.Find("Hidden/Natteens/Outline/Metadata");
            _compositeShader ??= Shader.Find("Hidden/Natteens/Outline/Composite");
            RecreateMaterials();
            _pass ??= new OutlineRenderPass();
            _pass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            _pass.requiresIntermediateTexture = true;
            _pass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            if (_profile != null)
                OutlineTarget.ConfigureRenderingLayerBit(_profile.TargetRenderingLayerBit);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_profile == null || !_profile.Enabled)
                return;
            if (!ResourcesReady)
            {
                if (!_reportedMissingResources)
                {
                    Debug.LogError("Outline cannot render because its hidden shaders are missing. Run Tools > Outline > Setup.", this);
                    _reportedMissingResources = true;
                }
                return;
            }
            _reportedMissingResources = false;
            OutlineTarget.ConfigureRenderingLayerBit(_profile.TargetRenderingLayerBit);
            _pass.Setup(_profile, _metadataMaterial, _compositeMaterial);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_metadataMaterial);
            CoreUtils.Destroy(_compositeMaterial);
            _metadataMaterial = null;
            _compositeMaterial = null;
            base.Dispose(disposing);
        }

        internal void SetResources(Shader metadataShader, Shader compositeShader)
        {
            _metadataShader = metadataShader;
            _compositeShader = compositeShader;
            RecreateMaterials();
        }

        private void RecreateMaterials()
        {
            CoreUtils.Destroy(_metadataMaterial);
            CoreUtils.Destroy(_compositeMaterial);
            _metadataMaterial = _metadataShader != null ? CoreUtils.CreateEngineMaterial(_metadataShader) : null;
            _compositeMaterial = _compositeShader != null ? CoreUtils.CreateEngineMaterial(_compositeShader) : null;
        }

        private sealed class OutlineRenderPass : ScriptableRenderPass
        {
            private static readonly List<ShaderTagId> DepthShaderTags = new()
            {
                new ShaderTagId("DepthOnly"),
                new ShaderTagId("DepthNormalsOnly"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit")
            };

            private static readonly List<ShaderTagId> MetadataShaderTags = new()
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("UniversalGBuffer"),
                new ShaderTagId("SRPDefaultUnlit")
            };

            private static readonly int MetadataId = Shader.PropertyToID("_OutlineMetadata");
            private static readonly int NormalsId = Shader.PropertyToID("_OutlineNormals");
            private static readonly int TargetColorId = Shader.PropertyToID("_OutlineTargetColors");
            private static readonly int SelectedDepthId = Shader.PropertyToID("_OutlineSelectedDepth");
            private static readonly int CameraDepthId = Shader.PropertyToID("_OutlineCameraDepth");
            private static readonly int DilatedId = Shader.PropertyToID("_OutlineDilated");
            private static readonly int FixedColorId = Shader.PropertyToID("_OutlineFixedColor");
            private static readonly int SettingsId = Shader.PropertyToID("_OutlineSettings");
            private static readonly int DetailId = Shader.PropertyToID("_OutlineDetail");
            private static readonly int DebugModeId = Shader.PropertyToID("_OutlineDebugMode");

            private static readonly ProfilingSampler SelectedDepthSampler = new("Outline / Selected Depth");
            private static readonly ProfilingSampler MetadataSampler = new("Outline / Metadata");
            private static readonly ProfilingSampler EdgeSampler = new("Outline / Edge Resolve");
            private static readonly ProfilingSampler CompositeSampler = new("Outline / Composite");

            private OutlineProfile _profile;
            private Material _metadataMaterial;
            private Material _compositeMaterial;

            public void Setup(OutlineProfile profile, Material metadataMaterial, Material compositeMaterial)
            {
                _profile = profile;
                _metadataMaterial = metadataMaterial;
                _compositeMaterial = compositeMaterial;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_profile == null || !_profile.Enabled || !ShouldRender(frameData.Get<UniversalCameraData>()))
                    return;

                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (!resources.cameraColor.IsValid() || !resources.cameraDepthTexture.IsValid() || !resources.activeColorTexture.IsValid())
                    return;

                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                TextureDesc colorDesc = renderGraph.GetTextureDesc(resources.cameraColor);

                TextureHandle selectedDepth = CreateDepthTexture(renderGraph, colorDesc, "Outline.SelectedDepth");
                TextureHandle metadata = CreateColorTexture(renderGraph, colorDesc, GraphicsFormat.R8G8B8A8_UNorm, "Outline.Metadata");
                TextureHandle normals = CreateColorTexture(renderGraph, colorDesc, GraphicsFormat.R8G8B8A8_UNorm, "Outline.Normals");
                TextureHandle targetColors = CreateColorTexture(renderGraph, colorDesc, GraphicsFormat.R8G8B8A8_UNorm, "Outline.TargetColors");

                SelectionLists lists = CreateSelectionLists(renderGraph, renderingData, cameraData, lightData);
                if (!lists.HasAny)
                    return;

                AddSelectedDepthPass(renderGraph, lists, selectedDepth);
                AddMetadataPass(renderGraph, lists, selectedDepth, metadata, normals, targetColors);

                TextureDesc sourceDesc = colorDesc;
                sourceDesc.name = "Outline.SourceColor";
                sourceDesc.depthBufferBits = DepthBits.None;
                sourceDesc.msaaSamples = MSAASamples.None;
                sourceDesc.clearBuffer = false;
                TextureHandle sourceColor = renderGraph.CreateTexture(sourceDesc);
                renderGraph.AddBlitPass(resources.cameraColor, sourceColor, Vector2.one, Vector2.zero, passName: "Outline / Copy Color");

                if (_profile.Thickness <= 1 || _profile.DebugMode is OutlineDebugMode.Selection or OutlineDebugMode.ObjectIds or OutlineDebugMode.DepthVisibility)
                {
                    AddCompositePass(renderGraph, sourceColor, resources.activeColorTexture, metadata, normals, targetColors,
                        selectedDepth, resources.cameraDepthTexture, default, 0, CompositeSampler);
                    return;
                }

                TextureHandle edges = CreateColorTexture(renderGraph, colorDesc, GraphicsFormat.R16G16B16A16_SFloat, "Outline.Edges");
                AddCompositePass(renderGraph, sourceColor, edges, metadata, normals, targetColors,
                    selectedDepth, resources.cameraDepthTexture, default, 1, EdgeSampler);

                TextureHandle horizontal = CreateColorTexture(renderGraph, colorDesc, GraphicsFormat.R16G16B16A16_SFloat, "Outline.HorizontalDilation");
                AddCompositePass(renderGraph, edges, horizontal, metadata, normals, targetColors,
                    selectedDepth, resources.cameraDepthTexture, default, 2, EdgeSampler);

                AddCompositePass(renderGraph, sourceColor, resources.activeColorTexture, metadata, normals, targetColors,
                    selectedDepth, resources.cameraDepthTexture, horizontal, 3, CompositeSampler);
            }

            private bool ShouldRender(UniversalCameraData cameraData)
            {
                Camera camera = cameraData.camera;
                if (camera == null || camera.cameraType is CameraType.Preview or CameraType.Reflection)
                    return false;
                if (cameraData.renderType == CameraRenderType.Overlay && !_profile.RenderOverlayCameras)
                    return false;
                if (camera.cameraType == CameraType.SceneView)
                    return _profile.RenderSceneView;
                return _profile.RenderGameCameras;
            }

            private SelectionLists CreateSelectionLists(RenderGraph renderGraph, UniversalRenderingData renderingData,
                UniversalCameraData cameraData, UniversalLightData lightData)
            {
                bool useTargets = _profile.SelectionMode is OutlineSelectionMode.Targets or OutlineSelectionMode.TargetsAndLayers;
                bool useLayers = _profile.SelectionMode is OutlineSelectionMode.Layers or OutlineSelectionMode.TargetsAndLayers;
                var lists = new SelectionLists { HasTargets = useTargets, HasLayers = useLayers };
                if (useLayers)
                {
                    FilteringSettings filtering = new(RenderQueueRange.opaque, _profile.LayerMask.value);
                    lists.LayerDepth = CreateRendererList(renderGraph, renderingData, cameraData, lightData, DepthShaderTags, filtering, null);
                    lists.LayerMetadata = CreateRendererList(renderGraph, renderingData, cameraData, lightData, MetadataShaderTags, filtering, _metadataMaterial);
                }
                if (useTargets)
                {
                    FilteringSettings filtering = new(RenderQueueRange.opaque, -1) { renderingLayerMask = 1u << _profile.TargetRenderingLayerBit };
                    lists.TargetDepth = CreateRendererList(renderGraph, renderingData, cameraData, lightData, DepthShaderTags, filtering, null);
                    lists.TargetMetadata = CreateRendererList(renderGraph, renderingData, cameraData, lightData, MetadataShaderTags, filtering, _metadataMaterial);
                }
                return lists;
            }

            private static RendererListHandle CreateRendererList(RenderGraph renderGraph, UniversalRenderingData renderingData,
                UniversalCameraData cameraData, UniversalLightData lightData, List<ShaderTagId> tags,
                FilteringSettings filtering, Material overrideMaterial)
            {
                DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(tags, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
                if (overrideMaterial != null)
                {
                    drawing.overrideMaterial = overrideMaterial;
                    drawing.overrideMaterialPassIndex = 0;
                }
                return renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
            }

            private static void AddSelectedDepthPass(RenderGraph renderGraph, SelectionLists lists, TextureHandle selectedDepth)
            {
                using var builder = renderGraph.AddRasterRenderPass<SelectionPassData>("Outline / Selected Depth", out SelectionPassData data, SelectedDepthSampler);
                data.HasLayers = lists.HasLayers;
                data.HasTargets = lists.HasTargets;
                data.LayerList = lists.LayerDepth;
                data.TargetList = lists.TargetDepth;
                if (data.HasLayers)
                    builder.UseRendererList(data.LayerList);
                if (data.HasTargets)
                    builder.UseRendererList(data.TargetList);
                builder.SetRenderAttachmentDepth(selectedDepth, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (SelectionPassData passData, RasterGraphContext context) =>
                {
                    if (passData.HasLayers)
                        context.cmd.DrawRendererList(passData.LayerList);
                    if (passData.HasTargets)
                        context.cmd.DrawRendererList(passData.TargetList);
                });
            }

            private static void AddMetadataPass(RenderGraph renderGraph, SelectionLists lists, TextureHandle selectedDepth,
                TextureHandle metadata, TextureHandle normals, TextureHandle targetColors)
            {
                using var builder = renderGraph.AddRasterRenderPass<SelectionPassData>("Outline / Metadata", out SelectionPassData data, MetadataSampler);
                data.HasLayers = lists.HasLayers;
                data.HasTargets = lists.HasTargets;
                data.LayerList = lists.LayerMetadata;
                data.TargetList = lists.TargetMetadata;
                if (data.HasLayers)
                    builder.UseRendererList(data.LayerList);
                if (data.HasTargets)
                    builder.UseRendererList(data.TargetList);
                builder.SetRenderAttachment(metadata, 0, AccessFlags.Write);
                builder.SetRenderAttachment(normals, 1, AccessFlags.Write);
                builder.SetRenderAttachment(targetColors, 2, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(selectedDepth, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (SelectionPassData passData, RasterGraphContext context) =>
                {
                    if (passData.HasLayers)
                        context.cmd.DrawRendererList(passData.LayerList);
                    if (passData.HasTargets)
                        context.cmd.DrawRendererList(passData.TargetList);
                });
            }

            private void AddCompositePass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination,
                TextureHandle metadata, TextureHandle normals, TextureHandle targetColors, TextureHandle selectedDepth,
                TextureHandle cameraDepth, TextureHandle dilated, int shaderPass, ProfilingSampler sampler)
            {
                using var builder = renderGraph.AddRasterRenderPass<CompositePassData>(shaderPass switch
                {
                    1 => "Outline / Edge Resolve",
                    2 => "Outline / Horizontal Dilation",
                    3 => "Outline / Composite",
                    _ => "Outline / Composite"
                }, out CompositePassData data, sampler);

                data.Source = source;
                data.Destination = destination;
                data.Metadata = metadata;
                data.Normals = normals;
                data.TargetColors = targetColors;
                data.SelectedDepth = selectedDepth;
                data.CameraDepth = cameraDepth;
                data.Dilated = dilated;
                data.Material = _compositeMaterial;
                data.ShaderPass = shaderPass;
                data.FixedColor = _profile.Color;
                data.Settings = new Vector4(_profile.Opacity, _profile.AdaptiveDarken, _profile.Thickness - 1, _profile.ColorMode == OutlineColorMode.Fixed ? 1f : 0f);
                data.Detail = new Vector4(_profile.InternalDetail ? 1f : 0f, _profile.InternalDetailStrength, _profile.DepthThreshold, _profile.NormalThreshold);
                data.DebugMode = (int)_profile.DebugMode;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(metadata, AccessFlags.Read);
                builder.UseTexture(normals, AccessFlags.Read);
                builder.UseTexture(targetColors, AccessFlags.Read);
                builder.UseTexture(selectedDepth, AccessFlags.Read);
                builder.UseTexture(cameraDepth, AccessFlags.Read);
                if (dilated.IsValid())
                    builder.UseTexture(dilated, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (CompositePassData passData, RasterGraphContext context) =>
                {
                    passData.Material.SetTexture(MetadataId, passData.Metadata);
                    passData.Material.SetTexture(NormalsId, passData.Normals);
                    passData.Material.SetTexture(TargetColorId, passData.TargetColors);
                    passData.Material.SetTexture(SelectedDepthId, passData.SelectedDepth);
                    passData.Material.SetTexture(CameraDepthId, passData.CameraDepth);
                    if (passData.Dilated.IsValid())
                        passData.Material.SetTexture(DilatedId, passData.Dilated);
                    passData.Material.SetColor(FixedColorId, passData.FixedColor);
                    passData.Material.SetVector(SettingsId, passData.Settings);
                    passData.Material.SetVector(DetailId, passData.Detail);
                    passData.Material.SetInt(DebugModeId, passData.DebugMode);
                    Blitter.BlitTexture(context.cmd, passData.Source, new Vector4(1f, 1f, 0f, 0f), passData.Material, passData.ShaderPass);
                });
            }

            private static TextureHandle CreateColorTexture(RenderGraph renderGraph, TextureDesc source, GraphicsFormat format, string name)
            {
                TextureDesc descriptor = source;
                descriptor.name = name;
                descriptor.colorFormat = format;
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                descriptor.clearBuffer = true;
                descriptor.clearColor = Color.clear;
                descriptor.filterMode = FilterMode.Point;
                descriptor.wrapMode = TextureWrapMode.Clamp;
                return renderGraph.CreateTexture(descriptor);
            }

            private static TextureHandle CreateDepthTexture(RenderGraph renderGraph, TextureDesc source, string name)
            {
                TextureDesc descriptor = source;
                descriptor.name = name;
                descriptor.colorFormat = GraphicsFormat.None;
                descriptor.depthBufferBits = DepthBits.Depth32;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                descriptor.clearBuffer = true;
                descriptor.filterMode = FilterMode.Point;
                descriptor.wrapMode = TextureWrapMode.Clamp;
                return renderGraph.CreateTexture(descriptor);
            }

            private struct SelectionLists
            {
                public bool HasLayers;
                public bool HasTargets;
                public RendererListHandle LayerDepth;
                public RendererListHandle TargetDepth;
                public RendererListHandle LayerMetadata;
                public RendererListHandle TargetMetadata;
                public bool HasAny => HasLayers || HasTargets;
            }

            private sealed class SelectionPassData
            {
                public bool HasLayers;
                public bool HasTargets;
                public RendererListHandle LayerList;
                public RendererListHandle TargetList;
            }

            private sealed class CompositePassData
            {
                public TextureHandle Source;
                public TextureHandle Destination;
                public TextureHandle Metadata;
                public TextureHandle Normals;
                public TextureHandle TargetColors;
                public TextureHandle SelectedDepth;
                public TextureHandle CameraDepth;
                public TextureHandle Dilated;
                public Material Material;
                public int ShaderPass;
                public Color FixedColor;
                public Vector4 Settings;
                public Vector4 Detail;
                public int DebugMode;
            }
        }
    }
}

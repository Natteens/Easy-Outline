using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Natteens.Outline.Editor
{
    internal static class OutlineEditorUi
    {
        public static VisualElement Clone(string assetName)
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"Packages/com.natteens.easyoutline/Editor/UI/{assetName}.uxml");
            return tree != null ? tree.CloneTree() : new HelpBox($"Missing editor layout: {assetName}", HelpBoxMessageType.Error);
        }

        public static void SetVisible(VisualElement element, bool visible)
        {
            element?.EnableInClassList("hidden", !visible);
        }

        public static string ProfileStatus(OutlineProfile profile)
        {
            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset
                ?? GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
                return "URP not active";

            bool featureFound = false;
            bool profileAssigned = false;
            foreach (ScriptableRendererData data in pipeline.rendererDataList)
            {
                if (data == null)
                    continue;
                foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                {
                    if (feature is not OutlineRendererFeature outline)
                        continue;
                    featureFound = true;
                    if (outline.Profile == profile)
                    {
                        profileAssigned = true;
                        if (outline.ResourcesReady)
                            return HasRenderingLayerConflict(profile) ? "Rendering layer conflict" : "Ready";
                    }
                }
            }
            return profileAssigned ? "Resources missing" : featureFound ? "Profile not assigned" : "Feature missing";
        }

        internal static bool HasRenderingLayerConflict(OutlineProfile profile)
        {
            if (profile.SelectionMode == OutlineSelectionMode.Layers)
                return false;

            uint bit = 1u << profile.TargetRenderingLayerBit;
            var owned = new HashSet<Renderer>();
            foreach (OutlineTarget target in Object.FindObjectsByType<OutlineTarget>(FindObjectsInactive.Include))
            {
                if (!target.isActiveAndEnabled || !target.Outlined)
                    continue;
                if (target.Renderers != null)
                    foreach (Renderer renderer in target.Renderers)
                        if (renderer != null)
                            owned.Add(renderer);
            }
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                if ((renderer.renderingLayerMask & bit) != 0 && !owned.Contains(renderer))
                    return true;
            return false;
        }

        internal static bool RepairRenderingLayer(OutlineProfile profile)
        {
            uint used = 0;
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                used |= renderer.renderingLayerMask;
            for (var bit = 30; bit > 0; bit--)
            {
                if ((used & (1u << bit)) != 0)
                    continue;
                profile.TargetRenderingLayerBit = bit;
                return true;
            }
            return false;
        }
    }
}

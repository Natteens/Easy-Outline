using UnityEditor;
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

        public static void SetStatus(Label label, string text, bool ready)
        {
            if (label == null)
                return;
            label.text = text;
            label.EnableInClassList("easyoutline-status-ready", ready);
            label.EnableInClassList("easyoutline-status-attention", !ready);
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
                            return "Ready";
                    }
                }
            }
            return profileAssigned ? "Resources missing" : featureFound ? "Profile not assigned" : "Feature missing";
        }
    }
}

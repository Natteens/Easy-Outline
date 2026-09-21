using UnityEditor;
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
    }
}

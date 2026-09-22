using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Natteens.Outline.Tests
{
    public sealed class OutlineInspectorTests
    {
        [TestCase("OutlineProfileInspector", 13)]
        [TestCase("OutlineTargetInspector", 6)]
        [TestCase("OutlineRendererFeatureInspector", 1)]
        public void UxmlResolvesEditorPropertyFields(string assetName, int expectedCount)
        {
            string path = $"Packages/com.natteens.easyoutline/Editor/UI/{assetName}.uxml";
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.That(asset, Is.Not.Null);
            VisualElement root = asset.CloneTree();
            Assert.That(root.Query<PropertyField>().ToList().Count, Is.EqualTo(expectedCount));
        }
    }
}

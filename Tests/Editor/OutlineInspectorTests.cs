using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Natteens.Outline.Tests
{
    public sealed class OutlineInspectorTests
    {
        [TestCase("OutlineProfileInspector")]
        [TestCase("OutlineTargetInspector")]
        [TestCase("OutlineRendererFeatureInspector")]
        public void UxmlResolvesCustomHeaderAndEditorControls(string assetName)
        {
            string path = $"Packages/com.natteens.easyoutline/Editor/UI/{assetName}.uxml";
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.That(asset, Is.Not.Null);
            VisualElement root = asset.CloneTree();
            Assert.That(root.Q<Label>("statusLabel"), Is.Not.Null);
            Assert.That(root.Query<PropertyField>().ToList().Count, Is.GreaterThan(0));
        }

        [Test]
        public void ProfileUsesBoundSlidersAndColorField()
        {
            VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.natteens.easyoutline/Editor/UI/OutlineProfileInspector.uxml").CloneTree();
            Assert.That(root.Q<SliderInt>("thicknessField")?.bindingPath, Is.EqualTo("_thickness"));
            Assert.That(root.Q<Slider>("opacityField")?.bindingPath, Is.EqualTo("_opacity"));
            Assert.That(root.Q<ColorField>("fixedColorField")?.bindingPath, Is.EqualTo("_color"));
        }

        [Test]
        public void TargetKeepsRendererArrayInManualFoldout()
        {
            VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.natteens.easyoutline/Editor/UI/OutlineTargetInspector.uxml").CloneTree();
            Assert.That(root.Q<Foldout>("manualRendererFoldout"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("rendererList"), Is.Not.Null);
            Assert.That(root.Q<Button>("refreshButton"), Is.Not.Null);
        }
    }
}

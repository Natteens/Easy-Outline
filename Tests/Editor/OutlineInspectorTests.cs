using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Natteens.Outline.Tests
{
    public sealed class OutlineInspectorTests
    {
        [TestCase("OutlineProfileInspector")]
        [TestCase("OutlineTargetInspector")]
        [TestCase("OutlineRendererFeatureInspector")]
        public void UxmlLoadsWithoutBrandedHeader(string assetName)
        {
            string path = $"Packages/com.natteens.easyoutline/Editor/UI/{assetName}.uxml";
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.That(asset, Is.Not.Null);
            VisualElement root = asset.CloneTree();
            Assert.That(root.Q<Label>("statusLabel"), Is.Null);
            Assert.That(root.Query<Label>().ToList().Exists(label => label.text?.StartsWith("EASY OUTLINE") == true), Is.False);
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
            Assert.That(root.Q<Toggle>("occlusionEdgesField")?.bindingPath, Is.EqualTo("_outlineOcclusionEdges"));
            Assert.That(root.Q<PropertyField>("layerMaskField")?.bindingPath, Is.EqualTo("_layerMask"));
            Assert.That(root.Query<PropertyField>().ToList().Exists(field => field.bindingPath == "_targetRenderingLayerBit"), Is.False);
        }

        [Test]
        public void SelectionLayersUseUnityLayerMask()
        {
            FieldInfo field = typeof(OutlineProfile).GetField("_layerMask", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field?.FieldType, Is.EqualTo(typeof(LayerMask)));
        }

        [Test]
        public void InternalRenderingLayerConflictIsDetectedAndRepairable()
        {
            var profile = ScriptableObject.CreateInstance<OutlineProfile>();
            GameObject unrelated = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unrelated.GetComponent<Renderer>().renderingLayerMask = 1u << profile.TargetRenderingLayerBit;

            Assert.That(Natteens.Outline.Editor.OutlineEditorUi.HasRenderingLayerConflict(profile), Is.True);
            Assert.That(Natteens.Outline.Editor.OutlineEditorUi.RepairRenderingLayer(profile), Is.True);
            Assert.That(Natteens.Outline.Editor.OutlineEditorUi.HasRenderingLayerConflict(profile), Is.False);

            profile.TargetRenderingLayerBit = 31;
            Object.DestroyImmediate(unrelated);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void TargetKeepsRendererArrayInManualFoldout()
        {
            VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.natteens.easyoutline/Editor/UI/OutlineTargetInspector.uxml").CloneTree();
            Assert.That(root.Q<Foldout>("manualRendererFoldout"), Is.Not.Null);
            Assert.That(root.Q<Foldout>("resolvedRendererFoldout"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("rendererList"), Is.Not.Null);
            Assert.That(root.Q<Button>("refreshButton"), Is.Not.Null);
        }
    }
}

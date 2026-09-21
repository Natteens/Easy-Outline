using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Natteens.Outline.Tests
{
    public sealed class OutlineSetupTests
    {
        private const string Folder = "Assets/OutlineSetupTestsTemp";
        private const string RendererPath = Folder + "/Renderer.asset";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "OutlineSetupTestsTemp");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void SetupInstallationIsIdempotent()
        {
            UniversalRendererData renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
            OutlineProfile profile = ScriptableObject.CreateInstance<OutlineProfile>();
            Assert.That(Natteens.Outline.Editor.OutlineSetup.InstallOnRendererData(renderer, profile), Is.True);
            Assert.That(Natteens.Outline.Editor.OutlineSetup.InstallOnRendererData(renderer, profile), Is.True);
            Assert.That(renderer.rendererFeatures.OfType<OutlineRendererFeature>().Count(), Is.EqualTo(1));
            Assert.That(renderer.rendererFeatures.OfType<OutlineRendererFeature>().Single().Profile, Is.SameAs(profile));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void PackageAssetsAreNeverWritableSetupTargets()
        {
            Assert.That(Natteens.Outline.Editor.OutlineSetup.IsWritableProjectAssetPath("Assets/Settings/Renderer.asset"), Is.True);
            Assert.That(Natteens.Outline.Editor.OutlineSetup.IsWritableProjectAssetPath("Packages/com.unity.render-pipelines.universal/Renderer.asset"), Is.False);
        }

        [Test]
        public void SetupRemovesDuplicateOutlineFeaturesWithoutTouchingOtherFeatureTypes()
        {
            UniversalRendererData renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
            var first = ScriptableObject.CreateInstance<OutlineRendererFeature>();
            var second = ScriptableObject.CreateInstance<OutlineRendererFeature>();
            AssetDatabase.AddObjectToAsset(first, renderer);
            AssetDatabase.AddObjectToAsset(second, renderer);
            renderer.rendererFeatures.Add(first);
            renderer.rendererFeatures.Add(second);
            OutlineProfile profile = ScriptableObject.CreateInstance<OutlineProfile>();

            Assert.That(Natteens.Outline.Editor.OutlineSetup.InstallOnRendererData(renderer, profile), Is.True);
            Assert.That(renderer.rendererFeatures.OfType<OutlineRendererFeature>().Count(), Is.EqualTo(1));
            Assert.That(renderer.rendererFeatures.OfType<OutlineRendererFeature>().Single(), Is.SameAs(first));
            Object.DestroyImmediate(profile);
        }
    }
}

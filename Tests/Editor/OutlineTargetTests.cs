using NUnit.Framework;
using UnityEngine;

namespace Natteens.Outline.Tests
{
    public sealed class OutlineTargetTests
    {
        [SetUp]
        public void SetUp()
        {
            var profile = ScriptableObject.CreateInstance<OutlineProfile>();
            profile.TargetRenderingLayerBit = 31;
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void TargetPreservesUnrelatedRenderingLayerBitsAndRestoresItsOwnBit()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.renderingLayerMask = 1u << 4;
            OutlineTarget target = gameObject.AddComponent<OutlineTarget>();
            Assert.That(renderer.renderingLayerMask & (1u << 4), Is.Not.Zero);
            Assert.That(renderer.renderingLayerMask & (1u << 31), Is.Not.Zero);
            target.enabled = false;
            Assert.That(renderer.renderingLayerMask & (1u << 4), Is.Not.Zero);
            Assert.That(renderer.renderingLayerMask & (1u << 31), Is.Zero);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TargetDoesNotClearPreexistingPackageBit()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.renderingLayerMask = 1u << 31;
            OutlineTarget target = gameObject.AddComponent<OutlineTarget>();
            target.enabled = false;
            Assert.That(renderer.renderingLayerMask & (1u << 31), Is.Not.Zero);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void HierarchyRenderersReceiveOneSharedGroupId()
        {
            var root = new GameObject("Root");
            GameObject first = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject second = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            first.transform.SetParent(root.transform);
            second.transform.SetParent(root.transform);
            OutlineTarget target = root.AddComponent<OutlineTarget>();
            target.RefreshRenderers();
            var block = new MaterialPropertyBlock();
            first.GetComponent<Renderer>().GetPropertyBlock(block);
            Color firstId = block.GetColor(OutlineTarget.ObjectId);
            second.GetComponent<Renderer>().GetPropertyBlock(block);
            Color secondId = block.GetColor(OutlineTarget.ObjectId);
            Assert.That(firstId, Is.EqualTo(secondId));
            Assert.That(firstId.a, Is.EqualTo(1f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RuntimeToggleRestoresAndReappliesOnlyThePackageBit()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.renderingLayerMask = 1u << 7;
            OutlineTarget target = gameObject.AddComponent<OutlineTarget>();

            target.Outlined = false;
            Assert.That(renderer.renderingLayerMask, Is.EqualTo(1u << 7));
            target.Outlined = true;
            Assert.That(renderer.renderingLayerMask, Is.EqualTo((1u << 7) | (1u << 31)));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void ChangingReservedBitMigratesActiveTargetsWithoutTouchingOtherBits()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.renderingLayerMask = 1u << 5;
            gameObject.AddComponent<OutlineTarget>();

            var profile = ScriptableObject.CreateInstance<OutlineProfile>();
            profile.TargetRenderingLayerBit = 29;

            Assert.That(renderer.renderingLayerMask & (1u << 31), Is.Zero);
            Assert.That(renderer.renderingLayerMask & (1u << 29), Is.Not.Zero);
            Assert.That(renderer.renderingLayerMask & (1u << 5), Is.Not.Zero);

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void MetadataPreservesUnrelatedMaterialPropertyBlockValues()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            int externalId = Shader.PropertyToID("_ExternalValue");
            var block = new MaterialPropertyBlock();
            block.SetFloat(externalId, 42f);
            renderer.SetPropertyBlock(block);

            gameObject.AddComponent<OutlineTarget>();
            renderer.GetPropertyBlock(block);

            Assert.That(block.GetFloat(externalId), Is.EqualTo(42f));
            Assert.That(block.GetColor(OutlineTarget.ObjectId).a, Is.EqualTo(1f));
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void ProfileDefaultsAreImmediatelyUsable()
        {
            var profile = ScriptableObject.CreateInstance<OutlineProfile>();
            Assert.That(profile.Enabled, Is.True);
            Assert.That(profile.Thickness, Is.EqualTo(2));
            Assert.That(profile.Color, Is.EqualTo(Color.black));
            Assert.That(profile.Opacity, Is.EqualTo(1f));
            Assert.That(profile.RenderGameCameras, Is.True);
            Assert.That(profile.RenderSceneView, Is.True);
            Object.DestroyImmediate(profile);
        }
    }
}

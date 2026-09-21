using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Natteens.Outline.Tests
{
    public sealed class OutlineTargetPlayModeTests
    {
        [UnityTest]
        public IEnumerator ComponentLifecycleRestoresTheReservedRenderingLayerBit()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.renderingLayerMask = 1u << 6;
            OutlineTarget target = gameObject.AddComponent<OutlineTarget>();

            yield return null;
            Assert.That(renderer.renderingLayerMask & (1u << 31), Is.Not.Zero);
            Assert.That(renderer.renderingLayerMask & (1u << 6), Is.Not.Zero);

            target.enabled = false;
            yield return null;
            Assert.That(renderer.renderingLayerMask, Is.EqualTo(1u << 6));
            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator RuntimeApiCanToggleAndRecolorAnActiveTarget()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            OutlineTarget target = gameObject.AddComponent<OutlineTarget>();
            target.ColorOverride = true;
            target.Color = Color.cyan;

            yield return null;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetFloat(OutlineTarget.UseColorId), Is.EqualTo(1f));
            Assert.That(block.GetColor(OutlineTarget.ColorId), Is.EqualTo(Color.cyan));

            target.Outlined = false;
            Assert.That(renderer.renderingLayerMask & (1u << 31), Is.Zero);
            Object.Destroy(gameObject);
        }
    }
}

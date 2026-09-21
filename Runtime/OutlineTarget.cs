using System.Collections.Generic;
using UnityEngine;

namespace Natteens.Outline
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class OutlineTarget : MonoBehaviour
    {
        internal static readonly int ColorId = Shader.PropertyToID("_OutlineTargetColor");
        internal static readonly int UseColorId = Shader.PropertyToID("_OutlineUseTargetColor");
        internal static readonly int ObjectId = Shader.PropertyToID("_OutlineObjectId");

        private static readonly List<OutlineTarget> ActiveTargets = new();
        private static int _renderingLayerBit = 31;

        [SerializeField] private bool _outlined = true;
        [SerializeField] private bool _autoCollect = true;
        [SerializeField] private bool _includeChildren = true;
        [SerializeField] private List<Renderer> _renderers = new();
        [SerializeField] private bool _colorOverride;
        [SerializeField] private Color _color = Color.yellow;

        private readonly Dictionary<Renderer, bool> _introducedBit = new();
        private MaterialPropertyBlock _propertyBlock;

        public bool Outlined
        {
            get => _outlined;
            set
            {
                if (_outlined == value)
                    return;
                _outlined = value;
                Apply();
            }
        }

        public bool ColorOverride
        {
            get => _colorOverride;
            set
            {
                if (_colorOverride == value)
                    return;
                _colorOverride = value;
                ApplyMetadata();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;
                _color = value;
                ApplyMetadata();
            }
        }

        public IReadOnlyList<Renderer> Renderers => _renderers;
        public int ResolvedRendererCount => _renderers?.Count ?? 0;

        private void Reset()
        {
            _autoCollect = true;
            _includeChildren = true;
            RefreshRenderers();
        }

        private void OnEnable()
        {
            if (!ActiveTargets.Contains(this))
                ActiveTargets.Add(this);
            if (_autoCollect && (_renderers == null || _renderers.Count == 0))
                CollectRenderers();
            Apply();
        }

        private void OnValidate()
        {
            if (_autoCollect)
                CollectRenderers();
            if (isActiveAndEnabled)
                Apply();
        }

        private void OnDisable()
        {
            ActiveTargets.Remove(this);
            RestoreRenderingLayers();
            ClearMetadata();
        }

        public void RefreshRenderers()
        {
            RestoreRenderingLayers();
            ClearMetadata();
            CollectRenderers();
            if (isActiveAndEnabled)
                Apply();
        }

        internal static void ConfigureRenderingLayerBit(int bit)
        {
            bit = Mathf.Clamp(bit, 0, 31);
            if (_renderingLayerBit == bit)
                return;
            for (var i = ActiveTargets.Count - 1; i >= 0; i--)
            {
                OutlineTarget target = ActiveTargets[i];
                if (target == null)
                {
                    ActiveTargets.RemoveAt(i);
                    continue;
                }
                target.RestoreRenderingLayers();
            }
            _renderingLayerBit = bit;
            for (var i = ActiveTargets.Count - 1; i >= 0; i--)
            {
                OutlineTarget target = ActiveTargets[i];
                if (target == null)
                    continue;
                target.ApplyRenderingLayers();
            }
        }

        private void Apply()
        {
            RestoreRenderingLayers();
            if (_outlined && isActiveAndEnabled)
                ApplyRenderingLayers();
            ApplyMetadata();
        }

        private void ApplyRenderingLayers()
        {
            if (!_outlined || _renderers == null)
                return;
            uint bit = 1u << _renderingLayerBit;
            for (var i = 0; i < _renderers.Count; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null)
                    continue;
                bool introduced = (renderer.renderingLayerMask & bit) == 0;
                _introducedBit[renderer] = introduced;
                if (introduced)
                    renderer.renderingLayerMask |= bit;
            }
        }

        private void RestoreRenderingLayers()
        {
            if (_introducedBit.Count == 0)
                return;
            uint bit = 1u << _renderingLayerBit;
            foreach (KeyValuePair<Renderer, bool> state in _introducedBit)
                if (state.Key != null && state.Value)
                    state.Key.renderingLayerMask &= ~bit;
            _introducedBit.Clear();
        }

        private void ApplyMetadata()
        {
            if (_renderers == null)
                return;
            _propertyBlock ??= new MaterialPropertyBlock();
            Color objectId = EncodeObjectId(unchecked((uint)EntityId.ToULong(GetEntityId())));
            for (var i = 0; i < _renderers.Count; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(ColorId, _color);
                _propertyBlock.SetFloat(UseColorId, _colorOverride ? 1f : 0f);
                _propertyBlock.SetColor(ObjectId, objectId);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ClearMetadata()
        {
            if (_renderers == null)
                return;
            _propertyBlock ??= new MaterialPropertyBlock();
            for (var i = 0; i < _renderers.Count; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(UseColorId, 0f);
                _propertyBlock.SetColor(ObjectId, Color.clear);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void CollectRenderers()
        {
            _renderers ??= new List<Renderer>();
            _renderers.Clear();
            if (_includeChildren)
                GetComponentsInChildren(true, _renderers);
            else if (TryGetComponent(out Renderer renderer))
                _renderers.Add(renderer);
        }

        private static Color EncodeObjectId(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            value &= 0x00FFFFFFu;
            if (value == 0)
                value = 1;
            return new Color((value & 0xFFu) / 255f, ((value >> 8) & 0xFFu) / 255f, ((value >> 16) & 0xFFu) / 255f, 1f);
        }
    }
}

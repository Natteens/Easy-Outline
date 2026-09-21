using UnityEngine;

namespace Natteens.Outline
{
    [CreateAssetMenu(menuName = "Outline/Profile", fileName = "OutlineProfile")]
    public sealed class OutlineProfile : ScriptableObject
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private OutlineSelectionMode _selectionMode = OutlineSelectionMode.Targets;
        [SerializeField, Range(0, 31)] private int _targetRenderingLayerBit = 31;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField, Range(1, 8)] private int _thickness = 2;
        [SerializeField] private OutlineColorMode _colorMode = OutlineColorMode.Fixed;
        [SerializeField] private Color _color = new(0.02f, 0.02f, 0.025f, 1f);
        [SerializeField, Range(0f, 1f)] private float _opacity = 1f;
        [SerializeField, Range(0f, 1f)] private float _adaptiveDarken = 0.55f;
        [SerializeField] private bool _internalDetail;
        [SerializeField, Range(0f, 1f)] private float _internalDetailStrength = 0.35f;
        [SerializeField, Range(0.0005f, 0.1f)] private float _depthThreshold = 0.012f;
        [SerializeField, Range(0.01f, 0.75f)] private float _normalThreshold = 0.16f;
        [SerializeField] private bool _renderGameCameras = true;
        [SerializeField] private bool _renderSceneView = true;
        [SerializeField] private bool _renderOverlayCameras;
        [SerializeField] private OutlineDebugMode _debugMode;

        public bool Enabled { get => _enabled; set => _enabled = value; }
        public OutlineSelectionMode SelectionMode { get => _selectionMode; set => _selectionMode = value; }
        public int TargetRenderingLayerBit
        {
            get => _targetRenderingLayerBit;
            set
            {
                _targetRenderingLayerBit = Mathf.Clamp(value, 0, 31);
                OutlineTarget.ConfigureRenderingLayerBit(_targetRenderingLayerBit);
            }
        }
        public LayerMask LayerMask { get => _layerMask; set => _layerMask = value; }
        public int Thickness { get => _thickness; set => _thickness = Mathf.Clamp(value, 1, 8); }
        public OutlineColorMode ColorMode { get => _colorMode; set => _colorMode = value; }
        public Color Color { get => _color; set => _color = value; }
        public float Opacity { get => _opacity; set => _opacity = Mathf.Clamp01(value); }
        public float AdaptiveDarken { get => _adaptiveDarken; set => _adaptiveDarken = Mathf.Clamp01(value); }
        public bool InternalDetail { get => _internalDetail; set => _internalDetail = value; }
        public float InternalDetailStrength { get => _internalDetailStrength; set => _internalDetailStrength = Mathf.Clamp01(value); }
        public float DepthThreshold { get => _depthThreshold; set => _depthThreshold = Mathf.Max(0.0005f, value); }
        public float NormalThreshold { get => _normalThreshold; set => _normalThreshold = Mathf.Clamp(value, 0.01f, 0.75f); }
        public bool RenderGameCameras { get => _renderGameCameras; set => _renderGameCameras = value; }
        public bool RenderSceneView { get => _renderSceneView; set => _renderSceneView = value; }
        public bool RenderOverlayCameras { get => _renderOverlayCameras; set => _renderOverlayCameras = value; }
        public OutlineDebugMode DebugMode { get => _debugMode; set => _debugMode = value; }

        private void OnEnable() => OutlineTarget.ConfigureRenderingLayerBit(_targetRenderingLayerBit);

        private void OnValidate()
        {
            _targetRenderingLayerBit = Mathf.Clamp(_targetRenderingLayerBit, 0, 31);
            _thickness = Mathf.Clamp(_thickness, 1, 8);
            _opacity = Mathf.Clamp01(_opacity);
            _adaptiveDarken = Mathf.Clamp01(_adaptiveDarken);
            _internalDetailStrength = Mathf.Clamp01(_internalDetailStrength);
            _depthThreshold = Mathf.Max(0.0005f, _depthThreshold);
            _normalThreshold = Mathf.Clamp(_normalThreshold, 0.01f, 0.75f);
            OutlineTarget.ConfigureRenderingLayerBit(_targetRenderingLayerBit);
        }
    }
}

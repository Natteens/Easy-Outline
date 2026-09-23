using UnityEngine;

namespace Natteens.Outline
{
    [CreateAssetMenu(menuName = "Outline/Profile", fileName = "OutlineProfile")]
    public sealed class OutlineProfile : ScriptableObject
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private OutlineSelectionMode _selectionMode = OutlineSelectionMode.Targets;
        [SerializeField, HideInInspector, Range(0, 31)] private int _targetRenderingLayerBit = 31;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField, Range(1, 8)] private int _thickness = 2;
        [SerializeField] private OutlineColorMode _colorMode = OutlineColorMode.Fixed;
        [SerializeField] private Color _color = Color.black;
        [SerializeField, Range(0f, 1f)] private float _opacity = 1f;
        [SerializeField, Range(0f, 1f)] private float _adaptiveDarken = 0.55f;
        [SerializeField] private bool _outlineOcclusionEdges;
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
        public bool OutlineOcclusionEdges { get => _outlineOcclusionEdges; set => _outlineOcclusionEdges = value; }
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
            OutlineTarget.ConfigureRenderingLayerBit(_targetRenderingLayerBit);
        }
    }
}

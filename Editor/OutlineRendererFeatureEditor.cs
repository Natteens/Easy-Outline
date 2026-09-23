using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Natteens.Outline.Editor
{
    [CustomEditor(typeof(OutlineRendererFeature))]
    public sealed class OutlineRendererFeatureEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = OutlineEditorUi.Clone("OutlineRendererFeatureInspector");
            HelpBox status = root.Q<HelpBox>("statusBox");
            Button setup = root.Q<Button>("setupButton");

            void RefreshStatus()
            {
                OutlineRendererFeature feature = (OutlineRendererFeature)target;
                status.text = feature.Profile == null ? "Assign an Outline Profile or run Setup."
                    : !feature.ResourcesReady ? "Hidden shader resources are missing. Repair Setup can reconnect them."
                    : string.Empty;
                OutlineEditorUi.SetVisible(status, !string.IsNullOrEmpty(status.text));
            }

            setup.clicked += () =>
            {
                OutlineSetup.Setup();
                serializedObject.Update();
                RefreshStatus();
            };
            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshStatus());
            RefreshStatus();
            return root;
        }
    }
}

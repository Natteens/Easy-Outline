using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Natteens.Outline.Editor
{
    [CustomEditor(typeof(OutlineRendererFeature))]
    public sealed class OutlineRendererFeatureEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = OutlineEditorUi.Clone("OutlineRendererFeatureInspector");
            Label statusLabel = root.Q<Label>("statusLabel");
            Label metadataResource = root.Q<Label>("metadataResource");
            Label compositeResource = root.Q<Label>("compositeResource");
            HelpBox status = root.Q<HelpBox>("statusBox");
            Button setup = root.Q<Button>("setupButton");

            void RefreshStatus()
            {
                bool metadataReady = serializedObject.FindProperty("_metadataShader").objectReferenceValue is Shader;
                bool compositeReady = serializedObject.FindProperty("_compositeShader").objectReferenceValue is Shader;
                bool profileReady = serializedObject.FindProperty("_profile").objectReferenceValue is OutlineProfile;
                bool ready = metadataReady && compositeReady && profileReady;

                metadataResource.text = metadataReady ? "Metadata shader: Ready" : "Metadata shader: Missing";
                compositeResource.text = compositeReady ? "Outline shader: Ready" : "Outline shader: Missing";
                OutlineEditorUi.SetStatus(statusLabel, ready ? "Ready" : "Setup Required", ready);
                status.text = !profileReady ? "Assign an Outline Profile or run Setup."
                    : !metadataReady || !compositeReady ? "Hidden shader resources are missing. Repair Setup can reconnect them."
                    : "Installed on this URP Renderer Data.";
                status.messageType = ready ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
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

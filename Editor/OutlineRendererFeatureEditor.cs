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
                var feature = (OutlineRendererFeature)target;
                if (status == null)
                    return;
                if (feature.Profile == null)
                {
                    status.text = "No Outline Profile is assigned. Run Setup to create and connect one.";
                    status.messageType = HelpBoxMessageType.Warning;
                }
                else if (!feature.ResourcesReady)
                {
                    status.text = "Hidden shader resources are missing. Run Setup to repair them.";
                    status.messageType = HelpBoxMessageType.Error;
                }
                else
                {
                    status.text = "Ready. Outlines render after opaques so later transparent content can cover them.";
                    status.messageType = HelpBoxMessageType.Info;
                }
            }

            if (setup != null)
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

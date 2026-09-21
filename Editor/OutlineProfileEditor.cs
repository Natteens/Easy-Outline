using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Natteens.Outline.Editor
{
    [CustomEditor(typeof(OutlineProfile))]
    public sealed class OutlineProfileEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = OutlineEditorUi.Clone("OutlineProfileInspector");
            PropertyField selectionMode = root.Q<PropertyField>("selectionModeField");
            PropertyField layerMask = root.Q<PropertyField>("layerMaskField");
            PropertyField colorMode = root.Q<PropertyField>("colorModeField");
            PropertyField fixedColor = root.Q<PropertyField>("fixedColorField");
            PropertyField adaptiveDarken = root.Q<PropertyField>("adaptiveDarkenField");
            PropertyField internalDetail = root.Q<PropertyField>("internalDetailField");
            PropertyField internalStrength = root.Q<PropertyField>("internalStrengthField");

            void Refresh()
            {
                OutlineSelectionMode selection = (OutlineSelectionMode)serializedObject.FindProperty("_selectionMode").enumValueIndex;
                OutlineEditorUi.SetVisible(layerMask, selection is OutlineSelectionMode.Layers or OutlineSelectionMode.TargetsAndLayers);
                OutlineColorMode color = (OutlineColorMode)serializedObject.FindProperty("_colorMode").enumValueIndex;
                OutlineEditorUi.SetVisible(fixedColor, color == OutlineColorMode.Fixed);
                OutlineEditorUi.SetVisible(adaptiveDarken, color == OutlineColorMode.Adaptive);
                OutlineEditorUi.SetVisible(internalStrength, serializedObject.FindProperty("_internalDetail").boolValue);
            }

            selectionMode?.RegisterValueChangeCallback(_ => Refresh());
            colorMode?.RegisterValueChangeCallback(_ => Refresh());
            internalDetail?.RegisterValueChangeCallback(_ => Refresh());
            root.Bind(serializedObject);
            Refresh();
            return root;
        }
    }
}

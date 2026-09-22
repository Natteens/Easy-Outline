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
            PropertyField layerMask = root.Q<PropertyField>("layerMaskField");
            ColorField fixedColor = root.Q<ColorField>("fixedColorField");
            PropertyField adaptiveDarken = root.Q<PropertyField>("adaptiveDarkenField");
            Label thicknessValue = root.Q<Label>("thicknessValue");
            Label opacityValue = root.Q<Label>("opacityValue");
            Label statusLabel = root.Q<Label>("statusLabel");
            HelpBox integrationHint = root.Q<HelpBox>("integrationHint");

            void Refresh()
            {
                OutlineSelectionMode selection = (OutlineSelectionMode)serializedObject.FindProperty("_selectionMode").enumValueIndex;
                OutlineEditorUi.SetVisible(layerMask, selection is OutlineSelectionMode.Layers or OutlineSelectionMode.TargetsAndLayers);
                OutlineColorMode colorMode = (OutlineColorMode)serializedObject.FindProperty("_colorMode").enumValueIndex;
                OutlineEditorUi.SetVisible(fixedColor, colorMode == OutlineColorMode.Fixed);
                OutlineEditorUi.SetVisible(adaptiveDarken, colorMode == OutlineColorMode.Adaptive);
                thicknessValue.text = $"{serializedObject.FindProperty("_thickness").intValue} px";
                opacityValue.text = serializedObject.FindProperty("_opacity").floatValue.ToString("0.00");

                string status = OutlineEditorUi.ProfileStatus((OutlineProfile)target);
                OutlineEditorUi.SetStatus(statusLabel, status, status == "Ready");
                integrationHint.text = status switch
                {
                    "URP not active" => "Easy Outline requires an active Universal Render Pipeline asset.",
                    "Feature missing" => "Run Tools > Outline > Setup to install the renderer feature.",
                    "Profile not assigned" => "Assign this profile in the active Outline Renderer Feature.",
                    "Resources missing" => "Run Tools > Outline > Setup to repair hidden shader resources.",
                    _ => string.Empty
                };
                OutlineEditorUi.SetVisible(integrationHint, status != "Ready");
            }

            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            Refresh();
            return root;
        }
    }
}

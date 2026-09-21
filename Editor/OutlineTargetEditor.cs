using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Natteens.Outline.Editor
{
    [CustomEditor(typeof(OutlineTarget))]
    [CanEditMultipleObjects]
    public sealed class OutlineTargetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = OutlineEditorUi.Clone("OutlineTargetInspector");
            PropertyField autoCollect = root.Q<PropertyField>("autoCollectField");
            PropertyField includeChildren = root.Q<PropertyField>("includeChildrenField");
            PropertyField renderers = root.Q<PropertyField>("renderersField");
            PropertyField colorOverride = root.Q<PropertyField>("colorOverrideField");
            PropertyField color = root.Q<PropertyField>("colorField");
            Label count = root.Q<Label>("rendererCount");
            HelpBox warning = root.Q<HelpBox>("rendererWarning");
            Button refresh = root.Q<Button>("refreshButton");

            void RefreshState()
            {
                bool automatic = serializedObject.FindProperty("_autoCollect").boolValue;
                OutlineEditorUi.SetVisible(includeChildren, automatic);
                OutlineEditorUi.SetVisible(renderers, !automatic);
                OutlineEditorUi.SetVisible(color, serializedObject.FindProperty("_colorOverride").boolValue);
                int resolved = 0;
                foreach (Object item in targets)
                    if (item is OutlineTarget outlineTarget)
                        resolved += outlineTarget.ResolvedRendererCount;
                if (count != null)
                    count.text = targets.Length == 1 ? $"{resolved} renderer(s) resolved" : $"{resolved} renderer(s) across {targets.Length} targets";
                OutlineEditorUi.SetVisible(warning, resolved == 0);
            }

            if (refresh != null)
            {
                refresh.clicked += () =>
                {
                    foreach (Object item in targets)
                    {
                        if (item is not OutlineTarget outlineTarget)
                            continue;
                        Undo.RecordObject(outlineTarget, "Refresh Outline Renderers");
                        outlineTarget.RefreshRenderers();
                        EditorUtility.SetDirty(outlineTarget);
                    }
                    serializedObject.Update();
                    RefreshState();
                };
            }

            autoCollect?.RegisterValueChangeCallback(_ => RefreshState());
            colorOverride?.RegisterValueChangeCallback(_ => RefreshState());
            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshState());
            RefreshState();
            return root;
        }
    }
}

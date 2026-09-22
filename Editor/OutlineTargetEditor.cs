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
            Toggle outlined = root.Q<Toggle>("outlinedField");
            Toggle includeChildren = root.Q<Toggle>("includeChildrenField");
            Foldout manualRenderers = root.Q<Foldout>("manualRendererFoldout");
            ColorField color = root.Q<ColorField>("colorField");
            Label statusLabel = root.Q<Label>("statusLabel");
            Label count = root.Q<Label>("rendererCount");
            VisualElement rendererList = root.Q<VisualElement>("rendererList");
            HelpBox warning = root.Q<HelpBox>("rendererWarning");
            Button refresh = root.Q<Button>("refreshButton");

            void RefreshState()
            {
                bool automatic = serializedObject.FindProperty("_autoCollect").boolValue;
                bool active = serializedObject.FindProperty("_outlined").boolValue;
                OutlineEditorUi.SetVisible(includeChildren, automatic);
                OutlineEditorUi.SetVisible(manualRenderers, !automatic);
                OutlineEditorUi.SetVisible(refresh, automatic);
                OutlineEditorUi.SetVisible(color, serializedObject.FindProperty("_colorOverride").boolValue);
                outlined.EnableInClassList("easyoutline-primary-off", !active);

                int resolved = 0;
                foreach (Object item in targets)
                    if (item is OutlineTarget outlineTarget)
                        resolved += outlineTarget.ResolvedRendererCount;
                count.text = targets.Length == 1
                    ? $"{resolved} renderer{(resolved == 1 ? "" : "s")} resolved"
                    : $"{resolved} renderers across {targets.Length} targets";
                OutlineEditorUi.SetVisible(warning, resolved == 0);

                rendererList.Clear();
                if (targets.Length == 1 && target is OutlineTarget single && single.Renderers != null)
                {
                    int shown = 0;
                    foreach (Renderer renderer in single.Renderers)
                    {
                        if (renderer == null)
                            continue;
                        if (shown++ == 6)
                        {
                            var remaining = new Label($"+{resolved - 6} more");
                            remaining.AddToClassList("easyoutline-renderer-name");
                            rendererList.Add(remaining);
                            break;
                        }
                        var name = new Label(renderer.name);
                        name.AddToClassList("easyoutline-renderer-name");
                        rendererList.Add(name);
                    }
                }

                string status = targets.Length > 1 ? "Multiple" : resolved == 0 ? "No Renderers" : active ? "Active" : "Off";
                OutlineEditorUi.SetStatus(statusLabel, status, status == "Active");
            }

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

            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshState());
            root.schedule.Execute(() => { if (EditorApplication.isPlaying) RefreshState(); }).Every(250);
            RefreshState();
            return root;
        }
    }
}

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
            Toggle includeChildren = root.Q<Toggle>("includeChildrenField");
            Foldout manualRenderers = root.Q<Foldout>("manualRendererFoldout");
            Foldout resolvedRenderers = root.Q<Foldout>("resolvedRendererFoldout");
            ColorField color = root.Q<ColorField>("colorField");
            Label count = root.Q<Label>("rendererCount");
            VisualElement rendererList = root.Q<VisualElement>("rendererList");
            HelpBox warning = root.Q<HelpBox>("rendererWarning");
            Button refresh = root.Q<Button>("refreshButton");

            void RefreshState()
            {
                bool automatic = serializedObject.FindProperty("_autoCollect").boolValue;
                OutlineEditorUi.SetVisible(includeChildren, automatic);
                OutlineEditorUi.SetVisible(manualRenderers, !automatic);
                OutlineEditorUi.SetVisible(refresh, automatic);
                OutlineEditorUi.SetVisible(color, serializedObject.FindProperty("_colorOverride").boolValue);

                int resolved = 0;
                foreach (Object item in targets)
                    if (item is OutlineTarget outlineTarget)
                        resolved += outlineTarget.ResolvedRendererCount;
                count.text = targets.Length == 1
                    ? $"{resolved} renderer{(resolved == 1 ? "" : "s")} resolved"
                    : $"{resolved} renderers across {targets.Length} targets";
                OutlineEditorUi.SetVisible(warning, resolved == 0);
                OutlineEditorUi.SetVisible(resolvedRenderers, targets.Length == 1 && resolved > 0);
                resolvedRenderers.text = $"Resolved Renderers ({resolved})";

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

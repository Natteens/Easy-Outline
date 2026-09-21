using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Natteens.Outline.Editor
{
    public static class OutlineSetup
    {
        public const string ProfilePath = "Assets/Outline/OutlineProfile.asset";

        [MenuItem("Tools/Outline/Setup", priority = 1)]
        public static void Setup()
        {
            EnsureFolder("Assets", "Outline");
            OutlineProfile profile = AssetDatabase.LoadAssetAtPath<OutlineProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<OutlineProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            Shader metadata = Shader.Find("Hidden/Natteens/Outline/Metadata");
            Shader composite = Shader.Find("Hidden/Natteens/Outline/Composite");
            var installed = 0;
            foreach (UniversalRendererData rendererData in FindActiveRendererData())
                if (InstallOnRendererData(rendererData, profile, metadata, composite))
                    installed++;

            OutlineTarget.ConfigureRenderingLayerBit(profile.TargetRenderingLayerBit);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);

            if (installed == 0)
                Debug.LogWarning("Outline profile was created, but no writable active Universal Renderer Data was found under Assets/.");
            else
                Debug.Log($"Outline setup complete. Configured {installed} active Universal Renderer Data asset(s).");
        }

        internal static bool InstallOnRendererData(UniversalRendererData rendererData, OutlineProfile profile,
            Shader metadata = null, Shader composite = null)
        {
            if (rendererData == null || !IsWritableProjectAssetPath(AssetDatabase.GetAssetPath(rendererData)))
                return false;

            List<OutlineRendererFeature> features = rendererData.rendererFeatures.OfType<OutlineRendererFeature>().ToList();
            OutlineRendererFeature feature = features.FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<OutlineRendererFeature>();
                feature.name = "Outline";
                feature.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                AssetDatabase.AddObjectToAsset(feature, rendererData);
                rendererData.rendererFeatures.Add(feature);
            }

            for (var i = 1; i < features.Count; i++)
            {
                OutlineRendererFeature duplicate = features[i];
                rendererData.rendererFeatures.Remove(duplicate);
                Object.DestroyImmediate(duplicate, true);
            }

            feature.Profile = profile;
            feature.SetResources(metadata ?? Shader.Find("Hidden/Natteens/Outline/Metadata"),
                composite ?? Shader.Find("Hidden/Natteens/Outline/Composite"));
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rendererData);
            rendererData.SetDirty();
            return true;
        }

        internal static bool IsWritableProjectAssetPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<UniversalRendererData> FindActiveRendererData()
        {
            var pipelines = new HashSet<UniversalRenderPipelineAsset>();
            AddPipeline(pipelines, GraphicsSettings.defaultRenderPipeline);
            AddPipeline(pipelines, GraphicsSettings.currentRenderPipeline);
            for (var i = 0; i < QualitySettings.names.Length; i++)
                AddPipeline(pipelines, QualitySettings.GetRenderPipelineAssetAt(i));

            var renderers = new HashSet<UniversalRendererData>();
            foreach (UniversalRenderPipelineAsset pipeline in pipelines)
            {
                foreach (ScriptableRendererData renderer in pipeline.rendererDataList)
                {
                    if (renderer is not UniversalRendererData universal)
                        continue;
                    string path = AssetDatabase.GetAssetPath(universal);
                    if (IsWritableProjectAssetPath(path))
                        renderers.Add(universal);
                }
            }
            return renderers;
        }

        private static void AddPipeline(ISet<UniversalRenderPipelineAsset> pipelines, RenderPipelineAsset pipeline)
        {
            if (pipeline is UniversalRenderPipelineAsset universal)
                pipelines.Add(universal);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}

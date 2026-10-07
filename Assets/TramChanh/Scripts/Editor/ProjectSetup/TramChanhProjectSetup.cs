using System.Collections.Generic;
using TramChanh.Core.GroundTruth;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TramChanh.EditorTools.ProjectSetup
{
    /// <summary>
    /// Idempotent project configuration (CX-001): physics layers, linear colour
    /// space and the URP pipeline asset. Runs once after every domain reload and
    /// only changes what is missing, so it is safe on an already configured project.
    /// </summary>
    [InitializeOnLoad]
    public static class TramChanhProjectSetup
    {
        public const string UrpAssetPath = "Assets/TramChanh/Settings/Rendering/URP_TramChanh.asset";
        public const string UrpRendererPath = "Assets/TramChanh/Settings/Rendering/URP_TramChanh_Renderer.asset";

        static TramChanhProjectSetup()
        {
            EditorApplication.delayCall += () => Apply(logChanges: true);
        }

        [MenuItem("Tram Chanh/Setup/Apply Project Settings")]
        public static void ApplyFromMenu()
        {
            Apply(logChanges: true);
        }

        public static List<string> Apply(bool logChanges)
        {
            var changes = new List<string>();
            EnsureLayers(changes);
            EnsureLinearColorSpace(changes);
            EnsureInputSystem(changes);
            EnsureUrp(changes);

            if (logChanges && changes.Count > 0)
            {
                Debug.Log("[TramChanh] Project setup applied:\n- " + string.Join("\n- ", changes));
            }

            return changes;
        }

        public static bool LayersConfigured()
        {
            foreach ((int index, string name) in TramChanhLayers.All)
            {
                if (LayerMask.LayerToName(index) != name)
                {
                    return false;
                }
            }

            return true;
        }

        private static void EnsureLayers(List<string> changes)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[TramChanh] TagManager.asset not found; layers not configured.");
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            bool dirty = false;

            foreach ((int index, string name) in TramChanhLayers.All)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(index);
                if (slot.stringValue == name)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(slot.stringValue))
                {
                    Debug.LogError($"[TramChanh] Layer {index} is '{slot.stringValue}', expected '{name}'. Fix manually.");
                    continue;
                }

                slot.stringValue = name;
                dirty = true;
                changes.Add($"layer {index} = {name}");
            }

            if (dirty)
            {
                tagManager.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureLinearColorSpace(List<string> changes)
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                changes.Add("color space = Linear");
            }
        }

        private static void EnsureInputSystem(List<string> changes)
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty handler = settings.FindProperty("activeInputHandler");
            if (handler.intValue == 0)
            {
                // Both backends: enable Gameplay actions while retaining any existing legacy input.
                handler.intValue = 2;
                settings.ApplyModifiedPropertiesWithoutUndo();
                changes.Add("active input handling = Both");
            }
        }

        private static void EnsureUrp(List<string> changes)
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            {
                return;
            }

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urp == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, UrpRendererPath);
                urp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urp, UrpAssetPath);
                AssetDatabase.SaveAssets();
                changes.Add("created " + UrpAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;
            changes.Add("default render pipeline = URP_TramChanh");
        }
    }
}

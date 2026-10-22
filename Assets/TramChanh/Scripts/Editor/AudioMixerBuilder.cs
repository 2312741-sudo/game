using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// Builds Assets/TramChanh/Resources/Audio/AM_TramChanh.mixer: groups Master, Master/Sfx, Master/Ambient and the exposed
    /// dB parameters MasterVolume, SfxVolume, AmbientVolume. Unity has no public API to create an AudioMixer, so this uses
    /// the editor's own internal class UnityEditor.Audio.AudioMixerController through reflection (the same calls the Audio
    /// Mixer window makes). Names were read from UnityEditor.CoreModule.dll of Unity 6000.6.0f1:
    ///   static AudioMixerController CreateMixerControllerAtPath(string)
    ///   AudioMixerGroupController masterGroup { get; }, AudioMixerGroupController[] children { get; }
    ///   AudioMixerGroupController CreateNewGroup(string name, bool storeUndoState)
    ///   void AddChildToParent(AudioMixerGroupController child, AudioMixerGroupController parent)
    ///   GUID AudioMixerGroupController.GetGUIDForVolume()
    ///   void AddExposedParameter(AudioParameterPath), bool ContainsExposedParameter(GUID)
    ///   ExposedAudioParameter[] exposedParameters { get; set; }  (struct with fields GUID guid, string name)
    ///   new AudioGroupParameterPath(AudioMixerGroupController group, GUID parameter)
    /// Safe to run again: it only adds what is missing. If Unity renames any of these, the tool stops with a clear error
    /// and creates nothing half-built beyond the asset itself.
    /// </summary>
    public static class AudioMixerBuilder
    {
        public const string AssetPath = "Assets/TramChanh/Resources/Audio/AM_TramChanh.mixer";
        // Must equal TramChanh.Audio.AudioMixerController constants (the Editor assembly does not reference the Audio assembly).
        // A test in Tests/EditMode/Audio compares them.
        public const string MasterGroup = "Master";
        public const string SfxGroupPath = "Master/Sfx";
        public const string AmbientGroupPath = "Master/Ambient";
        public const string MasterParameter = "MasterVolume";
        public const string SfxParameter = "SfxVolume";
        public const string AmbientParameter = "AmbientVolume";
        private const string ControllerType = "UnityEditor.Audio.AudioMixerController, UnityEditor.CoreModule";
        private const string GroupType = "UnityEditor.Audio.AudioMixerGroupController, UnityEditor.CoreModule";
        private const string GroupPathType = "UnityEditor.Audio.AudioGroupParameterPath, UnityEditor.CoreModule";
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        [MenuItem("Tram Chanh/Audio/Build Audio Mixer")]
        public static void Build()
        {
            try
            {
                string report = BuildAt(AssetPath);
                Debug.Log("[AudioMixerBuilder] " + report);
            }
            catch (Exception e)
            {
                Debug.LogError("[AudioMixerBuilder] Failed: " + e.Message + "\n" + e);
            }
        }

        /// <summary>Creates or completes the mixer at the given asset path. Returns a one-line report.</summary>
        public static string BuildAt(string assetPath)
        {
            Type controllerType = Type.GetType(ControllerType);
            Type groupType = Type.GetType(GroupType);
            Type groupPathType = Type.GetType(GroupPathType);
            if (controllerType == null || groupType == null || groupPathType == null)
            {
                throw new InvalidOperationException("Internal audio mixer types were not found in UnityEditor.CoreModule. Build the mixer by hand: see Docs/Design/COURSE_BAI5_BAI6_AUDIO_UI.md.");
            }

            string folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder)) { CreateFolders(folder); }

            bool created = false;
            UnityEngine.Object controller = AssetDatabase.LoadAssetAtPath<AudioMixer>(assetPath);
            if (controller == null)
            {
                controller = (UnityEngine.Object)Need(controllerType, "CreateMixerControllerAtPath", typeof(string)).Invoke(null, new object[] { assetPath });
                created = true;
            }
            if (controller == null || !controllerType.IsInstanceOfType(controller))
            {
                throw new InvalidOperationException("Asset at " + assetPath + " is not an AudioMixerController.");
            }

            object master = Need(controllerType, "get_masterGroup").Invoke(controller, null);
            if (master == null) { throw new InvalidOperationException("The new mixer has no master group."); }
            var masterObject = (UnityEngine.Object)master;
            if (masterObject.name != MasterGroup) { masterObject.name = MasterGroup; }

            object sfx = EnsureChild(controller, controllerType, groupType, master, "Sfx");
            object ambient = EnsureChild(controller, controllerType, groupType, master, "Ambient");

            Expose(controller, controllerType, groupType, groupPathType, master, MasterParameter);
            Expose(controller, controllerType, groupType, groupPathType, sfx, SfxParameter);
            Expose(controller, controllerType, groupType, groupPathType, ambient, AmbientParameter);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(masterObject);
            EditorUtility.SetDirty((UnityEngine.Object)sfx);
            EditorUtility.SetDirty((UnityEngine.Object)ambient);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(assetPath);
            if (mixer == null) { throw new InvalidOperationException("Mixer asset did not load after saving."); }
            bool groupsOk = mixer.FindMatchingGroups(SfxGroupPath).Length > 0
                && mixer.FindMatchingGroups(AmbientGroupPath).Length > 0;
            return (created ? "Created " : "Completed existing ") + assetPath + ". Groups Master/Sfx/Ambient found: " + groupsOk
                + ". Exposed: " + string.Join(", ", ExposedNames(controller, controllerType)) + ".";
        }

        private static object EnsureChild(UnityEngine.Object controller, Type controllerType, Type groupType, object parent, string name)
        {
            var children = (Array)Need(groupType, "get_children").Invoke(parent, null);
            if (children != null)
            {
                foreach (object child in children)
                {
                    if (child != null && ((UnityEngine.Object)child).name == name) { return child; }
                }
            }
            object group = Need(controllerType, "CreateNewGroup", typeof(string), typeof(bool)).Invoke(controller, new object[] { name, false });
            Need(controllerType, "AddChildToParent", groupType, groupType).Invoke(controller, new object[] { group, parent });
            return group;
        }

        private static void Expose(UnityEngine.Object controller, Type controllerType, Type groupType, Type groupPathType, object group, string parameterName)
        {
            object guid = Need(groupType, "GetGUIDForVolume").Invoke(group, null);
            var contains = (bool)Need(controllerType, "ContainsExposedParameter", guid.GetType()).Invoke(controller, new[] { guid });
            if (!contains)
            {
                object path = Activator.CreateInstance(groupPathType, All, null, new[] { group, guid }, null);
                Need(controllerType, "AddExposedParameter", groupPathType.BaseType).Invoke(controller, new[] { path });
            }

            // Rename the exposed entry that belongs to this group's volume (the default name is "MyExposedParam").
            PropertyInfo property = controllerType.GetProperty("exposedParameters", All);
            if (property == null || !property.CanRead || !property.CanWrite) { throw new MissingMemberException(controllerType.Name, "exposedParameters"); }
            var array = (Array)property.GetValue(controller);
            for (int i = 0; i < array.Length; i++)
            {
                object item = array.GetValue(i);
                FieldInfo guidField = item.GetType().GetField("guid", All);
                FieldInfo nameField = item.GetType().GetField("name", All);
                if (guidField == null || nameField == null) { throw new MissingMemberException(item.GetType().Name, "guid/name"); }
                if (!guidField.GetValue(item).Equals(guid)) { continue; }
                if ((string)nameField.GetValue(item) == parameterName) { return; }
                nameField.SetValue(item, parameterName);
                array.SetValue(item, i);
                property.SetValue(controller, array);
                MethodInfo changed = controllerType.GetMethod("OnChangedExposedParameter", All);
                if (changed != null) { changed.Invoke(controller, null); }
                return;
            }
            throw new InvalidOperationException("Exposed parameter for " + parameterName + " was not found after adding it.");
        }

        private static string[] ExposedNames(UnityEngine.Object controller, Type controllerType)
        {
            var array = (Array)controllerType.GetProperty("exposedParameters", All).GetValue(controller);
            var names = new string[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                object item = array.GetValue(i);
                names[i] = (string)item.GetType().GetField("name", All).GetValue(item);
            }
            return names;
        }

        private static MethodInfo Need(Type type, string name, params Type[] parameters)
        {
            MethodInfo method = type.GetMethod(name, All, null, parameters, null);
            if (method == null)
            {
                throw new MissingMethodException("Unity changed its internal audio mixer API: " + type.FullName + "." + name + " not found.");
            }
            return method;
        }

        private static void CreateFolders(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) { AssetDatabase.CreateFolder(current, parts[i]); }
                current = next;
            }
        }
    }
}

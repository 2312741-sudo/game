using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// Menu shortcuts so the playable scene, the Visual Shell preview and the old test scene are never confused.
    /// Editor-only; it changes no project data except the Build Settings scene list when you ask for it.
    /// </summary>
    public static class TramChanhSceneMenu
    {
        public const string Main = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        public const string PlayableDrinkWave = "Assets/TramChanh/Scenes/Gameplay/SCN_DrinkWave.unity";
        public const string Blockout = "Assets/TramChanh/Scenes/Gameplay/SCN_Gameplay_Blockout.unity";
        public const string RoadsideEnvironment = "Assets/TramChanh/Scenes/ACCEL01/SCN_AccelRoadsideEnvironment.unity";
        public const string TeaRackTest = "Assets/TramChanh/Scenes/Test/SCN_TeaRackPickupTest.unity";

        [MenuItem("Tram Chanh/Scenes/Open Main Game (SCN_TramChanh_Main) - primary", false, 0)]
        public static void OpenMain() => Open(Main);

        [MenuItem("Tram Chanh/Scenes/Open Drink Wave (gray blockout, drink only)")]
        public static void OpenPlayable() => Open(PlayableDrinkWave);

        [MenuItem("Tram Chanh/Scenes/Open Visual Shell Preview (Roadside Night, view only)")]
        public static void OpenRoadsideEnvironment() => Open(RoadsideEnvironment);

        [MenuItem("Tram Chanh/Scenes/Open Old Test Scene (Tea Rack Pickup)")]
        public static void OpenTeaRackTest() => Open(TeaRackTest);

        [MenuItem("Tram Chanh/Scenes/Set Build Settings To Playable Scenes")]
        public static void SetBuildSettingsToPlayableScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            // Main is first (startup scene). The drink wave loads the blockout additively by path, so it needs both.
            foreach (string path in new[] { Main, PlayableDrinkWave, Blockout })
            {
                if (File.Exists(path)) { scenes.Add(new EditorBuildSettingsScene(path, true)); }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            EditorUtility.DisplayDialog("Tram Chanh", "Build Settings now list " + scenes.Count + " playable scene(s). Test scenes are not included.", "OK");
        }

        private static void Open(string path)
        {
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Tram Chanh", "This scene is not on the current branch:\n" + path + "\n\nRun Automation/unity_local_sync.sh open-info to see what this checkout contains.", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }
    }
}

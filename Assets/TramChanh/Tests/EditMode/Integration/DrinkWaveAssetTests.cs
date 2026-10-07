using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using TramChanh.App;
using TramChanh.UI.Orders;
using TramChanh.UI.Prompt;
using TramChanh.UI.Localization;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Integration
{
    public sealed class DrinkWaveAssetTests
    {
        [Test]
        public void TC_DRINK_009_SavedSceneRetainsCompositionAndLocalizationReferences()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TramChanh/Scenes/Gameplay/SCN_DrinkWave.unity", OpenSceneMode.Additive);
            try
            {
                bool found = false;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.TryGetComponent(out DrinkWaveBootstrap bootstrap))
                    {
                        found = true;
                        var data = new SerializedObject(bootstrap);
                        foreach (string field in new[] { "_loader", "_balance", "_drinkDefinition", "_stationPrefab", "_tablePointPrefab", "_vehiclePointPrefab", "_input", "_player", "_interactor", "_heldView", "_prompt", "_entryUI", "_lobby" })
                        { Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field); }
                    }
                    if (root.TryGetComponent(out InteractionPromptView prompt)) { Assert.That(prompt.Table, Is.Not.Null); }
                    if (root.TryGetComponent(out OrderEntryUI entry)) { Assert.That(entry.Table, Is.Not.Null); }
                }
                Assert.That(found, Is.True);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void TC_DRINK_009_SavedShakeAnimationHasPlayableStates()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/TramChanh/Art/Animations/DrinkWave/AC_TeaBag_Shake.controller");
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.layers, Has.Length.EqualTo(1));
            Assert.That(controller.layers[0].stateMachine.states, Has.Length.EqualTo(2));
            foreach (var state in controller.layers[0].stateMachine.states) { Assert.That(state.state.motion, Is.Not.Null); }
            Assert.That(controller.parameters, Has.Length.EqualTo(1));
            Assert.That(controller.parameters[0].name, Is.EqualTo("Shaking"));
        }

        [Test]
        public void TC_ORDER_UI_CompositionLocalizesAllOrderEntryReasons()
        {
            var source = AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>("Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_OrderEntry.asset");
            var combined = AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>("Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_DrinkWave.asset");
            var entries = new SerializedObject(source).FindProperty("_entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                string key = entries.GetArrayElementAtIndex(i).FindPropertyRelative("_key").stringValue;
                foreach (string language in new[] { "en", "vi" })
                { Assert.That(combined.Resolve(key, language), Is.Not.Empty.And.Not.EqualTo(key), key); }
            }
        }

        [Test]
        public void TC_DRINK_009_PlayableWaveHasSavedCompositionSceneAndRecipe()
        {
            Assert.That(File.Exists("Assets/TramChanh/Scenes/Gameplay/SCN_DrinkWave.unity"), Is.True, "A playable composition scene is required.");
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TramChanh/ScriptableObjects/Recipes/SO_Recipe_Drink_Slice.asset"), Is.Not.Null);
            Assert.That(File.Exists("Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_DrinkStation.prefab"), Is.True);
        }
    }
}

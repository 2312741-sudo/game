using System.Linq;
using NUnit.Framework;
using TramChanh.App;
using TramChanh.Content;
using TramChanh.Core.GroundTruth;
using TramChanh.EditorTools.Placeholders;
using TramChanh.Interaction;
using TramChanh.Interaction.Player;
using TramChanh.Interaction.Preview;
using TramChanh.Stall.Anchors;
using TramChanh.UI.Localization;
using TramChanh.UI.Prompt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TramChanh.Tests.EditMode.Interaction
{
    public sealed class PlayerInteractionSceneTests
    {
        private Scene _preview;
        private Scene _blockout;
        [SetUp]
        public void SetUp()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayerInteractionSceneBuilder.ScenePath), Is.Not.Null,
                "The playable interaction scene must be saved.");
            _preview = EditorSceneManager.OpenScene(PlayerInteractionSceneBuilder.ScenePath, OpenSceneMode.Additive);
            _blockout = EditorSceneManager.OpenScene(GameplayBlockoutBuilder.ScenePath, OpenSceneMode.Additive);
        }
        [TearDown]
        public void TearDown()
        {
            if (_preview.IsValid())
            {
                EditorSceneManager.CloseScene(_preview, true);
            }
            if (_blockout.IsValid())
            {
                EditorSceneManager.CloseScene(_blockout, true);
            }
        }
        [Test]
        public void GT_001_InteractionScene_RetainsStallDimensionsAndStationLayout()
        {
            GameObject stall = _preview.GetRootGameObjects().Single(o => o.name == "PF_Stall_TramChanh");
            GameObject original = _blockout.GetRootGameObjects().Single(o => o.name == "PF_Stall_TramChanh");
            foreach (StallAnchor anchor in stall.GetComponentsInChildren<StallAnchor>())
            {
                StallAnchor source = original.GetComponentsInChildren<StallAnchor>().Single(a => a.Id == anchor.Id);
                Assert.That(anchor.transform.localPosition, Is.EqualTo(source.transform.localPosition), anchor.name);
                Assert.That(anchor.transform.localRotation, Is.EqualTo(source.transform.localRotation), anchor.name);
                Assert.That(anchor.transform.localScale, Is.EqualTo(source.transform.localScale), anchor.name);
                Assert.That(anchor.PositionConfirmed, Is.False);
                foreach (Transform station in anchor.transform)
                {
                    Transform oldStation = source.transform.Find(station.name);
                    Assert.That(station.localPosition, Is.EqualTo(oldStation.localPosition), station.name);
                    Assert.That(station.localRotation, Is.EqualTo(oldStation.localRotation), station.name);
                    Assert.That(station.localScale, Is.EqualTo(oldStation.localScale), station.name);
                }
            }
            Assert.That(stall.transform.localScale, Is.EqualTo(Vector3.one));
            Renderer[] counter = stall.transform.Find("Counter").GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            Assert.That(counter.Max(r => r.bounds.max.y), Is.EqualTo(StallDimensions.CounterHeight).Within(0.0001f));
            Assert.That(stall.transform.Find("Roof").GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y),
                Is.EqualTo(StallDimensions.ApproxTotalHeight).Within(0.0001f));
        }
        [Test]
        public void CX_011_InteractionScene_HasOneFirstPersonCameraAndConfiguredPlayer()
        {
            GameObject[] roots = _preview.GetRootGameObjects();
            GameObject player = roots.Single(o => o.name == "PF_Player");
            Assert.That(player.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(player.GetComponent<CharacterController>().minMoveDistance, Is.Zero,
                "High-frame-rate movement must retain small frame displacements.");
            Assert.That(player.GetComponent<PlayerInteractor>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerInputReader>(), Is.Not.Null);
            Assert.That(player.GetComponent<FirstPersonController>().ViewCamera.transform.localPosition.y, Is.EqualTo(1.6f));
            Assert.That(player.transform.Find("PlayerCamera/HandSocket"), Is.Not.Null);
            Assert.That(roots.SelectMany(o => o.GetComponentsInChildren<Camera>()).Count(c => c.enabled), Is.EqualTo(1));
            Assert.That(roots.SelectMany(o => o.GetComponentsInChildren<AudioListener>()).Count(a => a.enabled), Is.EqualTo(1));
        }
        [Test]
        public void TC_INT_005_InteractionScene_HasOnlyNeutralPreviewInteractablesAndValidReferences()
        {
            GameObject[] roots = _preview.GetRootGameObjects();
            InspectionInteractable[] targets = roots.SelectMany(o => o.GetComponentsInChildren<InspectionInteractable>()).ToArray();
            Assert.That(targets.Select(t => t.name), Is.EquivalentTo(new[] { "PF_RedTeaRack", "PF_Grill_Elmich", "PF_Placeholder_InteractionCube" }));
            Assert.That(targets.Select(t => t.Id.Value).Distinct().Count(), Is.EqualTo(3));
            foreach (InspectionInteractable target in targets)
            {
                Assert.That(target.InteractionPoint, Is.Not.Null);
                Assert.That(target.GetComponent<InteractableRef>().Target, Is.SameAs(target));
            }
            foreach (GameObject root in roots)
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero, child.name);
                }
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                {
                    Assert.That(renderer.sharedMaterials.All(m => m != null), Is.True, renderer.name);
                }
            }
        }
        [Test]
        public void CX_011_SavedScene_HasPersistentCompositionAndLocalizationReferences()
        {
            GameObject[] roots = _preview.GetRootGameObjects();
            var bootstrap = roots.SelectMany(o => o.GetComponentsInChildren<PlayerInteractionTestBootstrap>()).Single();
            var serialized = new SerializedObject(bootstrap);
            foreach (string field in new[] { "_balance", "_input", "_player", "_interactor", "_prompt" })
            {
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }
            Assert.That(serialized.FindProperty("_balance").objectReferenceValue,
                Is.SameAs(AssetDatabase.LoadAssetAtPath<BalanceConfig>(PlayerInteractionSceneBuilder.BalancePath)));
            var prompt = (InteractionPromptView)serialized.FindProperty("_prompt").objectReferenceValue;
            Assert.That(new SerializedObject(prompt).FindProperty("_table").objectReferenceValue,
                Is.SameAs(AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(PlayerInteractionSceneBuilder.LocalizationPath)));
        }
        [Test]
        public void CX_011_PlayerTuningAndPromptText_AreEditableAssetData()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(PlayerInteractionSceneBuilder.BalancePath);
            Assert.That(balance.ReachDistance, Is.GreaterThan(StallDimensions.Depth));
            Assert.That(balance.MoveSpeed, Is.GreaterThan(0f));
            Assert.That(balance.LookSensitivity, Is.GreaterThan(0f));
            Assert.That(balance.Gravity, Is.LessThan(0f));
            var table = AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(PlayerInteractionSceneBuilder.LocalizationPath);
            Assert.That(table.Resolve("preview.tea_rack.inspect", "en"), Does.Contain("tea rack"));
            Assert.That(table.Resolve("preview.tea_rack.inspect", "vi"), Does.Contain("trà"));
            Assert.That(table.Resolve("preview.grill.inspect", "en"), Does.Contain("grill"));
        }
    }
}

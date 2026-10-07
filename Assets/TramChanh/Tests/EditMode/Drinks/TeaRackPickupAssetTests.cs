using System.Linq;
using NUnit.Framework;
using TramChanh.App;
using TramChanh.Drinks.Runtime;
using TramChanh.EditorTools.Placeholders;
using TramChanh.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TramChanh.Tests.EditMode.Drinks
{
    public sealed class TeaRackPickupAssetTests
    {
        [Test]
        public void GT_003_PrePortionedBagHasGripAndClosedTeaVisualWithCanonicalHeldActions()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TeaRackPickupSceneBuilder.BagPath);
            Assert.That(prefab, Is.Not.Null);
            TeaBagItem bag = prefab.GetComponent<TeaBagItem>();
            Assert.That(bag.HandGrip, Is.SameAs(prefab.transform.Find("Anchors/HandGrip")));
            Assert.That(bag.PlacementPoint, Is.SameAs(prefab.transform.Find("Anchors/PlacementPoint")));
            Assert.That(prefab.transform.Find("Visual/Bag_Closed").GetComponent<Renderer>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Visual/TeaLiquid").GetComponent<Renderer>(), Is.Not.Null);
            Assert.That(prefab.GetComponents<MonoBehaviour>().Any(b => b is IHeldItemAction), Is.True);
            Assert.That(prefab.GetComponentsInChildren<MeshCollider>(true), Is.Empty);
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero);
            }
        }

        [Test]
        public void DRINK_001_SavedPickupSceneBindsRackHandViewAndDataWithoutMovingExistingObjects()
        {
            var original = EditorSceneManager.OpenScene(PlayerInteractionSceneBuilder.ScenePath, OpenSceneMode.Additive);
            var pickup = EditorSceneManager.OpenScene(TeaRackPickupSceneBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                TeaRackController rack = pickup.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<TeaRackController>()).Single();
                Assert.That(rack.GetComponent<InteractableRef>().Target, Is.SameAs(rack));
                Assert.That(rack.name, Is.EqualTo("PF_RedTeaRack"));
                var bootstrap = pickup.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<PlayerInteractionTestBootstrap>()).Single();
                Assert.That(bootstrap.Balance.TeaRackInitialStock, Is.GreaterThan(0));
                Assert.That(bootstrap.Balance.TeaRackInitialStock, Is.LessThanOrEqualTo(bootstrap.Balance.TeaRackCapacity));
                Assert.That(bootstrap.Player.GetComponent<HeldItemView>().HoldAnchor, Is.SameAs(bootstrap.Player.transform.Find("PlayerCamera/HandSocket/HoldAnchor")));
                foreach (GameObject beforeRoot in original.GetRootGameObjects())
                {
                    GameObject afterRoot = pickup.GetRootGameObjects().Single(o => o.name == beforeRoot.name);
                    CompareTransforms(beforeRoot.transform, afterRoot.transform);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(pickup, true);
                EditorSceneManager.CloseScene(original, true);
            }
        }

        private static void CompareTransforms(Transform before, Transform after)
        {
            Assert.That(Vector3.Distance(before.localPosition, after.localPosition), Is.LessThan(0.0001f), before.name);
            Assert.That(Quaternion.Angle(before.localRotation, after.localRotation), Is.LessThan(0.001f), before.name);
            Assert.That(Vector3.Distance(before.localScale, after.localScale), Is.LessThan(0.0001f), before.name);
            for (int i = 0; i < before.childCount; i++)
            {
                Transform child = before.GetChild(i);
                Assert.That(after.childCount, Is.GreaterThan(i), child.name);
                Transform match = after.GetChild(i);
                Assert.That(match.name, Is.EqualTo(child.name), "Preserve child order even when sibling names repeat.");
                CompareTransforms(child, match);
            }
        }
    }
}

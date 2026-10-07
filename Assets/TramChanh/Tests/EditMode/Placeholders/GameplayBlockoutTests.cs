using System;
using System.Linq;
using NUnit.Framework;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.EditorTools.Placeholders;
using TramChanh.Stall.Anchors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TramChanh.Tests.EditMode.Placeholders
{
    public sealed class GameplayBlockoutTests
    {
        private Scene _scene;
        private GameObject _stall;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.OpenScene(GameplayBlockoutBuilder.ScenePath, OpenSceneMode.Additive);
            _stall = _scene.GetRootGameObjects().Single(o => o.name == "PF_Stall_TramChanh");
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
            {
                EditorSceneManager.CloseScene(_scene, true);
            }
        }

        [Test]
        public void GT_001_SavedScene_StallIsExactScaleAndCounterIsOneMeter()
        {
            Bounds bounds = BoundsOf(_stall.transform.Find("Structure"), _stall.transform.Find("Counter"),
                _stall.transform.Find("Frame"), _stall.transform.Find("Roof"), _stall.transform.Find("Wheels"));
            Assert.That(bounds.size.x, Is.EqualTo(1.8f).Within(0.0001f));
            Assert.That(bounds.size.z, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(bounds.max.y, Is.EqualTo(2.2f).Within(0.0001f));
            Assert.That(BoundsOf(_stall.transform.Find("Counter")).max.y, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(_stall.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void ART_STALL_001_SavedScene_RequestedAnchorsKeepCanonicalIds()
        {
            var expected = new[] { "TeaRackAnchor", "ToppingStationAnchor", "IceBinAnchor", "GrillAnchor", "SauceAnchor", "WrappingAnchor", "ReadyCounterAnchor" };
            Transform group = _stall.transform.Find("Anchors");
            foreach (string name in expected)
            {
                Assert.That(group.Find(name), Is.Not.Null, name);
            }

            StallAnchor[] anchors = _stall.GetComponentsInChildren<StallAnchor>();
            Assert.That(anchors.Select(a => a.Id), Is.EquivalentTo(Enum.GetValues(typeof(StallAnchorId))));
            Assert.That(anchors.All(a => !a.PositionConfirmed), Is.True);
            Assert.That(group.Find("ToppingStationAnchor").GetComponent<StallAnchor>().Id, Is.EqualTo(StallAnchorId.Topping));
            Assert.That(group.Find("WrappingAnchor").GetComponent<StallAnchor>().Id, Is.EqualTo(StallAnchorId.Wrap));
            foreach (StallAnchor anchor in anchors)
            {
                foreach (Transform child in anchor.transform)
                {
                    Assert.That(child.localPosition, Is.EqualTo(Vector3.zero), child.name);
                    Assert.That(child.localScale, Is.EqualTo(Vector3.one), child.name);
                }
            }
        }

        [Test]
        public void GT_002_SavedScene_ContainsOnlyTheNewSign()
        {
            Transform sign = _stall.transform.Find("Sign");
            Assert.That(sign.childCount, Is.EqualTo(1));
            Assert.That(sign.GetChild(0).name, Is.EqualTo("PF_Sign_TramChanh_New"));
            string[] prefabs = AssetDatabase.FindAssets("PF_Sign_ t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(prefabs, Is.EquivalentTo(new[] { StallPlaceholderBuilder.SignPrefabPath }));
        }

        [Test]
        public void ART_STALL_001_Prefabs_HaveNoMissingAssetsAndOnlyPrimitiveGeometry()
        {
            foreach (string name in GameplayBlockoutBuilder.EquipmentNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayBlockoutBuilder.PrefabPath(name));
                Assert.That(prefab, Is.Not.Null, name);
                Assert.That(prefab.GetComponent<PlaceholderAsset>(), Is.Not.Null, name);
                Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), name);
                Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero), name);
                Assert.That(prefab.GetComponentsInChildren<MeshCollider>(true), Is.Empty, name);
                ValidateObjects(prefab);
                if (name != "PF_ReadyCounterPoint")
                {
                    Assert.That(BoundsOf(prefab.transform).min.y, Is.EqualTo(0f).Within(0.0001f), name + " bottom-centre pivot");
                }
            }

            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                ValidateObjects(root);
            }
        }

        [Test]
        public void ART_STALL_001_Prefabs_EquipmentAndFurnitureHaveDocumentedAnchors()
        {
            var paths = new[]
            {
                ("PF_RedTeaRack", "InteractionPoint"), ("PF_RedTeaRack", "OutputPoint"), ("PF_RedTeaRack", "BagSlots"),
                ("PF_ToppingStation", "CoconutJellyBin/InteractionPoint"), ("PF_ToppingStation", "LemonJellyBin/InteractionPoint"),
                ("PF_ToppingStation", "CoverPivot/TransparentCover"),
                ("PF_IceBin", "InteractionPoint"), ("PF_IceBin", "ScoopRestPoint"), ("PF_IceBin", "IceVolume"),
                ("PF_Grill_Elmich", "LidPivot/Lid"), ("PF_Grill_Elmich", "LidPivot/UpperPlate"), ("PF_Grill_Elmich", "LidPivot/Handle"),
                ("PF_Grill_Elmich", "Anchors/CakePlacementPoint"), ("PF_Grill_Elmich", "Anchors/InteractionPoint"),
                ("PF_Grill_Elmich", "Anchors/SpatulaPoint"), ("PF_Grill_Elmich", "Anchors/AudioPoint"),
                ("PF_ReadyCounterPoint", "InteractionTrigger"), ("PF_ReadyCounterPoint", "DrinkPlacement"),
                ("PF_ReadyCounterPoint", "CakePlacement"), ("PF_ReadyCounterPoint", "OrderIndicator"),
                ("PF_PlasticStool", "SeatPoint"), ("PF_YellowCrateTable", "DeliveryPoint"), ("PF_YellowCrateTable", "InteractionPoint"),
            };
            foreach ((string name, string path) in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayBlockoutBuilder.PrefabPath(name));
                Assert.That(prefab.transform.Find(path), Is.Not.Null, name + "/" + path);
            }

            var grill = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayBlockoutBuilder.PrefabPath("PF_Grill_Elmich"));
            Assert.That(grill.transform.Find("LidPivot").localPosition.z,
                Is.EqualTo(BoundsOf(grill.transform.Find("Base")).max.z).Within(0.0001f));
            Assert.That(grill.transform.Find("LidPivot/Lid").GetComponent<BoxCollider>(), Is.Not.Null);
            var stool = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayBlockoutBuilder.PrefabPath("PF_PlasticStool"));
            Assert.That(Vector3.Distance(BoundsOf(stool.transform).size, new Vector3(0.3f, 0.3f, 0.3f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void ART_STALL_001_SavedScene_HasHeightReferenceCameraGroundAndRecessedBins()
        {
            GameObject[] roots = _scene.GetRootGameObjects();
            Transform reference = roots.Single(o => o.name == "ScaleReferences").transform.Find("PlayerHeightReference_1.7m");
            Assert.That(BoundsOf(reference).size.y, Is.EqualTo(1.7f).Within(0.0001f));
            Assert.That(BoundsOf(reference).min.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(roots.Single(o => o.name == "Environment").transform.Find("GroundPlane").GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(roots.SelectMany(o => o.GetComponentsInChildren<Camera>()).Count(c => c.enabled), Is.EqualTo(1));
            Assert.That(roots.SelectMany(o => o.GetComponentsInChildren<AudioListener>()).Count(), Is.EqualTo(1));
            Transform topping = _stall.transform.Find("Anchors/ToppingStationAnchor/PF_ToppingStation");
            Assert.That(BoundsOf(topping.Find("CoconutJellyBin"), topping.Find("LemonJellyBin")).max.y, Is.EqualTo(1f).Within(0.0001f));
            Transform ice = _stall.transform.Find("Anchors/IceBinAnchor/PF_IceBin");
            Assert.That(BoundsOf(ice.Find("Bin")).max.y, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ART_STALL_001_SavedScene_InstantiatesEveryRequestedPrefab()
        {
            string[] names = _scene.GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<Transform>(true))
                .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                .Select(t => t.name).ToArray();
            foreach (string name in GameplayBlockoutBuilder.EquipmentNames)
            {
                Assert.That(names, Does.Contain(name), name);
            }

            foreach (string name in GameplayBlockoutBuilder.EquipmentNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayBlockoutBuilder.PrefabPath(name));
                int layer = name == "PF_PlasticStool" || name == "PF_YellowCrateTable"
                    ? TramChanhLayers.EnvironmentIndex : TramChanhLayers.InteractableIndex;
                foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
                {
                    Assert.That(collider.gameObject.layer, Is.EqualTo(layer), name + "/" + collider.name);
                }
            }
        }

        private static void ValidateObjects(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero, child.name);
                foreach (MonoBehaviour behaviour in child.GetComponents<MonoBehaviour>())
                {
                    Assert.That(behaviour is PlaceholderAsset || behaviour is StallAnchor || behaviour is StallAnchorSet, Is.True,
                        child.name + " must not contain production gameplay");
                }
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.That(filter.sharedMesh, Is.Not.Null, filter.name);
                Assert.That(AssetDatabase.GetAssetPath(filter.sharedMesh), Is.EqualTo("Library/unity default resources"), filter.name);
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(renderer.sharedMaterials.All(m => m != null && m.shader != null), Is.True, renderer.name);
            }

            Assert.That(root.GetComponentsInChildren<MeshCollider>(true), Is.Empty);
        }

        private static Bounds BoundsOf(params Transform[] groups)
        {
            Renderer[] renderers = groups.SelectMany(g => g.GetComponentsInChildren<Renderer>()).Where(r => r.enabled).ToArray();
            Assert.That(renderers, Is.Not.Empty);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}

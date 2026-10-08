using System;
using System.Linq;
using NUnit.Framework;
using TramChanh.Core.Provisional;
using TramChanh.EditorTools.Placeholders;
using TramChanh.Stall.Anchors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace TramChanh.Tests.EditMode.Placeholders
{
    public sealed class AccelRoadsideSceneTests
    {
        [Test]
        public void ACCEL_SCENE_001_SavedEnvironmentPreservesCanonicalStallScaleAndAnchors()
        {
            GameObject environment = Environment();
            Transform stall = environment.transform.Find("StallRoot/PF_Stall_TramChanh");
            Assert.That(stall, Is.Not.Null);
            Assert.That(stall.localScale, Is.EqualTo(Vector3.one));
            Bounds bounds = BoundsOf(stall.Find("Structure"), stall.Find("Counter"), stall.Find("Frame"), stall.Find("Roof"), stall.Find("Wheels"));
            Assert.That(bounds.size.x, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(bounds.size.z, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(bounds.max.y, Is.EqualTo(2.2f).Within(0.001f));
            Assert.That(BoundsOf(stall.Find("Counter")).max.y, Is.EqualTo(1f).Within(0.001f));
            var anchors = stall.GetComponentsInChildren<StallAnchor>(true);
            Assert.That(anchors.Select(a => a.Id), Is.EquivalentTo(Enum.GetValues(typeof(StallAnchorId))));
            Assert.That(anchors.All(a => !a.PositionConfirmed), Is.True);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(stall.gameObject), Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(StallPlaceholderBuilder.StallPrefabPath)));
        }

        [Test]
        public void GT_002_ACCEL_SCENE_ContainsSingleNewSignWithReplaceableBranding()
        {
            GameObject environment = Environment();
            Transform[] all = environment.GetComponentsInChildren<Transform>(true);
            Assert.That(all.Count(t => t.name == "PF_Sign_TramChanh_New"), Is.EqualTo(1));
            Assert.That(all.Any(t => t.name.Contains("OldSign") || t.name.Contains("IlluminatedLetters")), Is.False);
            Assert.That(environment.transform.Find("StallRoot/PF_Stall_TramChanh/Sign/PF_Sign_TramChanh_New/BrandingPlaceholder"), Is.Not.Null);
            Assert.That(environment.GetComponentsInChildren<TextMesh>().Any(t => t.text == "Trạm Chanh"), Is.True);
        }

        [Test]
        public void ACCEL_SCENE_002_EnvironmentHasExplicitCompositionSeamsAndCustomerFurniture()
        {
            GameObject environment = Environment();
            foreach (string name in new[] { "StallRoot", "TablePoint", "VehiclePoint", "PlayerSpawn", "LobbyPosition", "ReadyHandoff" })
            {
                Transform anchor = environment.transform.Find(name);
                Assert.That(anchor, Is.Not.Null, name);
                Assert.That(anchor.localScale, Is.EqualTo(Vector3.one), name);
            }
            Assert.That(environment.transform.Find("TablePoint/PF_YellowCrateTable/DeliveryPoint"), Is.Not.Null);
            Assert.That(environment.GetComponentsInChildren<Transform>().Count(t => t.name == "PF_PlasticStool"), Is.GreaterThanOrEqualTo(3));
            Assert.That(environment.transform.Find("DineInCustomer"), Is.Not.Null);
            Assert.That(environment.transform.Find("VehiclePoint/GenericVehicleVisual"), Is.Not.Null);
            Assert.That(environment.transform.Find("VehiclePoint/TakeawayCustomer"), Is.Not.Null);
            Assert.That(environment.GetComponent<PlaceholderAsset>().Notes, Does.Contain("provisional"));
        }

        [Test]
        public void ACCEL_SCENE_003_WalkAndIntakeZonesHaveClearBodySpaceAboveSolidGround()
        {
            GameObject environment = Environment();
            Collider[] colliders = environment.GetComponentsInChildren<Collider>();
            foreach (string name in new[] { "PlayerSpawn", "LobbyPosition" })
            {
                Vector3 body = environment.transform.Find(name).position + Vector3.up * 0.85f;
                Bounds actor = new Bounds(body, new Vector3(0.5f, 1.65f, 0.5f));
                Assert.That(colliders.Where(c => !c.isTrigger).Any(c => c.bounds.Intersects(actor)), Is.False, name + " body clearance");
            }
            Transform sidewalk = environment.transform.Find("Roadside/Sidewalk");
            Assert.That(sidewalk.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(sidewalk.GetComponent<Renderer>().bounds.max.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(environment.transform.Find("Roadside/Street").GetComponent<BoxCollider>(), Is.Not.Null);
            // Players can pass either end of the fixed 1.8m counter and reach its worker side.
            foreach (Vector3 body in new[] { new Vector3(-1.25f, 0.85f, 0f), new Vector3(1.1f, 0.85f, -1.2f), new Vector3(0f, 0.85f, -1.2f) })
            {
                Assert.That(colliders.Where(c => !c.isTrigger).Any(c => c.bounds.Intersects(new Bounds(body, new Vector3(0.5f, 1.65f, 0.5f)))), Is.False, body.ToString());
            }
        }

        [Test]
        public void ACCEL_SCENE_004_AllVisualReferencesResolveAndEnvironmentContainsNoGameplayOrAudioListener()
        {
            GameObject environment = Environment();
            foreach (Transform child in environment.GetComponentsInChildren<Transform>(true))
            {
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero, child.name);
                foreach (MonoBehaviour behaviour in child.GetComponents<MonoBehaviour>())
                {
                    Assert.That(behaviour is PlaceholderAsset || behaviour is StallAnchor || behaviour is StallAnchorSet, Is.True, child.name);
                }
            }
            foreach (MeshFilter filter in environment.GetComponentsInChildren<MeshFilter>()) { Assert.That(filter.sharedMesh, Is.Not.Null, filter.name); }
            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>())
            {
                Assert.That(renderer.sharedMaterials.All(m => m != null && m.shader != null), Is.True, renderer.name);
            }
            Assert.That(environment.GetComponentsInChildren<MeshCollider>(), Is.Empty);
            Assert.That(environment.GetComponentsInChildren<Camera>(), Is.Empty);
            Assert.That(environment.GetComponentsInChildren<AudioListener>(), Is.Empty);
            Assert.That(environment.GetComponentsInChildren<Light>().Where(l => l.type != LightType.Directional).All(l => l.shadows == LightShadows.None), Is.True);
        }

        [Test]
        public void ACCEL_SCENE_006_CakeIntakeIsIndependentAndAccessibleFromLobby()
        {
            GameObject environment = Environment();
            Transform point = environment.transform.Find("CakeTablePoint");
            Assert.That(point, Is.Not.Null);
            Assert.That(point.localPosition, Is.EqualTo(new Vector3(-3.35f, 0f, 0.2f)));
            Assert.That(point.Find("PF_YellowCrateTable/DeliveryPoint"), Is.Not.Null);
            Assert.That(environment.transform.Find("CakeDineInCustomer/SeatedBody"), Is.Not.Null);
            Vector3 approach = point.position + new Vector3(0.7f, 0.85f, 0f);
            Bounds actor = new Bounds(approach, new Vector3(0.5f, 1.65f, 0.5f));
            Assert.That(environment.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).Any(c => c.bounds.Intersects(actor)), Is.False);
        }

        [Test]
        public void ACCEL_SCENE_005_SavedPreviewSceneHasNightLightingAndNoInventedMenuPrices()
        {
            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(AccelRoadsideSceneBuilder.ScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) { scene = EditorSceneManager.OpenScene(AccelRoadsideSceneBuilder.ScenePath, OpenSceneMode.Additive); }
            try
            {
                SceneManager.SetActiveScene(scene);
                Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
                Assert.That(RenderSettings.fog, Is.True);
                Assert.That(RenderSettings.fogColor.b, Is.GreaterThan(RenderSettings.fogColor.r));
                GameObject[] roots = scene.GetRootGameObjects();
                Assert.That(roots.Any(o => o.name == "PF_AccelRoadsideEnvironment"), Is.True);
                Assert.That(roots.SelectMany(o => o.GetComponentsInChildren<Camera>()).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(o => o.GetComponentsInChildren<AudioListener>()), Is.Empty);
                Assert.That(roots.SelectMany(o => o.GetComponentsInChildren<Light>()).Any(l => l.color.r > l.color.b && l.type == LightType.Point), Is.True);
                Assert.That(roots.SelectMany(o => o.GetComponentsInChildren<Light>()).Any(l => l.color.b > l.color.r), Is.True);
                TextMesh menu = roots.SelectMany(o => o.GetComponentsInChildren<TextMesh>()).Single(t => t.name == "MenuText");
                Assert.That(menu.text, Does.Contain("Bánh Lăn Truyền Thống"));
                Assert.That(menu.text.Any(char.IsDigit), Is.False);
                Assert.That(menu.text, Does.Not.Contain("₫"));
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded) { SceneManager.SetActiveScene(previousActive); }
                if (!wasLoaded) { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        private static GameObject Environment()
        {
            GameObject environment = AssetDatabase.LoadAssetAtPath<GameObject>(AccelRoadsideSceneBuilder.EnvironmentPrefabPath);
            Assert.That(environment, Is.Not.Null); return environment;
        }

        private static Bounds BoundsOf(params Transform[] roots)
        {
            Renderer[] renderers = roots.SelectMany(t => t.GetComponentsInChildren<Renderer>()).Where(r => r.enabled).ToArray();
            Assert.That(renderers, Is.Not.Empty);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) { bounds.Encapsulate(renderer.bounds); }
            return bounds;
        }
    }
}

#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TramChanh.App;
using TramChanh.Cakes;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Stall.Runtime;
using TramChanh.UI.Orders;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TramChanh.Tests.PlayMode.Integration
{
    /// <summary>MAIN-001: SCN_TramChanh_Main composes environment, drink + cake stations and Lobby points.</summary>
    public sealed class TramChanhMainSceneTests
    {
        private const string Path = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        private Scene _scene;
        private TramChanhMainBootstrap _bootstrap;
        private OrderEntryUI _entry;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(Path);
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out TramChanhMainBootstrap bootstrap)) { _bootstrap = bootstrap; }
                if (root.TryGetComponent(out OrderEntryUI entry)) { _entry = entry; }
            }
            Assert.That(_bootstrap, Is.Not.Null); Assert.That(_entry, Is.Not.Null);
            Assert.That(_bootstrap.IsInitialized, Is.True, "Composition runs in Awake.");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded) { yield return SceneManager.UnloadSceneAsync(_scene); }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        [UnityTest]
        public IEnumerator MAIN_001_ComposesEnvironmentStationsAndCustomerPointsWithoutMissingScripts()
        {
            Assert.That(_bootstrap.Environment, Is.Not.Null);
            Assert.That(_bootstrap.DrinkStation.GetComponentInChildren<TeaRackController>(), Is.Not.Null);
            Assert.That(_bootstrap.CakeStation.Grill, Is.Not.Null, "Cake station must be initialized.");
            ReadyCounterPoint counter = _bootstrap.DrinkStation.GetComponentInChildren<ReadyCounterPoint>();
            Assert.That(counter.DrinkPlacementPoint, Is.Not.Null); Assert.That(counter.CakePlacementPoint, Is.Not.Null);
            Assert.That(_bootstrap.DrinkStation.GetComponentInChildren<ReadyOrderPickupPoint>(), Is.Not.Null);
            Assert.That(_bootstrap.CustomerPoints.Count, Is.EqualTo(3));
            foreach (OrderPoint point in _bootstrap.CustomerPoints)
            {
                Assert.That(point.ActiveOrder.IsValid, Is.True, point.name + " should have a waiting customer.");
                Assert.That(_bootstrap.Orders.Get(point.ActiveOrder).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            }
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None), Has.Exactly(1).Matches<Camera>(camera => camera.isActiveAndEnabled));
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    foreach (Component component in child.GetComponents<Component>())
                    { Assert.That(component, Is.Not.Null, "Missing script on " + child.name); }
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MAIN_001_EveryCustomerPointIsAimableFromStandingEyeHeightWithinReach()
        {
            Physics.SyncTransforms();
            Vector3 spawn = _bootstrap.Interactor.transform.position;
            foreach (OrderPoint point in _bootstrap.CustomerPoints)
            {
                Collider trigger = point.GetComponentInChildren<Collider>();
                Assert.That(trigger, Is.Not.Null, point.name);
                Assert.That(trigger.gameObject.layer, Is.EqualTo(TramChanhLayers.InteractableIndex), point.name);
                Assert.That(trigger.bounds.max.y, Is.GreaterThan(0.4f), point.name + " trigger must not lie on the ground.");
                Vector3 towardPlayer = spawn - point.transform.position; towardPlayer.y = 0f;
                Vector3 eye = point.transform.position + towardPlayer.normalized * 1.0f + Vector3.up * 1.6f;
                Vector3 target = trigger.bounds.center;
                bool hit = Physics.Raycast(eye, (target - eye).normalized, out RaycastHit info, 1.8f, 1 << TramChanhLayers.InteractableIndex, QueryTriggerInteraction.Collide);
                Assert.That(hit, Is.True, point.name + " is not reachable from 1 m away at eye height.");
                Assert.That(info.collider.GetComponentInParent<InteractableRef>().Target, Is.SameAs(point), point.name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MAIN_001_TakeawayMixedOrderGoesThroughLobbyToBothStallQueues()
        {
            InteractionContext context = _bootstrap.Interactor.Context;
            OrderPoint vehicle = _bootstrap.Vehicle;
            OrderId order = vehicle.ActiveOrder;
            Assert.That(_bootstrap.Orders.Get(order).Items.Count, Is.EqualTo(2));
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Drink) || _bootstrap.Tickets.HasPending(ItemKind.Cake), Is.False, "No ticket before Lobby intake.");
            vehicle.Execute(context);
            Assert.That(_entry.IsOpen, Is.True);
            Assert.That(_entry.Enter(), Is.True);
            Assert.That(_entry.Send(), Is.True);
            Assert.That(_entry.IsOpen, Is.False);
            Assert.That(_bootstrap.Orders.Get(order).Status, Is.EqualTo(OrderStatus.SentToStall));
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Drink), Is.True);
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Cake), Is.True);
            TeaRackController rack = _bootstrap.DrinkStation.GetComponentInChildren<TeaRackController>();
            Assert.That(rack.Query(context).Availability.IsAvailable, Is.True);
            _bootstrap.CakeStation.Cup.Execute(context);
            Assert.That(context.Hands.Current, Is.SameAs(_bootstrap.CakeStation.Cup));
            yield return null;
        }
    }
}
#endif

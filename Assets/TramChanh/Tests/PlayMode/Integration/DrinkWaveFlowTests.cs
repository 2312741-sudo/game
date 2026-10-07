#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.App;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Drinks.Domain;
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
using Object = UnityEngine.Object;

namespace TramChanh.Tests.PlayMode.Integration
{
    public sealed class DrinkWaveFlowTests
    {
        private const string Path = "Assets/TramChanh/Scenes/Gameplay/SCN_DrinkWave.unity";
        private Scene _scene;
        private DrinkWaveBootstrap _bootstrap;
        private OrderEntryUI _entry;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(Path);
            foreach (var root in _scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out DrinkWaveBootstrap bootstrap)) { _bootstrap = bootstrap; }
                if (root.TryGetComponent(out OrderEntryUI entry)) { _entry = entry; }
            }
            Assert.That(_bootstrap, Is.Not.Null); Assert.That(_entry, Is.Not.Null);
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!_bootstrap.IsInitialized && Time.realtimeSinceStartup < deadline) { yield return null; }
            Assert.That(_bootstrap.IsInitialized, Is.True);
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded) { yield return SceneManager.UnloadSceneAsync(_scene); }
            Scene baseScene = SceneManager.GetSceneByPath("Assets/TramChanh/Scenes/Gameplay/SCN_Gameplay_Blockout.unity");
            float deadline = Time.realtimeSinceStartup + 5f;
            while (baseScene.IsValid() && baseScene.isLoaded && Time.realtimeSinceStartup < deadline) { yield return null; }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        [UnityTest] public IEnumerator TC_DRINK_009_DineInLobbyToDrinkReadyAndWholeOrderPickup() => Flow(false, false);
        [UnityTest] public IEnumerator TC_DRINK_009_VehicleLobbyToReadySurvivesThrowingObserver() => Flow(true, true);
        private IEnumerator Flow(bool vehicle, bool throwObserver)
        {
            InteractionContext context = _bootstrap.Interactor.Context;
            TeaRackController rack = _bootstrap.Station.GetComponentInChildren<TeaRackController>();
            ReadyCounterPoint counter = _bootstrap.Station.GetComponentInChildren<ReadyCounterPoint>();
            ReadyOrderPickupPoint pickup = _bootstrap.Station.GetComponentInChildren<ReadyOrderPickupPoint>();
            ToppingBin coconut = null, lemon = null;
            foreach (var bin in _bootstrap.Station.GetComponentsInChildren<ToppingBin>())
            {
                if (bin.ToppingType == ToppingType.CoconutJelly) { coconut = bin; } else { lemon = bin; }
            }
            IceBin ice = _bootstrap.Station.GetComponentInChildren<IceBin>();
            WipeInteraction wipe = _bootstrap.Station.GetComponentInChildren<WipeInteraction>();
            Assert.That(rack.Query(context).BlockedReasonKey, Is.EqualTo("stall.no_ticket.drink"));
            OrderPoint point = vehicle ? (OrderPoint)_bootstrap.Vehicle : _bootstrap.Table;
            OrderId orderId = point.ActiveOrder;
            Assert.That(_bootstrap.Orders.Get(orderId).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            point.Execute(context);
            Assert.That(_entry.IsOpen, Is.True); Assert.That(context.Clock.IsPaused, Is.True);
            Assert.That(_bootstrap.Interactor.GetComponent<TramChanh.Interaction.Player.PlayerInputReader>().enabled, Is.False);
            Assert.That(_bootstrap.Orders.Get(orderId).Status, Is.EqualTo(OrderStatus.TakingOrder));
            Assert.That(_entry.Send(), Is.True);
            Assert.That(_entry.BlockedReasonKey, Is.EqualTo("order.transition.invalid"));
            Assert.That(_entry.ReasonText, Is.Not.Empty.And.Not.EqualTo(_entry.BlockedReasonKey));
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Drink), Is.False, "Send cannot replace the separate Enter confirmation.");
            Assert.That(_entry.Enter(), Is.True);
            Assert.That(_bootstrap.Orders.Get(orderId).Status, Is.EqualTo(OrderStatus.Entered));
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Drink), Is.False);
            Assert.That(_entry.Send(), Is.True); Assert.That(_entry.IsOpen, Is.False); Assert.That(context.Clock.IsPaused, Is.False);
            Assert.That(_bootstrap.Orders.Get(orderId).Status, Is.EqualTo(OrderStatus.SentToStall));
            int stock = rack.Stock; rack.Execute(context);
            var bag = context.Hands.Current as TeaBagItem; Assert.That(bag, Is.Not.Null);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.PickedUp)); Assert.That(rack.Stock, Is.EqualTo(stock - 1));
            Assert.That(bag.BoundItem.OrderId, Is.EqualTo(orderId));
            rack.Execute(context); Assert.That(rack.Stock, Is.EqualTo(stock - 1));
            Assert.That(_bootstrap.Orders.Get(orderId).Status, Is.EqualTo(OrderStatus.InPreparation));
            var steps = new List<TeaBagState>();
            using var stepEvents = _bootstrap.Events.Subscribe<DrinkStepCompleted>(e => steps.Add(e.State));
            var driver = new InteractionActionDriver(context); driver.BeginHeld();
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Opened));
            lemon.Execute(context); Assert.That(bag.State, Is.EqualTo(TeaBagState.Opened));
            coconut.Execute(context); lemon.Execute(context); ice.Execute(context);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.IceAdded));
            counter.Execute(context); Assert.That(context.Hands.Current, Is.SameAs(bag));
            Assert.That(_bootstrap.Shelf.Occupied(ItemKind.Drink), Is.False);
            driver.BeginHeld();
            Assert.That(bag.GetComponent<TeaBagStateView>().IsShaking, Is.True);
            Assert.That(bag.GetComponent<Animator>().GetBool("Shaking"), Is.True);
            yield return CompleteHold(driver, null, bag.Recipe.ShakeHoldSeconds);
            Assert.That(bag.GetComponent<Animator>().GetBool("Shaking"), Is.False);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Shaken));
            counter.Execute(context); Assert.That(context.Hands.Current, Is.SameAs(bag));
            driver.Begin(wipe); yield return CompleteHold(driver, wipe, bag.Recipe.WipeHoldSeconds);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Wiped));
            IDisposable throwing = null;
            if (throwObserver)
            {
                LogAssert.Expect(LogType.Exception, new Regex("Integration observer fault"));
                throwing = _bootstrap.Events.Subscribe<OrderItemStatusChanged>(e => { if (e.Status == OrderItemStatus.Ready) { throw new InvalidOperationException("Integration observer fault"); } });
            }
            try { counter.Execute(context); } finally { throwing?.Dispose(); }
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Ready)); Assert.That(context.Hands.Current, Is.Null);
            Assert.That(_bootstrap.Orders.Get(orderId).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_bootstrap.Orders.Get(orderId).Items[0].Status, Is.EqualTo(OrderItemStatus.Ready));
            Assert.That(bag.transform.parent, Is.SameAs(counter.DrinkPlacementPoint));
            Assert.That(Vector3.Distance(bag.PlacementPoint.position, counter.DrinkPlacementPoint.position), Is.LessThan(0.0001f));
            Assert.That(bag.gameObject.layer, Is.EqualTo(TramChanhLayers.EnvironmentIndex));
            Assert.That(bag.GetComponent<Collider>().enabled, Is.True);
            Assert.That(bag.transform.Find("Visual/Bag_Open").gameObject.activeSelf, Is.True);
            Assert.That(bag.transform.Find("Visual/Contents/CoconutJelly").gameObject.activeSelf, Is.True);
            Assert.That(bag.transform.Find("Visual/Contents/LemonJelly").gameObject.activeSelf, Is.True);
            Assert.That(bag.transform.Find("Visual/Contents/Ice").gameObject.activeSelf, Is.True);
            Assert.That(steps, Is.EqualTo(new[] { TeaBagState.Opened, TeaBagState.CoconutJellyAdded, TeaBagState.LemonJellyAdded, TeaBagState.IceAdded, TeaBagState.Shaken, TeaBagState.Wiped }));
            pickup.Execute(context); var bundle = context.Hands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null); Assert.That(bundle.OrderId, Is.EqualTo(orderId)); Assert.That(bundle.Items[0], Is.SameAs(bag));
            Assert.That(_bootstrap.Shelf.Occupied(ItemKind.Drink), Is.False); Assert.That(counter.DrinkPlacementPoint.childCount, Is.Zero);
            Assert.That(_bootstrap.Orders.Get(orderId).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(bag.transform.IsChildOf(bundle.transform), Is.True);
        }
        [UnityTest]
        public IEnumerator TC_ORDER_UI_CancelResumesInputWithoutCreatingStallTicket()
        {
            var context = _bootstrap.Interactor.Context; _bootstrap.Table.Execute(context);
            Assert.That(_entry.IsOpen && context.Clock.IsPaused, Is.True);
            _entry.Cancel(); yield return null;
            Assert.That(_entry.IsOpen, Is.False); Assert.That(context.Clock.IsPaused, Is.False);
            Assert.That(_bootstrap.Interactor.GetComponent<TramChanh.Interaction.Player.PlayerInputReader>().enabled, Is.True);
            Assert.That(_bootstrap.Orders.Get(_bootstrap.Table.ActiveOrder).Status, Is.EqualTo(OrderStatus.TakingOrder));
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Drink), Is.False);
        }
        private static IEnumerator CompleteHold(InteractionActionDriver driver, IInteractable target, float duration)
        {
            Assert.That(driver.IsRunning, Is.True); float deadline = Time.realtimeSinceStartup + duration + 5f;
            while (driver.IsRunning && Time.realtimeSinceStartup < deadline) { driver.Tick(target); yield return null; }
            Assert.That(driver.IsRunning, Is.False, "Recipe hold must complete on the injected game clock.");
        }
    }
}
#endif

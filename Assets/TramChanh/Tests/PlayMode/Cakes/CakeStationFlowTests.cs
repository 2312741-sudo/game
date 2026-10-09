#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TramChanh.Cakes;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using TramChanh.Stall.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.PlayMode.Cakes
{
    public sealed class CakeStationFlowTests
    {
        private GameObject _stationObject, _counterObject;
        private CakeStation _station;
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private InteractionContext _context;
        private CakeRecipe _recipe;
        private OrderId _order;
        [UnitySetUp] public IEnumerator SetUp()
        {
            _recipe = AssetDatabase.LoadAssetAtPath<CakeRecipe>("Assets/TramChanh/Data/ACCEL01/Cakes/SO_CakeRecipe_DEV_TBD.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_CakeStation.prefab"); Assert.That(prefab, Is.Not.Null); Assert.That(_recipe, Is.Not.Null);
            _events = new EventBus(); _clock = new ManualClock(); var ids = new SequentialIdGenerator();
            _orders = new OrderService(_clock, _events, ids, new ContentDatabase(new[] { _recipe.ItemDefinition }));
            _queue = new StallTicketQueue(_orders); _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Stall, new HeldItemSlot(_events), _clock, _events);
            _stationObject = Object.Instantiate(prefab); _station = _stationObject.GetComponent<CakeStation>();
            _station.Initialize(_queue, ids, _events, _clock, new CakeRecipeCatalog(_queue, new[] { _recipe }));
            _counterObject = new GameObject("ReadyCounter"); var counter = _counterObject.AddComponent<ReadyCounterPoint>();
            Set(counter, "_drinkPlacementPoint", new GameObject("DrinkPlacement").transform); Set(counter, "_cakePlacementPoint", new GameObject("CakePlacement").transform);
            counter.DrinkPlacementPoint.SetParent(_counterObject.transform); counter.CakePlacementPoint.SetParent(_counterObject.transform);
            counter.Initialize(_shelf, _events); yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            var held = _context.Hands.Current as Component; if (held != null && !held.transform.IsChildOf(_stationObject.transform) && !held.transform.IsChildOf(_counterObject.transform)) { Object.Destroy(held.gameObject); }
            Object.Destroy(_stationObject); Object.Destroy(_counterObject); yield return null; _shelf.Dispose(); _events.Dispose();
        }
        [UnityTest] public IEnumerator TC_CAKE_ProductionTicketMeasurePourCookFinishReadyLeavesHands()
        {
            _station.Cup.Execute(_context); var fill = Point(CakeStationAction.Fill);
            Assert.That(fill.Query(_context).BlockedReasonKey, Is.EqualTo("stall.no_ticket.cake"));
            SendOrder(); var driver = new InteractionActionDriver(_context); driver.Begin(fill); _clock.Advance(2d); driver.Release();
            Assert.That(_station.Current.Measurement.MeasuredMl, Is.EqualTo(120f)); Assert.That(_orders.Get(_order).Status, Is.EqualTo(OrderStatus.InPreparation));
            PreparationId firstId = _station.Current.PreparationId;
            _station.Cup.ExecuteUse(_context); Assert.That(_station.Current, Is.Null); Assert.That(_queue.HasPending(ItemKind.Cake), Is.True);
            driver.Begin(fill); _clock.Advance(1d); driver.Release(); driver.Begin(fill); _clock.Advance(1d); driver.Release();
            Assert.That(_station.Current.Measurement.MeasuredMl, Is.EqualTo(120f)); Assert.That(_station.Current.PreparationId, Is.Not.EqualTo(firstId));
            var grill = Point(CakeStationAction.Grill); _station.Advance(); grill.Execute(_context); grill.Execute(_context);
            Assert.That(_context.Hands.Current, Is.Null); Assert.That(_station.Current.PouredMl, Is.EqualTo(120f));
            Assert.That(_station.Cup.transform.parent.name, Is.EqualTo("BatterArea")); Assert.That(_station.Cup.Query(_context).BlockedReasonKey, Is.EqualTo("cake.station_busy"));
            grill.Execute(_context); Assert.That(_station.Current.State, Is.EqualTo(CakeState.Cooking));
            _clock.Pause(); _clock.Advance(100d); _station.Advance(); Assert.That(_station.Current.Doneness, Is.Zero); _clock.Resume();
            _clock.Advance(_recipe.CookedThreshold); _station.Advance(); Assert.That(_station.Current.State, Is.EqualTo(CakeState.Cooked));
            grill.Execute(_context); grill.Execute(_context); Assert.That(_station.Current.State, Is.EqualTo(CakeState.Flipped)); Assert.That(_station.CurrentItem.transform.parent.name, Is.EqualTo("RollArea"));
            var roll = Point(CakeStationAction.RollArea); Hold(driver, roll, _recipe.CutHoldSeconds); Assert.That(_station.Current.State, Is.EqualTo(CakeState.Cut));
            var sauce = Point(CakeStationAction.Sauce); Hold(driver, sauce, _recipe.SauceHoldSeconds); Hold(driver, roll, _recipe.RollHoldSeconds);
            CakeItem item = _station.CurrentItem; Assert.That(item.Preparation.State, Is.EqualTo(CakeState.Rolled));
            Point(CakeStationAction.Wrap).Execute(_context); Assert.That(_context.Hands.Current, Is.SameAs(item)); Assert.That(item.IsFinished, Is.True);
            Assert.That(_station.Current, Is.Null); Assert.That(item.transform.Find("SM_Cake_RolledVertical").gameObject.activeSelf, Is.True);
            bool committed = false; using var subscription = _events.Subscribe<OrderItemStatusChanged>(message =>
            { if (message.Status == OrderItemStatus.Ready) { committed = _shelf.Occupied(ItemKind.Cake) && item.Preparation.State == CakeState.Ready && _orders.Get(_order).Status == OrderStatus.Ready; } });
            var counter = _counterObject.GetComponent<ReadyCounterPoint>(); counter.Execute(_context);
            Assert.That(committed, Is.True); Assert.That(_context.Hands.Current, Is.Null); Assert.That(item.transform.parent, Is.EqualTo(counter.CakePlacementPoint));
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(_order)); yield return null;
        }
        [UnityTest] public IEnumerator TC_CAKE_ContinuousFillCapsCapacityAndChangedHandsCannotFill()
        {
            SendOrder(); _station.Cup.Execute(_context); var driver = new InteractionActionDriver(_context); var fill = Point(CakeStationAction.Fill);
            driver.Begin(fill); _clock.Advance(100d); driver.Release(); Assert.That(_station.Current.Measurement.MeasuredMl, Is.EqualTo(500f));
            _station.Cup.ExecuteUse(_context); driver.Begin(fill); _clock.Advance(1d); _context.Hands.TryRelease(); driver.Release();
            Assert.That(_station.Current, Is.Null); yield return null;
        }
        [UnityTest] public IEnumerator TC_CAKE_InvalidSequenceAndStaleBindingDoNotMutate()
        {
            SendOrder(); _station.Cup.Execute(_context); var driver = new InteractionActionDriver(_context); var fill = Point(CakeStationAction.Fill); driver.Begin(fill); _clock.Advance(2d); driver.Release();
            Assert.That(Point(CakeStationAction.Sauce).Query(_context).Availability.IsAvailable, Is.False); Assert.That(Point(CakeStationAction.Wrap).Query(_context).Availability.IsAvailable, Is.False);
            _orders.Fail(_order, FailureReason.CancelledByDebug); Assert.That(fill.Query(_context).BlockedReasonKey, Is.EqualTo("ready.no_order"));
            float before = _station.Current.Measurement.MeasuredMl; fill.OnHoldStarted(_context); fill.OnContinuousReleased(_context, 2f); Assert.That(_station.Current.Measurement.MeasuredMl, Is.EqualTo(before));
            _station.Advance(); Assert.That(_station.Current, Is.Null); Assert.That(_station.Grill.CakeOnPlate, Is.Null); Assert.That(fill.Query(_context).BlockedReasonKey, Is.EqualTo("stall.no_ticket.cake"));
            SendOrder(); Assert.That(_station.Measure(_context, 2f).IsSuccess, Is.True); yield return null;
        }
        [UnityTest] public IEnumerator TC_CAKE_BurnRemovalRequeuesAndDiscardReleasesHands()
        {
            SendOrder(); _station.Cup.Execute(_context); Assert.That(_station.Measure(_context, 2f).IsSuccess, Is.True);
            _clock.Advance(2d); _station.Advance(); var grill = Point(CakeStationAction.Grill); grill.Execute(_context); grill.Execute(_context); grill.Execute(_context);
            _clock.Advance(_recipe.BurnThreshold); _station.Advance(); Assert.That(_station.Current.State, Is.EqualTo(CakeState.Ruined));
            grill.Execute(_context); grill.Execute(_context); var burnt = _context.Hands.Current as CakeItem;
            Assert.That(burnt, Is.Not.Null); Assert.That(burnt.IsFinished, Is.False); Assert.That(_station.Current, Is.Null); Assert.That(_queue.HasPending(ItemKind.Cake), Is.True);
            burnt.ExecuteUse(_context); Assert.That(_context.Hands.Current, Is.Null); _station.Cup.Execute(_context);
            Assert.That(_station.Measure(_context, 2f).IsSuccess, Is.True); yield return null;
        }
        [UnityTest] public IEnumerator TC_CAKE_StepEventsPublishOncePerCanonicalStepAndNeverForLidOrTopUp()
        {
            var published = new System.Collections.Generic.List<CakeState>();
            using var steps = _events.Subscribe<CakeStepCompleted>(message => published.Add(message.State));
            SendOrder(); var driver = new InteractionActionDriver(_context); CakeItem item = DriveToWrapped(driver, true);
            Assert.That(_context.Hands.Current, Is.SameAs(item));
            Assert.That(published, Is.EqualTo(new[] { CakeState.BatterMeasured, CakeState.BatterPoured, CakeState.Cooking, CakeState.Cooked,
                CakeState.Flipped, CakeState.Cut, CakeState.Sauced, CakeState.Rolled, CakeState.Wrapped }));
            yield return null;
        }
        [UnityTest] public IEnumerator TC_CAKE_WrappedCakeOfFailedOrderCanBeDiscardedFromHands()
        {
            SendOrder(); var driver = new InteractionActionDriver(_context); CakeItem item = DriveToWrapped(driver, false);
            Assert.That(item.QueryUse(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            _orders.Fail(_order, FailureReason.CancelledByDebug);
            var counter = _counterObject.GetComponent<ReadyCounterPoint>();
            Assert.That(counter.Query(_context).BlockedReasonKey, Is.EqualTo("ready.no_order"));
            InteractionQuery discard = item.QueryUse(_context);
            Assert.That(discard.Availability.IsAvailable, Is.True); Assert.That(discard.PromptKey, Is.EqualTo("cake.discard_orphan"));
            _clock.Pause(); Assert.That(item.QueryUse(_context).BlockedReasonKey, Is.EqualTo("game.paused")); _clock.Resume();
            driver.BeginHeld(); Assert.That(_context.Hands.Current, Is.Null); yield return null; Assert.That(item == null, Is.True);
            SendOrder(); _station.Cup.Execute(_context); Assert.That(_station.Measure(_context, 2f).IsSuccess, Is.True); yield return null;
        }
        [UnityTest] public IEnumerator TC_CAKE_OccupiedReadyCakeSlotBlocksNewClaimUntilCleared()
        {
            SendOrder(); OrderId first = _order; var driver = new InteractionActionDriver(_context); DriveToWrapped(driver, false);
            _counterObject.GetComponent<ReadyCounterPoint>().Execute(_context); Assert.That(_shelf.Occupied(ItemKind.Cake), Is.True); Assert.That(_context.Hands.Current, Is.Null);
            var gatedObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_CakeStation.prefab"));
            try
            {
                var gated = gatedObject.GetComponent<CakeStation>();
                gated.Initialize(_queue, new SequentialIdGenerator(), _events, _clock, new CakeRecipeCatalog(_queue, new[] { _recipe }), _shelf);
                SendOrder(2); OrderId second = _order; gated.Cup.Execute(_context);
                var fill = gated.GetComponentsInChildren<CakeStationPoint>().Single(point => point.Action == CakeStationAction.Fill);
                Assert.That(fill.Query(_context).BlockedReasonKey, Is.EqualTo("ready.slot_full"));
                driver.Begin(fill); _clock.Advance(2d); driver.Release();
                Assert.That(gated.Measure(_context, 2f).ReasonKey, Is.EqualTo("ready.slot_full"));
                Assert.That(gated.Current, Is.Null); Assert.That(_queue.HasPending(ItemKind.Cake), Is.True); Assert.That(_orders.Get(second).Status, Is.EqualTo(OrderStatus.SentToStall));
                Assert.That(_shelf.PickUp(first, new ActorRef(1)).IsSuccess, Is.True); Assert.That(_shelf.Occupied(ItemKind.Cake), Is.False);
                Assert.That(fill.Query(_context).Availability.IsAvailable, Is.True);
                driver.Begin(fill); _clock.Advance(2d); driver.Release();
                Assert.That(gated.Current, Is.Not.Null); Assert.That(gated.Current.BoundItem.OrderId, Is.EqualTo(second)); Assert.That(_orders.Get(second).Status, Is.EqualTo(OrderStatus.InPreparation));
                gated.Cup.ExecuteUse(_context); _context.Hands.TryRelease();
            }
            finally { Object.Destroy(gatedObject); }
            yield return null;
        }
        [UnityTest] public IEnumerator TC_CAKE_ReadyGateDoesNotStopCakeAlreadyInProgress()
        {
            var gate = new ToggleGate(); var gatedObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_CakeStation.prefab"));
            try
            {
                var gated = gatedObject.GetComponent<CakeStation>();
                gated.Initialize(_queue, new SequentialIdGenerator(), _events, _clock, new CakeRecipeCatalog(_queue, new[] { _recipe }), gate);
                SendOrder(); gated.Cup.Execute(_context);
                var fill = gated.GetComponentsInChildren<CakeStationPoint>().Single(point => point.Action == CakeStationAction.Fill);
                Assert.That(gated.Measure(_context, 1f).IsSuccess, Is.True); gate.IsOccupied = true;
                Assert.That(fill.Query(_context).Availability.IsAvailable, Is.True); Assert.That(gated.Measure(_context, 1f).IsSuccess, Is.True);
                Assert.That(gated.Current.Measurement.MeasuredMl, Is.EqualTo(120f));
                gated.Cup.ExecuteUse(_context); Assert.That(gated.Current, Is.Null);
                Assert.That(fill.Query(_context).BlockedReasonKey, Is.EqualTo("ready.slot_full")); _context.Hands.TryRelease();
            }
            finally { Object.Destroy(gatedObject); }
            yield return null;
        }
        private sealed class ToggleGate : IReadyShelfPlacement
        {
            public bool IsOccupied;
            public Availability CanPlace(IPreparedItem item) => Availability.Blocked("test.gate");
            public Result PlaceReady(IPreparedItem item) => Result.Fail("test.gate");
            public bool Occupied(ItemKind kind) => IsOccupied && kind == ItemKind.Cake;
        }
        private CakeItem DriveToWrapped(InteractionActionDriver driver, bool topUp)
        {
            _station.Cup.Execute(_context); var fill = Point(CakeStationAction.Fill);
            if (topUp) { driver.Begin(fill); _clock.Advance(1d); driver.Release(); driver.Begin(fill); _clock.Advance(1d); driver.Release(); }
            else { driver.Begin(fill); _clock.Advance(2d); driver.Release(); }
            var grill = Point(CakeStationAction.Grill); _clock.Advance(2d); _station.Advance();
            grill.Execute(_context); grill.Execute(_context); grill.Execute(_context); Assert.That(_station.Current.State, Is.EqualTo(CakeState.Cooking));
            _clock.Advance(_recipe.CookedThreshold); _station.Advance(); grill.Execute(_context); grill.Execute(_context);
            Assert.That(_station.Current.State, Is.EqualTo(CakeState.Flipped));
            var roll = Point(CakeStationAction.RollArea); Hold(driver, roll, _recipe.CutHoldSeconds); Hold(driver, Point(CakeStationAction.Sauce), _recipe.SauceHoldSeconds); Hold(driver, roll, _recipe.RollHoldSeconds);
            CakeItem item = _station.CurrentItem; Point(CakeStationAction.Wrap).Execute(_context);
            Assert.That(item.Preparation.State, Is.EqualTo(CakeState.Wrapped)); Assert.That(_context.Hands.Current, Is.SameAs(item)); return item;
        }
        private void SendOrder(int table = 1)
        {
            var items = new[] { new ItemRequest(_recipe.ItemDefinition.Id, 1) }; var origin = OrderOrigin.ForTable(new TableId(table));
            _order = _orders.RequestService(origin, new CustomerId(1), items).Value; Assert.That(_orders.BeginTaking(_order, new ActorRef(1), origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(_order, items).IsSuccess, Is.True); Assert.That(_orders.SendToStall(_order).IsSuccess, Is.True);
        }
        private CakeStationPoint Point(CakeStationAction action) => _station.GetComponentsInChildren<CakeStationPoint>().Single(point => point.Action == action);
        private void Hold(InteractionActionDriver driver, CakeStationPoint point, float seconds) { driver.Begin(point); _clock.Advance(seconds); driver.Tick(point); }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
#endif

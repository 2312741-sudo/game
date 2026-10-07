using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Stall.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TramChanh.Tests.EditMode.Ready
{
    /// <summary>Ready adapters against the real OrderService, StallTicketQueue and ReadyShelf (no shelf fake).</summary>
    public sealed class ReadyRealServiceTests
    {
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private GameObject _root;
        private ReadyCounterPoint _counter;
        private ReadyOrderPickupPoint _pickup;
        private Transform _drinkPlacement;
        private Transform _cakePlacement;
        private int _preparation;
        private int _expectedFaults;

        private void Build(int capacity)
        {
            _events = new EventBus();
            _clock = new ManualClock();
            _hands = new HeldItemSlot(_events);
            var content = new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake),
            });
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), content, capacity, capacity);
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, capacity, capacity);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Stall | ActorRole.Lobby, _hands, _clock, _events);
            _root = new GameObject("Ready real-service test");
            _drinkPlacement = Child("DrinkPlacement").transform;
            _cakePlacement = Child("CakePlacement").transform;
            _counter = Child("ReadyCounter").AddComponent<ReadyCounterPoint>();
            Set(_counter, "_id", 41);
            Set(_counter, "_interactionPoint", _counter.transform);
            Set(_counter, "_drinkPlacementPoint", _drinkPlacement);
            Set(_counter, "_cakePlacementPoint", _cakePlacement);
            _counter.Initialize(_shelf, _events);
            _pickup = Child("ReadyPickup").AddComponent<ReadyOrderPickupPoint>();
            Set(_pickup, "_id", 42);
            Set(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf);
        }

        [SetUp] public void SetUp() => Build(1);

        [TearDown]
        public void TearDown()
        {
            Assert.That(_orders.ObserverFaults.Count, Is.EqualTo(_expectedFaults), "Unexpected observer fault count");
            _expectedFaults = 0;
            _shelf.Dispose();
            UnityEngine.Object.DestroyImmediate(_root);
            _events.Dispose();
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            return child;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private OrderId NewOrder(int table, params string[] items)
        {
            var requests = new List<ItemRequest>();
            foreach (string item in items) { requests.Add(new ItemRequest(item, 1)); }
            OrderOrigin origin = OrderOrigin.ForTable(new TableId(table));
            OrderId id = _orders.RequestService(origin, new CustomerId(table), requests).Value;
            Assert.That(_orders.BeginTaking(id, new ActorRef(1), origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, requests).IsSuccess, Is.True);
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            return id;
        }

        private Bag NewBag(ItemKind kind)
        {
            var bag = Child(kind + "Bag").AddComponent<Bag>();
            bag.Kind = kind;
            bag.BoundItem = _queue.ClaimNext(kind, new PreparationId(++_preparation)).Value;
            bag.gameObject.AddComponent<BoxCollider>();
            var anchors = new GameObject("Anchors");
            anchors.transform.SetParent(bag.transform, false);
            var point = new GameObject("PlacementPoint");
            point.transform.SetParent(anchors.transform, false);
            return bag;
        }

        private void PlaceFromHand(Bag bag)
        {
            Assert.That(_hands.TryPickUp(bag), Is.True);
            _counter.Execute(_context);
        }

        [Test]
        public void B3_TwoItemOrderIsCarriedAsOneBundleInAscendingItemIdOrder()
        {
            OrderId id = NewOrder(1, "cake", "drink");
            Bag cake = NewBag(ItemKind.Cake);
            Bag drink = NewBag(ItemKind.Drink);
            PlaceFromHand(drink);
            PlaceFromHand(cake);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
            _pickup.Execute(_context);
            var bundle = _hands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null);
            Assert.That(bundle.OrderId, Is.EqualTo(id));
            Assert.That(bundle.Items, Is.EqualTo(new IPreparedItem[] { cake, drink }), "Cake was entered first, so it has the lower OrderItemId.");
            Assert.That(bundle.Items[0].BoundItem.OrderItemId.Value, Is.LessThan(bundle.Items[1].BoundItem.OrderItemId.Value));
            Assert.That(cake.transform.parent, Is.SameAs(bundle.transform));
            Assert.That(drink.transform.parent, Is.SameAs(bundle.transform));
            Assert.That(_drinkPlacement.childCount + _cakePlacement.childCount, Is.Zero);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(_shelf.Occupied(ItemKind.Drink) || _shelf.Occupied(ItemKind.Cake), Is.False);
        }

        [Test]
        public void B3_PartialOrderCannotBePickedUp()
        {
            NewOrder(1, "cake", "drink");
            PlaceFromHand(NewBag(ItemKind.Drink));
            Assert.That(_shelf.NextReadyOrder.IsValid, Is.False);
            Assert.That(_pickup.Query(_context).Availability.IsAvailable, Is.False);
        }

        [Test]
        public void B3_CapacityTwoShelfHandsOutTheOldestReadyOrderFirst()
        {
            TearDown();
            Build(2);
            OrderId first = NewOrder(1, "drink");
            OrderId second = NewOrder(2, "drink");
            Bag firstBag = NewBag(ItemKind.Drink);
            Bag secondBag = NewBag(ItemKind.Drink);
            _clock.Advance(1);
            PlaceFromHand(firstBag);
            _clock.Advance(1);
            PlaceFromHand(secondBag);
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(first));
            Assert.That(_shelf.PickUp(second, new ActorRef(1)).ReasonKey, Is.EqualTo("ready.no_complete_order"), "A newer Ready order cannot jump the queue.");
            _pickup.Execute(_context);
            var bundle = _hands.Current as ServedOrder;
            Assert.That(bundle.OrderId, Is.EqualTo(first));
            Assert.That(bundle.Items, Is.EqualTo(new IPreparedItem[] { firstBag }));
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(second));
            Assert.That(secondBag.transform.parent, Is.SameAs(_drinkPlacement), "The newer order's product stays on the counter.");
            Assert.That(_drinkPlacement.childCount, Is.EqualTo(1));
            Assert.That(_hands.TryRelease(), Is.True);
            _pickup.Execute(_context);
            Assert.That(((ServedOrder)_hands.Current).OrderId, Is.EqualTo(second));
        }

        [Test]
        public void B1_ThrowingHeldItemChangedListenerStillPresentsTheCommittedPlacement()
        {
            OrderId id = NewOrder(1, "drink");
            Bag drink = NewBag(ItemKind.Drink);
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null) { throw new InvalidOperationException("view fault"); } });
            LogAssert.Expect(LogType.Exception, new Regex("view fault"));
            PlaceFromHand(drink);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.True);
            Assert.That(_hands.Current, Is.Null);
            Assert.That(drink.transform.parent, Is.SameAs(_drinkPlacement));
            Assert.That(drink.gameObject.layer, Is.EqualTo(TramChanhLayers.EnvironmentIndex));
            _hands.TryRelease();
            _pickup.Execute(_context);
            Assert.That(((ServedOrder)_hands.Current).Items, Is.EqualTo(new IPreparedItem[] { drink }), "The product is still tracked and can be picked up.");
        }

        [Test]
        public void B1_FaultDuringPlacementFlushAndFaultDuringReleaseBothLeaveAConsistentCounter()
        {
            OrderId id = NewOrder(1, "drink");
            Bag drink = NewBag(ItemKind.Drink);
            _events.Subscribe<OrderStatusChanged>(change => { if (change.Status == OrderStatus.Ready) { throw new InvalidOperationException("order fault"); } });
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null) { throw new InvalidOperationException("view fault"); } });
            LogAssert.Expect(LogType.Exception, new Regex("view fault"));
            PlaceFromHand(drink);
            Assert.That(_orders.ObserverFaults.Count, Is.EqualTo(1), "The order observer fault is reported to the service, not thrown.");
            Assert.That(drink.transform.parent, Is.SameAs(_drinkPlacement));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
            _expectedFaults = 1;
        }

        [Test]
        public void B2_RealLobbyPickupInsideThePlacementFlushKeepsTheItemInTheBundle()
        {
            OrderId id = NewOrder(1, "drink");
            Bag drink = NewBag(ItemKind.Drink);
            var lobbyHands = new HeldItemSlot(_events);
            var lobby = new InteractionContext(new ActorRef(2), ActorRole.Lobby, lobbyHands, _clock, _events);
            _events.Subscribe<OrderStatusChanged>(change => { if (change.Status == OrderStatus.Ready) { _pickup.Execute(lobby); } });
            PlaceFromHand(drink);
            var bundle = lobbyHands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null);
            Assert.That(bundle.Items, Is.EqualTo(new IPreparedItem[] { drink }));
            Assert.That(drink.transform.parent, Is.SameAs(bundle.transform), "Placement must not unparent an item the bundle adopted.");
            Assert.That(drink.gameObject.activeSelf, Is.True);
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_drinkPlacement.childCount, Is.Zero);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
        }

        [Test]
        public void B1_FailureInsideTheHandReleaseCallbackDiscardsTheProductInsteadOfPresentingIt()
        {
            OrderId id = NewOrder(1, "drink");
            Bag drink = NewBag(ItemKind.Drink);
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null && change.Previous == drink) { _orders.Fail(id, FailureReason.CustomerLeft); } });
            PlaceFromHand(drink);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Failed));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_drinkPlacement.childCount, Is.Zero, "A failed product is never presented on the counter.");
            Assert.That(drink == null || !drink.gameObject.activeSelf, Is.True);
            // The adapter is clean afterwards: the next order places normally.
            OrderId next = NewOrder(2, "drink");
            Bag second = NewBag(ItemKind.Drink);
            PlaceFromHand(second);
            Assert.That(_orders.Get(next).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(second.transform.parent, Is.SameAs(_drinkPlacement));
        }

        [Test]
        public void B2_RealLobbyPickupInsideTheHandReleaseCallbackKeepsTheBundleParent()
        {
            OrderId id = NewOrder(1, "drink");
            Bag drink = NewBag(ItemKind.Drink);
            var lobbyHands = new HeldItemSlot(_events);
            var lobby = new InteractionContext(new ActorRef(2), ActorRole.Lobby, lobbyHands, _clock, _events);
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null && change.Previous == drink) { _pickup.Execute(lobby); } });
            PlaceFromHand(drink);
            var bundle = lobbyHands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null, "The Lobby pickup committed during the hand release.");
            Assert.That(bundle.Items, Is.EqualTo(new IPreparedItem[] { drink }));
            Assert.That(drink.transform.parent, Is.SameAs(bundle.transform), "The newly adopted item is not re-parented onto Ready.");
            Assert.That(drink.gameObject.layer, Is.EqualTo(TramChanhLayers.HeldItemIndex));
            Assert.That(_drinkPlacement.childCount, Is.Zero);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(_hands.Current, Is.Null);
        }

        [Test]
        public void ReentrantFailureStillDiscardsTheItemAndFreesTheSlot()
        {
            OrderId id = NewOrder(1, "drink");
            Bag drink = NewBag(ItemKind.Drink);
            _events.Subscribe<OrderItemStatusChanged>(change => { if (change.Status == OrderItemStatus.Ready) { _orders.Fail(id, FailureReason.CustomerLeft); } });
            PlaceFromHand(drink);
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
            Assert.That(_drinkPlacement.childCount, Is.Zero);
            Assert.That(drink == null || !drink.gameObject.activeSelf, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Failed));
        }

        public sealed class Bag : MonoBehaviour, IPreparedItem, IHoldable
        {
            public ItemKind Kind { get; set; }
            public OrderItemRef BoundItem { get; set; }
            public bool IsFinished => true;
            public int Quality => 100;
            public bool Ready;
            public Transform HandGrip => transform;
            public Result MarkReady()
            {
                if (Ready) { return Result.Fail("ready.already_ready"); }
                Ready = true;
                return Result.Success();
            }
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }
    }
}

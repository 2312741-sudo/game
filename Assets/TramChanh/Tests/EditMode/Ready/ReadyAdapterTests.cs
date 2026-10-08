using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Stall.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TramChanh.Tests.EditMode.Ready
{
    public sealed class ReadyAdapterTests
    {
        private GameObject _root;
        private EventBus _events;
        private ManualClock _clock;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private Shelf _shelf;
        private ReadyCounterPoint _counter;
        private ReadyOrderPickupPoint _pickup;
        private Transform _drinkPlacement;
        private Transform _cakePlacement;
        private PreparedHoldable _item;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Ready adapter test");
            _events = new EventBus();
            _clock = new ManualClock();
            _hands = new HeldItemSlot(_events);
            _context = Context(_hands, ActorRole.Stall | ActorRole.Lobby);
            _shelf = new Shelf(_events);
            _counter = Child("ReadyCounter").AddComponent<ReadyCounterPoint>();
            _drinkPlacement = Child("DrinkPlacement").transform;
            _cakePlacement = Child("CakePlacement").transform;
            Configure(_counter, "_id", 31);
            Configure(_counter, "_interactionPoint", _counter.transform);
            Configure(_counter, "_drinkPlacementPoint", _drinkPlacement);
            Configure(_counter, "_cakePlacementPoint", _cakePlacement);
            _counter.Initialize(_shelf, _events);
            _pickup = Child("LobbyPickup").AddComponent<ReadyOrderPickupPoint>();
            Configure(_pickup, "_id", 32);
            Configure(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf);
            _item = Child("PreparedDrink").AddComponent<PreparedHoldable>();
            _item.Initialize(ItemKind.Drink);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hands.Current is Component held && held.transform.IsChildOf(_root.transform) == false)
            {
                UnityEngine.Object.DestroyImmediate(held.gameObject);
            }
            UnityEngine.Object.DestroyImmediate(_root);
            if (_item != null)
            {
                UnityEngine.Object.DestroyImmediate(_item.gameObject);
            }
            _events.Dispose();
        }

        [TestCase(ItemKind.Drink)]
        [TestCase(ItemKind.Cake)]
        public void TC_ORDER_006_ReadyPlacementCommitsThenReleasesAndRestoresPresentation(ItemKind kind)
        {
            _item.Initialize(kind);
            _hands.TryPickUp(_item);
            _item.gameObject.layer = TramChanhLayers.HeldItemIndex;
            _item.GetComponent<Collider>().enabled = false;
            _counter.Execute(_context);
            Assert.That(_shelf.PlacementCalls, Is.EqualTo(1));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_item.Ready, Is.True);
            Assert.That(_item.transform.parent, Is.SameAs(kind == ItemKind.Drink ? _drinkPlacement : _cakePlacement));
            Assert.That(_item.gameObject.layer, Is.Not.EqualTo(TramChanhLayers.HeldItemIndex));
            Assert.That(_item.GetComponent<Collider>().enabled, Is.True);
            Transform placementPoint = _item.transform.Find("Anchors/PlacementPoint");
            Assert.That(Vector3.Distance(placementPoint.position, _item.transform.parent.position), Is.LessThan(0.0001f));
        }

        [Test]
        public void TC_ORDER_006_FailedPlacementKeepsHandsAndVisualsUnchanged()
        {
            _hands.TryPickUp(_item);
            Transform parent = _item.transform.parent;
            _shelf.PlacementFailure = true;
            _counter.Execute(_context);
            Assert.That(_hands.Current, Is.SameAs(_item));
            Assert.That(_item.Ready, Is.False);
            Assert.That(_item.transform.parent, Is.SameAs(parent));
        }

        [Test]
        public void TC_ORDER_006_PlacementRequiresStallRoleAndUnpausedClock()
        {
            _hands.TryPickUp(_item);
            _counter.Execute(Context(_hands, ActorRole.Lobby));
            _clock.Pause();
            _counter.Execute(_context);
            Assert.That(_shelf.PlacementCalls, Is.Zero);
            Assert.That(_hands.Current, Is.SameAs(_item));
        }

        [Test]
        public void TC_ORDER_006_ShelfPickupEventClearsCounterWithoutDestroyingPreparedItem()
        {
            _hands.TryPickUp(_item);
            _counter.Execute(_context);
            _events.Publish(new OrderStatusChanged(_item.BoundItem.OrderId, OrderStatus.PickedUpByLobby));
            Assert.That(_item != null, Is.True);
            Assert.That(_item.transform.parent, Is.Null);
            Assert.That(_item.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void TC_ORDER_007_LobbyPickupCarriesBoundBundleAndEmptiesCounter()
        {
            _hands.TryPickUp(_item);
            _counter.Execute(_context);
            _pickup.Execute(_context);
            Assert.That(_hands.Current, Is.TypeOf<ServedOrder>());
            var bundle = (ServedOrder)_hands.Current;
            Assert.That(bundle.OrderId, Is.EqualTo(_item.BoundItem.OrderId));
            Assert.That(bundle.Items.Count, Is.EqualTo(1));
            Assert.That(bundle.Items[0], Is.SameAs(_item));
            Assert.That(bundle.HandGrip, Is.Not.Null);
            Assert.That(_item.transform.parent, Is.SameAs(bundle.transform));
            Assert.That(_item.GetComponent<Collider>().enabled, Is.False);
            Assert.That(_shelf.NextReadyOrder.IsValid, Is.False);
        }

        [Test]
        public void TC_ORDER_007_RefusingHandsNeverConsumesReadyShelf()
        {
            _shelf.Stored = _item;
            _pickup.Execute(Context(new RefusingHands(), ActorRole.Lobby));
            Assert.That(_shelf.PickupCalls, Is.Zero);
            Assert.That(_shelf.NextReadyOrder.IsValid, Is.True);
            Assert.That(_shelf.DomainEvents, Is.Zero);
        }

        [Test]
        public void TC_ORDER_007_ReservationChangedByListenerNeverConsumesShelf()
        {
            _shelf.Stored = _item;
            using (_events.Subscribe<HeldItemChanged>(change =>
            {
                if (change.Current is ServedOrder)
                {
                    _hands.TryRelease();
                }
            }))
            {
                _pickup.Execute(_context);
            }
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_shelf.PickupCalls, Is.Zero);
            Assert.That(_shelf.DomainEvents, Is.Zero);
        }

        [Test]
        public void TC_ORDER_007_ShelfFailureReleasesEmptyReservationWithoutDomainEvents()
        {
            _hands.TryPickUp(_item);
            _counter.Execute(_context);
            _shelf.PickupFailure = true;
            _shelf.DomainEvents = 0;
            _pickup.Execute(_context);
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_shelf.NextReadyOrder.IsValid, Is.True);
            Assert.That(_item.transform.parent, Is.SameAs(_drinkPlacement));
            Assert.That(_shelf.DomainEvents, Is.Zero);
        }

        [Test]
        public void TC_ORDER_007_PickupRequiresLobbyRoleEmptyHandsAndUnpausedClock()
        {
            _shelf.Stored = _item;
            _pickup.Execute(Context(_hands, ActorRole.Stall));
            _hands.TryPickUp(_item);
            _pickup.Execute(_context);
            _hands.TryRelease();
            _clock.Pause();
            _pickup.Execute(_context);
            Assert.That(_shelf.PickupCalls, Is.Zero);
        }

        [Test]
        public void TC_INT_007_ReadyAdapterQueriesAllocateNothingAndDoNotMutate()
        {
            _hands.TryPickUp(_item);
            _counter.Query(_context);
            _pickup.Query(_context);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                _counter.Query(_context);
                _pickup.Query(_context);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            Assert.That(_shelf.PlacementCalls, Is.Zero);
            Assert.That(_shelf.PickupCalls, Is.Zero);
            Assert.That(_item.Ready, Is.False);
        }

        [Test]
        public void TC_READY_009_ReentrantFailureDoesNotLeaveStaleReadyVisual()
        {
            _shelf.FailDuringPlacement = true; _hands.TryPickUp(_item);
            _counter.Execute(_context);
            Assert.That(_hands.Current, Is.Null); Assert.That(_shelf.Stored, Is.Null);
            Assert.That(_counter.DrinkPlacementPoint.childCount, Is.Zero);
            Assert.That(_item == null || !_item.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void B1_ThrowingHeldItemChangedListenerDoesNotSkipPresentationAfterCommit()
        {
            _hands.TryPickUp(_item);
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null) { throw new InvalidOperationException("view fault"); } });
            LogAssert.Expect(LogType.Exception, new Regex("view fault"));
            Assert.DoesNotThrow(() => _counter.Execute(_context));
            Assert.That(_shelf.Stored, Is.SameAs(_item), "The committed placement is not undone.");
            Assert.That(_hands.Current, Is.Null, "The hand result is the slot's actual state.");
            Assert.That(_item.transform.parent, Is.SameAs(_drinkPlacement), "Presentation still ran.");
            Assert.That(_item.gameObject.layer, Is.EqualTo(TramChanhLayers.EnvironmentIndex));
            Assert.That(_item.GetComponent<Collider>().enabled, Is.True);
        }

        [Test]
        public void B1_HandThatStillHoldsTheItemAfterAFaultedReleaseIsReportedAndPresentationStillRuns()
        {
            var stuck = new StuckHands(_item);
            LogAssert.Expect(LogType.Exception, new Regex("release fault"));
            LogAssert.Expect(LogType.Error, new Regex("did not release"));
            _counter.Execute(Context(stuck, ActorRole.Stall));
            Assert.That(_shelf.Stored, Is.SameAs(_item));
            Assert.That(_item.transform.parent, Is.SameAs(_drinkPlacement));
            Assert.That(stuck.Current, Is.SameAs(_item), "The adapter reports the invariant break instead of hiding it.");
        }

        [Test]
        public void B1_ReleaseAlreadyDoneByAListenerIsNotRepeated()
        {
            _hands.TryPickUp(_item);
            int releases = 0;
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null) { releases++; } });
            _shelf.OnPlaced = () => _hands.TryRelease();
            _counter.Execute(_context);
            Assert.That(releases, Is.EqualTo(1));
            Assert.That(_item.transform.parent, Is.SameAs(_drinkPlacement));
        }

        [Test]
        public void B2_LobbyPickupInsideThePlacementFlushKeepsTheItemInTheBundle()
        {
            _hands.TryPickUp(_item);
            var lobbyHands = new HeldItemSlot(_events);
            InteractionContext lobby = new InteractionContext(new ActorRef(2), ActorRole.Lobby, lobbyHands, _clock, _events);
            _shelf.OnPlaced = () => _pickup.Execute(lobby);
            _counter.Execute(_context);
            var bundle = lobbyHands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null, "The Lobby pickup committed during the flush.");
            Assert.That(bundle.Items, Is.EqualTo(new IPreparedItem[] { _item }));
            Assert.That(_item.transform.parent, Is.SameAs(bundle.transform), "The adopted item is never unparented.");
            Assert.That(_item.gameObject.layer, Is.EqualTo(TramChanhLayers.HeldItemIndex));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_drinkPlacement.childCount, Is.Zero);
            Assert.That(_item.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void B2_PickupWithoutAnyAdopterLeavesTheOriginalHandOnlyOnce()
        {
            _hands.TryPickUp(_item);
            Transform handParent = _item.transform.parent;
            _shelf.OnPlaced = () => _events.Publish(new OrderStatusChanged(_item.BoundItem.OrderId, OrderStatus.PickedUpByLobby));
            _counter.Execute(_context);
            Assert.That(handParent, Is.Not.Null);
            Assert.That(_item.transform.parent, Is.Null, "No bundle adopted it, so it leaves the hand it was in.");
            Assert.That(_drinkPlacement.childCount, Is.Zero);
        }

        [Test]
        public void B1_FailureDuringTheHandReleaseCallbackDiscardsTheProduct()
        {
            _hands.TryPickUp(_item);
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null) { _events.Publish(new OrderStatusChanged(_item.BoundItem.OrderId, OrderStatus.Failed)); } });
            _counter.Execute(_context);
            Assert.That(_drinkPlacement.childCount, Is.Zero);
            Assert.That(_item == null || !_item.gameObject.activeSelf, Is.True);
            Assert.That(_hands.Current, Is.Null);
        }

        [Test]
        public void B2_LobbyPickupDuringTheHandReleaseCallbackKeepsTheBundleParent()
        {
            _hands.TryPickUp(_item);
            var lobbyHands = new HeldItemSlot(_events);
            InteractionContext lobby = new InteractionContext(new ActorRef(2), ActorRole.Lobby, lobbyHands, _clock, _events);
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null && ReferenceEquals(change.Previous, _item)) { _pickup.Execute(lobby); } });
            _counter.Execute(_context);
            var bundle = lobbyHands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null);
            Assert.That(_item.transform.parent, Is.SameAs(bundle.transform));
            Assert.That(_drinkPlacement.childCount, Is.Zero);
        }

        [Test]
        public void B3_PickupRequestsTheOldestReadyOrderAndCarriesItsItemsInSnapshotOrder()
        {
            var drink = Child("SnapshotDrink").AddComponent<PreparedHoldable>();
            drink.Initialize(ItemKind.Drink, 7, 1);
            var cake = Child("SnapshotCake").AddComponent<PreparedHoldable>();
            cake.Initialize(ItemKind.Cake, 7, 2);
            _shelf.NextOverride = new OrderId(7);
            _shelf.Snapshot = new IPreparedItem[] { drink, cake };
            _pickup.Execute(_context);
            var bundle = _hands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null);
            Assert.That(_shelf.PickedOrders, Is.EqualTo(new[] { new OrderId(7) }), "Only the shelf's oldest Ready order is requested.");
            Assert.That(bundle.OrderId, Is.EqualTo(new OrderId(7)));
            Assert.That(bundle.Items, Is.EqualTo(new IPreparedItem[] { drink, cake }));
            Assert.That(bundle.Items[0].BoundItem.OrderItemId.Value, Is.LessThan(bundle.Items[1].BoundItem.OrderItemId.Value));
            Assert.That(drink.transform.parent, Is.SameAs(bundle.transform));
            Assert.That(cake.transform.parent, Is.SameAs(bundle.transform));
        }

        private InteractionContext Context(IHeldItemSlot hands, ActorRole role) => new InteractionContext(new ActorRef(1), role, hands, _clock, _events);
        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            return child;
        }
        private static void Configure(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Configure(UnityEngine.Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public sealed class PreparedHoldable : MonoBehaviour, IHoldable, IPreparedItem
        {
            public ItemKind Kind { get; private set; }
            public OrderItemRef BoundItem { get; private set; }
            public bool IsFinished => true;
            public int Quality => 100;
            public bool Ready;
            public Transform HandGrip => transform;
            public void Initialize(ItemKind kind) => Initialize(kind, 1, 1);
            public void Initialize(ItemKind kind, int order, int orderItem)
            {
                Kind = kind;
                BoundItem = new OrderItemRef(new OrderId(order), new OrderItemId(orderItem), new PreparationId(orderItem));
                if (GetComponent<Collider>() == null)
                {
                    gameObject.AddComponent<BoxCollider>();
                    var anchors = new GameObject("Anchors");
                    anchors.transform.SetParent(transform, false);
                    var point = new GameObject("PlacementPoint");
                    point.transform.SetParent(anchors.transform, false);
                    point.transform.localPosition = new Vector3(0f, 0.4f, 0.1f);
                }
            }
            public Result MarkReady() { Ready = true; return Result.Success(); }
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }

        private sealed class StuckHands : IHeldItemSlot
        {
            public IHoldable Current { get; }
            public StuckHands(IHoldable item) => Current = item;
            public bool TryPickUp(IHoldable item) => false;
            public bool TryRelease() => throw new InvalidOperationException("release fault");
        }

        private sealed class RefusingHands : IHeldItemSlot
        {
            public IHoldable Current => null;
            public bool TryPickUp(IHoldable item) => false;
            public bool TryRelease() => false;
        }

        private sealed class Shelf : IReadyShelf
        {
            private readonly IEventBus _events;
            public IPreparedItem Stored;
            public int PlacementCalls;
            public int PickupCalls;
            public int DomainEvents;
            public bool PlacementFailure;
            public bool FailDuringPlacement;
            public bool PickupFailure;
            public Action OnPlaced;
            public OrderId NextOverride;
            public IReadOnlyList<IPreparedItem> Snapshot;
            public readonly List<OrderId> PickedOrders = new List<OrderId>();
            public OrderId NextReadyOrder => NextOverride.IsValid ? NextOverride : Stored != null ? Stored.BoundItem.OrderId : default;
            public Shelf(IEventBus events) => _events = events;
            public Availability CanPlace(IPreparedItem item) => Stored == null ? Availability.Available : Availability.Blocked("ready.occupied");
            public bool Occupied(ItemKind kind) => Stored != null && Stored.Kind == kind;
            public Result PlaceReady(IPreparedItem item)
            {
                PlacementCalls++;
                if (PlacementFailure)
                {
                    return Result.Fail("ready.rejected");
                }
                item.MarkReady();
                Stored = item;
                DomainEvents++;
                _events.Publish(new OrderStatusChanged(item.BoundItem.OrderId, OrderStatus.Ready));
                OnPlaced?.Invoke();
                if (FailDuringPlacement) { Stored = null; _events.Publish(new OrderStatusChanged(item.BoundItem.OrderId, OrderStatus.Failed)); }
                return Result.Success();
            }
            public Result<IReadOnlyList<IPreparedItem>> PickUp(OrderId orderId, ActorRef actor)
            {
                PickupCalls++;
                PickedOrders.Add(orderId);
                if (Snapshot != null) { return Result<IReadOnlyList<IPreparedItem>>.Success(Snapshot); }
                if (PickupFailure)
                {
                    return Result<IReadOnlyList<IPreparedItem>>.Fail("ready.rejected");
                }
                IPreparedItem item = Stored;
                Stored = null;
                DomainEvents++;
                _events.Publish(new OrderStatusChanged(orderId, OrderStatus.PickedUpByLobby));
                return Result<IReadOnlyList<IPreparedItem>>.Success(new[] { item });
            }
        }
    }
}

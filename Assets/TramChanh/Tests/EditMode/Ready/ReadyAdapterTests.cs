using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Stall.Runtime;
using UnityEditor;
using UnityEngine;

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
            public void Initialize(ItemKind kind)
            {
                Kind = kind;
                BoundItem = new OrderItemRef(new OrderId(1), new OrderItemId(1), new PreparationId(1));
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
            public bool PickupFailure;
            public OrderId NextReadyOrder => Stored != null ? Stored.BoundItem.OrderId : default;
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
                return Result.Success();
            }
            public Result<IReadOnlyList<IPreparedItem>> PickUp(OrderId orderId, ActorRef actor)
            {
                PickupCalls++;
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

#if UNITY_EDITOR
using System.Collections;
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
using UnityEngine.TestTools;

namespace TramChanh.Tests.PlayMode.Ready
{
    public sealed class ReadyPresentationTests
    {
        private GameObject _root;
        private EventBus _events;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private Shelf _shelf;
        private ReadyCounterPoint _counter;
        private ReadyOrderPickupPoint _pickup;
        private Transform _placement;
        private Transform _handAnchor;
        private PreparedItem _item;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Ready presentation test");
            _events = new EventBus();
            _hands = new HeldItemSlot(_events);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Stall | ActorRole.Lobby, _hands, new ManualClock(), _events);
            _shelf = new Shelf(_events);
            _placement = Child("DrinkPlacement").transform;
            _handAnchor = Child("HoldAnchor").transform;
            var view = Child("HeldView").AddComponent<HeldItemView>();
            Configure(view, "_holdAnchor", _handAnchor);
            view.Initialize(_hands, _events);
            _counter = Child("Counter").AddComponent<ReadyCounterPoint>();
            Configure(_counter, "_id", 1);
            Configure(_counter, "_interactionPoint", _counter.transform);
            Configure(_counter, "_drinkPlacementPoint", _placement);
            Configure(_counter, "_cakePlacementPoint", Child("CakePlacement").transform);
            _counter.Initialize(_shelf, _events);
            _pickup = Child("LobbyPickup").AddComponent<ReadyOrderPickupPoint>();
            Configure(_pickup, "_id", 2);
            Configure(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf);
            _item = Item("PreparedDrink", 1);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(_root);
            yield return null;
            _events.Dispose();
        }

        [UnityTest]
        public IEnumerator TC_ORDER_007_HeldViewTransfersPreparedVisualFromHandToShelfToServedBundle()
        {
            _hands.TryPickUp(_item);
            Assert.That(_item.transform.parent, Is.SameAs(_handAnchor));
            Assert.That(_item.GetComponent<Collider>().enabled, Is.False);
            _counter.Execute(_context);
            Assert.That(_item.transform.parent, Is.SameAs(_placement));
            Assert.That(_item.GetComponent<Collider>().enabled, Is.True);
            _pickup.Execute(_context);
            var bundle = (ServedOrder)_hands.Current;
            Assert.That(bundle.transform.parent, Is.SameAs(_handAnchor));
            Assert.That(_item.transform.parent, Is.SameAs(bundle.transform));
            Assert.That(_item.gameObject.layer, Is.EqualTo(TramChanhLayers.HeldItemIndex));
            Assert.That(_item.GetComponent<Collider>().enabled, Is.False);
            Assert.That(_placement.childCount, Is.Zero);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TC_ORDER_007_LobbyPickupFreesCounterForNextStallOrder()
        {
            _hands.TryPickUp(_item);
            _counter.Execute(_context);
            var lobbyHands = new HeldItemSlot();
            var lobby = new InteractionContext(new ActorRef(2), ActorRole.Lobby, lobbyHands, _context.Clock, _events);
            _pickup.Execute(lobby);
            var bundle = (ServedOrder)lobbyHands.Current;
            bundle.transform.SetParent(_root.transform);
            var next = Item("NextDrink", 2);
            _hands.TryPickUp(next);
            _counter.Execute(_context);
            Assert.That(_shelf.Stored, Is.SameAs(next));
            Assert.That(next.transform.parent, Is.SameAs(_placement));
            Assert.That(_placement.childCount, Is.EqualTo(1));
            Assert.That(_item.transform.parent, Is.SameAs(bundle.transform));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TC_ORDER_006_FailedOrderClearsItsShelfVisual()
        {
            _hands.TryPickUp(_item);
            _counter.Execute(_context);
            _events.Publish(new OrderStatusChanged(_item.BoundItem.OrderId, OrderStatus.Failed));
            yield return null;
            Assert.That(_placement.childCount, Is.Zero);
            Assert.That(_item == null || !_item.gameObject.activeInHierarchy, Is.True);
        }

        private PreparedItem Item(string name, int order)
        {
            var item = Child(name).AddComponent<PreparedItem>();
            item.Initialize(order);
            return item;
        }
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

        public sealed class PreparedItem : MonoBehaviour, IPreparedItem, IHoldable
        {
            public ItemKind Kind => ItemKind.Drink;
            public OrderItemRef BoundItem { get; private set; }
            public bool IsFinished => true;
            public int Quality => 100;
            public Transform HandGrip => transform;
            public void Initialize(int order)
            {
                BoundItem = new OrderItemRef(new OrderId(order), new OrderItemId(order), new PreparationId(order));
                gameObject.AddComponent<BoxCollider>();
                var anchors = new GameObject("Anchors");
                anchors.transform.SetParent(transform, false);
                var placement = new GameObject("PlacementPoint");
                placement.transform.SetParent(anchors.transform, false);
            }
            public Result MarkReady() => Result.Success();
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }
        private sealed class Shelf : IReadyShelf
        {
            private readonly IEventBus _events;
            public IPreparedItem Stored;
            public OrderId NextReadyOrder => Stored?.BoundItem.OrderId ?? default;
            public Shelf(IEventBus events) => _events = events;
            public Availability CanPlace(IPreparedItem item) => Stored == null ? Availability.Available : Availability.Blocked("ready.occupied");
            public bool Occupied(ItemKind kind) => Stored != null;
            public Result PlaceReady(IPreparedItem item)
            {
                item.MarkReady();
                Stored = item;
                _events.Publish(new OrderStatusChanged(item.BoundItem.OrderId, OrderStatus.Ready));
                return Result.Success();
            }
            public Result<IReadOnlyList<IPreparedItem>> PickUp(OrderId orderId, ActorRef actor)
            {
                IPreparedItem item = Stored;
                Stored = null;
                _events.Publish(new OrderStatusChanged(orderId, OrderStatus.PickedUpByLobby));
                return Result<IReadOnlyList<IPreparedItem>>.Success(new[] { item });
            }
        }
    }
}
#endif

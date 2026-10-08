#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.PlayMode.Lobby
{
    public sealed class DeliveryPresentationTests
    {
        private GameObject _root;
        private EventBus _events;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private TableOrderPoint _table;
        private ReadyOrderPickupPoint _pickup;
        private Transform _holdAnchor;
        private int _expectedFaults;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Delivery presentation"); _events = new EventBus(); var clock = new ManualClock();
            _orders = new OrderService(clock, _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            { new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink), new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake) }));
            _queue = new StallTicketQueue(_orders); _shelf = new ReadyShelf(_orders, _queue, 1, 1); _hands = new HeldItemSlot(_events);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Lobby, _hands, clock, _events);
            var controller = Child("Lobby").AddComponent<LobbyOrderController>(); controller.Initialize(_orders, _events);
            _table = Child("Table").AddComponent<TableOrderPoint>(); _table.Initialize(21, _table.transform, _orders, controller, new TableId(1));
            _holdAnchor = Child("HoldAnchor").transform; var view = Child("Held view").AddComponent<HeldItemView>(); Set(view, "_holdAnchor", _holdAnchor); view.Initialize(_hands, _events);
            _pickup = Child("Pickup").AddComponent<ReadyOrderPickupPoint>(); Set(_pickup, "_id", 22); Set(_pickup, "_interactionPoint", _pickup.transform); _pickup.Initialize(_shelf, _orders, _events);
            _expectedFaults = 0;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Assert.That(_orders.ObserverFaults.Count, Is.EqualTo(_expectedFaults)); _shelf.Dispose(); Object.Destroy(_root); yield return null; _events.Dispose();
        }
        [UnityTest]
        public IEnumerator TC_ORDER_001_MixedDeliveryRetiresBothPreparedVisualsAndFreesHeldSpace()
        {
            OrderId id = PrepareMixed(); _pickup.Execute(_context); var bundle = (ServedOrder)_hands.Current;
            Assert.That(bundle.transform.parent, Is.SameAs(_holdAnchor)); Assert.That(bundle.Items.Count, Is.EqualTo(2));
            foreach (IPreparedItem item in bundle.Items) { Assert.That(((Component)item).transform.parent, Is.SameAs(bundle.transform)); }
            _table.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed)); Assert.That(_hands.Current, Is.Null);
            Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True); yield return null;
            Assert.That(bundle == null, Is.True); Assert.That(_holdAnchor.childCount, Is.Zero); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator TC_ORDER_008_FaultBeforeFailureListenerCannotStrandACarriedBundle()
        {
            OrderId id = PrepareMixed();
            _events.Subscribe<OrderStatusChanged>(change => { if (change.Status == OrderStatus.Failed) { throw new InvalidOperationException("preceding failure observer"); } });
            _pickup.Execute(_context); var bundle = (ServedOrder)_hands.Current;
            Assert.That(_orders.Fail(id, FailureReason.CustomerLeft).IsSuccess, Is.True); _expectedFaults = 1;
            yield return null;
            Assert.That(_hands.Current, Is.Null); Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True);
            Assert.That(_table.ActiveOrder.IsValid, Is.False); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator TC_ORDER_008_FailedOldBundleNeverReleasesAReplacementHeldItem()
        {
            OrderId id = PrepareMixed(); _pickup.Execute(_context); var bundle = (ServedOrder)_hands.Current;
            _hands.TryRelease(); var replacement = Child("Replacement").AddComponent<PreparedBag>(); _hands.TryPickUp(replacement);
            _orders.Fail(id, FailureReason.CustomerLeft); yield return null;
            Assert.That(_hands.Current, Is.SameAs(replacement)); Assert.That(replacement.transform.parent, Is.SameAs(_holdAnchor));
            Assert.That(bundle == null, Is.True); LogAssert.NoUnexpectedReceived();
        }
        private OrderId PrepareMixed()
        {
            var requests = new[] { new ItemRequest("drink", 1), new ItemRequest("cake", 1) };
            OrderId id = _table.RequestCustomerService(new CustomerId(7), requests).Value;
            _table.Execute(_context); _events.Publish(new OrderEntryConfirmed(id, requests)); _events.Publish(new OrderSendRequested(id));
            int preparation = 0;
            foreach (ItemRequest request in requests)
            {
                ItemKind kind = request.ItemDefinitionId == "cake" ? ItemKind.Cake : ItemKind.Drink;
                var bag = Child("Prepared " + kind).AddComponent<PreparedBag>(); bag.Kind = kind; bag.Binding = _queue.ClaimNext(kind, new PreparationId(++preparation)).Value;
                Assert.That(_shelf.PlaceReady(bag).IsSuccess, Is.True);
            }
            return id;
        }
        private GameObject Child(string name) { var child = new GameObject(name); child.transform.SetParent(_root.transform, false); return child; }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        public sealed class PreparedBag : MonoBehaviour, IPreparedItem, IHoldable
        {
            public ItemKind Kind { get; set; } = ItemKind.Drink; public OrderItemRef Binding { get; set; } public OrderItemRef BoundItem => Binding;
            public bool IsFinished => true; public int Quality => 100; public Transform HandGrip => transform;
            public Result MarkReady() => Result.Success(); public void OnPickedUp(IHeldItemSlot hands) { } public void OnReleased() { }
        }
    }
}
#endif

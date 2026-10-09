using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Lobby
{
    public sealed class DeliveryAdapterTests
    {
        private GameObject _root;
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private LobbyOrderController _controller;
        private TableOrderPoint _table;
        private VehicleOrderPoint _vehicle;
        private ReadyOrderPickupPoint _pickup;
        private int _preparation;
        private int _expectedFaults;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Delivery adapters");
            _events = new EventBus(); _clock = new ManualClock(); _hands = new HeldItemSlot(_events);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Lobby, _hands, _clock, _events);
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            { new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink), new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake) }));
            _queue = new StallTicketQueue(_orders); _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _controller = Child("Lobby").AddComponent<LobbyOrderController>(); _controller.Initialize(_orders, _events);
            _table = Child("Table").AddComponent<TableOrderPoint>(); _table.Initialize(21, _table.transform, _orders, _controller, new TableId(1));
            _vehicle = Child("Vehicle").AddComponent<VehicleOrderPoint>(); _vehicle.Initialize(22, _vehicle.transform, _orders, _controller, new VehicleId(1));
            _pickup = Child("Ready pickup").AddComponent<ReadyOrderPickupPoint>();
            Set(_pickup, "_id", 23); Set(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf, _orders, _events);
            _preparation = 0; _expectedFaults = 0;
        }
        [TearDown]
        public void TearDown()
        {
            Assert.That(_orders.ObserverFaults.Count, Is.EqualTo(_expectedFaults));
            if (_hands.Current is Component held && held != null) { Object.DestroyImmediate(held.gameObject); }
            Object.DestroyImmediate(_root); _shelf.Dispose(); _events.Dispose();
        }

        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void TC_ORDER_001_TableVehicleAndMixedBundlesDeliverCompleteAndFreePoint(bool vehicle, bool mixed)
        {
            OrderPoint point = vehicle ? (OrderPoint)_vehicle : _table;
            OrderId id = Prepare(point, 7, mixed);
            ServedOrder bundle = Carry();
            Assert.That(point.Query(_context).PromptKey, Is.EqualTo("order.point.deliver"));
            Assert.That(point.Query(_context).Availability.IsAvailable, Is.True);
            Assert.That(((IPreparationFeedback)bundle).NextActionKey, Is.EqualTo("hud.next.delivery"));
            point.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True);
            Assert.That(point.ActiveOrder.IsValid, Is.False);
            Result<OrderId> next = point.RequestCustomerService(new CustomerId(8), Requests(false));
            Assert.That(next.IsSuccess, Is.True);
            Assert.That(_orders.Get(next.Value).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.False, "Point reuse still requires separate Lobby confirmations.");
        }

        [Test]
        public void TC_ORDER_004_WrongPointRoutesHeldOrderAndKeepsEveryVisualInTheBundle()
        {
            OrderId id = Prepare(_table, 7, true);
            _vehicle.RequestCustomerService(new CustomerId(8), Requests(false));
            ServedOrder bundle = Carry();
            Assert.That(_vehicle.Query(_context).Availability.IsAvailable, Is.True);
            _vehicle.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(_hands.Current, Is.SameAs(bundle));
            Assert.That(((IOrderDelivery)_orders).TryGetInfo(id, out OrderDeliveryInfo info), Is.True);
            Assert.That(info.DeliveryAttempts, Is.EqualTo(1));
            foreach (IPreparedItem item in bundle.Items)
            { Assert.That(((Component)item).transform.parent, Is.SameAs(bundle.transform)); }
            _table.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
        }

        [Test]
        public void TC_ORDER_004_EmptyPointRejectsWithoutCountingAValidWrongTarget()
        {
            OrderId id = Prepare(_table, 7, false); ServedOrder bundle = Carry();
            Assert.That(_vehicle.Query(_context).BlockedReasonKey, Is.EqualTo("order.delivery.no_customer"));
            _vehicle.Execute(_context);
            Assert.That(_hands.Current, Is.SameAs(bundle));
            ((IOrderDelivery)_orders).TryGetInfo(id, out OrderDeliveryInfo info);
            Assert.That(info.DeliveryAttempts, Is.Zero);
        }

        [TestCase(ActorRole.Stall, false, "interaction.lobby_role_required")]
        [TestCase(ActorRole.Lobby, true, "interaction.paused")]
        public void TC_ORDER_006_RoleAndPauseGuardsDoNotDeliver(ActorRole role, bool paused, string reason)
        {
            OrderId id = Prepare(_table, 7, false); ServedOrder bundle = Carry();
            if (paused) { _clock.Pause(); }
            var context = new InteractionContext(new ActorRef(1), role, _hands, _clock, _events);
            Assert.That(_table.Query(context).BlockedReasonKey, Is.EqualTo(reason)); _table.Execute(context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby)); Assert.That(_hands.Current, Is.SameAs(bundle));
        }

        [Test]
        public void TC_ORDER_006_AnotherHeldItemCannotDeliverOrOpenTheOrderEntry()
        {
            OrderId id = _table.RequestCustomerService(new CustomerId(7), Requests(false)).Value;
            var other = Child("Other item").AddComponent<PreparedBag>(); _hands.TryPickUp(other);
            Assert.That(_table.Query(_context).BlockedReasonKey, Is.EqualTo("hands.full")); _table.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
        }

        [Test]
        public void TC_ORDER_007_FaultBeforeBundleStatusListenerStillRetiresCommittedDelivery()
        {
            OrderId id = Prepare(_table, 7, false);
            _events.Subscribe<OrderStatusChanged>(change => { if (change.Status == OrderStatus.Delivered) { throw new InvalidOperationException("delivery status fault"); } });
            ServedOrder bundle = Carry(); _table.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed)); Assert.That(_hands.Current, Is.Null);
            Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True); _expectedFaults = 1;
        }

        [Test]
        public void TC_ORDER_007_ThrowingHandObserverDoesNotKeepDeliveredVisualsOrPreventCompletion()
        {
            OrderId id = Prepare(_table, 7, false); ServedOrder bundle = Carry();
            _events.Subscribe<HeldItemChanged>(change => { if (change.Current == null) { throw new InvalidOperationException("delivery hand fault"); } });
            LogAssert.Expect(LogType.Exception, new Regex("delivery hand fault")); _table.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed)); Assert.That(_hands.Current, Is.Null);
            Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void TC_ORDER_007_HandReleaseCallbackReplacementRemainsHeldAfterOldBundleCleanup()
        {
            OrderId id = Prepare(_table, 7, false); ServedOrder bundle = Carry();
            var replacement = Child("Replacement").AddComponent<PreparedBag>();
            _events.Subscribe<HeldItemChanged>(change => { if (ReferenceEquals(change.Previous, bundle)) { _hands.TryPickUp(replacement); } });
            _table.Execute(_context);
            Assert.That(_hands.Current, Is.SameAs(replacement)); Assert.That(replacement.gameObject.activeSelf, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
        }

        [Test]
        public void TC_ORDER_007_ReleasedBundleChildChosenAsNewHeldItemSurvivesCleanup()
        {
            OrderId id = Prepare(_table, 7, true); ServedOrder bundle = Carry();
            var replacement = (PreparedBag)bundle.Items[0];
            _events.Subscribe<HeldItemChanged>(change => { if (ReferenceEquals(change.Previous, bundle)) { _hands.TryPickUp(replacement); } });
            _table.Execute(_context);
            Assert.That(_hands.Current, Is.SameAs(replacement)); Assert.That(replacement != null, Is.True);
            Assert.That(replacement.gameObject.activeSelf, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
        }

        [Test]
        public void TC_ORDER_007_ReentrantCompletionAndNewCustomerPreserveNewPointOrder()
        {
            OrderId id = Prepare(_table, 7, false); Carry(); OrderId next = default;
            _events.Subscribe<OrderStatusChanged>(change =>
            {
                if (change.Status == OrderStatus.Delivered) { ((IOrderDelivery)_orders).Complete(id); }
                if (change.Status == OrderStatus.Completed) { next = _table.RequestCustomerService(new CustomerId(8), Requests(false)).Value; }
            });
            _table.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed)); Assert.That(next.IsValid, Is.True);
            Assert.That(_table.ActiveOrder, Is.EqualTo(next)); Assert.That(_orders.Get(next).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
        }

        [Test]
        public void TC_ORDER_008_FailedCarriedBundleReleasesOnlyItsOwnHandsAndCannotDeliver()
        {
            OrderId id = Prepare(_table, 7, true); ServedOrder bundle = Carry();
            Assert.That(_orders.Fail(id, FailureReason.CustomerLeft).IsSuccess, Is.True);
            Assert.That(_hands.Current, Is.Null); Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True);
            _table.Execute(_context); Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Failed));
            Assert.That(_table.ActiveOrder.IsValid, Is.False);
        }

        [Test]
        public void TC_ORDER_008_ReentrantFailureDuringReadyPickupDoesNotPopulateARetiredReservation()
        {
            OrderId id = Prepare(_table, 7, false);
            _events.Subscribe<OrderStatusChanged>(change => { if (change.Status == OrderStatus.PickedUpByLobby) { _orders.Fail(id, FailureReason.CustomerLeft); } });
            _pickup.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Failed)); Assert.That(_hands.Current, Is.Null);
        }

        [Test]
        public void TC_ORDER_007_ReentrantDeliveryDuringReadyPickupDoesNotPopulateARetiredReservation()
        {
            OrderId id = Prepare(_table, 7, false);
            _events.Subscribe<OrderStatusChanged>(change =>
            {
                if (change.Status == OrderStatus.PickedUpByLobby)
                {
                    ((IOrderDelivery)_orders).Deliver(id, _context.Actor, new DeliveryTarget(_table.Origin, new CustomerId(7)));
                    ((IOrderDelivery)_orders).Complete(id);
                }
            });
            _pickup.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed)); Assert.That(_hands.Current, Is.Null);
        }

        [Test]
        public void TC_ORDER_007_ReentrantWrongTargetCorrectionDoesNotLeaveACompletedBundleHeld()
        {
            OrderId id = Prepare(_table, 7, false); _vehicle.RequestCustomerService(new CustomerId(8), Requests(false)); Carry();
            _events.Subscribe<DeliveryRejected>(_ => { ((IOrderDelivery)_orders).Deliver(id, _context.Actor, new DeliveryTarget(_table.Origin, new CustomerId(7))); });
            _vehicle.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed)); Assert.That(_hands.Current, Is.Null);
        }

        [Test]
        public void TC_ORDER_007_CompletionRejectionRetiresBundleAndNextInteractionRetriesOnlyComplete()
        {
            var delivery = new RetryCompletionService(_orders);
            _table.Initialize(21, _table.transform, delivery, _controller, new TableId(1));
            OrderId id = Prepare(_table, 7, false);
            ServedOrder bundle = Carry();
            _table.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Delivered));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True);
            Assert.That(_table.ActiveOrder, Is.EqualTo(id));
            Assert.That(_table.RequestCustomerService(new CustomerId(8), Requests(false)).IsSuccess, Is.False);
            Assert.That(_table.Query(_context).Availability.IsAvailable, Is.True);
            Assert.That(_table.Query(_context).PromptKey, Is.EqualTo("order.point.complete"));
            _table.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(delivery.DeliverCalls, Is.EqualTo(1));
            Assert.That(delivery.CompleteCalls, Is.EqualTo(2));
            Assert.That(_table.ActiveOrder.IsValid, Is.False);
        }

        private sealed class RetryCompletionService : IOrderService, IOrderDelivery
        {
            private readonly OrderService _inner;
            public bool HideOrders { get; set; }
            public int DeliverCalls { get; private set; }
            public int CompleteCalls { get; private set; }
            public RetryCompletionService(OrderService inner) { _inner = inner; }
            public IReadOnlyList<IReadOnlyOrder> Active => _inner.Active;
            public IReadOnlyOrder Get(OrderId id) => HideOrders ? null : _inner.Get(id);
            public Result<OrderId> RequestService(OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested) => _inner.RequestService(origin, customer, requested);
            public Result BeginTaking(OrderId id, ActorRef actor, OrderOrigin point) => _inner.BeginTaking(id, actor, point);
            public Result Enter(OrderId id, IReadOnlyList<ItemRequest> entered) => _inner.Enter(id, entered);
            public Result SendToStall(OrderId id) => _inner.SendToStall(id);
            public Result Fail(OrderId id, FailureReason reason) => _inner.Fail(id, reason);
            public bool TryGetInfo(OrderId id, out OrderDeliveryInfo info) => _inner.TryGetInfo(id, out info);
            public Result Deliver(OrderId id, ActorRef actor, DeliveryTarget target)
            {
                DeliverCalls++;
                return _inner.Deliver(id, actor, target);
            }
            public Result Complete(OrderId id)
            {
                CompleteCalls++;
                return CompleteCalls == 1 ? Result.Fail("order.complete.retry") : _inner.Complete(id);
            }
        }

        [TestCase(OrderStatus.Failed)]
        [TestCase(OrderStatus.Delivered)]
        [TestCase(OrderStatus.Completed)]
        public void TC_ORDER_008_LegacyUnboundTerminalBundleSelfHealsThroughInteractionDriver(OrderStatus terminal)
        {
            OrderId id = Prepare(_table, 7, true);
            var pickedUp = _shelf.PickUp(id, _context.Actor);
            var bundle = Child("Legacy unbound bundle").AddComponent<ServedOrder>();
            bundle.Initialize(id);
            _hands.TryPickUp(bundle);
            bundle.Populate(pickedUp.Value);
            if (terminal == OrderStatus.Failed) { _orders.Fail(id, FailureReason.CustomerLeft); }
            else
            {
                ((IOrderDelivery)_orders).Deliver(id, _context.Actor, new DeliveryTarget(_table.Origin, new CustomerId(7)));
                if (terminal == OrderStatus.Completed) { ((IOrderDelivery)_orders).Complete(id); }
            }
            Assert.That(_hands.Current, Is.SameAs(bundle), "The legacy unbound path has no automatic lifecycle subscription.");
            Assert.That(_table.Query(_context).Availability.IsAvailable, Is.True);
            new InteractionActionDriver(_context).Begin(_table);
            Assert.That(_hands.Current, Is.Null, "The player interaction driver must reach terminal bundle cleanup.");
            Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(terminal));
        }

        [Test]
        public void TC_ORDER_008_UnknownUnboundBundleSelfHealsWithoutChangingThePointOrder()
        {
            OrderId waiting = _table.RequestCustomerService(new CustomerId(7), Requests(false)).Value;
            var bundle = Child("Unknown bundle").AddComponent<ServedOrder>();
            bundle.Initialize(new OrderId(9999));
            _hands.TryPickUp(bundle);
            Assert.That(_table.Query(_context).Availability.IsAvailable, Is.True);
            new InteractionActionDriver(_context).Begin(_table);
            Assert.That(_hands.Current, Is.Null);
            Assert.That(bundle == null || !bundle.gameObject.activeSelf, Is.True);
            Assert.That(_orders.Get(waiting).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
        }

        [Test]
        public void TC_ORDER_008_PickupInitializerCannotSilentlyUseTheOldOneArgumentPath()
        {
            MethodInfo method = typeof(ReadyOrderPickupPoint).GetMethod(nameof(ReadyOrderPickupPoint.Initialize));
            Assert.That(method, Is.Not.Null);
            Assert.Throws<TargetParameterCountException>(() => method.Invoke(_pickup, new object[] { _shelf }));
            ParameterInfo[] parameters = method.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(3));
            foreach (ParameterInfo parameter in parameters) { Assert.That(parameter.IsOptional, Is.False); }
            Assert.That(_hands.Current, Is.Null);
            Assert.Throws<ArgumentNullException>(() => _pickup.Initialize(_shelf, null, _events));
            Assert.Throws<ArgumentNullException>(() => _pickup.Initialize(_shelf, _orders, null));
        }

        [Test]
        public void TC_ORDER_008_FailedLifecycleBindRetiresItsEmptyReservation()
        {
            Prepare(_table, 7, false);
            var missing = new RetryCompletionService(_orders) { HideOrders = true };
            _pickup.Initialize(_shelf, missing, _events);
            int roots = _pickup.gameObject.scene.GetRootGameObjects().Length;
            Assert.Throws<InvalidOperationException>(() => _pickup.Execute(_context));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_pickup.gameObject.scene.GetRootGameObjects().Length, Is.EqualTo(roots), "Binding failure must not leak a ServedOrder reservation.");
        }

        private OrderId Prepare(OrderPoint point, int customer, bool mixed)
        {
            ItemRequest[] requests = Requests(mixed); OrderId id = point.RequestCustomerService(new CustomerId(customer), requests).Value;
            point.Execute(_context); Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.TakingOrder));
            _events.Publish(new OrderEntryConfirmed(id, requests)); Assert.That(_queue.HasPending(ItemKind.Drink), Is.False);
            _events.Publish(new OrderSendRequested(id)); Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.SentToStall));
            foreach (ItemRequest request in requests)
            {
                ItemKind kind = request.ItemDefinitionId == "cake" ? ItemKind.Cake : ItemKind.Drink;
                var item = Child("Prepared " + kind).AddComponent<PreparedBag>(); item.Kind = kind;
                item.Binding = _queue.ClaimNext(kind, new PreparationId(++_preparation)).Value;
                Assert.That(_shelf.PlaceReady(item).IsSuccess, Is.True);
            }
            return id;
        }
        private ServedOrder Carry()
        {
            _pickup.Execute(_context); var bundle = _hands.Current as ServedOrder; Assert.That(bundle, Is.Not.Null);
            bundle.transform.SetParent(_root.transform, false); return bundle;
        }
        private static ItemRequest[] Requests(bool mixed) => mixed ? new[] { new ItemRequest("drink", 1), new ItemRequest("cake", 1) } : new[] { new ItemRequest("drink", 1) };
        private GameObject Child(string name) { var child = new GameObject(name); child.transform.SetParent(_root.transform, false); return child; }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        public sealed class PreparedBag : MonoBehaviour, IPreparedItem, IHoldable
        {
            public ItemKind Kind { get; set; } = ItemKind.Drink;
            public OrderItemRef Binding { get; set; }
            public OrderItemRef BoundItem => Binding;
            public bool IsFinished => true; public int Quality => 100; public Transform HandGrip => transform;
            public Result MarkReady() => Result.Success(); public void OnPickedUp(IHeldItemSlot hands) { } public void OnReleased() { }
        }
    }
}

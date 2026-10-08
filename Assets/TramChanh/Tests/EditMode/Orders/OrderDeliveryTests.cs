using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    public sealed class OrderDeliveryTests
    {
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private IOrderDelivery _delivery;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private int _preparation;
        private int _expectedFaults;
        private readonly ActorRef _actor = new ActorRef(1);
        private readonly CustomerId _customer = new CustomerId(7);
        private readonly OrderOrigin _table = OrderOrigin.ForTable(new TableId(1));

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _clock = new ManualClock();
            var content = new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake)
            });
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), content);
            _delivery = (object)_orders as IOrderDelivery;
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _preparation = 0;
            _expectedFaults = 0;
            Assert.That(_delivery, Is.Not.Null, "The real order service must expose the additive delivery capability.");
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(_orders.ObserverFaults.Count, Is.EqualTo(_expectedFaults));
            _shelf.Dispose();
            _events.Dispose();
        }

        [TestCase(false, "drink")]
        [TestCase(true, "cake")]
        [TestCase(false, "mixed")]
        [TestCase(true, "mixed")]
        public void TC_ORDER_001_FullCanonicalFlowEndsCompletedAndKeepsOrigin(bool vehicle, string item)
        {
            OrderOrigin origin = vehicle ? OrderOrigin.ForVehicle(new VehicleId(1)) : _table;
            var statuses = new List<OrderStatus>();
            using var subscription = _events.Subscribe<OrderStatusChanged>(change => statuses.Add(change.Status));
            var requests = Requests(item);
            OrderId id = Request(origin, requests);
            PrepareAndPickUp(id, origin, requests);
            Assert.That(_delivery.Deliver(id, _actor, new DeliveryTarget(origin, _customer)).IsSuccess, Is.True);
            Assert.That(_delivery.Complete(id).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_orders.Get(id).Origin, Is.EqualTo(origin));
            Assert.That(_orders.Get(id).CustomerId, Is.EqualTo(_customer));
            Assert.That(statuses, Is.EqualTo(new[] { OrderStatus.WaitingForLobby, OrderStatus.TakingOrder,
                OrderStatus.Entered, OrderStatus.SentToStall, OrderStatus.InPreparation, OrderStatus.Ready,
                OrderStatus.PickedUpByLobby, OrderStatus.Delivered, OrderStatus.Completed }));
            Assert.That(_orders.Active, Is.Empty);
            Assert.That(_shelf.Occupied(ItemKind.Drink) || _shelf.Occupied(ItemKind.Cake), Is.False);
        }

        [TestCase(OrderStatus.WaitingForLobby)]
        [TestCase(OrderStatus.TakingOrder)]
        [TestCase(OrderStatus.Entered)]
        [TestCase(OrderStatus.SentToStall)]
        [TestCase(OrderStatus.InPreparation)]
        [TestCase(OrderStatus.Ready)]
        [TestCase(OrderStatus.Delivered)]
        [TestCase(OrderStatus.Completed)]
        [TestCase(OrderStatus.Failed)]
        public void TC_ORDER_006_DeliveryRequiresExactlyPickedUpByLobby(OrderStatus status)
        {
            OrderId id = InStatus(status);
            int events = 0;
            using var rejected = _events.Subscribe<DeliveryRejected>(_ => events++);
            using var changed = _events.Subscribe<OrderStatusChanged>(_ => events++);
            Assert.That(_delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer)).ReasonKey, Is.EqualTo("order.transition.invalid"));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(status));
            Assert.That(Info(id).DeliveryAttempts, Is.Zero);
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void TC_ORDER_004_WrongOriginAndWrongCustomerCountOnlyValidAttempts()
        {
            OrderId id = InStatus(OrderStatus.PickedUpByLobby);
            var rejected = new List<DeliveryRejected>();
            using var subscription = _events.Subscribe<DeliveryRejected>(rejected.Add);
            DeliveryTarget vehicle = new DeliveryTarget(OrderOrigin.ForVehicle(new VehicleId(1)), _customer);
            DeliveryTarget otherCustomer = new DeliveryTarget(_table, new CustomerId(8));
            Assert.That(_delivery.Deliver(id, _actor, vehicle).ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
            Assert.That(_delivery.Deliver(id, _actor, otherCustomer).ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(Info(id).DeliveryAttempts, Is.EqualTo(2));
            Assert.That(Info(id).QualityScore, Is.Zero);
            Assert.That(((IOrderDeliveryInfo)_orders.Get(id)).DeliveryAttempts, Is.EqualTo(Info(id).DeliveryAttempts));
            Assert.That(((IOrderDeliveryInfo)_orders.Get(id)).QualityScore, Is.EqualTo(Info(id).QualityScore));
            Assert.That(rejected.Count, Is.EqualTo(2));
            Assert.That(rejected[0].Order, Is.EqualTo(id));
            Assert.That(rejected[0].AttemptedTarget, Is.EqualTo(vehicle));
            Assert.That(rejected[1].AttemptedTarget, Is.EqualTo(otherCustomer));
            Assert.That(rejected[1].ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
            Assert.That(_delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer)).IsSuccess, Is.True);
            Assert.That(Info(id).DeliveryAttempts, Is.EqualTo(2));
        }

        [Test]
        public void TC_ORDER_006_InvalidActorTargetAndMissingOrderNeverMutate()
        {
            OrderId id = InStatus(OrderStatus.PickedUpByLobby);
            int events = 0;
            using var rejected = _events.Subscribe<DeliveryRejected>(_ => events++);
            using var changed = _events.Subscribe<OrderStatusChanged>(_ => events++);
            Assert.That(_delivery.Deliver(id, default, new DeliveryTarget(_table, _customer)).ReasonKey, Is.EqualTo("order.actor.invalid"));
            Assert.That(_delivery.Deliver(id, _actor, default).ReasonKey, Is.EqualTo("order.delivery.invalid_target"));
            Assert.That(_delivery.Deliver(default, _actor, new DeliveryTarget(_table, _customer)).IsSuccess, Is.False);
            Assert.That(Info(id).DeliveryAttempts, Is.Zero);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void TC_ORDER_007_QualityMeanIsCommittedBeforeDeliveredObserversAndSnapshotIsImmutable()
        {
            var requests = Requests("mixed");
            OrderId id = Request(_table, requests);
            PrepareAndPickUp(id, _table, requests, 75, 80);
            OrderDeliveryInfo before = Info(id);
            Assert.That(before.QualityScore, Is.Zero);
            int observed = -1;
            using var subscription = _events.Subscribe<OrderStatusChanged>(change =>
            {
                if (change.Status == OrderStatus.Delivered) { observed = Info(id).QualityScore; }
            });
            Assert.That(_delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer)).IsSuccess, Is.True);
            Assert.That(observed, Is.EqualTo(78));
            Assert.That(Info(id).QualityScore, Is.EqualTo(78));
            Assert.That(before.QualityScore, Is.Zero);
            Assert.That(((IOrderDeliveryInfo)_orders.Get(id)).QualityScore, Is.EqualTo(78));
        }

        [Test]
        public void TC_ORDER_006_MissingInfoReturnsFalseAndDefaultSnapshot()
        {
            Assert.That(_delivery.TryGetInfo(default, out OrderDeliveryInfo info), Is.False);
            Assert.That(info.DeliveryAttempts, Is.Zero);
            Assert.That(info.QualityScore, Is.Zero);
        }

        [Test]
        public void TC_ORDER_006_CompleteRequiresDeliveredAndRejectsTerminalRepetition()
        {
            OrderId id = InStatus(OrderStatus.PickedUpByLobby);
            Assert.That(_delivery.Complete(id).ReasonKey, Is.EqualTo("order.transition.invalid"));
            Assert.That(_orders.Active.Count, Is.EqualTo(1));
            _delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer));
            Assert.That(_delivery.Complete(id).IsSuccess, Is.True);
            Assert.That(_delivery.Complete(id).IsSuccess, Is.False);
            Assert.That(_delivery.Complete(default).IsSuccess, Is.False);
            Assert.That(_orders.Active, Is.Empty);
            Assert.That(_orders.Fail(id, FailureReason.CustomerLeft).IsSuccess, Is.False);
        }

        [Test]
        public void TC_ORDER_001_CompletedOriginIsFreeBeforeObserverRequestsItsNextOrder()
        {
            OrderId id = InStatus(OrderStatus.PickedUpByLobby);
            _delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer));
            Result<OrderId> next = default;
            using var subscription = _events.Subscribe<OrderStatusChanged>(change =>
            {
                if (change.Status == OrderStatus.Completed)
                {
                    Assert.That(_orders.Active, Is.Empty);
                    next = _orders.RequestService(_table, new CustomerId(8), Requests("drink"));
                }
            });
            Assert.That(_delivery.Complete(id).IsSuccess, Is.True);
            Assert.That(next.IsSuccess, Is.True);
            Assert.That(_orders.Get(next.Value).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.False);
        }

        [Test]
        public void TC_ORDER_007_ObserverFaultsCannotRollBackDeliveryOrCompletion()
        {
            OrderId id = InStatus(OrderStatus.PickedUpByLobby);
            using var subscription = _events.Subscribe<OrderStatusChanged>(change =>
            {
                if (change.Status == OrderStatus.Delivered || change.Status == OrderStatus.Completed)
                { throw new InvalidOperationException("delivery observer fault"); }
            });
            Assert.That(_delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer)).IsSuccess, Is.True);
            Assert.That(_delivery.Complete(id).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_orders.Active, Is.Empty);
            _expectedFaults = 2;
        }

        [Test]
        public void TC_ORDER_007_ReentrantCompletionKeepsDeliveredBeforeCompletedForAllObservers()
        {
            OrderId id = InStatus(OrderStatus.PickedUpByLobby);
            var first = new List<OrderStatus>();
            var second = new List<OrderStatus>();
            using var completing = _events.Subscribe<OrderStatusChanged>(change =>
            {
                first.Add(change.Status);
                if (change.Status == OrderStatus.Delivered) { Assert.That(_delivery.Complete(id).IsSuccess, Is.True); }
            });
            using var follower = _events.Subscribe<OrderStatusChanged>(change => second.Add(change.Status));
            Assert.That(_delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer)).IsSuccess, Is.True);
            Assert.That(first, Is.EqualTo(new[] { OrderStatus.Delivered, OrderStatus.Completed }));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
        }

        [Test]
        public void TC_ORDER_004_RejectionObserverMayDeliverAndCompleteWithoutOutboxReordering()
        {
            OrderId id = InStatus(OrderStatus.PickedUpByLobby);
            var events = new List<string>();
            using var rejected = _events.Subscribe<DeliveryRejected>(change =>
            {
                events.Add("rejected");
                Assert.That(Info(id).DeliveryAttempts, Is.EqualTo(1));
                Assert.That(_delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer)).IsSuccess, Is.True);
                Assert.That(_delivery.Complete(id).IsSuccess, Is.True);
            });
            using var changed = _events.Subscribe<OrderStatusChanged>(change => events.Add(change.Status.ToString()));
            Assert.That(_delivery.Deliver(id, _actor, new DeliveryTarget(OrderOrigin.ForVehicle(new VehicleId(2)), _customer)).IsSuccess, Is.False);
            Assert.That(events, Is.EqualTo(new[] { "rejected", "Delivered", "Completed" }));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
        }

        private OrderDeliveryInfo Info(OrderId id)
        {
            Assert.That(_delivery.TryGetInfo(id, out OrderDeliveryInfo info), Is.True);
            return info;
        }
        private OrderId Request(OrderOrigin origin, ItemRequest[] requests) => _orders.RequestService(origin, _customer, requests).Value;
        private static ItemRequest[] Requests(string item) => item == "mixed"
            ? new[] { new ItemRequest("drink", 1), new ItemRequest("cake", 1) }
            : new[] { new ItemRequest(item, 1) };
        private void PrepareAndPickUp(OrderId id, OrderOrigin origin, ItemRequest[] requests, int drinkQuality = 100, int cakeQuality = 100)
        {
            _orders.BeginTaking(id, _actor, origin);
            _orders.Enter(id, requests);
            _orders.SendToStall(id);
            foreach (ItemRequest request in requests)
            {
                ItemKind kind = request.ItemDefinitionId == "cake" ? ItemKind.Cake : ItemKind.Drink;
                OrderItemRef binding = _queue.ClaimNext(kind, new PreparationId(++_preparation)).Value;
                Assert.That(_shelf.PlaceReady(new Prepared(kind, binding, kind == ItemKind.Cake ? cakeQuality : drinkQuality)).IsSuccess, Is.True);
            }
            Assert.That(_shelf.PickUp(id, _actor).IsSuccess, Is.True);
        }
        private OrderId InStatus(OrderStatus desired)
        {
            ItemRequest[] requests = Requests("drink");
            OrderId id = Request(_table, requests);
            if (desired == OrderStatus.WaitingForLobby) { return id; }
            _orders.BeginTaking(id, _actor, _table);
            if (desired == OrderStatus.TakingOrder) { return id; }
            _orders.Enter(id, requests);
            if (desired == OrderStatus.Entered) { return id; }
            _orders.SendToStall(id);
            if (desired == OrderStatus.SentToStall) { return id; }
            OrderItemRef binding = _queue.ClaimNext(ItemKind.Drink, new PreparationId(++_preparation)).Value;
            if (desired == OrderStatus.InPreparation) { return id; }
            _shelf.PlaceReady(new Prepared(ItemKind.Drink, binding, 100));
            if (desired == OrderStatus.Ready) { return id; }
            _shelf.PickUp(id, _actor);
            if (desired == OrderStatus.PickedUpByLobby) { return id; }
            if (desired == OrderStatus.Failed) { _orders.Fail(id, FailureReason.CustomerLeft); return id; }
            _delivery.Deliver(id, _actor, new DeliveryTarget(_table, _customer));
            if (desired == OrderStatus.Completed) { _delivery.Complete(id); }
            return id;
        }
        private sealed class Prepared : IPreparedItem
        {
            public ItemKind Kind { get; }
            public OrderItemRef BoundItem { get; }
            public bool IsFinished => true;
            public int Quality { get; }
            private bool _ready;
            public Prepared(ItemKind kind, OrderItemRef binding, int quality) { Kind = kind; BoundItem = binding; Quality = quality; }
            public Result MarkReady() { if (_ready) { return Result.Fail("ready.already_ready"); } _ready = true; return Result.Success(); }
        }
    }
}

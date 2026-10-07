using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    public sealed class OrderTransactionTests
    {
        private EventBus _bus;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private List<Exception> _faults;
        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus(); _faults = new List<Exception>();
            var db = new ContentDatabase(new[] { new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink), new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake) });
            _orders = new OrderService(new ManualClock(), _bus, new SequentialIdGenerator(), db, 1, 1, _faults.Add);
            _queue = new StallTicketQueue(_orders); _shelf = new ReadyShelf(_orders, _queue, 1, 1);
        }
        [TearDown] public void TearDown() { _shelf.Dispose(); _bus.Dispose(); }
        [Test]
        public void TC_READY_007_ThrowingReadyItemObserverPreservesCommitAndDeliversOrderEvent()
        {
            var item = Prepared(1); int delivered = 0;
            using var throwing = _bus.Subscribe<OrderItemStatusChanged>(_ => throw new InvalidOperationException("observer"));
            using var remainingEvent = _bus.Subscribe<OrderStatusChanged>(e => { if (e.Status == OrderStatus.Ready) { delivered++; } });
            Result result = _shelf.PlaceReady(item);
            Assert.That(result.IsSuccess, Is.True); Assert.That(item.Ready, Is.True);
            Assert.That(_orders.Get(item.BoundItem.OrderId).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.True); Assert.That(delivered, Is.EqualTo(1));
            Assert.That(_faults.Count, Is.EqualTo(1)); Assert.That(_orders.ObserverFaults[0], Is.SameAs(_faults[0]));
        }
        [Test]
        public void TC_READY_007_ThrowingPickupObserverStillReturnsSnapshotAndFreesShelf()
        {
            var item = Prepared(1); _shelf.PlaceReady(item);
            using var throwing = _bus.Subscribe<OrderStatusChanged>(_ => throw new InvalidOperationException("observer"));
            var result = _shelf.PickUp(item.BoundItem.OrderId, new ActorRef(1));
            Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value[0], Is.SameAs(item));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
            Assert.That(_orders.Get(item.BoundItem.OrderId).Status, Is.EqualTo(OrderStatus.PickedUpByLobby)); Assert.That(_faults.Count, Is.EqualTo(1));
        }
        [Test]
        public void TC_READY_007_ThrowingFailureItemObserverDoesNotSkipOrderFailureEvent()
        {
            var item = Prepared(1); _shelf.PlaceReady(item); int failures = 0;
            using var throwing = _bus.Subscribe<OrderItemStatusChanged>(_ => throw new InvalidOperationException("observer"));
            using var remaining = _bus.Subscribe<OrderStatusChanged>(e => { if (e.Status == OrderStatus.Failed) { failures++; } });
            Assert.That(_orders.Fail(item.BoundItem.OrderId, FailureReason.CustomerLeft).IsSuccess, Is.True);
            Assert.That(_queue.IsBound(item.BoundItem), Is.False); Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
            Assert.That(failures, Is.EqualTo(1)); Assert.That(_faults.Count, Is.EqualTo(1));
        }
        [Test]
        public void TC_READY_008_FirstNotificationReadsCommittedItemOrderShelfAndBinding()
        {
            var item = Prepared(1); bool occupied = false, bound = false, finished = false; OrderStatus status = default; OrderId next = default;
            using var observer = _bus.Subscribe<OrderItemStatusChanged>(e =>
            {
                if (e.Status != OrderItemStatus.Ready) { return; }
                occupied = _shelf.Occupied(ItemKind.Drink); bound = _queue.IsBound(e.Item); finished = item.Ready;
                status = _orders.Get(e.Item.OrderId).Status; next = _shelf.NextReadyOrder;
            });
            _shelf.PlaceReady(item);
            Assert.That(occupied && bound && finished, Is.True); Assert.That(status, Is.EqualTo(OrderStatus.Ready)); Assert.That(next, Is.EqualTo(item.BoundItem.OrderId)); Assert.That(_faults, Is.Empty);
        }
        [Test]
        public void TC_READY_009_ReentrantFailureEventsFollowOriginalReadyTransaction()
        {
            var item = Prepared(1); var events = new List<string>();
            using var observeItem = _bus.Subscribe<OrderItemStatusChanged>(e =>
            {
                events.Add("item:" + e.Status);
                if (e.Status == OrderItemStatus.Ready) { _orders.Fail(e.Item.OrderId, FailureReason.CancelledByDebug); }
            });
            using var observeSecondItem = _bus.Subscribe<OrderItemStatusChanged>(e => events.Add("item2:" + e.Status));
            using var observeOrder = _bus.Subscribe<OrderStatusChanged>(e => events.Add("order:" + e.Status));
            Assert.That(_shelf.PlaceReady(item).IsSuccess, Is.True);
            Assert.That(events, Is.EqualTo(new[] { "item:Ready", "item2:Ready", "order:Ready", "item:Pending", "item2:Pending", "order:Failed" }));
            Assert.That(_orders.Get(item.BoundItem.OrderId).Status, Is.EqualTo(OrderStatus.Failed)); Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False); Assert.That(_faults, Is.Empty);
        }
        [Test]
        public void TC_READY_003_PickupSnapshotIsOrderedAndImmutableForMixedOrder()
        {
            var requests = new[] { new ItemRequest("cake", 1), new ItemRequest("drink", 1) }; var origin = OrderOrigin.ForVehicle(new VehicleId(1));
            OrderId id = _orders.RequestService(origin, new CustomerId(1), requests).Value;
            _orders.BeginTaking(id, new ActorRef(1), origin); _orders.Enter(id, requests); _orders.SendToStall(id);
            var drink = new PreparedFake(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value);
            var cake = new PreparedFake(ItemKind.Cake, _queue.ClaimNext(ItemKind.Cake, new PreparationId(11)).Value);
            _shelf.PlaceReady(drink); _shelf.PlaceReady(cake); var result = _shelf.PickUp(id, new ActorRef(1));
            Assert.That(result.Value, Is.EqualTo(new IPreparedItem[] { cake, drink }));
            Assert.Throws<NotSupportedException>(() => ((IList<IPreparedItem>)result.Value).Clear());
            Assert.That(_shelf.Occupied(ItemKind.Drink) || _shelf.Occupied(ItemKind.Cake), Is.False); Assert.That(_faults, Is.Empty);
        }
        [Test]
        public void TC_READY_002_ReleaseCannotRequeueAnItemAlreadyOnReadyShelf()
        {
            var item = Prepared(1); _shelf.PlaceReady(item);
            Assert.That(_queue.Release(item.BoundItem).ReasonKey, Is.EqualTo("stall.ticket.not_bound"));
            Assert.That(_queue.IsBound(item.BoundItem), Is.True); Assert.That(_shelf.Occupied(ItemKind.Drink), Is.True);
            Assert.That(_orders.Get(item.BoundItem.OrderId).Status, Is.EqualTo(OrderStatus.Ready));
        }
        [Test]
        public void TC_READY_004_OldestReadyPickupGuardWorksWithLargerConfiguredShelf()
        {
            _shelf.Dispose(); _bus.Dispose(); _bus = new EventBus();
            var db = new ContentDatabase(new[] { new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink) });
            _orders = new OrderService(new ManualClock(), _bus, new SequentialIdGenerator(), db, 2, 1, _faults.Add);
            _queue = new StallTicketQueue(_orders); _shelf = new ReadyShelf(_orders, _queue, 2, 1);
            var first = Prepared(1); var second = Prepared(2); _shelf.PlaceReady(first); _shelf.PlaceReady(second);
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(first.BoundItem.OrderId));
            Assert.That(_shelf.PickUp(second.BoundItem.OrderId, new ActorRef(1)).IsSuccess, Is.False);
            Assert.That(_orders.Get(second.BoundItem.OrderId).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.PickUp(first.BoundItem.OrderId, new ActorRef(1)).IsSuccess, Is.True);
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(second.BoundItem.OrderId));
        }

        private PreparedFake Prepared(int table)
        {
            var request = new[] { new ItemRequest("drink", 1) }; var origin = OrderOrigin.ForTable(new TableId(table));
            OrderId id = _orders.RequestService(origin, new CustomerId(table), request).Value;
            _orders.BeginTaking(id, new ActorRef(1), origin); _orders.Enter(id, request); _orders.SendToStall(id);
            return new PreparedFake(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10 + table)).Value);
        }
        private sealed class PreparedFake : IPreparedItem
        {
            public ItemKind Kind { get; } public OrderItemRef BoundItem { get; } public bool IsFinished => true; public int Quality => 100; public bool Ready { get; private set; }
            public PreparedFake(ItemKind kind, OrderItemRef binding) { Kind = kind; BoundItem = binding; }
            public Result MarkReady() { if (Ready) { return Result.Fail("ready.already_ready"); } Ready = true; return Result.Success(); }
        }
    }
}

using System.Collections.Generic;
using System;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Orders;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace TramChanh.Tests.EditMode.Orders
{
    public sealed class OrderGameplayTests
    {
        private EventBus _events;
        private OrderService _orders;
        private ManualClock _clock;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private readonly ItemRequest[] _drink = { new ItemRequest("drink", 1) };

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            var catalogue = new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake)
            });
            _clock = new ManualClock();
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), catalogue);
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
        }

        [TearDown]
        public void TearDown() { Assert.That(_orders.ObserverFaults, Is.Empty, "Unexpected event-observer fault"); _shelf.Dispose(); _events.Dispose(); }

        [Test]
        public void GT_004_OnlyLobbyCanonicalTransitionsExposeTickets()
        {
            OrderOrigin point = OrderOrigin.ForTable(new TableId(1));
            OrderId id = _orders.RequestService(point, new CustomerId(1), _drink).Value;
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.False);
            Assert.That(_orders.Enter(id, _drink).IsSuccess, Is.False);
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.False);
            Assert.That(_orders.BeginTaking(id, new ActorRef(1), point).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, _drink).IsSuccess, Is.True);
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.False);
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.True);
            Assert.That(_queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value.OrderId, Is.EqualTo(id));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation));
        }

        [Test]
        public void TC_ORDER_003_TicketsClaimFifoBySendOrderRatherThanArrival()
        {
            OrderId first = Request(1);
            OrderId second = Request(2);
            Send(second, 2);
            _clock.Advance(1);
            Send(first, 1);
            OrderItemRef claim = _queue.ClaimNext(ItemKind.Drink, new PreparationId(20)).Value;
            Assert.That(claim.OrderId, Is.EqualTo(second));
            Assert.That(_queue.ClaimNext(ItemKind.Drink, new PreparationId(21)).Value.OrderId, Is.EqualTo(first));
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.False);
            Assert.That(_queue.ClaimNext(ItemKind.Drink, new PreparationId(22)).ReasonKey, Is.EqualTo("stall.no_ticket.drink"));
        }

        [Test]
        public void TC_ORDER_005_ReadyRequiresEveryEnteredItemAndPlacementIsExactlyOnce()
        {
            var request = new[] { new ItemRequest("drink", 1), new ItemRequest("cake", 1) };
            OrderOrigin point = OrderOrigin.ForTable(new TableId(1));
            OrderId id = _orders.RequestService(point, new CustomerId(1), request).Value;
            _orders.BeginTaking(id, new ActorRef(1), point);
            _orders.Enter(id, request);
            _orders.SendToStall(id);
            var drink = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value);
            Assert.That(_shelf.PlaceReady(drink).IsSuccess, Is.True);
            Assert.That(drink.ReadyCount, Is.EqualTo(1));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation));
            Assert.That(_shelf.PlaceReady(drink).IsSuccess, Is.False);
            Assert.That(drink.ReadyCount, Is.EqualTo(1));
            var cake = new Prepared(ItemKind.Cake, _queue.ClaimNext(ItemKind.Cake, new PreparationId(11)).Value);
            Assert.That(_shelf.PlaceReady(cake).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.True);
        }

        [Test]
        public void TC_ORDER_005_UnfinishedUnboundWrongKindAndFullShelfDoNotMutate()
        {
            var unfinished = new Prepared(ItemKind.Drink, default) { Finished = false };
            Assert.That(_shelf.CanPlace(unfinished).ReasonKey, Is.EqualTo("ready.not_finished"));
            Assert.That(_shelf.CanPlace(new Prepared(ItemKind.Drink, default)).ReasonKey, Is.EqualTo("ready.no_order"));
            OrderId id = Request(1);
            Send(id, 1);
            OrderItemRef bound = _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value;
            Assert.That(_shelf.PlaceReady(new Prepared(ItemKind.Cake, bound)).IsSuccess, Is.False);
            var valid = new Prepared(ItemKind.Drink, bound);
            Assert.That(_shelf.PlaceReady(valid).IsSuccess, Is.True);
            OrderId second = Request(2);
            Send(second, 2);
            var blocked = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(11)).Value);
            Assert.That(_shelf.PlaceReady(blocked).ReasonKey, Is.EqualTo("ready.slot_full"));
            Assert.That(blocked.ReadyCount, Is.Zero);
            Assert.That(_orders.Get(second).Status, Is.EqualTo(OrderStatus.InPreparation));
        }

        [Test]
        public void TC_ORDER_008_FailedOrderUnbindsPreparationAndCannotBecomeReady()
        {
            OrderId id = Request(1);
            Send(id, 1);
            OrderItemRef binding = _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value;
            Assert.That(_orders.Fail(id, FailureReason.CustomerLeft).IsSuccess, Is.True);
            Assert.That(_queue.IsBound(binding), Is.False);
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.False);
            Assert.That(_shelf.PlaceReady(new Prepared(ItemKind.Drink, binding)).ReasonKey, Is.EqualTo("ready.no_order"));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Failed));
        }

        [Test]
        public void TC_ORDER_009_ReleaseRequeuesAndInvalidatesStalePreparationIdentity()
        {
            OrderId id = Request(1);
            Send(id, 1);
            OrderItemRef old = _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value;
            Assert.That(_queue.Release(old).IsSuccess, Is.True);
            OrderItemRef current = _queue.ClaimNext(ItemKind.Drink, new PreparationId(11)).Value;
            Assert.That(current.OrderItemId, Is.EqualTo(old.OrderItemId));
            Assert.That(_queue.IsBound(old), Is.False);
            Assert.That(_queue.IsBound(current), Is.True);
            Assert.That(_queue.Release(old).IsSuccess, Is.False);
            Assert.That(_shelf.PlaceReady(new Prepared(ItemKind.Drink, old)).ReasonKey, Is.EqualTo("ready.no_order"));
        }

        [Test]
        public void TC_ORDER_006_WrongPointAndMismatchedEntryCannotAdvanceOrder()
        {
            OrderId id = Request(1);
            Assert.That(_orders.BeginTaking(id, new ActorRef(1), OrderOrigin.ForVehicle(new VehicleId(1))).IsSuccess, Is.False);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(_orders.BeginTaking(id, new ActorRef(1), OrderOrigin.ForTable(new TableId(1))).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, new[] { new ItemRequest("cake", 1) }).IsSuccess, Is.False);
            Assert.That(_orders.Enter(id, new[] { new ItemRequest("missing", 1) }).IsSuccess, Is.False);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.TakingOrder));
        }

        [Test]
        public void GT_005_GT_006_OriginsAndOccupiedCustomerPointsArePreserved()
        {
            OrderId table = Request(1);
            Assert.That(_orders.RequestService(OrderOrigin.ForTable(new TableId(1)), new CustomerId(2), _drink).IsSuccess, Is.False);
            OrderId vehicle = _orders.RequestService(OrderOrigin.ForVehicle(new VehicleId(1)), new CustomerId(3), _drink).Value;
            Assert.That(_orders.Get(table).Origin.TableId, Is.EqualTo(new TableId(1)));
            Assert.That(_orders.Get(table).Origin.VehicleId.IsValid, Is.False);
            Assert.That(_orders.Get(vehicle).Origin.VehicleId, Is.EqualTo(new VehicleId(1)));
            Assert.That(_orders.Get(vehicle).Origin.TableId.IsValid, Is.False);
            Assert.That(_orders.RequestService(default, new CustomerId(1), _drink).IsSuccess, Is.False);
        }

        [Test]
        public void TC_ORDER_006_DuplicatePreparationIdCannotClaimTwoItems()
        {
            OrderId first = Request(1);
            OrderId second = Request(2);
            Send(first, 1);
            Send(second, 2);
            Assert.That(_queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).IsSuccess, Is.True);
            Assert.That(_queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).IsSuccess, Is.False);
            Assert.That(_orders.Get(second).Items[0].Status, Is.EqualTo(OrderItemStatus.Pending));
        }

        [Test]
        public void TC_ORDER_003_EqualSendTimeUsesOrderIdTieBreak()
        {
            OrderId first = Request(1);
            OrderId second = Request(2);
            Send(second, 2);
            Send(first, 1);
            Assert.That(_queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value.OrderId, Is.EqualTo(first));
        }

        [Test]
        public void TC_ORDER_005_MarkReadyFailureDoesNotCommitOrderOrSlotOrPublishEvents()
        {
            OrderId id = Request(1);
            Send(id, 1);
            var item = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value) { RejectReady = true };
            int events = 0;
            using var subscription = _events.Subscribe<OrderItemStatusChanged>(_ => events++);
            Assert.That(_shelf.PlaceReady(item).ReasonKey, Is.EqualTo("fake.ready.reject"));
            Assert.That(_orders.Get(id).Items[0].Status, Is.EqualTo(OrderItemStatus.InPreparation));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
            Assert.That(item.ReadyCount, Is.Zero);
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void TC_ORDER_005_ReadyAndPickupEventsObserveFullyCommittedShelfAndOrder()
        {
            OrderId id = Request(1);
            Send(id, 1);
            var item = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value);
            int readyEvents = 0;
            using var itemSubscription = _events.Subscribe<OrderItemStatusChanged>(change =>
            {
                if (change.Status != OrderItemStatus.Ready) { return; }
                Assert.That(item.ReadyCount, Is.EqualTo(1));
                Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
                Assert.That(_shelf.Occupied(ItemKind.Drink), Is.True);
                readyEvents++;
            });
            using var orderSubscription = _events.Subscribe<OrderStatusChanged>(change =>
            {
                if (change.Status == OrderStatus.PickedUpByLobby) { Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False); }
            });
            Assert.That(_shelf.PlaceReady(item).IsSuccess, Is.True);
            Assert.That(readyEvents, Is.EqualTo(1));
            Assert.That(_shelf.PickUp(id, new ActorRef(1)).Value[0], Is.SameAs(item));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
        }

        [Test]
        public void TC_ORDER_005_PickupFreesCapacityForAnotherOrderAndRejectsRepeat()
        {
            OrderId first = Request(1);
            OrderId second = Request(2);
            Send(first, 1);
            Send(second, 2);
            var one = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value);
            var two = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(11)).Value);
            Assert.That(_shelf.PlaceReady(one).IsSuccess, Is.True);
            Assert.That(_shelf.PlaceReady(two).ReasonKey, Is.EqualTo("ready.slot_full"));
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(first));
            Assert.That(_shelf.PickUp(first, new ActorRef(1)).IsSuccess, Is.True);
            Assert.That(_shelf.PickUp(first, new ActorRef(1)).IsSuccess, Is.False);
            Assert.That(_shelf.PlaceReady(two).IsSuccess, Is.True);
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(second));
        }

        [Test]
        public void TC_ORDER_008_FailureClearsReadySlotsBeforeFailureObservers()
        {
            OrderId id = Request(1);
            Send(id, 1);
            var item = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value);
            _shelf.PlaceReady(item);
            using var subscription = _events.Subscribe<OrderStatusChanged>(change =>
            {
                if (change.Status == OrderStatus.Failed) { Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False); }
            });
            Assert.That(_orders.Fail(id, FailureReason.CustomerLeft).IsSuccess, Is.True);
            Assert.That(_shelf.NextReadyOrder.IsValid, Is.False);
        }

        [Test]
        public void TC_ORDER_006_EntryRejectsMoreOfOneKindThanTheShelfCanHold()
        {
            var requested = new[] { new ItemRequest("drink", 2) };
            OrderOrigin origin = OrderOrigin.ForVehicle(new VehicleId(1));
            OrderId id = _orders.RequestService(origin, new CustomerId(1), requested).Value;
            _orders.BeginTaking(id, new ActorRef(1), origin);
            Assert.That(_orders.Enter(id, requested).ReasonKey, Is.EqualTo("order.too_many_for_shelf"));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.TakingOrder));
            Assert.That(_orders.Get(id).Items, Is.Empty);
        }

        [Test]
        public void TC_ORDER_005_ShelfAndTicketQueriesAllocateNothingAndDoNotMutate()
        {
            OrderId id = Request(1);
            Send(id, 1);
            var item = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value);
            _shelf.CanPlace(item);
            Assert.That((TestDelegate)(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    _queue.HasPending(ItemKind.Drink);
                    _queue.IsBound(item.BoundItem);
                    _shelf.CanPlace(item);
                    _ = _shelf.NextReadyOrder;
                }
            }), Is.Not.AllocatingGCMemory());
            Assert.That(item.ReadyCount, Is.Zero);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation));
        }

        [Test]
        public void TC_ORDER_006_RequestInputsAreCopiedAndCannotBeMutatedThroughReadOnlyViews()
        {
            var requests = new[] { new ItemRequest("drink", 1) };
            OrderId id = _orders.RequestService(OrderOrigin.ForTable(new TableId(1)), new CustomerId(1), requests).Value;
            requests[0] = new ItemRequest("cake", 1);
            Assert.That(_orders.Get(id).RequestedItems[0].ItemDefinitionId, Is.EqualTo("drink"));
            Assert.Throws<NotSupportedException>(() => ((IList<ItemRequest>)_orders.Get(id).RequestedItems)[0] = requests[0]);
        }

        [TestCaseSource(nameof(TransitionCases))]
        public void TC_ORDER_006_IntakeAndShelfTransitionMatrix(OrderStatus source, string action, bool expected)
        {
            OrderId id = Reach(source);
            OrderStatus before = _orders.Get(id).Status;
            Result result;
            switch (action)
            {
                case "begin": result = _orders.BeginTaking(id, new ActorRef(1), OrderOrigin.ForTable(new TableId(1))); break;
                case "enter": result = _orders.Enter(id, _drink); break;
                case "send": result = _orders.SendToStall(id); break;
                case "pickup":
                    Result<IReadOnlyList<IPreparedItem>> pickup = _shelf.PickUp(id, new ActorRef(1));
                    result = pickup.IsSuccess ? Result.Success() : Result.Fail(pickup.ReasonKey);
                    break;
                default: result = _orders.Fail(id, FailureReason.CustomerLeft); break;
            }
            Assert.That(result.IsSuccess, Is.EqualTo(expected));
            if (!expected) { Assert.That(_orders.Get(id).Status, Is.EqualTo(before)); }
        }

        private static IEnumerable<TestCaseData> TransitionCases()
        {
            foreach (OrderStatus status in new[] { OrderStatus.WaitingForLobby, OrderStatus.TakingOrder, OrderStatus.Entered, OrderStatus.SentToStall, OrderStatus.InPreparation, OrderStatus.Ready, OrderStatus.PickedUpByLobby, OrderStatus.Failed })
            {
                foreach (string action in new[] { "begin", "enter", "send", "pickup", "fail" })
                {
                    bool expected = action == "begin" ? status == OrderStatus.WaitingForLobby :
                        action == "enter" ? status == OrderStatus.TakingOrder :
                        action == "send" ? status == OrderStatus.Entered :
                        action == "pickup" ? status == OrderStatus.Ready : status != OrderStatus.Failed;
                    yield return new TestCaseData(status, action, expected);
                }
            }
        }

        private OrderId Reach(OrderStatus status)
        {
            OrderId id = Request(1);
            if (status == OrderStatus.WaitingForLobby) { return id; }
            _orders.BeginTaking(id, new ActorRef(1), OrderOrigin.ForTable(new TableId(1)));
            if (status == OrderStatus.TakingOrder) { return id; }
            _orders.Enter(id, _drink);
            if (status == OrderStatus.Entered) { return id; }
            _orders.SendToStall(id);
            if (status == OrderStatus.SentToStall) { return id; }
            var item = new Prepared(ItemKind.Drink, _queue.ClaimNext(ItemKind.Drink, new PreparationId(10)).Value);
            if (status == OrderStatus.InPreparation) { return id; }
            _shelf.PlaceReady(item);
            if (status == OrderStatus.Ready) { return id; }
            _shelf.PickUp(id, new ActorRef(1));
            if (status == OrderStatus.Failed) { _orders.Fail(id, FailureReason.CustomerLeft); }
            return id;
        }

        private OrderId Request(int table) => _orders.RequestService(OrderOrigin.ForTable(new TableId(table)), new CustomerId(table), _drink).Value;

        private void Send(OrderId id, int table)
        {
            Assert.That(_orders.BeginTaking(id, new ActorRef(1), OrderOrigin.ForTable(new TableId(table))).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, _drink).IsSuccess, Is.True);
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
        }

        private sealed class Prepared : IPreparedItem
        {
            public ItemKind Kind { get; }
            public OrderItemRef BoundItem { get; }
            public bool Finished { get; set; } = true;
            public bool IsFinished => Finished;
            public int Quality => 100;
            public int ReadyCount { get; private set; }
            public bool RejectReady { get; set; }
            public Prepared(ItemKind kind, OrderItemRef binding) { Kind = kind; BoundItem = binding; }
            public Result MarkReady()
            {
                if (RejectReady) { return Result.Fail("fake.ready.reject"); }
                if (!Finished) { return Result.Fail("ready.not_finished"); }
                if (ReadyCount > 0) { return Result.Fail("ready.already_ready"); }
                ReadyCount++;
                return Result.Success();
            }
        }
    }
}

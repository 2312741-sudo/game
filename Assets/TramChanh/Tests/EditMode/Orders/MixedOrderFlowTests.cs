using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    /// <summary>MAIN-004: a drink + cake order end to end through OrderService, StallTicketQueue, ReadyShelf and delivery.</summary>
    public sealed class MixedOrderFlowTests
    {
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private IOrderDelivery _delivery;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private int _preparation;
        private readonly ActorRef _lobby = new ActorRef(1);
        private static readonly ItemRequest[] Mixed = { new ItemRequest("drink", 1), new ItemRequest("cake", 1) };

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _clock = new ManualClock();
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake)
            }));
            _delivery = _orders;
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _preparation = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(_orders.ObserverFaults, Is.Empty);
            _shelf.Dispose();
            _events.Dispose();
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void MAIN004_MixedOrderIsReadyOnlyWhenBothKindsAreOnTheShelfAndIsDeliveredToItsOrigin(bool vehicle, bool cakeFirst)
        {
            OrderOrigin origin = vehicle ? OrderOrigin.ForVehicle(new VehicleId(1)) : OrderOrigin.ForTable(new TableId(1));
            var customer = new CustomerId(5);
            var statuses = new List<OrderStatus>();
            var itemEvents = new List<OrderItemStatusChanged>();
            using var orderSubscription = _events.Subscribe<OrderStatusChanged>(change => statuses.Add(change.Status));
            using var itemSubscription = _events.Subscribe<OrderItemStatusChanged>(itemEvents.Add);

            OrderId id = _orders.RequestService(origin, customer, Mixed).Value;
            Assert.That(_queue.HasPending(ItemKind.Drink) || _queue.HasPending(ItemKind.Cake), Is.False, "No ticket before the Lobby sends.");
            Assert.That(_orders.BeginTaking(id, _lobby, origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, Mixed).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Items.Count, Is.EqualTo(2));
            Assert.That(_queue.Tickets, Is.Empty, "Entered is not yet sent.");
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            Assert.That(_queue.Tickets, Is.EqualTo(new[] { id }));
            Assert.That(_queue.HasPending(ItemKind.Drink), Is.True);
            Assert.That(_queue.HasPending(ItemKind.Cake), Is.True);

            ItemKind firstKind = cakeFirst ? ItemKind.Cake : ItemKind.Drink;
            ItemKind secondKind = cakeFirst ? ItemKind.Drink : ItemKind.Cake;
            Prepared first = Claim(firstKind);
            Assert.That(first.BoundItem.OrderId, Is.EqualTo(id));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation));
            Assert.That(_queue.HasPending(firstKind), Is.False, "The claimed kind has no other pending item.");
            Assert.That(_queue.HasPending(secondKind), Is.True, "The other kind is served by its own station.");
            Assert.That(_queue.ClaimNext(firstKind, new PreparationId(900)).ReasonKey,
                Is.EqualTo(firstKind == ItemKind.Drink ? "stall.no_ticket.drink" : "stall.no_ticket.cake"));

            Assert.That(_shelf.PlaceReady(first).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation), "One of two items is not Ready.");
            Assert.That(_queue.Tickets, Is.EqualTo(new[] { id }), "A partially ready order keeps its ticket.");
            Assert.That(_shelf.NextReadyOrder.IsValid, Is.False);
            Assert.That(_shelf.PickUp(id, _lobby).ReasonKey, Is.EqualTo("ready.no_complete_order"));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation));

            Prepared second = Claim(secondKind);
            Assert.That(second.BoundItem.OrderId, Is.EqualTo(id));
            Assert.That(_shelf.PlaceReady(second).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_queue.Tickets, Is.Empty, "A Ready order leaves the stall queue.");
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(id));
            foreach (IReadOnlyOrderItem item in _orders.Get(id).Items) { Assert.That(item.Status, Is.EqualTo(OrderItemStatus.Ready)); }

            Result<IReadOnlyList<IPreparedItem>> pickup = _shelf.PickUp(id, _lobby);
            Assert.That(pickup.IsSuccess, Is.True);
            Assert.That(pickup.Value.Count, Is.EqualTo(2));
            Assert.That(new[] { pickup.Value[0].Kind, pickup.Value[1].Kind }, Is.EquivalentTo(new[] { ItemKind.Drink, ItemKind.Cake }));
            Assert.That(_shelf.Occupied(ItemKind.Drink) || _shelf.Occupied(ItemKind.Cake), Is.False);

            OrderOrigin wrong = vehicle ? OrderOrigin.ForTable(new TableId(1)) : OrderOrigin.ForVehicle(new VehicleId(1));
            Assert.That(_delivery.Deliver(id, _lobby, new DeliveryTarget(wrong, customer)).ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(_delivery.Deliver(id, _lobby, new DeliveryTarget(origin, customer)).IsSuccess, Is.True);
            Assert.That(_delivery.Complete(id).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_orders.Get(id).Origin, Is.EqualTo(origin));
            Assert.That(_orders.Active, Is.Empty);
            Assert.That(statuses, Is.EqualTo(new[] { OrderStatus.WaitingForLobby, OrderStatus.TakingOrder, OrderStatus.Entered,
                OrderStatus.SentToStall, OrderStatus.InPreparation, OrderStatus.Ready, OrderStatus.PickedUpByLobby,
                OrderStatus.Delivered, OrderStatus.Completed }));
            Assert.That(itemEvents.ConvertAll(change => change.Status), Is.EqualTo(new[]
            { OrderItemStatus.InPreparation, OrderItemStatus.Ready, OrderItemStatus.InPreparation, OrderItemStatus.Ready }));
        }

        [Test]
        public void MAIN004_DrinkAndCakeQueuesServeKindsSeparatelyAcrossTableAndVehicleOrders()
        {
            OrderOrigin table = OrderOrigin.ForTable(new TableId(1));
            OrderOrigin vehicle = OrderOrigin.ForVehicle(new VehicleId(1));
            OrderId drinkOnly = Send(table, new CustomerId(1), new[] { new ItemRequest("drink", 1) });
            _clock.Advance(1);
            OrderId mixed = Send(vehicle, new CustomerId(2), Mixed);

            Assert.That(_queue.Tickets, Is.EqualTo(new[] { drinkOnly, mixed }));
            Prepared cake = Claim(ItemKind.Cake);
            Assert.That(cake.BoundItem.OrderId, Is.EqualTo(mixed), "The cake station skips tickets without a cake.");
            Assert.That(_queue.HasPending(ItemKind.Cake), Is.False);
            Prepared firstDrink = Claim(ItemKind.Drink);
            Assert.That(firstDrink.BoundItem.OrderId, Is.EqualTo(drinkOnly), "Drinks are claimed FIFO by send time.");

            Assert.That(_shelf.PlaceReady(cake).IsSuccess, Is.True);
            Assert.That(_orders.Get(mixed).Status, Is.EqualTo(OrderStatus.InPreparation));
            Assert.That(_shelf.PlaceReady(firstDrink).IsSuccess, Is.True);
            Assert.That(_orders.Get(drinkOnly).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(drinkOnly), "Only the complete order can be picked up.");
            Assert.That(_shelf.PickUp(mixed, _lobby).ReasonKey, Is.EqualTo("ready.no_complete_order"));
            Assert.That(_shelf.PickUp(drinkOnly, _lobby).Value.Count, Is.EqualTo(1));
            Assert.That(_shelf.Occupied(ItemKind.Cake), Is.True, "The mixed order's cake stays on the shelf.");
            Assert.That(_delivery.Deliver(drinkOnly, _lobby, new DeliveryTarget(table, new CustomerId(1))).IsSuccess, Is.True);
            Assert.That(_delivery.Complete(drinkOnly).IsSuccess, Is.True);

            Prepared secondDrink = Claim(ItemKind.Drink);
            Assert.That(secondDrink.BoundItem.OrderId, Is.EqualTo(mixed));
            Assert.That(_shelf.PlaceReady(secondDrink).IsSuccess, Is.True);
            Assert.That(_orders.Get(mixed).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.PickUp(mixed, _lobby).Value.Count, Is.EqualTo(2));
            Assert.That(_delivery.Deliver(mixed, _lobby, new DeliveryTarget(vehicle, new CustomerId(2))).IsSuccess, Is.True);
            Assert.That(_delivery.Complete(mixed).IsSuccess, Is.True);
            Assert.That(_orders.Get(mixed).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_orders.Active, Is.Empty);
            Assert.That(_queue.Tickets, Is.Empty);
        }

        [Test]
        public void MAIN004_ReleasedCakeOfAPartiallyReadyMixedOrderIsReclaimedBeforeReady()
        {
            OrderOrigin table = OrderOrigin.ForTable(new TableId(1));
            OrderId id = Send(table, new CustomerId(1), Mixed);
            Prepared drink = Claim(ItemKind.Drink);
            Assert.That(_shelf.PlaceReady(drink).IsSuccess, Is.True);
            Prepared ruined = Claim(ItemKind.Cake);
            Assert.That(_queue.Release(ruined.BoundItem).IsSuccess, Is.True);
            Assert.That(_shelf.CanPlace(ruined).IsAvailable, Is.False, "A released preparation can no longer be placed.");
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation));
            Assert.That(_queue.HasPending(ItemKind.Cake), Is.True);
            Prepared retry = Claim(ItemKind.Cake);
            Assert.That(retry.BoundItem.OrderId, Is.EqualTo(id));
            Assert.That(_shelf.PlaceReady(retry).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.PickUp(id, _lobby).Value.Count, Is.EqualTo(2));
        }

        private OrderId Send(OrderOrigin origin, CustomerId customer, ItemRequest[] items)
        {
            OrderId id = _orders.RequestService(origin, customer, items).Value;
            Assert.That(_orders.BeginTaking(id, _lobby, origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, items).IsSuccess, Is.True);
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            return id;
        }

        private Prepared Claim(ItemKind kind)
        {
            Result<OrderItemRef> claim = _queue.ClaimNext(kind, new PreparationId(++_preparation));
            Assert.That(claim.IsSuccess, Is.True, claim.ReasonKey);
            return new Prepared(kind, claim.Value);
        }

        private sealed class Prepared : IPreparedItem
        {
            private bool _ready;
            public ItemKind Kind { get; }
            public OrderItemRef BoundItem { get; }
            public bool IsFinished => true;
            public int Quality => 100;
            public Prepared(ItemKind kind, OrderItemRef binding) { Kind = kind; BoundItem = binding; }
            public Result MarkReady()
            {
                if (_ready) { return Result.Fail("ready.already_ready"); }
                _ready = true;
                return Result.Success();
            }
        }
    }
}

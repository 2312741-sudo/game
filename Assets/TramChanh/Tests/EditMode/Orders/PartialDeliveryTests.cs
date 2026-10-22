using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    /// <summary>
    /// A finished drink can be served while the cake of the same order is still being made. Whole-order pickup stays the rule for the
    /// items that are left (see MixedOrderFlowTests); these tests cover only the additive partial path.
    /// </summary>
    public sealed class PartialDeliveryTests
    {
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private int _preparation;
        private readonly ActorRef _lobby = new ActorRef(1);
        private static readonly ItemRequest[] Mixed = { new ItemRequest("drink", 1), new ItemRequest("cake", 1) };
        private static readonly OrderOrigin Table = OrderOrigin.ForTable(new TableId(1));
        private static readonly CustomerId Customer = new CustomerId(5);

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

        [Test]
        public void ReadyDrinkCanBeCarriedWhileTheCakeIsStillMissing()
        {
            OrderId id = Send(Table, Customer, Mixed);
            Assert.That(_shelf.NextPartialOrder.IsValid, Is.False, "Nothing is on the shelf yet.");
            Assert.That(_shelf.PlaceReady(Claim(ItemKind.Drink)).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation));
            Assert.That(_shelf.NextReadyOrder.IsValid, Is.False, "The order is not complete.");
            Assert.That(_shelf.NextPartialOrder, Is.EqualTo(id));

            Result<IReadOnlyList<IPreparedItem>> taken = _shelf.PickUpPartial(id, _lobby);
            Assert.That(taken.IsSuccess, Is.True, taken.ReasonKey);
            Assert.That(taken.Value.Count, Is.EqualTo(1));
            Assert.That(taken.Value[0].Kind, Is.EqualTo(ItemKind.Drink));
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False, "The drink slot is free again.");
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation));
            Assert.That(ItemOf(id, ItemKind.Drink).Status, Is.EqualTo(OrderItemStatus.PickedUp));
            Assert.That(_shelf.NextPartialOrder.IsValid, Is.False, "A carried bundle is not offered twice.");
            Assert.That(_shelf.PickUpPartial(id, _lobby).ReasonKey, Is.EqualTo("ready.nothing_to_serve"));
            Assert.That(_shelf.PickUp(id, _lobby).ReasonKey, Is.EqualTo("ready.no_complete_order"));
        }

        [Test]
        public void PartialDeliveryKeepsTheOrderOpenAndTheRestIsDeliveredTheNormalWay()
        {
            OrderId id = Send(Table, Customer, Mixed);
            var statuses = new List<OrderStatus>();
            using var subscription = _events.Subscribe<OrderStatusChanged>(change => statuses.Add(change.Status));
            Assert.That(_shelf.PlaceReady(Claim(ItemKind.Drink)).IsSuccess, Is.True);
            Assert.That(_shelf.PickUpPartial(id, _lobby).IsSuccess, Is.True);

            OrderOrigin wrong = OrderOrigin.ForTable(new TableId(2));
            Assert.That(_orders.DeliverPartial(id, _lobby, new DeliveryTarget(wrong, Customer)).ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
            Assert.That(ItemOf(id, ItemKind.Drink).Status, Is.EqualTo(OrderItemStatus.PickedUp), "A wrong table delivers nothing.");
            Assert.That(_orders.DeliverPartial(id, _lobby, new DeliveryTarget(Table, Customer)).IsSuccess, Is.True);
            Assert.That(ItemOf(id, ItemKind.Drink).Status, Is.EqualTo(OrderItemStatus.Delivered));
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.InPreparation), "The cake is still being made.");
            Assert.That(_orders.DeliverPartial(id, _lobby, new DeliveryTarget(Table, Customer)).ReasonKey, Is.EqualTo("order.transition.invalid"), "Nothing left to hand over.");

            Assert.That(_shelf.PlaceReady(Claim(ItemKind.Cake)).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready), "The delivered drink no longer blocks Ready.");
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(id));
            Result<IReadOnlyList<IPreparedItem>> rest = _shelf.PickUp(id, _lobby);
            Assert.That(rest.IsSuccess, Is.True, rest.ReasonKey);
            Assert.That(rest.Value.Count, Is.EqualTo(1));
            Assert.That(rest.Value[0].Kind, Is.EqualTo(ItemKind.Cake));
            Assert.That(_orders.Deliver(id, _lobby, new DeliveryTarget(Table, Customer)).IsSuccess, Is.True);
            Assert.That(_orders.Complete(id).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_orders.TryGetInfo(id, out OrderDeliveryInfo info), Is.True);
            Assert.That(info.QualityScore, Is.EqualTo(100), "Quality averages every item, including the one served first.");
            Assert.That(_orders.Active, Is.Empty);
            Assert.That(statuses, Is.EqualTo(new[] { OrderStatus.InPreparation, OrderStatus.Ready, OrderStatus.PickedUpByLobby, OrderStatus.Delivered, OrderStatus.Completed }));
        }

        [Test]
        public void CakeFinishingWhileTheDrinkIsOnItsWayWaitsForTheDrinkToBeHandedOver()
        {
            OrderId id = Send(Table, Customer, Mixed);
            Assert.That(_shelf.PlaceReady(Claim(ItemKind.Drink)).IsSuccess, Is.True);
            Assert.That(_shelf.PickUpPartial(id, _lobby).IsSuccess, Is.True);
            Assert.That(_shelf.PlaceReady(Claim(ItemKind.Cake)).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.NextReadyOrder.IsValid, Is.False, "Hidden while the drink bundle is carried.");
            Assert.That(_shelf.PickUp(id, _lobby).ReasonKey, Is.EqualTo("ready.no_complete_order"));

            Assert.That(_orders.DeliverPartial(id, _lobby, new DeliveryTarget(Table, Customer)).IsSuccess, Is.True);
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(id));
            Assert.That(_shelf.PickUp(id, _lobby).Value.Count, Is.EqualTo(1));
            Assert.That(_orders.Deliver(id, _lobby, new DeliveryTarget(Table, Customer)).IsSuccess, Is.True);
            Assert.That(_orders.Complete(id).IsSuccess, Is.True);
        }

        [Test]
        public void FreedDrinkSlotTakesTheNextCustomersDrinkWhileTheFirstCakeIsStillMissing()
        {
            OrderId first = Send(Table, Customer, Mixed);
            OrderId second = Send(OrderOrigin.ForTable(new TableId(2)), new CustomerId(6), new[] { new ItemRequest("drink", 1) });
            Prepared firstDrink = Claim(ItemKind.Drink);
            Assert.That(firstDrink.BoundItem.OrderId, Is.EqualTo(first));
            Assert.That(_shelf.PlaceReady(firstDrink).IsSuccess, Is.True);
            Prepared secondDrink = Claim(ItemKind.Drink);
            Assert.That(_shelf.CanPlace(secondDrink).IsAvailable, Is.False, "The drink slot is taken by the first order's drink.");
            Assert.That(_shelf.PickUpPartial(first, _lobby).IsSuccess, Is.True);
            Assert.That(_shelf.PlaceReady(secondDrink).IsSuccess, Is.True, "Carrying the first drink away frees the slot.");
            Assert.That(_orders.Get(second).Status, Is.EqualTo(OrderStatus.Ready));
        }

        [Test]
        public void FailingAnOrderWhileItsDrinkIsCarriedClearsTheBundle()
        {
            OrderId id = Send(Table, Customer, Mixed);
            Assert.That(_shelf.PlaceReady(Claim(ItemKind.Drink)).IsSuccess, Is.True);
            Assert.That(_shelf.PickUpPartial(id, _lobby).IsSuccess, Is.True);
            Assert.That(_orders.Fail(id, FailureReason.CustomerLeft).IsSuccess, Is.True);
            Assert.That(_orders.DeliverPartial(id, _lobby, new DeliveryTarget(Table, Customer)).ReasonKey, Is.EqualTo("order.transition.invalid"));
            Assert.That(ItemOf(id, ItemKind.Drink).Status, Is.EqualTo(OrderItemStatus.Pending));
        }

        [Test]
        public void ADrinkOnlyOrderStillUsesTheWholeOrderPath()
        {
            OrderId id = Send(Table, Customer, new[] { new ItemRequest("drink", 1) });
            Assert.That(_shelf.PlaceReady(Claim(ItemKind.Drink)).IsSuccess, Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_shelf.NextPartialOrder.IsValid, Is.False, "A complete order is not a partial one.");
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(id));
            Assert.That(_shelf.PickUp(id, _lobby).Value.Count, Is.EqualTo(1));
        }

        private IReadOnlyOrderItem ItemOf(OrderId id, ItemKind kind)
        {
            foreach (IReadOnlyOrderItem item in _orders.Get(id).Items) { if (item.Kind == kind) { return item; } }
            Assert.Fail("No " + kind + " item in order " + id);
            return null;
        }

        private OrderId Send(OrderOrigin origin, CustomerId customer, ItemRequest[] items)
        {
            OrderId id = _orders.RequestService(origin, customer, items).Value;
            Assert.That(_orders.BeginTaking(id, _lobby, origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, items).IsSuccess, Is.True);
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            _clock.Advance(1);
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

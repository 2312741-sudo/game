using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.UI.Orders;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Lobby
{
    /// <summary>
    /// MAIN-004: mixed drink + cake orders from a table and a vehicle through the real Lobby path
    /// (point -> LobbyOrderController -> OrderEntryModel Enter/Send) into the real queue, shelf and delivery.
    /// </summary>
    public sealed class MixedOrderLobbyFlowTests
    {
        private static readonly ItemRequest[] Mixed = { new ItemRequest("drink", 1), new ItemRequest("cake", 1) };
        private GameObject _root;
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private LobbyOrderController _controller;
        private OrderEntryModel _entry;
        private TableOrderPoint _table;
        private VehicleOrderPoint _vehicle;
        private ReadyOrderPickupPoint _pickup;
        private readonly List<ActionBlocked> _blocked = new List<ActionBlocked>();
        private int _preparation;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Mixed Lobby flow");
            _events = new EventBus();
            _clock = new ManualClock();
            _hands = new HeldItemSlot(_events);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Lobby | ActorRole.Stall, _hands, _clock, _events);
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            { new KeyValuePair<string, ItemKind>("drink", ItemKind.Drink), new KeyValuePair<string, ItemKind>("cake", ItemKind.Cake) }));
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _entry = new OrderEntryModel(_events);
            _controller = Child("Lobby").AddComponent<LobbyOrderController>();
            _controller.Initialize(_orders, _events);
            _table = Child("Table").AddComponent<TableOrderPoint>();
            _table.Initialize(21, _table.transform, _orders, _controller, new TableId(1));
            _vehicle = Child("Vehicle").AddComponent<VehicleOrderPoint>();
            _vehicle.Initialize(22, _vehicle.transform, _orders, _controller, new VehicleId(1));
            _pickup = Child("Ready pickup").AddComponent<ReadyOrderPickupPoint>();
            Set(_pickup, "_id", 23);
            Set(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf, _orders, _events);
            _blocked.Clear();
            _events.Subscribe<ActionBlocked>(_blocked.Add);
            _preparation = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(_orders.ObserverFaults, Is.Empty);
            if (_hands.Current is Component held && held != null) { Object.DestroyImmediate(held.gameObject); }
            Object.DestroyImmediate(_root);
            _entry.Dispose();
            _shelf.Dispose();
            _events.Dispose();
        }

        [Test]
        public void MAIN004_MixedTableAndVehicleOrdersGoThroughLobbyEntryAndAreDeliveredToTheirOwnPoints()
        {
            OrderId dineIn = _table.RequestCustomerService(new CustomerId(1), Mixed).Value;
            OrderId takeaway = _vehicle.RequestCustomerService(new CustomerId(2), Mixed).Value;

            // Lobby bypass guard: no ticket exists, and stray UI events without a Lobby session are ignored.
            _events.Publish(new OrderEntryConfirmed(takeaway, Mixed));
            _events.Publish(new OrderSendRequested(takeaway));
            Assert.That(_entry.Enter(), Is.False, "The entry UI cannot confirm before the Lobby opens it.");
            Assert.That(_orders.Get(takeaway).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(_queue.Tickets, Is.Empty);

            TakeOrder(_table, dineIn);
            _clock.Advance(1);
            TakeOrder(_vehicle, takeaway);
            Assert.That(_queue.Tickets, Is.EqualTo(new[] { dineIn, takeaway }));

            // Cake station and drink station work independently; partial readiness never opens pickup.
            var tableCake = Prepare(ItemKind.Cake, dineIn);
            Assert.That(_shelf.PlaceReady(tableCake).IsSuccess, Is.True);
            Assert.That(_orders.Get(dineIn).Status, Is.EqualTo(OrderStatus.InPreparation));
            Assert.That(_pickup.Query(_context).BlockedReasonKey, Is.EqualTo("ready.no_ready_order"));
            var tableDrink = Prepare(ItemKind.Drink, dineIn);
            Assert.That(_shelf.PlaceReady(tableDrink).IsSuccess, Is.True);
            Assert.That(_orders.Get(dineIn).Status, Is.EqualTo(OrderStatus.Ready));

            ServedOrder bundle = Carry(dineIn);
            Assert.That(bundle.Items.Count, Is.EqualTo(2));
            _vehicle.Execute(_context);
            Assert.That(_orders.Get(dineIn).Status, Is.EqualTo(OrderStatus.PickedUpByLobby), "Wrong point never delivers.");
            Assert.That(_blocked[_blocked.Count - 1].ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
            _table.Execute(_context);
            Assert.That(_orders.Get(dineIn).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_hands.Current, Is.Null);
            Assert.That(_table.ActiveOrder.IsValid, Is.False);

            var vehicleDrink = Prepare(ItemKind.Drink, takeaway);
            Assert.That(_shelf.PlaceReady(vehicleDrink).IsSuccess, Is.True);
            Assert.That(_orders.Get(takeaway).Status, Is.EqualTo(OrderStatus.InPreparation));
            var vehicleCake = Prepare(ItemKind.Cake, takeaway);
            Assert.That(_shelf.PlaceReady(vehicleCake).IsSuccess, Is.True);
            Assert.That(_orders.Get(takeaway).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(Carry(takeaway).Items.Count, Is.EqualTo(2));
            _vehicle.Execute(_context);
            Assert.That(_orders.Get(takeaway).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_orders.Get(takeaway).Origin, Is.EqualTo(OrderOrigin.ForVehicle(new VehicleId(1))));
            Assert.That(_orders.Active, Is.Empty);
            Assert.That(_queue.Tickets, Is.Empty);
        }

        private void TakeOrder(OrderPoint point, OrderId id)
        {
            Assert.That(point.Query(_context).PromptKey, Is.EqualTo(OrderPoint.TakeOrderPromptKey));
            point.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.TakingOrder));
            Assert.That(_entry.IsOpen, Is.True);
            Assert.That(_entry.OrderId, Is.EqualTo(id));
            Assert.That(_entry.Items, Is.EqualTo(Mixed));
            Assert.That(_entry.Enter(), Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Entered));
            Assert.That(_queue.Tickets, Does.Not.Contain(id));
            Assert.That(_entry.Send(), Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.SentToStall));
            Assert.That(_entry.IsOpen, Is.False);
            Assert.That(_controller.HasEntrySession, Is.False);
        }

        private DeliveryAdapterTests.PreparedBag Prepare(ItemKind kind, OrderId expected)
        {
            var item = Child("Prepared " + kind).AddComponent<DeliveryAdapterTests.PreparedBag>();
            item.Kind = kind;
            Result<OrderItemRef> claim = _queue.ClaimNext(kind, new PreparationId(++_preparation));
            Assert.That(claim.IsSuccess, Is.True, claim.ReasonKey);
            Assert.That(claim.Value.OrderId, Is.EqualTo(expected));
            item.Binding = claim.Value;
            return item;
        }

        private ServedOrder Carry(OrderId expected)
        {
            Assert.That(_pickup.Query(_context).Availability.IsAvailable, Is.True);
            _pickup.Execute(_context);
            var bundle = _hands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null);
            Assert.That(bundle.OrderId, Is.EqualTo(expected));
            Assert.That(_orders.Get(expected).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            bundle.transform.SetParent(_root.transform, false);
            return bundle;
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(_root.transform, false);
            return child;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Customers;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Tests.EditMode.Lobby;
using TramChanh.UI.Orders;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Customers
{
    /// <summary>
    /// TABLES-01: the director over ten real <see cref="TableOrderPoint"/>s with the real OrderService,
    /// StallTicketQueue, ReadyShelf and Lobby entry flow (same wiring as MixedOrderLobbyFlowTests).
    /// </summary>
    public sealed class CustomerTableIntegrationTests
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
        private OrderEntryModel _entry;
        private ReadyOrderPickupPoint _pickup;
        private TableOrderPoint[] _tables;
        private OrderPointSeat[] _seats;
        private int _preparation;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Customer tables");
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
            _pickup = Child("Ready pickup").AddComponent<ReadyOrderPickupPoint>();
            Set(_pickup, "_id", 90);
            Set(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf, _orders, _events);
            _tables = new TableOrderPoint[10];
            _seats = new OrderPointSeat[10];
            for (int i = 0; i < 10; i++)
            {
                _tables[i] = Child("TABLE_" + (i + 1).ToString("00")).AddComponent<TableOrderPoint>();
                _tables[i].Initialize(31 + i, _tables[i].transform, _orders, _controller, new TableId(i + 1));
                _seats[i] = new OrderPointSeat(_tables[i], i + 1);
            }
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
        public void TenTableSeats_HaveUniqueTableNumbersOneToTen_MatchingTheirPoints()
        {
            Assert.That(_seats.Select(s => s.TableNumber), Is.EqualTo(Enumerable.Range(1, 10)));
            for (int i = 0; i < 10; i++) { Assert.That(_seats[i].TableNumber, Is.EqualTo(_tables[i].TableId.Value)); }
            Assert.Throws<System.ArgumentException>(() => new OrderPointSeat(_tables[0], 2), "Seat number must match the TableId.");
            Assert.Throws<System.ArgumentNullException>(() => new OrderPointSeat(null, 1));
            Assert.That(new CustomerDirector(_seats, new CustomerDirectorSettings(), "drink", "cake", 1).SeatCount, Is.EqualTo(10));
        }

        [Test]
        public void SeatedOrder_GoesThroughLobbyWithTheSeatTableAsOrigin_AndOccupiedTablesAreSkipped()
        {
            // Table 5 already serves a customer placed directly at its point: the director must not pick it.
            OrderId external = _tables[4].RequestCustomerService(new CustomerId(1), new[] { new ItemRequest("drink", 1) }).Value;
            var director = new CustomerDirector(_seats, new CustomerDirectorSettings(10), "drink", "cake", 11);
            var seated = new List<CustomerSeatedEvent>();
            director.CustomerSeated += seated.Add;

            for (int n = 0; n < 9; n++) { Assert.That(director.SpawnNow(), Is.Not.EqualTo(-1)); }
            Assert.That(director.SpawnNow(), Is.EqualTo(-1), "Nine free tables, then none.");
            Assert.That(seated.Select(e => e.SeatIndex), Has.No.Member(4));
            Assert.That(_orders.Get(external).CustomerId, Is.EqualTo(new CustomerId(1)));

            foreach (CustomerSeatedEvent e in seated)
            {
                IReadOnlyOrder order = _orders.Get(e.Order);
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo(OrderStatus.WaitingForLobby), "Arrivals enter Lobby intake, not the stall.");
                Assert.That(order.Origin, Is.EqualTo(OrderOrigin.ForTable(new TableId(_seats[e.SeatIndex].TableNumber))));
                Assert.That(order.Origin.TableId.Value, Is.EqualTo(e.SeatIndex + 1));
                Assert.That(order.CustomerId, Is.EqualTo(e.Customer));
                Assert.That(_tables[e.SeatIndex].ActiveOrder, Is.EqualTo(e.Order));
                Assert.That(_seats[e.SeatIndex].HasLiveOrder, Is.True);
            }
            Assert.That(_queue.Tickets, Is.Empty, "Nothing reaches the stall before the Lobby takes the order.");
        }

        [Test]
        public void CompletedOrder_ReleasesTableAfterClearDelay_AndANewCustomerReusesIt()
        {
            var director = new CustomerDirector(new[] { _seats[6] }, new CustomerDirectorSettings(1, 1000f, 2f, 0f, 0f, 1f), "drink", "cake", 3, 200);
            var seated = new List<CustomerSeatedEvent>();
            var left = new List<int>();
            director.CustomerSeated += seated.Add;
            director.CustomerLeft += left.Add;

            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            OrderId order = seated[0].Order;
            Assert.That(_orders.Get(order).RequestedItems.Select(r => r.ItemDefinitionId), Is.EqualTo(new[] { "drink", "cake" }));
            Assert.That(_orders.Get(order).Status, Is.EqualTo(OrderStatus.WaitingForLobby));

            TakeOrder(_tables[6], order);
            director.Tick(1d);
            Assert.That(director.IsOccupied(0), Is.True);
            Assert.That(_shelf.PlaceReady(Prepare(ItemKind.Drink, order)).IsSuccess, Is.True);
            Assert.That(_shelf.PlaceReady(Prepare(ItemKind.Cake, order)).IsSuccess, Is.True);
            Assert.That(_orders.Get(order).Status, Is.EqualTo(OrderStatus.Ready));
            _pickup.Execute(_context);
            var bundle = _hands.Current as ServedOrder;
            Assert.That(bundle, Is.Not.Null);
            bundle.transform.SetParent(_root.transform, false);
            director.Tick(1d);
            Assert.That(director.IsOccupied(0), Is.True, "Order is still live while carried.");
            _tables[6].Execute(_context);
            Assert.That(_orders.Get(order).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_seats[6].HasLiveOrder, Is.False);

            director.Tick(1d);                   // detection; clear delay begins
            director.Tick(1.5d);
            Assert.That(director.IsOccupied(0), Is.True);
            Assert.That(left, Is.Empty);
            director.Tick(0.5d);
            Assert.That(left, Is.EqualTo(new[] { 0 }));
            Assert.That(director.IsOccupied(0), Is.False);

            Assert.That(director.SpawnNow(), Is.EqualTo(0));
            Assert.That(seated[1].Customer.Value, Is.EqualTo(201));
            Assert.That(seated[1].Order, Is.Not.EqualTo(order));
            Assert.That(_orders.Get(seated[1].Order).Origin.TableId, Is.EqualTo(new TableId(7)));
            Assert.That(_orders.Get(seated[1].Order).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
        }

        [Test]
        public void UnconfiguredPoint_FailsRequest_AndTheSeatStaysFree()
        {
            var bare = Child("TABLE_BARE").AddComponent<TableOrderPoint>();
            var director = new CustomerDirector(new[] { new OrderPointSeat(bare, 1) }, new CustomerDirectorSettings(1), "drink", "cake", 5);
            var failed = new List<CustomerRequestFailedEvent>();
            director.CustomerRequestFailed += failed.Add;
            Assert.That(director.SpawnNow(), Is.EqualTo(-1));
            Assert.That(failed.Single().ReasonKey, Is.EqualTo("order.point.not_configured"));
            Assert.That(director.IsOccupied(0), Is.False);
            Assert.That(_orders.Active, Is.Empty);
        }

        private void TakeOrder(OrderPoint point, OrderId id)
        {
            Assert.That(point.Query(_context).PromptKey, Is.EqualTo(OrderPoint.TakeOrderPromptKey));
            point.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.TakingOrder));
            Assert.That(_entry.Enter(), Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Entered));
            Assert.That(_entry.Send(), Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.SentToStall));
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

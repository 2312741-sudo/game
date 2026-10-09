using System.Collections.Generic;
using System.Linq;
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
    /// TABLES-02: ten dine-in TableOrderPoints (TableId 1..10, interactable ids 31..40) plus the takeaway
    /// vehicle, all driven through the real Lobby path (point -> LobbyOrderController -> OrderEntryModel)
    /// into the real OrderService, StallTicketQueue, ReadyShelf (1 drink + 1 cake) and delivery.
    /// </summary>
    public sealed class TenTableOrderFlowTests
    {
        private const int TableCount = 10;
        private const int FirstTableInteractable = 31;
        private const int VehicleInteractable = 22;
        private static readonly ItemRequest[] Mixed = { new ItemRequest("drink.a", 1), new ItemRequest("cake.a", 1) };
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
        private TableOrderPoint[] _tables;
        private VehicleOrderPoint _vehicle;
        private ReadyOrderPickupPoint _pickup;
        private readonly List<ActionBlocked> _blocked = new List<ActionBlocked>();
        private int _preparation;
        private int _customer;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Ten table Lobby flow");
            _events = new EventBus();
            _clock = new ManualClock();
            _hands = new HeldItemSlot(_events);
            _context = new InteractionContext(new ActorRef(1), ActorRole.Lobby | ActorRole.Stall, _hands, _clock, _events);
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink.a", ItemKind.Drink), new KeyValuePair<string, ItemKind>("drink.b", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake.a", ItemKind.Cake), new KeyValuePair<string, ItemKind>("cake.b", ItemKind.Cake)
            }));
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _entry = new OrderEntryModel(_events);
            _controller = Child("Lobby").AddComponent<LobbyOrderController>();
            _controller.Initialize(_orders, _events);
            _tables = new TableOrderPoint[TableCount];
            for (int i = 0; i < TableCount; i++)
            {
                _tables[i] = Child("Table" + (i + 1)).AddComponent<TableOrderPoint>();
                _tables[i].Initialize(FirstTableInteractable + i, _tables[i].transform, _orders, _controller, new TableId(i + 1));
            }
            _vehicle = Child("Vehicle").AddComponent<VehicleOrderPoint>();
            _vehicle.Initialize(VehicleInteractable, _vehicle.transform, _orders, _controller, new VehicleId(1));
            _pickup = Child("Ready pickup").AddComponent<ReadyOrderPickupPoint>();
            Set(_pickup, "_id", 23);
            Set(_pickup, "_interactionPoint", _pickup.transform);
            _pickup.Initialize(_shelf, _orders, _events);
            _blocked.Clear();
            _events.Subscribe<ActionBlocked>(_blocked.Add);
            _preparation = 0;
            _customer = 0;
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

        private TableOrderPoint T(int tableId) => _tables[tableId - 1];

        // ---------- 1. Ten points, own identity, Lobby never bypassed ----------

        [Test]
        public void TABLES02_TenTablePointsHaveUniqueIdsAndEveryRequestKeepsItsTableAndWaitsForTheLobby()
        {
            Assert.That(_tables.Select(t => t.TableId.Value), Is.EqualTo(Enumerable.Range(1, TableCount)));
            Assert.That(_tables.Select(t => t.Id.Value), Is.EqualTo(Enumerable.Range(FirstTableInteractable, TableCount)));
            var interactables = _tables.Select(t => t.Id).Append(_vehicle.Id).Append(_pickup.Id).ToList();
            Assert.That(interactables.Distinct().Count(), Is.EqualTo(interactables.Count), "Interactable ids are unique across all Lobby points.");
            Assert.That(_tables.Select(t => t.Origin).Distinct().Count(), Is.EqualTo(TableCount));

            // Request out of table order so origin cannot be confused with order id or request sequence.
            int[] requestOrder = { 7, 1, 10, 3, 9, 2, 8, 4, 6, 5 };
            var byTable = new Dictionary<int, OrderId>();
            foreach (int table in requestOrder)
            {
                OrderId id = Request(T(table), Mixed);
                byTable.Add(table, id);
                IReadOnlyOrder order = _orders.Get(id);
                Assert.That(order.Origin, Is.EqualTo(OrderOrigin.ForTable(new TableId(table))));
                Assert.That(order.Origin.Type, Is.EqualTo(OrderType.DineIn));
                Assert.That(order.Origin.TableId.Value, Is.EqualTo(table));
                Assert.That(order.Origin.VehicleId.IsValid, Is.False);
                Assert.That(order.Status, Is.EqualTo(OrderStatus.WaitingForLobby));
                Assert.That(T(table).ActiveOrder, Is.EqualTo(id));
                Assert.That(T(table).Query(_context).PromptKey, Is.EqualTo(OrderPoint.TakeOrderPromptKey));
            }
            Assert.That(byTable.Values.Distinct().Count(), Is.EqualTo(TableCount));
            Assert.That(_orders.Active.Count, Is.EqualTo(TableCount));

            // Lobby bypass guard: stray UI events and stall claims do nothing until the Lobby takes the order.
            foreach (OrderId id in byTable.Values)
            {
                _events.Publish(new OrderEntryConfirmed(id, Mixed));
                _events.Publish(new OrderSendRequested(id));
            }
            Assert.That(_entry.Enter(), Is.False);
            Assert.That(_queue.Tickets, Is.Empty);
            Assert.That(_queue.ClaimNext(ItemKind.Drink, new PreparationId(900)).ReasonKey, Is.EqualTo("stall.no_ticket.drink"));
            Assert.That(_orders.Active.All(o => o.Status == OrderStatus.WaitingForLobby), Is.True);

            // A second customer cannot open a second order on an occupied table.
            Assert.That(T(7).RequestCustomerService(new CustomerId(99), Mixed).ReasonKey, Is.EqualTo("order.point.occupied"));
            Assert.That(T(7).ActiveOrder, Is.EqualTo(byTable[7]));
        }

        // ---------- 2. Several tables at once: FIFO per kind in send order ----------

        [Test]
        public void TABLES02_FourTablesAndTheVehicleAreServedFifoPerKindInSendOrder()
        {
            ItemRequest[] drinkOnly = { new ItemRequest("drink.b", 1) };
            ItemRequest[] cakeOnly = { new ItemRequest("cake.b", 1) };
            // Requested in one order, sent in another: tickets follow send order, never request order.
            OrderId t7 = Request(T(7), Mixed);
            OrderId t2 = Request(T(2), drinkOnly);
            OrderId t10 = Request(T(10), cakeOnly);
            OrderId t4 = Request(T(4), Mixed);
            OrderId vehicle = Request(_vehicle, Mixed);

            TakeOrder(T(10), t10, cakeOnly);
            _clock.Advance(1);
            TakeOrder(_vehicle, vehicle, Mixed);
            _clock.Advance(1);
            TakeOrder(T(2), t2, drinkOnly);
            _clock.Advance(1);
            TakeOrder(T(7), t7, Mixed);
            _clock.Advance(1);
            TakeOrder(T(4), t4, Mixed);

            Assert.That(_queue.Tickets, Is.EqualTo(new[] { t10, vehicle, t2, t7, t4 }));
            foreach (OrderId ticket in _queue.Tickets)
            {
                Assert.That(_orders.Get(ticket).Origin.IsValid, Is.True, "Every ticket identifies its source point.");
            }

            var drinkClaims = new List<OrderId>();
            while (_queue.HasPending(ItemKind.Drink)) { drinkClaims.Add(Claim(ItemKind.Drink).OrderId); }
            var cakeClaims = new List<OrderId>();
            while (_queue.HasPending(ItemKind.Cake)) { cakeClaims.Add(Claim(ItemKind.Cake).OrderId); }

            Assert.That(drinkClaims, Is.EqualTo(new[] { vehicle, t2, t7, t4 }));
            Assert.That(cakeClaims, Is.EqualTo(new[] { t10, vehicle, t7, t4 }));
            Assert.That(drinkClaims.Select(Where), Is.EqualTo(new[] { "V1", "T2", "T7", "T4" }));
            Assert.That(cakeClaims.Select(Where), Is.EqualTo(new[] { "T10", "V1", "T7", "T4" }));
            Assert.That(_queue.ClaimNext(ItemKind.Drink, new PreparationId(900)).ReasonKey, Is.EqualTo("stall.no_ticket.drink"));
        }

        // ---------- 3. Delivery identity ----------

        [Test]
        public void TABLES02_BundleIsRejectedAtEveryWrongTableAndTheVehicleAndCompletesOnlyAtItsOwnTable()
        {
            var ids = new Dictionary<int, OrderId>();
            for (int table = 1; table <= TableCount; table++) { ids[table] = Request(T(table), Mixed); }
            OrderId takeaway = Request(_vehicle, Mixed);

            // Table 7 and table 1 both live with the same item mix; table 7 was sent first.
            TakeOrder(T(7), ids[7], Mixed);
            _clock.Advance(1);
            TakeOrder(T(1), ids[1], Mixed);
            MakeReady(ids[7]);
            ServedOrder bundle = Carry(ids[7]);

            int attempts = 0;
            foreach (OrderPoint wrong in _tables.Where(t => t.TableId.Value != 7).Cast<OrderPoint>().Append(_vehicle))
            {
                Assert.That(wrong.Query(_context).PromptKey, Is.EqualTo(OrderPoint.DeliverPromptKey));
                wrong.Execute(_context);
                attempts++;
                Assert.That(_orders.Get(ids[7]).Status, Is.EqualTo(OrderStatus.PickedUpByLobby), wrong.name);
                Assert.That(ReferenceEquals(_hands.Current, bundle), Is.True, "The bundle stays in hand after " + wrong.name);
                Assert.That(bundle.IsRetired, Is.False);
                Assert.That(_blocked[_blocked.Count - 1].ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
                Assert.That(_blocked[_blocked.Count - 1].InteractableId, Is.EqualTo(wrong.Id));
                Assert.That(((IOrderDelivery)_orders).TryGetInfo(ids[7], out OrderDeliveryInfo info) && info.DeliveryAttempts == attempts, Is.True);
            }
            Assert.That(_orders.Get(ids[1]).Status, Is.EqualTo(OrderStatus.SentToStall), "Table 1's own order is untouched.");
            Assert.That(_orders.Get(takeaway).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(T(1).ActiveOrder, Is.EqualTo(ids[1]));

            T(7).Execute(_context);
            Assert.That(_orders.Get(ids[7]).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_hands.Current, Is.Null);

            // Table 1's identical order is now carried: table 7 is free (no customer) and never accepts it.
            MakeReady(ids[1]);
            ServedOrder second = Carry(ids[1]);
            Assert.That(T(7).Query(_context).BlockedReasonKey, Is.EqualTo("order.delivery.no_customer"));
            T(7).Execute(_context);
            Assert.That(ReferenceEquals(_hands.Current, second), Is.True);
            Assert.That(_orders.Get(ids[1]).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            T(1).Execute(_context);
            Assert.That(_orders.Get(ids[1]).Status, Is.EqualTo(OrderStatus.Completed));
        }

        [Test]
        public void TABLES02_TableSevenOrderIsNeverDeliverableAtTableOneWithTheSameMixAndCustomerNumber()
        {
            // Same items and even the same customer number: the table identity alone must decide.
            OrderId t1 = T(1).RequestCustomerService(new CustomerId(5), Mixed).Value;
            OrderId t7 = T(7).RequestCustomerService(new CustomerId(5), Mixed).Value;
            TakeOrder(T(7), t7, Mixed);
            MakeReady(t7);
            Carry(t7);
            var delivery = (IOrderDelivery)_orders;
            Assert.That(delivery.Deliver(t7, new ActorRef(1), new DeliveryTarget(T(1).Origin, new CustomerId(5))).ReasonKey,
                Is.EqualTo("order.delivery.wrong_target"));
            T(1).Execute(_context);
            Assert.That(_orders.Get(t7).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(_orders.Get(t1).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            T(7).Execute(_context);
            Assert.That(_orders.Get(t7).Status, Is.EqualTo(OrderStatus.Completed));
        }

        // ---------- 4. Mixed drink + cake across tables with the 1+1 Ready shelf ----------

        [Test]
        public void TABLES02_MixedOrdersAcrossTablesRespectReadyCapacityAndBundlesHoldOnlyTheirOwnItems()
        {
            ItemRequest[] t3Items = { new ItemRequest("drink.a", 1), new ItemRequest("cake.a", 1) };
            ItemRequest[] t8Items = { new ItemRequest("drink.b", 1) };
            ItemRequest[] t5Items = { new ItemRequest("cake.b", 1), new ItemRequest("drink.a", 1) };
            OrderId t3 = Request(T(3), t3Items);
            OrderId t8 = Request(T(8), t8Items);
            OrderId t5 = Request(T(5), t5Items);
            TakeOrder(T(3), t3, t3Items);
            _clock.Advance(1);
            TakeOrder(T(8), t8, t8Items);
            _clock.Advance(1);
            TakeOrder(T(5), t5, t5Items);

            var t3Drink = Prepare(ItemKind.Drink, t3);
            Assert.That(_shelf.PlaceReady(t3Drink).IsSuccess, Is.True);
            var t8Drink = Prepare(ItemKind.Drink, t8);
            Assert.That(_shelf.PlaceReady(t8Drink).ReasonKey, Is.EqualTo("ready.slot_full"), "One drink on the shelf at a time.");
            Assert.That(_pickup.Query(_context).BlockedReasonKey, Is.EqualTo("ready.no_ready_order"), "Partial table 3 is not pickable.");
            var t3Cake = Prepare(ItemKind.Cake, t3);
            Assert.That(_shelf.PlaceReady(t3Cake).IsSuccess, Is.True);
            Assert.That(_orders.Get(t3).Status, Is.EqualTo(OrderStatus.Ready));

            ServedOrder bundle3 = Carry(t3);
            AssertOwnItems(bundle3, t3, "drink.a", "cake.a");
            T(8).Execute(_context);
            T(5).Execute(_context);
            Assert.That(_orders.Get(t3).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            T(3).Execute(_context);
            Assert.That(_orders.Get(t3).Status, Is.EqualTo(OrderStatus.Completed));

            // Shelf now holds table 8's finished drink next to table 5's cake: only table 8 is complete.
            Assert.That(_shelf.PlaceReady(t8Drink).IsSuccess, Is.True);
            var t5Cake = Prepare(ItemKind.Cake, t5);
            Assert.That(_shelf.PlaceReady(t5Cake).IsSuccess, Is.True);
            Assert.That(_orders.Get(t8).Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(_orders.Get(t5).Status, Is.EqualTo(OrderStatus.InPreparation));
            Assert.That(_shelf.NextReadyOrder, Is.EqualTo(t8));
            ServedOrder bundle8 = Carry(t8);
            AssertOwnItems(bundle8, t8, "drink.b");
            Assert.That(_shelf.Occupied(ItemKind.Cake), Is.True, "Table 5's cake stays on the shelf.");
            Assert.That(_shelf.Occupied(ItemKind.Drink), Is.False);
            T(5).Execute(_context);
            Assert.That(_orders.Get(t8).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            T(8).Execute(_context);
            Assert.That(_orders.Get(t8).Status, Is.EqualTo(OrderStatus.Completed));

            var t5Drink = Prepare(ItemKind.Drink, t5);
            Assert.That(_shelf.PlaceReady(t5Drink).IsSuccess, Is.True);
            ServedOrder bundle5 = Carry(t5);
            AssertOwnItems(bundle5, t5, "drink.a", "cake.b");
            Assert.That(bundle5.Items, Has.Member(t5Cake).And.Member(t5Drink));
            T(5).Execute(_context);
            Assert.That(_orders.Get(t5).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(_orders.Active, Is.Empty);
            Assert.That(_queue.Tickets, Is.Empty);
        }

        // ---------- 5. Completion frees exactly one table ----------

        [Test]
        public void TABLES02_CompletionFreesOnlyItsTableWhichThenAcceptsANewCustomerAndRejectsASecondCompletion()
        {
            var ids = new Dictionary<int, OrderId>();
            for (int table = 1; table <= TableCount; table++) { ids[table] = Request(T(table), Mixed); }
            TakeOrder(T(4), ids[4], Mixed);
            MakeReady(ids[4]);
            Carry(ids[4]);
            T(4).Execute(_context);

            Assert.That(_orders.Get(ids[4]).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(T(4).ActiveOrder, Is.EqualTo(default(OrderId)));
            Assert.That(T(4).Query(_context).Availability.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            for (int table = 1; table <= TableCount; table++)
            {
                if (table == 4) { continue; }
                Assert.That(T(table).ActiveOrder, Is.EqualTo(ids[table]), "Table " + table + " keeps its order.");
                Assert.That(_orders.Get(ids[table]).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
                Assert.That(T(table).RequestCustomerService(new CustomerId(900 + table), Mixed).ReasonKey, Is.EqualTo("order.point.occupied"));
            }
            Assert.That(_orders.Active.Count, Is.EqualTo(TableCount - 1));

            var delivery = (IOrderDelivery)_orders;
            Assert.That(delivery.Complete(ids[4]).ReasonKey, Is.EqualTo("order.transition.invalid"), "Completion twice is rejected.");
            Assert.That(delivery.Deliver(ids[4], new ActorRef(1), new DeliveryTarget(T(4).Origin, new CustomerId(4))).IsSuccess, Is.False);
            int blocked = _blocked.Count;
            T(4).Execute(_context);
            Assert.That(_blocked.Count, Is.EqualTo(blocked), "A free table ignores interaction.");
            Assert.That(_orders.Get(ids[4]).Status, Is.EqualTo(OrderStatus.Completed));

            OrderId next = Request(T(4), Mixed);
            Assert.That(next, Is.Not.EqualTo(ids[4]));
            Assert.That(_orders.Get(next).Origin, Is.EqualTo(OrderOrigin.ForTable(new TableId(4))));
            Assert.That(_orders.Get(next).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(T(4).ActiveOrder, Is.EqualTo(next));
            TakeOrder(T(4), next, Mixed);
            MakeReady(next);
            Carry(next);
            T(4).Execute(_context);
            Assert.That(_orders.Get(next).Status, Is.EqualTo(OrderStatus.Completed));
            Assert.That(T(4).ActiveOrder.IsValid, Is.False);
        }

        // ---------- 6. Takeaway vehicle unchanged next to ten tables ----------

        [Test]
        public void TABLES02_VehicleFlowIsUnchangedWhileTenTablesAreLive()
        {
            var tableIds = new List<OrderId>();
            for (int table = 1; table <= TableCount; table++) { tableIds.Add(Request(T(table), Mixed)); }
            OrderId takeaway = Request(_vehicle, Mixed);
            var statuses = new List<OrderStatus> { _orders.Get(takeaway).Status };
            _events.Subscribe<OrderStatusChanged>(changed => { if (changed.OrderId == takeaway) { statuses.Add(changed.Status); } });

            TakeOrder(_vehicle, takeaway, Mixed);
            MakeReady(takeaway);
            Carry(takeaway);
            T(1).Execute(_context);
            T(10).Execute(_context);
            Assert.That(_orders.Get(takeaway).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            _vehicle.Execute(_context);

            Assert.That(statuses, Is.EqualTo(new[]
            {
                OrderStatus.WaitingForLobby, OrderStatus.TakingOrder, OrderStatus.Entered, OrderStatus.SentToStall, OrderStatus.InPreparation,
                OrderStatus.Ready, OrderStatus.PickedUpByLobby, OrderStatus.Delivered, OrderStatus.Completed
            }));
            IReadOnlyOrder order = _orders.Get(takeaway);
            Assert.That(order.Origin, Is.EqualTo(OrderOrigin.ForVehicle(new VehicleId(1))));
            Assert.That(order.Origin.TableId.IsValid, Is.False);
            Assert.That(_vehicle.ActiveOrder.IsValid, Is.False);
            Assert.That(tableIds.All(id => _orders.Get(id).Status == OrderStatus.WaitingForLobby), Is.True);
            Assert.That(_orders.Active.Count, Is.EqualTo(TableCount));
        }

        // ---------- 8. Order entry at a two-digit table ----------

        [Test]
        public void TABLES02_OrderEntryAtTableTenStaysBoundToTableTenWhenTheLobbyVisitsTableNineMidway()
        {
            ItemRequest[] t9Items = { new ItemRequest("drink.b", 1) };
            OrderId t10 = Request(T(10), Mixed);
            OrderId t9 = Request(T(9), t9Items);

            T(10).Execute(_context);
            Assert.That(_entry.IsOpen && _entry.OrderId == t10, Is.True);
            Assert.That(_entry.Items, Is.EqualTo(Mixed));
            Assert.That(_entry.Enter(), Is.True);
            Assert.That(_orders.Get(t10).Status, Is.EqualTo(OrderStatus.Entered));

            // The Lobby walks to table 9 before sending table 10: the entry session moves with it.
            TakeOrder(T(9), t9, t9Items);
            Assert.That(_orders.Get(t10).Status, Is.EqualTo(OrderStatus.Entered), "Table 10 is not sent by table 9's entry.");
            Assert.That(_queue.Tickets, Is.EqualTo(new[] { t9 }));

            // Walking back takes game time; equal send timestamps fall back to the pinned order-id tie-break (TC-ORDER-003).
            _clock.Advance(1);
            Assert.That(T(10).Query(_context).PromptKey, Is.EqualTo(OrderPoint.ContinueOrderPromptKey));
            T(10).Execute(_context);
            Assert.That(_entry.OrderId, Is.EqualTo(t10));
            Assert.That(_entry.Items, Is.EqualTo(Mixed));
            Assert.That(_entry.Send(), Is.True);
            Assert.That(_queue.Tickets, Is.EqualTo(new[] { t9, t10 }));
            Assert.That(_orders.Get(t10).Origin.TableId.Value, Is.EqualTo(10));
        }

        // ---------- helpers ----------

        private OrderId Request(OrderPoint point, IReadOnlyList<ItemRequest> items)
        {
            Result<OrderId> result = point.RequestCustomerService(new CustomerId(++_customer), items);
            Assert.That(result.IsSuccess, Is.True, result.ReasonKey);
            return result.Value;
        }

        private string Where(OrderId id)
        {
            OrderOrigin origin = _orders.Get(id).Origin;
            return origin.Type == OrderType.DineIn ? "T" + origin.TableId.Value : "V" + origin.VehicleId.Value;
        }

        private void TakeOrder(OrderPoint point, OrderId id, IReadOnlyList<ItemRequest> items)
        {
            Assert.That(point.Query(_context).PromptKey, Is.EqualTo(OrderPoint.TakeOrderPromptKey));
            point.Execute(_context);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.TakingOrder));
            Assert.That(_entry.IsOpen, Is.True);
            Assert.That(_entry.OrderId, Is.EqualTo(id));
            Assert.That(_entry.Items, Is.EqualTo(items));
            Assert.That(_entry.Enter(), Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Entered));
            Assert.That(_entry.Send(), Is.True);
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.SentToStall));
            Assert.That(_entry.IsOpen, Is.False);
        }

        private OrderItemRef Claim(ItemKind kind)
        {
            Result<OrderItemRef> claim = _queue.ClaimNext(kind, new PreparationId(++_preparation));
            Assert.That(claim.IsSuccess, Is.True, claim.ReasonKey);
            return claim.Value;
        }

        private DeliveryAdapterTests.PreparedBag Prepare(ItemKind kind, OrderId expected)
        {
            var item = Child("Prepared " + kind).AddComponent<DeliveryAdapterTests.PreparedBag>();
            item.Kind = kind;
            item.Binding = Claim(kind);
            Assert.That(item.Binding.OrderId, Is.EqualTo(expected), "Claimed " + kind + " for " + Where(item.Binding.OrderId));
            return item;
        }

        private void MakeReady(OrderId id)
        {
            foreach (IReadOnlyOrderItem item in _orders.Get(id).Items)
            {
                Assert.That(_shelf.PlaceReady(Prepare(item.Kind, id)).IsSuccess, Is.True);
            }
            Assert.That(_orders.Get(id).Status, Is.EqualTo(OrderStatus.Ready));
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

        private void AssertOwnItems(ServedOrder bundle, OrderId id, params string[] definitions)
        {
            IReadOnlyOrder order = _orders.Get(id);
            Assert.That(bundle.Items.Count, Is.EqualTo(order.Items.Count));
            Assert.That(bundle.Items.All(item => item.BoundItem.OrderId == id), Is.True, "No item from another table.");
            var carried = bundle.Items
                .Select(item => order.Items.Single(own => own.Id == item.BoundItem.OrderItemId).ItemDefinitionId)
                .OrderBy(d => d, System.StringComparer.Ordinal);
            Assert.That(carried, Is.EqualTo(definitions.OrderBy(d => d, System.StringComparer.Ordinal)));
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

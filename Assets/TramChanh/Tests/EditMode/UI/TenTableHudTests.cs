using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Orders;
using TramChanh.UI.Hud;

namespace TramChanh.Tests.EditMode.UI
{
    /// <summary>
    /// TABLES-02: the order HUD with ten dine-in tables plus the takeaway vehicle, rendered through the
    /// real Vietnamese prompt table and driven by the real OrderService, queue and Ready shelf.
    /// </summary>
    public sealed class TenTableHudTests
    {
        private const int TableCount = 10;
        private static readonly ItemRequest[] Mixed = { new ItemRequest("drink.slice", 1), new ItemRequest("cake.dev.tbd", 1) };
        private static readonly Regex RawKey = new Regex(@"\b[a-z][a-z0-9_]*(\.[A-Za-z0-9_]+){1,}\b");
        private readonly ActorRef _lobby = new ActorRef(1);
        private PromptTableFile _text;
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private int _preparation;

        [SetUp]
        public void SetUp()
        {
            _text = PromptTableFile.LoadMain();
            _events = new EventBus();
            _clock = new ManualClock();
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink.slice", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake.dev.tbd", ItemKind.Cake)
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

        private static OrderOrigin Table(int id) => OrderOrigin.ForTable(new TableId(id));
        private static OrderOrigin Vehicle(int id) => OrderOrigin.ForVehicle(new VehicleId(id));

        private string Render(HudText text)
        {
            string rendered = text.Render(k => _text.Resolve(k, "vi"));
            Assert.That(rendered, Does.Not.Contain("{"), rendered);
            Assert.That(RawKey.IsMatch(rendered), Is.False, rendered);
            return rendered;
        }

        private OrderId Request(OrderOrigin origin, int customer) => _orders.RequestService(origin, new CustomerId(customer), Mixed).Value;

        private Dictionary<int, OrderId> RequestAllTables()
        {
            var ids = new Dictionary<int, OrderId>();
            for (int table = 1; table <= TableCount; table++) { ids[table] = Request(Table(table), table); }
            return ids;
        }

        [Test]
        public void TABLES02_ElevenLiveOrdersAreLabelledBan1ToBan10AndXe1WithTheirOwnDestination()
        {
            using var list = new ActiveOrderListModel(_orders, _events);
            Dictionary<int, OrderId> ids = RequestAllTables();
            OrderId takeaway = Request(Vehicle(1), 50);

            var labels = new List<string>();
            foreach (IReadOnlyOrder order in _orders.Active)
            {
                OrderRow row = ActiveOrderListModel.Describe(order);
                string origin = Render(row.Origin);
                labels.Add(origin);
                Assert.That(Render(row.NextStep), Is.EqualTo("Tới " + origin + " nhận đơn"));
                Assert.That(Render(row.Header), Does.StartWith(origin + " · "));
            }
            Assert.That(labels, Is.EqualTo(Enumerable.Range(1, TableCount).Select(n => "Bàn " + n).Append("Xe 1")));
            Assert.That(Render(HudKeys.Origin(_orders.Get(ids[10]).Origin)), Is.EqualTo("Bàn 10"));
            Assert.That(Render(HudKeys.Origin(_orders.Get(takeaway).Origin)), Is.EqualTo("Xe 1"));

            // Eleven orders: the panel stays at MaxRows rows plus one "+N đơn khác" line; the title counts all.
            Assert.That(list.Rows.Count, Is.EqualTo(ActiveOrderListModel.MaxRows));
            Assert.That(list.HiddenCount, Is.EqualTo(TableCount + 1 - ActiveOrderListModel.MaxRows));
            Assert.That(list.Rows.Select(r => r.Id).Distinct().Count(), Is.EqualTo(list.Rows.Count));
            Assert.That(Render(list.Title), Is.EqualTo("Đơn hàng (11)"));
            Assert.That(Render(HudText.Of(HudKeys.OrdersMore, list.HiddenCount)), Is.EqualTo("+7 đơn khác"));
            Assert.That(list.Primary.Id, Is.EqualTo(ids[1]), "All waiting: the oldest table is primary.");
            Assert.That(Render(list.PrimaryNextStep), Is.EqualTo("Tiếp theo: Tới Bàn 1 nhận đơn"));
            foreach (OrderRow row in list.Rows)
            {
                Assert.That(Render(row.Header).Length, Is.LessThanOrEqualTo(40), "Row header fits one HUD line.");
            }
        }

        [Test]
        public void TABLES02_EveryStageHintOfATwoDigitTableNamesThatTable()
        {
            using var list = new ActiveOrderListModel(_orders, _events);
            Dictionary<int, OrderId> ids = RequestAllTables();
            OrderId id = ids[10];
            string Hint() => Render(ActiveOrderListModel.Describe(_orders.Get(id)).NextStep);

            Assert.That(Hint(), Is.EqualTo("Tới Bàn 10 nhận đơn"));
            Assert.That(_orders.BeginTaking(id, _lobby, Table(10)).IsSuccess, Is.True);
            Assert.That(list.Primary.Id, Is.EqualTo(id), "An open entry outranks waiting tables.");
            Assert.That(Hint(), Is.EqualTo("Nhập đơn Bàn 10 rồi gửi tới quầy"), "The entry hint says which table to go back to.");
            Assert.That(Render(list.PrimaryNextStep), Is.EqualTo("Tiếp theo: Nhập đơn Bàn 10 rồi gửi tới quầy"));
            Assert.That(_orders.Enter(id, Mixed).IsSuccess, Is.True);
            Assert.That(Hint(), Is.EqualTo("Gửi đơn Bàn 10 tới quầy"));
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            Place(ItemKind.Drink, id);
            Place(ItemKind.Cake, id);
            Assert.That(Hint(), Is.EqualTo("Lấy đơn ở quầy giao"));
            Assert.That(_shelf.PickUp(id, _lobby).IsSuccess, Is.True);
            Assert.That(Hint(), Is.EqualTo("Mang đơn tới Bàn 10"));
            Assert.That(list.Primary.Id, Is.EqualTo(id));
            Assert.That(list.Rows.Any(r => r.Id == id && r.IsPrimary), Is.True, "The carried table-10 order is listed despite overflow.");
            Assert.That(Render(list.PrimaryNextStep), Is.EqualTo("Tiếp theo: Mang đơn tới Bàn 10"));
            Assert.That(((IOrderDelivery)_orders).Deliver(id, _lobby, new DeliveryTarget(Table(10), new CustomerId(10))).IsSuccess, Is.True);
            Assert.That(Hint(), Is.EqualTo("Hoàn tất đơn ở Bàn 10"));
            Assert.That(((IOrderDelivery)_orders).Complete(id).IsSuccess, Is.True);
            Assert.That(list.Rows.Any(r => r.Id == id), Is.False);
            Assert.That(list.HiddenCount + list.Rows.Count, Is.EqualTo(TableCount - 1));
        }

        [Test]
        public void TABLES02_CarriedOrderAmongElevenPointsTheHintAtItsOwnTableNotAnotherWithTheSameItems()
        {
            using var list = new ActiveOrderListModel(_orders, _events);
            Dictionary<int, OrderId> ids = RequestAllTables();
            Request(Vehicle(1), 50);
            foreach (int table in new[] { 7, 1 })
            {
                Assert.That(_orders.BeginTaking(ids[table], _lobby, Table(table)).IsSuccess, Is.True);
                Assert.That(_orders.Enter(ids[table], Mixed).IsSuccess, Is.True);
                Assert.That(_orders.SendToStall(ids[table]).IsSuccess, Is.True);
                _clock.Advance(1);
            }
            Place(ItemKind.Drink, ids[7]);
            Place(ItemKind.Cake, ids[7]);
            Assert.That(_shelf.PickUp(ids[7], _lobby).IsSuccess, Is.True);

            Assert.That(list.Primary.Id, Is.EqualTo(ids[7]));
            Assert.That(Render(list.PrimaryNextStep), Is.EqualTo("Tiếp theo: Mang đơn tới Bàn 7"));
            OrderRow t1 = list.Rows.Single(r => r.Id == ids[1]);
            Assert.That(Render(t1.Origin), Is.EqualTo("Bàn 1"));
            Assert.That(Render(t1.NextStep), Is.EqualTo("Lấy túi trà ở kệ"), "Table 1's identical order is still at the stall.");
            Assert.That(list.Rows.Count, Is.EqualTo(ActiveOrderListModel.MaxRows));
            Assert.That(list.Rows.Count(r => r.IsPrimary), Is.EqualTo(1));
        }

        private void Place(ItemKind kind, OrderId expected)
        {
            Result<OrderItemRef> claim = _queue.ClaimNext(kind, new PreparationId(++_preparation));
            Assert.That(claim.IsSuccess, Is.True, claim.ReasonKey);
            Assert.That(claim.Value.OrderId, Is.EqualTo(expected));
            Assert.That(_shelf.PlaceReady(new Prepared(kind, claim.Value)).IsSuccess, Is.True);
        }

        private sealed class Prepared : IPreparedItem
        {
            public Prepared(ItemKind kind, OrderItemRef binding) { Kind = kind; BoundItem = binding; }
            public ItemKind Kind { get; }
            public OrderItemRef BoundItem { get; }
            public bool IsFinished => true;
            public int Quality => 100;
            public Result MarkReady() => Result.Success();
        }
    }
}

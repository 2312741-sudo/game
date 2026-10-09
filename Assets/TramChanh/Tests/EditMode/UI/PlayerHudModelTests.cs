using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using TramChanh.UI.Hud;
using UnityEngine;

namespace TramChanh.Tests.EditMode.UI
{
    /// <summary>MAIN-103: engine-free player HUD models driven by the real OrderService, queue and shelf.</summary>
    public sealed class PlayerHudModelTests
    {
        private static readonly ItemRequest[] Mixed = { new ItemRequest("drink.slice", 1), new ItemRequest("cake.dev.tbd", 1) };
        private static readonly Regex RawKey = new Regex(@"\b[a-z][a-z0-9_]*(\.[A-Za-z0-9_]+){1,}\b");
        private readonly ActorRef _lobby = new ActorRef(1);
        private EventBus _events;
        private ManualClock _clock;
        private OrderService _orders;
        private StallTicketQueue _queue;
        private ReadyShelf _shelf;
        private HeldItemSlot _hands;
        private int _preparation;
        private double _now;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _clock = new ManualClock();
            _orders = new OrderService(_clock, _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink.slice", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake.dev.tbd", ItemKind.Cake)
            }));
            _queue = new StallTicketQueue(_orders);
            _shelf = new ReadyShelf(_orders, _queue, 1, 1);
            _hands = new HeldItemSlot(_events);
            _preparation = 0;
            _now = 0d;
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(_orders.ObserverFaults, Is.Empty);
            _shelf.Dispose();
            _events.Dispose();
        }

        private static OrderOrigin Vehicle(int id) => OrderOrigin.ForVehicle(new VehicleId(id));
        private static OrderOrigin Table(int id) => OrderOrigin.ForTable(new TableId(id));
        private OrderId Request(OrderOrigin origin, int customer = 1) => _orders.RequestService(origin, new CustomerId(customer), Mixed).Value;

        private void Send(OrderId id, OrderOrigin origin)
        {
            Assert.That(_orders.BeginTaking(id, _lobby, origin).IsSuccess, Is.True);
            Assert.That(_orders.Enter(id, Mixed).IsSuccess, Is.True);
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
        }

        private Prepared Claim(ItemKind kind)
        {
            Result<OrderItemRef> claim = _queue.ClaimNext(kind, new PreparationId(++_preparation));
            Assert.That(claim.IsSuccess, Is.True, claim.ReasonKey);
            return new Prepared(kind, claim.Value);
        }

        [Test]
        public void MAIN103_HudTextRendersNestedArgumentsAndToleratesBadTemplates()
        {
            var table = new Dictionary<string, string> { ["next"] = "Mang đơn tới {0}", ["veh"] = "Xe {0}", ["bad"] = "{0} {x} {7} {", ["n"] = "{0}%" };
            Func<string, string> resolve = k => table.TryGetValue(k, out string v) ? v : k;
            Assert.That(HudText.Of("next", HudText.Of("veh", 1)).Render(resolve), Is.EqualTo("Mang đơn tới Xe 1"));
            Assert.That(HudText.Of("bad", "a").Render(resolve), Is.EqualTo("a {x} {7} {"));
            Assert.That(HudText.Of("n", 45).Render(resolve), Is.EqualTo("45%"));
            Assert.That(HudText.Empty.Render(resolve), Is.Empty);
            Assert.That(HudText.Of("next", HudText.Of("veh", 1)), Is.EqualTo(HudText.Of("next", HudText.Of("veh", 1))));
            Assert.That(HudText.Of("next", HudText.Of("veh", 1)), Is.Not.EqualTo(HudText.Of("next", HudText.Of("veh", 2))));
            Assert.That(HudText.Of("next", HudText.Of("veh", 1)).Keys(), Is.EqualTo(new[] { "next", "veh" }));
        }

        [Test]
        public void MAIN103_MixedVehicleOrderShowsOriginItemsStageAndWhereToGoAtEveryStep()
        {
            PromptTableFile text = PromptTableFile.LoadMain();
            using var list = new ActiveOrderListModel(_orders, _events);
            int changes = 0;
            list.Changed += () => changes++;
            string Render(HudText t) { string s = t.Render(k => text.Resolve(k, "vi")); Assert.That(RawKey.IsMatch(s), Is.False, s); return s; }
            OrderRow Row() { Assert.That(list.Rows.Count, Is.EqualTo(1)); return list.Rows[0]; }

            OrderOrigin origin = Vehicle(1);
            OrderId id = Request(origin);
            Assert.That(changes, Is.GreaterThan(0), "Orders events refresh the list.");
            Assert.That(Render(Row().Origin), Is.EqualTo("Xe 1"));
            Assert.That(Render(Row().Stage), Is.EqualTo("Chờ nhận đơn"));
            Assert.That(Row().Items.Select(Render), Is.EqualTo(new[] { "Đồ uống (tên tạm) x1", "Bánh thử nghiệm x1" }));
            Assert.That(Render(Row().NextStep), Is.EqualTo("Tới Xe 1 nhận đơn"));
            Assert.That(Render(list.PrimaryNextStep), Is.EqualTo("Tiếp theo: Tới Xe 1 nhận đơn"));
            Assert.That(Render(list.Title), Is.EqualTo("Đơn hàng (1)"));

            Assert.That(_orders.BeginTaking(id, _lobby, origin).IsSuccess, Is.True);
            Assert.That(Row().NextStep.Key, Is.EqualTo(HudKeys.NextEnterOrder));
            Assert.That(_orders.Enter(id, Mixed).IsSuccess, Is.True);
            Assert.That(Row().NextStep.Key, Is.EqualTo(HudKeys.NextSendOrder));
            Assert.That(_orders.SendToStall(id).IsSuccess, Is.True);
            Assert.That(Render(Row().Stage), Is.EqualTo("Đã gửi quầy (0/2 món xong)"));
            Assert.That(Row().Items.Select(Render), Is.EqualTo(new[] { "Đồ uống (tên tạm): Chờ làm", "Bánh thử nghiệm: Chờ làm" }));
            Assert.That(Render(Row().NextStep), Is.EqualTo("Lấy túi trà ở kệ"));

            Prepared drink = Claim(ItemKind.Drink);
            Assert.That(Render(Row().Stage), Is.EqualTo("Đang làm (0/2 món xong)"));
            Assert.That(Render(Row().NextStep), Is.EqualTo("Làm xong Đồ uống (tên tạm) rồi đặt lên quầy giao"));
            Assert.That(_shelf.PlaceReady(drink).IsSuccess, Is.True);
            Assert.That(Render(Row().Stage), Is.EqualTo("Đang làm (1/2 món xong)"));
            Assert.That(Render(Row().NextStep), Is.EqualTo("Làm bánh ở bếp nướng"));
            Prepared cake = Claim(ItemKind.Cake);
            Assert.That(_shelf.PlaceReady(cake).IsSuccess, Is.True);
            Assert.That(Render(Row().Stage), Is.EqualTo("Sẵn sàng"));
            Assert.That(Render(Row().NextStep), Is.EqualTo("Lấy đơn ở quầy giao"));

            Assert.That(_shelf.PickUp(id, _lobby).IsSuccess, Is.True);
            Assert.That(Render(Row().Stage), Is.EqualTo("Đang mang đi"));
            Assert.That(Render(Row().NextStep), Is.EqualTo("Mang đơn tới Xe 1"));
            Assert.That(((IOrderDelivery)_orders).Deliver(id, _lobby, new DeliveryTarget(origin, new CustomerId(1))).IsSuccess, Is.True);
            Assert.That(Render(Row().NextStep), Is.EqualTo("Hoàn tất đơn ở Xe 1"));
            Assert.That(((IOrderDelivery)_orders).Complete(id).IsSuccess, Is.True);
            Assert.That(list.Rows, Is.Empty);
            Assert.That(Render(list.PrimaryNextStep), Is.EqualTo("Chưa có đơn, chờ khách tới"));
        }

        [Test]
        public void MAIN103_PrimaryOrderIsTheMostUrgentAndAlwaysListed()
        {
            using var list = new ActiveOrderListModel(_orders, _events);
            OrderId first = Request(Table(1), 1);
            OrderId ready = default;
            for (int i = 1; i <= 5; i++)
            {
                OrderId id = Request(Vehicle(i), 10 + i);
                if (i == 5) { ready = id; }
            }
            Assert.That(list.Primary.Id, Is.EqualTo(first), "All waiting: the oldest is primary.");
            Send(ready, Vehicle(5));
            Prepared drink = Claim(ItemKind.Drink);
            Prepared cake = Claim(ItemKind.Cake);
            Assert.That(_shelf.PlaceReady(drink).IsSuccess && _shelf.PlaceReady(cake).IsSuccess, Is.True);
            Assert.That(list.Primary.Id, Is.EqualTo(ready));
            Assert.That(list.Primary.Status, Is.EqualTo(OrderStatus.Ready));
            Assert.That(list.Rows.Count, Is.EqualTo(ActiveOrderListModel.MaxRows));
            Assert.That(list.HiddenCount, Is.EqualTo(2));
            Assert.That(list.Rows.Count(r => r.IsPrimary), Is.EqualTo(1));
            Assert.That(list.Rows.Any(r => r.Id == ready && r.IsPrimary), Is.True, "Primary survives overflow.");
            Assert.That(list.PrimaryNextStep, Is.EqualTo(HudText.Of(HudKeys.NextPrefix, HudText.Of(HudKeys.NextPickUp))));
            Assert.That(list.Title, Is.EqualTo(HudText.Of(HudKeys.OrdersTitle, 6)));

            Assert.That(_orders.Fail(first, FailureReason.CustomerLeft).IsSuccess, Is.True);
            Assert.That(list.Rows.Any(r => r.Id == first), Is.False, "Failed orders leave the list.");
            Assert.That(list.HiddenCount, Is.EqualTo(1));
        }

        [Test]
        public void MAIN103_OrderListNeverThrowsIntoThePublisher()
        {
            var broken = new BrokenOrders();
            using var list = new ActiveOrderListModel(broken, _events);
            Assert.That(() => _events.Publish(new OrderStatusChanged(new OrderId(1), OrderStatus.Ready)), Throws.Nothing);
            Assert.That(list.LastFault, Is.Not.Null);
            Assert.That(list.Rows, Is.Empty);
        }

        [Test]
        public void MAIN103_HeldPanelShowsEmptyHandsThenFeedbackStateAndNextAction()
        {
            using var held = new HeldItemFeedbackModel(_hands, _events, _orders);
            Assert.That(held.HasItem, Is.False);
            Assert.That(held.Name, Is.EqualTo(HudText.Of(HudKeys.HeldEmpty)));
            Assert.That(held.NextAction, Is.EqualTo(HudText.Of(HudKeys.HeldEmptyHint)));

            var cup = new FeedbackHoldable { StateKey = "cake.state.waiting", NextKey = "cake.fill" };
            int changes = 0;
            held.Changed += () => changes++;
            Assert.That(_hands.TryPickUp(cup), Is.True);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(held.Name, Is.EqualTo(HudText.Of("held.FeedbackHoldable")));
            Assert.That(held.State, Is.EqualTo(HudText.Of("cake.state.waiting")));
            Assert.That(held.NextAction, Is.EqualTo(HudText.Of("cake.fill")));
            Assert.That(held.Order.IsEmpty, Is.True);

            held.Refresh();
            Assert.That(changes, Is.EqualTo(1), "Polling without a change raises nothing.");
            cup.StateKey = "cake.state.cooking";
            cup.NextKey = "cake.wait";
            held.Refresh();
            Assert.That(changes, Is.EqualTo(2), "Timed state changes are caught by polling.");
            Assert.That(held.State, Is.EqualTo(HudText.Of("cake.state.cooking")));

            Assert.That(_hands.TryRelease(), Is.True);
            Assert.That(held.HasItem, Is.False);
        }

        [Test]
        public void MAIN103_HeldPreparedItemWithoutFeedbackUsesOrderNameStatusAndItsOwnAction()
        {
            using var held = new HeldItemFeedbackModel(_hands, _events, _orders);
            OrderId id = Request(Vehicle(1));
            Send(id, Vehicle(1));
            Prepared drink = Claim(ItemKind.Drink);
            var bag = new PreparedHoldable(drink);
            Assert.That(_hands.TryPickUp(bag), Is.True);
            Assert.That(held.Name, Is.EqualTo(HudText.Of("item.drink.slice")));
            Assert.That(held.State, Is.EqualTo(HudText.Of("order.item.status.InPreparation")));
            Assert.That(held.Order, Is.EqualTo(HudText.Of(HudKeys.HeldForOrder, HudText.Of(HudKeys.OriginVehicle, 1))));
            Assert.That(held.NextAction, Is.EqualTo(HudText.Of(HudKeys.NextDrinkSteps)), "No usable held action: the drink step guide.");

            var shake = new InteractionQuery(Availability.Available, "drink.bag.shake", InteractionKind.Hold, 1f);
            _events.Publish(new InteractionPromptChanged(default, shake, 0f, true));
            Assert.That(held.NextAction, Is.EqualTo(HudText.Of("drink.bag.shake")));
            _events.Publish(new InteractionPromptChanged(default, new InteractionQuery(Availability.Blocked("drink.need_ice_first"), "drink.bag.shake"), 0f, true));
            Assert.That(held.NextAction, Is.EqualTo(HudText.Of(HudKeys.NextDrinkSteps)), "A blocked held action is not the next step.");

            drink.Finished = true;
            held.Refresh();
            Assert.That(held.State, Is.EqualTo(HudText.Of(HudKeys.HeldStateFinished)));
            Assert.That(held.NextAction, Is.EqualTo(HudText.Of("ready.place")));

            Assert.That(_orders.Fail(id, FailureReason.CustomerLeft).IsSuccess, Is.True);
            Assert.That(held.Order, Is.EqualTo(HudText.Of(HudKeys.HeldOrderCancelled)));
        }

        [Test]
        public void MAIN103_CarriedOrderBundlePointsToItsOrigin()
        {
            using var held = new HeldItemFeedbackModel(_hands, _events, _orders);
            OrderId id = Request(Table(2));
            Send(id, Table(2));
            Prepared drink = Claim(ItemKind.Drink);
            Prepared cake = Claim(ItemKind.Cake);
            Assert.That(_shelf.PlaceReady(drink).IsSuccess && _shelf.PlaceReady(cake).IsSuccess, Is.True);
            Assert.That(_shelf.PickUp(id, _lobby).IsSuccess, Is.True);
            // Same keys as Lobby.ServedOrder, without referencing Lobby.
            var bundle = new FeedbackHoldable { StateKey = "order.status.PickedUpByLobby", NextKey = "hud.next.delivery" };
            Assert.That(_hands.TryPickUp(bundle), Is.True);
            Assert.That(held.State, Is.EqualTo(HudText.Of("order.status.PickedUpByLobby")));
            Assert.That(held.NextAction, Is.EqualTo(HudText.Of(HudKeys.NextDeliverTo, HudText.Of(HudKeys.OriginTable, 2))));
        }

        [Test]
        public void MAIN103_HeldNameFallsBackWhenTheTypeHasNoLocalizedName()
        {
            using var held = new HeldItemFeedbackModel(_hands, _events, null, key => false);
            Assert.That(_hands.TryPickUp(new PreparedHoldable(new Prepared(ItemKind.Cake, default))), Is.True);
            Assert.That(held.Name, Is.EqualTo(HudText.Of(HudKeys.HeldKindCake)));
            Assert.That(held.NextAction, Is.EqualTo(HudText.Of(HudKeys.NextCakeSteps)));
            Assert.That(_hands.TryRelease(), Is.True);
            Assert.That(_hands.TryPickUp(new FeedbackHoldable()), Is.True);
            Assert.That(held.Name, Is.EqualTo(HudText.Of(HudKeys.HeldUnknown)));
        }

        [Test]
        public void MAIN103_ToastAnnouncesMilestonesWithOriginAndExpires()
        {
            using var toast = new OrderToastModel(_orders, _events, () => _now, 3d);
            OrderOrigin origin = Vehicle(1);
            OrderId id = Request(origin);
            Assert.That(toast.IsVisible, Is.False, "Waiting is not announced.");
            Send(id, origin);
            Assert.That(toast.Current, Is.EqualTo(HudText.Of(HudKeys.ToastSent, HudText.Of(HudKeys.OriginVehicle, 1))));
            _now = 2.9d;
            Assert.That(toast.Tick(), Is.False);
            _now = 3.1d;
            Assert.That(toast.Tick(), Is.True);
            Assert.That(toast.IsVisible, Is.False);

            Prepared drink = Claim(ItemKind.Drink);
            Prepared cake = Claim(ItemKind.Cake);
            Assert.That(_shelf.PlaceReady(drink).IsSuccess && _shelf.PlaceReady(cake).IsSuccess, Is.True);
            Assert.That(toast.Current.Key, Is.EqualTo(HudKeys.ToastReady));
            Assert.That(_shelf.PickUp(id, _lobby).IsSuccess, Is.True);
            var delivery = (IOrderDelivery)_orders;
            Assert.That(delivery.Deliver(id, _lobby, new DeliveryTarget(Table(1), new CustomerId(1))).IsSuccess, Is.False);
            Assert.That(toast.Current, Is.EqualTo(HudText.Of(HudKeys.ToastWrongTarget, HudText.Of(HudKeys.OriginVehicle, 1))));
            Assert.That(toast.IsPositive, Is.False);
            Assert.That(delivery.Deliver(id, _lobby, new DeliveryTarget(origin, new CustomerId(1))).IsSuccess, Is.True);
            Assert.That(delivery.Complete(id).IsSuccess, Is.True);
            Assert.That(toast.Current, Is.EqualTo(HudText.Of(HudKeys.ToastCompleted, HudText.Of(HudKeys.OriginVehicle, 1))), "Completed orders stay queryable for their origin.");
            Assert.That(toast.IsPositive, Is.True);

            OrderId left = Request(Table(3), 7);
            Assert.That(_orders.Fail(left, FailureReason.CustomerLeft).IsSuccess, Is.True);
            Assert.That(toast.Current, Is.EqualTo(HudText.Of(HudKeys.ToastFailed, HudText.Of(HudKeys.OriginTable, 3))));
            Assert.That(toast.IsPositive, Is.False);
        }

        [Test]
        public void MAIN103_HoldProgressCaptionsHoldAndContinuousActions()
        {
            var hold = new InteractionQuery(Availability.Available, "cake.cut", InteractionKind.Hold, 2f);
            var blockedHold = new InteractionQuery(Availability.Blocked("cake.need_flip_first"), "cake.cut", InteractionKind.Hold, 2f);
            var fill = new InteractionQuery(Availability.Available, "cake.fill", InteractionKind.Continuous);
            var press = new InteractionQuery(Availability.Available, "cake.wrap");
            Assert.That(HoldProgressModel.ShowsBar(hold), Is.True);
            Assert.That(HoldProgressModel.ShowsBar(blockedHold), Is.False);
            Assert.That(HoldProgressModel.ShowsBar(fill), Is.False);
            Assert.That(HoldProgressModel.Caption(hold, 0.456f), Is.EqualTo(HudText.Of(HudKeys.HoldProgress, 45)));
            Assert.That(HoldProgressModel.Percent(float.NaN), Is.EqualTo(0));
            Assert.That(HoldProgressModel.Percent(-1f), Is.EqualTo(0));
            Assert.That(HoldProgressModel.Percent(1.5f), Is.EqualTo(100));
            Assert.That(HoldProgressModel.Caption(fill, 0f), Is.EqualTo(HudText.Of(HudKeys.HoldContinuous)));
            Assert.That(HoldProgressModel.Caption(press, 0f).IsEmpty, Is.True);
            Assert.That(HoldProgressModel.Caption(blockedHold, 0.5f).IsEmpty, Is.True);
        }

        [Test]
        public void MAIN103_HudNeverReferencesPreparationOrLobbyAssemblies()
        {
            foreach (var reference in typeof(HudKeys).Assembly.GetReferencedAssemblies())
            {
                Assert.That(new[] { "TramChanh.Drinks", "TramChanh.Cakes", "TramChanh.Lobby", "TramChanh.Stall" }, Has.No.Member(reference.Name));
            }
        }

        private sealed class Prepared : IPreparedItem
        {
            private bool _ready;
            public Prepared(ItemKind kind, OrderItemRef binding) { Kind = kind; BoundItem = binding; }
            public ItemKind Kind { get; }
            public OrderItemRef BoundItem { get; }
            /// <summary>Shelf placement needs a finished item; a held item starts unfinished.</summary>
            public bool Finished { get; set; } = true;
            public bool IsFinished => Finished;
            public int Quality => 100;
            public Result MarkReady()
            {
                if (_ready) { return Result.Fail("ready.already_ready"); }
                _ready = true;
                return Result.Success();
            }
        }

        private sealed class FeedbackHoldable : IHoldable, IPreparationFeedback
        {
            public string StateKey { get; set; }
            public string NextKey { get; set; }
            public string PreparationStateKey => StateKey;
            public string NextActionKey => NextKey;
            public Transform HandGrip => null;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }

        private sealed class PreparedHoldable : IHoldable, IPreparedItem
        {
            private readonly Prepared _inner;
            public PreparedHoldable(Prepared inner) { _inner = inner; inner.Finished = false; }
            public ItemKind Kind => _inner.Kind;
            public OrderItemRef BoundItem => _inner.BoundItem;
            public bool IsFinished => _inner.IsFinished;
            public int Quality => _inner.Quality;
            public Result MarkReady() => _inner.MarkReady();
            public Transform HandGrip => null;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }

        private sealed class BrokenOrders : IOrderService
        {
            private bool _armed;
            public IReadOnlyList<IReadOnlyOrder> Active
            {
                get
                {
                    if (_armed) { throw new InvalidOperationException("broken"); }
                    _armed = true;
                    return Array.Empty<IReadOnlyOrder>();
                }
            }
            public IReadOnlyOrder Get(OrderId id) => throw new InvalidOperationException("broken");
            public Result<OrderId> RequestService(OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested) => throw new NotSupportedException();
            public Result BeginTaking(OrderId id, ActorRef actor, OrderOrigin point) => throw new NotSupportedException();
            public Result Enter(OrderId id, IReadOnlyList<ItemRequest> entered) => throw new NotSupportedException();
            public Result SendToStall(OrderId id) => throw new NotSupportedException();
            public Result Fail(OrderId id, FailureReason reason) => throw new NotSupportedException();
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using TramChanh.UI.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.Tests.EditMode.UI
{
    /// <summary>MAIN-103: the UIElements presenter renders the HUD models into a detached root (no panel or scene needed).</summary>
    public sealed class PlayerHudPresenterTests
    {
        private EventBus _events;
        private OrderService _orders;
        private HeldItemSlot _hands;
        private PromptTableFile _text;
        private double _now;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _orders = new OrderService(new ManualClock(), _events, new SequentialIdGenerator(), new ContentDatabase(new[]
            {
                new KeyValuePair<string, ItemKind>("drink.slice", ItemKind.Drink),
                new KeyValuePair<string, ItemKind>("cake.dev.tbd", ItemKind.Cake)
            }));
            _hands = new HeldItemSlot(_events);
            _text = PromptTableFile.LoadMain();
            _now = 0d;
        }

        [TearDown]
        public void TearDown() => _events.Dispose();

        [Test]
        public void MAIN103_PresenterShowsHeldItemOrdersAndToastInVietnamese()
        {
            var root = new VisualElement();
            using var hud = new PlayerHudPresenter(root, key => _text.Resolve(key, "vi"), _text.Contains, _events, _hands, _orders, () => _now);
            Assert.That(root.childCount, Is.EqualTo(3), "Held panel, order list and toast.");
            Assert.That(hud.HeldText, Does.Contain("Tay không"));
            Assert.That(hud.OrdersText, Does.Contain("Chưa có đơn, chờ khách tới"));

            var origin = OrderOrigin.ForVehicle(new VehicleId(1));
            OrderId id = _orders.RequestService(origin, new CustomerId(1), new[] { new ItemRequest("drink.slice", 1), new ItemRequest("cake.dev.tbd", 1) }).Value;
            hud.Tick();
            Assert.That(hud.OrdersText, Does.Contain("Xe 1 · Chờ nhận đơn"));
            Assert.That(hud.OrdersText, Does.Contain("Tiếp theo: Tới Xe 1 nhận đơn"));

            Assert.That(_orders.Fail(id, FailureReason.CustomerLeft).IsSuccess, Is.True);
            hud.Tick();
            Assert.That(hud.ToastText, Is.EqualTo("Xe 1: khách đã bỏ đi"));
            _now = 10d;
            hud.Tick();
            Assert.That(hud.ToastText, Is.Empty);

            Assert.That(_hands.TryPickUp(new Bundle()), Is.True);
            hud.Tick();
            Assert.That(hud.HeldText, Does.Contain("Trạng thái: Đã đong bột"));
            Assert.That(hud.HeldText, Does.Contain("Tiếp: Đổ bột lên khuôn"));
        }

        private sealed class Bundle : IHoldable, IPreparationFeedback
        {
            public string PreparationStateKey => "cake.state.battermeasured";
            public string NextActionKey => "cake.pour";
            public Transform HandGrip => null;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    /// <summary>Reusable delivery assertions for any service with a caller-prepared carried order.</summary>
    public static class DeliveryContractAssertions
    {
        public static void RejectWrongTargetsThenDeliver(IOrderService orders, IOrderDelivery delivery, IEventBus events,
            OrderId id, ActorRef actor, DeliveryTarget correct, IReadOnlyList<DeliveryTarget> wrongTargets)
        {
            Assert.That(orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(correct.IsValid && actor.Value > 0, Is.True);
            Assert.That(wrongTargets, Is.Not.Empty);
            Assert.That(delivery.TryGetInfo(id, out OrderDeliveryInfo before), Is.True);
            var rejected = new List<DeliveryRejected>();
            int statusEvents = 0;
            using var rejection = events.Subscribe<DeliveryRejected>(rejected.Add);
            using var statuses = events.Subscribe<OrderStatusChanged>(_ => statusEvents++);
            for (int i = 0; i < wrongTargets.Count; i++)
            {
                DeliveryTarget target = wrongTargets[i];
                Assert.That(target.IsValid, Is.True, "Only valid wrong destinations count as attempts.");
                Assert.That(target, Is.Not.EqualTo(correct));
                Assert.That(delivery.Deliver(id, actor, target).ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
                Assert.That(orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
                Assert.That(delivery.TryGetInfo(id, out OrderDeliveryInfo info), Is.True);
                Assert.That(info.DeliveryAttempts, Is.EqualTo(before.DeliveryAttempts + i + 1));
                Assert.That(info.QualityScore, Is.EqualTo(before.QualityScore));
                Assert.That(rejected.Count, Is.EqualTo(i + 1), "Each wrong valid target must emit exactly one rejection.");
                Assert.That(rejected[i].Order, Is.EqualTo(id));
                Assert.That(rejected[i].AttemptedTarget, Is.EqualTo(target));
                Assert.That(rejected[i].ReasonKey, Is.EqualTo("order.delivery.wrong_target"));
                Assert.That(statusEvents, Is.Zero);
                AssertOptionalDetailsAgree(orders.Get(id), info);
            }
            Assert.That(delivery.Deliver(id, actor, correct).IsSuccess, Is.True, "A rejected attempt cannot latch out matching delivery.");
            Assert.That(orders.Get(id).Status, Is.EqualTo(OrderStatus.Delivered));
            Assert.That(delivery.TryGetInfo(id, out OrderDeliveryInfo delivered), Is.True);
            Assert.That(delivered.DeliveryAttempts, Is.EqualTo(before.DeliveryAttempts + wrongTargets.Count));
            Assert.That(rejected.Count, Is.EqualTo(wrongTargets.Count));
            Assert.That(statusEvents, Is.EqualTo(1));
            AssertOptionalDetailsAgree(orders.Get(id), delivered);
        }

        public static void RejectInvalidInputsThenDeliver(IOrderService orders, IOrderDelivery delivery, IEventBus events,
            OrderId id, ActorRef actor, DeliveryTarget correct)
        {
            Assert.That(orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(correct.IsValid && actor.Value > 0, Is.True);
            Assert.That(delivery.TryGetInfo(id, out OrderDeliveryInfo before), Is.True);
            int rejected = 0, statusEvents = 0;
            using var rejection = events.Subscribe<DeliveryRejected>(_ => rejected++);
            using var statuses = events.Subscribe<OrderStatusChanged>(_ => statusEvents++);
            Assert.That(delivery.Deliver(id, default, correct).ReasonKey, Is.EqualTo("order.actor.invalid"));
            AssertUnchanged(orders, delivery, id, before);
            Assert.That(delivery.Deliver(id, actor, default).ReasonKey, Is.EqualTo("order.delivery.invalid_target"));
            AssertUnchanged(orders, delivery, id, before);
            Assert.That(delivery.Deliver(default, actor, correct).IsSuccess, Is.False);
            AssertUnchanged(orders, delivery, id, before);
            Assert.That(rejected, Is.Zero);
            Assert.That(statusEvents, Is.Zero);
            Assert.That(delivery.Deliver(id, actor, correct).IsSuccess, Is.True, "Invalid input cannot latch out matching delivery.");
            Assert.That(orders.Get(id).Status, Is.EqualTo(OrderStatus.Delivered));
            Assert.That(delivery.TryGetInfo(id, out OrderDeliveryInfo delivered), Is.True);
            Assert.That(delivered.DeliveryAttempts, Is.EqualTo(before.DeliveryAttempts));
            Assert.That(rejected, Is.Zero);
            Assert.That(statusEvents, Is.EqualTo(1));
            AssertOptionalDetailsAgree(orders.Get(id), delivered);
        }

        private static void AssertUnchanged(IOrderService orders, IOrderDelivery delivery, OrderId id, OrderDeliveryInfo before)
        {
            Assert.That(orders.Get(id).Status, Is.EqualTo(OrderStatus.PickedUpByLobby));
            Assert.That(delivery.TryGetInfo(id, out OrderDeliveryInfo info), Is.True);
            Assert.That(info.DeliveryAttempts, Is.EqualTo(before.DeliveryAttempts));
            Assert.That(info.QualityScore, Is.EqualTo(before.QualityScore));
            AssertOptionalDetailsAgree(orders.Get(id), info);
        }

        private static void AssertOptionalDetailsAgree(IReadOnlyOrder order, OrderDeliveryInfo snapshot)
        {
            if (order is IOrderDeliveryInfo details)
            {
                Assert.That(details.DeliveryAttempts, Is.EqualTo(snapshot.DeliveryAttempts));
                Assert.That(details.QualityScore, Is.EqualTo(snapshot.QualityScore));
            }
        }
    }
}

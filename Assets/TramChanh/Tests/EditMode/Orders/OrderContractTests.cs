using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    public sealed class OrderContractTests
    {
        [Test]
        public void TC_ORDER_009_BindingEqualityIncludesPreparationIdentity()
        {
            var first = new OrderItemRef(new OrderId(1), new OrderItemId(2), new PreparationId(3));
            var second = new OrderItemRef(new OrderId(1), new OrderItemId(2), new PreparationId(4));
            Assert.That(first.IsValid, Is.True);
            Assert.That(default(OrderItemRef).IsValid, Is.False);
            Assert.That(first, Is.Not.EqualTo(second));
            IStallTicketQueue queue = new TicketFixture(first);
            Assert.That(queue.IsBound(first), Is.True);
            Assert.That(queue.IsBound(second), Is.False);
            Assert.That(queue.Release(second).IsSuccess, Is.False);
            Assert.That(queue.IsBound(first), Is.True);
        }

        [Test]
        public void TC_ORDER_005_PreparedContractCanRejectReadinessWithoutMutation()
        {
            var fixture = new PreparedFixture();
            IPreparedItem item = fixture;
            Assert.That(item.MarkReady().ReasonKey, Is.EqualTo("ready.not_finished"));
            Assert.That(fixture.WasMarkedReady, Is.False);
            fixture.Finished = true;
            Assert.That(item.MarkReady().IsSuccess, Is.True);
            Assert.That(fixture.WasMarkedReady, Is.True);
        }

        [Test]
        public void GT_005_GT_006_OriginsRejectDefaultIdentifiers()
        {
            Assert.That(OrderOrigin.ForTable(new TableId(1)).IsValid, Is.True);
            Assert.That(OrderOrigin.ForVehicle(new VehicleId(1)).IsValid, Is.True);
            Assert.That(default(OrderOrigin).IsValid, Is.False);
            Assert.Throws<System.ArgumentException>(() => OrderOrigin.ForTable(default));
            Assert.Throws<System.ArgumentException>(() => OrderOrigin.ForVehicle(default));
        }

        private sealed class PreparedFixture : IPreparedItem
        {
            public ItemKind Kind => ItemKind.Drink;
            public OrderItemRef BoundItem => default;
            public bool Finished { get; set; }
            public bool IsFinished => Finished;
            public int Quality => 100;
            public bool WasMarkedReady { get; private set; }
            public Result MarkReady()
            {
                if (!Finished) { return Result.Fail("ready.not_finished"); }
                WasMarkedReady = true;
                return Result.Success();
            }
        }

        private sealed class TicketFixture : IStallTicketQueue
        {
            private OrderItemRef _binding;
            public IReadOnlyList<OrderId> Tickets { get; }
            public TicketFixture(OrderItemRef binding) { _binding = binding; Tickets = new[] { binding.OrderId }; }
            public bool HasPending(ItemKind kind) => false;
            public Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId preparationId) => Result<OrderItemRef>.Fail("stall.no_ticket.drink");
            public bool IsBound(OrderItemRef item) => item.IsValid && item == _binding;
            public Result Release(OrderItemRef item)
            {
                if (!IsBound(item)) { return Result.Fail("stall.ticket.not_bound"); }
                _binding = default;
                return Result.Success();
            }
        }
    }
}

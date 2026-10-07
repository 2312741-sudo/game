using System.Collections.Generic;
using System;
using System.Reflection;
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

        [Test]
        public void ARCH001_PublicContractsReferenceOnlyCoreOrdersAndSystemTypes()
        {
            foreach (Type contract in new[] { typeof(IOrderService), typeof(IStallTicketQueue), typeof(IReadyShelfPlacement), typeof(IReadyShelfPickup), typeof(IPreparedItem) })
            {
                foreach (MethodInfo method in contract.GetMethods())
                {
                    AssertBoundary(method.ReturnType);
                    foreach (ParameterInfo parameter in method.GetParameters()) { AssertBoundary(parameter.ParameterType); }
                }
            }
            Assert.That(typeof(IOrderService).GetMethod("PickUp"), Is.Null, "Only the ready-shelf transaction exposes T7.");
            Assert.That(typeof(IReadyShelfPlacement).GetMethod("PickUp"), Is.Null);
            Assert.That(typeof(IReadyShelfPickup).GetMethod("PlaceReady"), Is.Null);
        }

        [Test]
        public void ARCH001_ItemKindValuesAreStableAcrossSerializedAssets()
        {
            Assert.That((int)ItemKind.Drink, Is.Zero);
            Assert.That((int)ItemKind.Cake, Is.EqualTo(1));
        }

        [Test]
        public void TC_ORDER_005_FinishedPreparedFixtureObeysSharedContract()
        {
            PreparedItemContractAssertions.AssertReadyTransition(new PreparedFixture { Finished = true });
        }

        private static void AssertBoundary(Type type)
        {
            string assembly = type.Assembly.GetName().Name;
            Assert.That(assembly == "TramChanh.Core" || assembly == "TramChanh.Orders" || assembly == "mscorlib" || assembly.StartsWith("System"), Is.True, type.FullName);
            foreach (Type argument in type.GetGenericArguments()) { AssertBoundary(argument); }
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
                if (WasMarkedReady) { return Result.Fail("ready.already_ready"); }
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

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
            var item = new PreparedFixture { Finished = true };
            PreparedItemContractAssertions.AssertReadyTransition(item, () => item.State, 1, () => item.EventCount);
        }

        [Test]
        public void TC_ORDER_005_UnfinishedPreparedFixtureObeysSharedContract()
        {
            var item = new PreparedFixture();
            PreparedItemContractAssertions.AssertUnfinishedTransition(item, () => item.State, () => item.EventCount);
        }

        [TestCase(ContractFault.FailureChangesKind)]
        [TestCase(ContractFault.FailureChangesSourceState)]
        [TestCase(ContractFault.LatchUnfinishedRejection)]
        public void TC_ORDER_005_SharedFixtureDetectsMutationOnUnfinishedFailure(ContractFault fault)
        {
            var item = new FaultyPrepared(fault, false);
            Assert.Throws<AssertionException>(() => PreparedItemContractAssertions.AssertUnfinishedTransition(item, () => item.State, () => item.EventCount));
        }

        [TestCase(ContractFault.SuccessChangesBinding)]
        [TestCase(ContractFault.RepeatedFailureChangesQuality)]
        public void TC_ORDER_005_SharedFixtureDetectsMutationDuringSuccessfulOrRepeatedReady(ContractFault fault)
        {
            var item = new FaultyPrepared(fault, true);
            Assert.Throws<AssertionException>(() => PreparedItemContractAssertions.AssertReadyTransition(item, () => item.State, 1, () => item.EventCount));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TC_ORDER_005_SharedFixtureDetectsEventsBeforeShelfCommit(bool finished)
        {
            using var events = new EventBus();
            int count = 0;
            using var subscription = events.Subscribe<OrderStatusChanged>(_ => count++);
            var item = new FaultyPrepared(finished ? ContractFault.EventsOnSuccess : ContractFault.EventsOnFailure, finished,
                () => events.Publish(new OrderStatusChanged(new OrderId(1), OrderStatus.Ready)));
            if (finished)
            {
                Assert.Throws<AssertionException>(() => PreparedItemContractAssertions.AssertReadyTransition(item, () => item.State, 1, () => count));
            }
            else
            {
                Assert.Throws<AssertionException>(() => PreparedItemContractAssertions.AssertUnfinishedTransition(item, () => item.State, () => count));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TC_ORDER_005_SharedFixtureRequiresSourceStateAndEventObservers(bool omitState)
        {
            var item = new PreparedFixture { Finished = true };
            Func<object> state = omitState ? null : () => item.State;
            Func<int> count = omitState ? () => item.EventCount : null;
            Assert.Throws<ArgumentNullException>(() => PreparedItemContractAssertions.AssertReadyTransition(item, state, 1, count));
            item.Finished = false;
            Assert.Throws<ArgumentNullException>(() => PreparedItemContractAssertions.AssertUnfinishedTransition(item, state, count));
        }

        [Test]
        public void TC_ORDER_006_EntryEventsSnapshotRequestsAndExposeReadOnlyLists()
        {
            var source = new[] { new ItemRequest("drink", 1) };
            var requested = new OrderEntryRequested(new OrderId(1), source);
            var confirmed = new OrderEntryConfirmed(new OrderId(1), source);
            source[0] = new ItemRequest("cake", 1);
            Assert.That(requested.RequestedItems[0].ItemDefinitionId, Is.EqualTo("drink"));
            Assert.That(confirmed.Items[0].ItemDefinitionId, Is.EqualTo("drink"));
            Assert.Throws<NotSupportedException>(() => ((IList<ItemRequest>)requested.RequestedItems)[0] = source[0]);
            Assert.Throws<NotSupportedException>(() => ((IList<ItemRequest>)confirmed.Items)[0] = source[0]);
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
            public int State => WasMarkedReady ? 1 : 0;
            public int EventCount => 0;
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

        public enum ContractFault { FailureChangesKind, FailureChangesSourceState, SuccessChangesBinding, RepeatedFailureChangesQuality, LatchUnfinishedRejection, EventsOnSuccess, EventsOnFailure }

        private sealed class FaultyPrepared : IPreparedItem
        {
            private readonly ContractFault _fault;
            private readonly Action _publish;
            private bool _rejected;
            public ItemKind Kind { get; private set; } = ItemKind.Drink;
            public OrderItemRef BoundItem { get; private set; } = new OrderItemRef(new OrderId(1), new OrderItemId(2), new PreparationId(3));
            public bool IsFinished { get; }
            public int Quality { get; private set; } = 100;
            public int State { get; private set; }
            public int EventCount { get; private set; }
            public FaultyPrepared(ContractFault fault, bool finished, Action publish = null) { _fault = fault; IsFinished = finished; _publish = publish; }
            public Result MarkReady()
            {
                if (!IsFinished)
                {
                    if (_fault == ContractFault.LatchUnfinishedRejection && _rejected) { return Result.Fail("ready.already_ready"); }
                    _rejected = true;
                    if (_fault == ContractFault.FailureChangesKind) { Kind = ItemKind.Cake; }
                    if (_fault == ContractFault.FailureChangesSourceState) { State++; }
                    if (_fault == ContractFault.EventsOnFailure) { EventCount++; _publish?.Invoke(); }
                    return Result.Fail("ready.not_finished");
                }
                if (State == 1)
                {
                    if (_fault == ContractFault.RepeatedFailureChangesQuality) { Quality--; }
                    return Result.Fail("ready.already_ready");
                }
                State = 1;
                if (_fault == ContractFault.SuccessChangesBinding) { BoundItem = default; }
                if (_fault == ContractFault.EventsOnSuccess) { EventCount++; _publish?.Invoke(); }
                return Result.Success();
            }
        }
    }
}

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
            IStallTicketQueue queue = new StallTicketContractFake(first);
            Assert.That(queue.IsBound(first), Is.True);
            Assert.That(queue.IsBound(second), Is.False);
            Assert.That(queue.Release(second).IsSuccess, Is.False);
            Assert.That(queue.IsBound(first), Is.True);
        }

        [Test]
        public void TC_ORDER_005_PreparedContractCanRejectReadinessWithoutMutation()
        {
            var fixture = new PreparedItemContractFake();
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
        public void TC_ORDER_005_FinishedPreparedItemContractFakeObeysSharedContract()
        {
            var item = new PreparedItemContractFake { Finished = true };
            PreparedItemContractAssertions.AssertReadyTransition(item, () => item.State, 1, () => item.EventCount);
        }

        [Test]
        public void TC_ORDER_005_UnfinishedPreparedItemContractFakeObeysSharedContract()
        {
            var item = new PreparedItemContractFake();
            PreparedItemContractAssertions.AssertUnfinishedTransition(item, () => item.State, () => item.EventCount);
        }

        [Test]
        public void TC_ORDER_005_SamePreparedInstanceCanFinishAfterRejectedReady()
        {
            var item = new PreparedItemContractFake();
            PreparedItemContractAssertions.AssertLifecycle(item, () => item.Finished = true, () => item.State, 1, () => item.EventCount);
        }

        [Test]
        public void TC_ORDER_005_SharedLifecycleDetectsRejectedLatchPoisoningLaterSuccess()
        {
            var item = new FaultyPreparedContractFake(PreparedContractFault.RejectedLatchPoisonsSuccess, false);
            Assert.Throws<AssertionException>(() => PreparedItemContractAssertions.AssertLifecycle(item, () => item.Finished = true, () => item.State, 1, () => item.EventCount));
        }

        [TestCase(PreparedContractFault.FailureChangesKind)]
        [TestCase(PreparedContractFault.FailureChangesSourceState)]
        [TestCase(PreparedContractFault.LatchUnfinishedRejection)]
        public void TC_ORDER_005_SharedFixtureDetectsMutationOnUnfinishedFailure(PreparedContractFault fault)
        {
            var item = new FaultyPreparedContractFake(fault, false);
            Assert.Throws<AssertionException>(() => PreparedItemContractAssertions.AssertUnfinishedTransition(item, () => item.State, () => item.EventCount));
        }

        [TestCase(PreparedContractFault.SuccessChangesBinding)]
        [TestCase(PreparedContractFault.RepeatedFailureChangesQuality)]
        public void TC_ORDER_005_SharedFixtureDetectsMutationDuringSuccessfulOrRepeatedReady(PreparedContractFault fault)
        {
            var item = new FaultyPreparedContractFake(fault, true);
            Assert.Throws<AssertionException>(() => PreparedItemContractAssertions.AssertReadyTransition(item, () => item.State, 1, () => item.EventCount));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TC_ORDER_005_SharedFixtureDetectsEventsBeforeShelfCommit(bool finished)
        {
            using var events = new EventBus();
            int count = 0;
            using var subscription = events.Subscribe<OrderStatusChanged>(_ => count++);
            var item = new FaultyPreparedContractFake(finished ? PreparedContractFault.EventsOnSuccess : PreparedContractFault.EventsOnFailure, finished,
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
            var item = new PreparedItemContractFake { Finished = true };
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








    }
}

using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Drinks
{
    /// <summary>
    /// MAIN-102 (lead priority): the endless customer loop must not hit drink.rack.empty forever after the initial
    /// stock. Pre-portioned bags are restocked from storage (quantity policy DEC-015 TBD).
    /// </summary>
    public sealed class TeaRackRestockTests
    {
        private DrinkReliabilityKit _kit;

        [SetUp]
        public void SetUp() => _kit = new DrinkReliabilityKit();

        [TearDown]
        public void TearDown() => _kit.Dispose();

        [Test]
        public void MAIN_102_ThreeDrinkOrdersAreServedOneAfterAnotherWithCapacityTwo()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            var seen = new HashSet<PreparationId>();
            for (int table = 1; table <= 3; table++)
            {
                OrderId order = _kit.SendOrder(table, "drink");
                TeaBagItem bag = _kit.TakeBag(rack);
                Assert.That(seen.Add(bag.PreparationId), Is.True, "Every bag has a fresh preparation identity.");
                Assert.That(bag.BoundItem.OrderId, Is.EqualTo(order));
                Assert.That(rack.Stock, Is.LessThanOrEqualTo(rack.Capacity));
                _kit.ServeHeldDrink(order, table);
                Assert.That(_kit.Orders.Get(order).Status, Is.EqualTo(OrderStatus.Completed));
                Assert.That(rack.Stock, Is.LessThanOrEqualTo(rack.Capacity));
            }
            Assert.That(_kit.Blocked, Is.Empty);
        }

        [Test]
        public void MAIN_102_TenDrinkOrdersNeverDeadlockAndStockStaysWithinCapacity()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            for (int table = 1; table <= 10; table++)
            {
                OrderId order = _kit.SendOrder(table, "drink");
                _kit.TakeBag(rack);
                _kit.ServeHeldDrink(order, table);
                rack.Query(_kit.Context);
                Assert.That(rack.Stock, Is.InRange(0, rack.Capacity));
            }
        }

        [Test]
        public void MAIN_102_DiscardedOrphanSlotIsRestocked()
        {
            TeaRackController rack = _kit.CreateRack(1, 1);
            OrderId failed = _kit.SendOrder(1, "drink");
            TeaBagItem orphan = _kit.TakeBag(rack);
            _kit.Orders.Fail(failed, FailureReason.CustomerLeft);
            orphan.ExecuteUse(_kit.Context);
            Assert.That(_kit.Hands.Current, Is.Null);

            OrderId next = _kit.SendOrder(2, "drink");
            TeaBagItem fresh = _kit.TakeBag(rack);
            Assert.That(fresh.BoundItem.OrderId, Is.EqualTo(next));
            Assert.That(fresh.PreparationId, Is.Not.EqualTo(orphan.PreparationId));
        }

        [Test]
        public void MAIN_102_SlotWhoseBagIsStillHeldOrInPreparationIsNeverRefilled()
        {
            TeaRackController rack = _kit.CreateRack(1, 1);
            _kit.SendOrder(1, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            _kit.SendOrder(2, "drink");
            Assert.That(rack.RestockEmptySlots(), Is.Zero, "Held bag keeps its slot.");
            Assert.That(rack.Query(_kit.Context).BlockedReasonKey, Is.EqualTo("hands.full"));

            // Even with empty hands (bag out of hand but still in preparation, not Ready), nothing is refilled.
            bag.Preparation.Open();
            Assert.That(_kit.Hands.TryRelease(), Is.True);
            Assert.That(rack.RestockEmptySlots(), Is.Zero);
            Assert.That(rack.Stock, Is.Zero);
            Assert.That(rack.Query(_kit.Context).BlockedReasonKey, Is.EqualTo("drink.rack.empty"));
        }

        [Test]
        public void MAIN_102_RestockCreatesUnboundStoredBagsAndPublishesNothing()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            for (int table = 1; table <= 2; table++)
            {
                OrderId order = _kit.SendOrder(table, "drink");
                _kit.TakeBag(rack);
                _kit.ServeHeldDrink(order, table);
            }
            int events = _kit.OrderEvents;
            int pendingTickets = _kit.Queue.Tickets.Count;
            Assert.That(rack.RestockEmptySlots(), Is.EqualTo(2));
            Assert.That(rack.Stock, Is.EqualTo(2));
            Assert.That(rack.RestockEmptySlots(), Is.Zero, "No restock while stored bags remain.");
            Assert.That(_kit.OrderEvents, Is.EqualTo(events));
            Assert.That(_kit.Queue.Tickets.Count, Is.EqualTo(pendingTickets));
            foreach (TeaBagItem bag in rack.GetComponentsInChildren<TeaBagItem>(true))
            {
                Assert.That(bag.State, Is.EqualTo(TeaBagState.Stored));
                Assert.That(bag.BoundItem.IsValid, Is.False);
            }
        }

        [Test]
        public void MAIN_102_IntentionallyEmptyRackIsNeverRestocked()
        {
            var inventory = new TeaRackInventory(2, 0, new SequentialIdGenerator());
            Assert.That(inventory.RestockWhenEmpty(), Is.Zero);
            Assert.That(inventory.Stock, Is.Zero);
            Assert.That(inventory.CanTake(true).ReasonKey, Is.EqualTo("drink.rack.empty"));
        }

        [Test]
        public void MAIN_102_InventoryRestockOnlyWhenEmptyAndOnlyTerminalSlots()
        {
            var inventory = new TeaRackInventory(3, 2, new SequentialIdGenerator());
            Assert.That(inventory.RestockWhenEmpty(), Is.Zero, "Stored bags remain.");
            DrinkPreparation held = inventory.BagAt(0);
            DrinkPreparation ready = inventory.BagAt(1);
            held.TryPickUp();
            ready.TryPickUp();
            ready.Open(); ready.AddCoconutJelly(); ready.AddLemonJelly(); ready.AddIce(); ready.Shake(); ready.Wipe();
            Assert.That(ready.MarkReady().IsSuccess, Is.True);
            var restocked = new List<int>();
            Assert.That(inventory.RestockWhenEmpty(restocked.Add), Is.EqualTo(1));
            Assert.That(restocked, Is.EqualTo(new[] { 1 }), "Only the Ready slot; the held bag and the never-stocked slot are untouched.");
            Assert.That(inventory.BagAt(0), Is.SameAs(held));
            Assert.That(inventory.BagAt(2), Is.Null);
            Assert.That(inventory.Stock, Is.EqualTo(1));
            Assert.That(inventory.Stock, Is.LessThanOrEqualTo(inventory.Capacity));
        }
    }
}

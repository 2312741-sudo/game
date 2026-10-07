using System;
using NUnit.Framework;
using TramChanh.Drinks.Domain;

namespace TramChanh.Tests.EditMode.Drinks
{
    public sealed class TeaRackPickupTests
    {
        [Test]
        public void TC_DRINK_002_ConfiguredStockStartsStoredAndPickupConsumesExactlyOne()
        {
            var rack = new TeaRackInventory(3, 2);
            Assert.That(rack.Capacity, Is.EqualTo(3));
            Assert.That(rack.Stock, Is.EqualTo(2));
            TeaBagPickup bag = rack.BagAt(rack.NextStoredIndex());
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Stored));
            Assert.That(rack.CanTake(true).IsAvailable, Is.True);
            Assert.That(bag.TryPickUp().IsSuccess, Is.True);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Held));
            Assert.That(rack.Stock, Is.EqualTo(1));
            Assert.That(rack.BagAt(rack.NextStoredIndex()), Is.Not.SameAs(bag));
        }

        [Test]
        public void DRINK_001_EmptyRackAndFullHandsAreBlockedWithoutMutation()
        {
            var empty = new TeaRackInventory(2, 0);
            Assert.That(empty.CanTake(true).ReasonKey, Is.EqualTo("drink.rack.empty"));
            Assert.That(empty.NextStoredIndex(), Is.EqualTo(-1));
            var stocked = new TeaRackInventory(2, 2);
            Assert.That(stocked.CanTake(false).ReasonKey, Is.EqualTo("hands.full"));
            Assert.That(stocked.Stock, Is.EqualTo(2));
        }

        [Test]
        public void DRINK_001_SameBagCannotBePickedUpTwice()
        {
            var rack = new TeaRackInventory(1, 1);
            TeaBagPickup bag = rack.BagAt(0);
            Assert.That(bag.TryPickUp().IsSuccess, Is.True);
            Assert.That(bag.TryPickUp().ReasonKey, Is.EqualTo("drink.bag.not_stored"));
            Assert.That(rack.Stock, Is.Zero);
            Assert.That(rack.NextStoredIndex(), Is.EqualTo(-1));
        }

        [TestCase(-1, 0)]
        [TestCase(1, -1)]
        [TestCase(1, 2)]
        public void DRINK_001_InvalidStockConfigurationRejected(int capacity, int stock)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TeaRackInventory(capacity, stock));
        }

        [Test]
        public void GT_003_NoTeaMeasuringStepOrQuantityIsIntroduced()
        {
            CollectionAssert.AreEqual(new[] { "Stored", "Held" }, Enum.GetNames(typeof(TeaBagState)));
            Assert.That(typeof(TeaBagPickup).GetProperties().Length, Is.EqualTo(1));
            Assert.That(typeof(TeaBagPickup).GetProperty("State"), Is.Not.Null);
        }
    }
}

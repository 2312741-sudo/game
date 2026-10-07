using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Interaction;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Interaction
{
    public sealed class HeldItemEventTests
    {
        [Test]
        public void TC_INT_004_SuccessfulSlotChangesPublishOnceAndFailuresPublishNothing()
        {
            using var events = new EventBus();
            var hands = new HeldItemSlot(events);
            var first = new Item();
            var second = new Item();
            int count = 0;
            HeldItemChanged change = default;
            using var subscription = events.Subscribe<HeldItemChanged>(e => { count++; change = e; });
            Assert.That(hands.TryPickUp(first), Is.True);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(change.Previous, Is.Null);
            Assert.That(change.Current, Is.SameAs(first));
            Assert.That(hands.TryPickUp(second), Is.False);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(hands.TryRelease(), Is.True);
            Assert.That(count, Is.EqualTo(2));
            Assert.That(change.Previous, Is.SameAs(first));
            Assert.That(change.Current, Is.Null);
            Assert.That(hands.TryRelease(), Is.False);
            Assert.That(count, Is.EqualTo(2));
        }

        private sealed class Item : IHoldable
        {
            public Transform HandGrip => null;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
        }
    }
}

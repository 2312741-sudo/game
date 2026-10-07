using System;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    /// <summary>Every preparation module runs this fixture with a finished, not-yet-Ready item.</summary>
    public static class PreparedItemContractAssertions
    {
        public static void AssertReadyTransition(IPreparedItem item, Func<object> readState = null, object expectedReadyState = null)
        {
            Assert.That(item.IsFinished, Is.True);
            var initial = new Snapshot(item, readState);
            Assert.That(item.MarkReady().IsSuccess, Is.True);
            AssertStableIdentityAndQuality(item, initial);
            Assert.That(item.IsFinished, Is.True, "Final-step completion remains true after Ready.");
            if (readState != null && expectedReadyState != null)
            {
                Assert.That(readState(), Is.EqualTo(expectedReadyState));
            }
            var ready = new Snapshot(item, readState);
            Assert.That(item.MarkReady().ReasonKey, Is.EqualTo("ready.already_ready"));
            AssertUnchanged(item, ready, readState);
        }

        public static void AssertUnfinishedDoesNotMutate(IPreparedItem item, Func<object> readState = null)
        {
            Assert.That(item.IsFinished, Is.False);
            var before = new Snapshot(item, readState);
            Assert.That(item.MarkReady().ReasonKey, Is.EqualTo("ready.not_finished"));
            AssertUnchanged(item, before, readState);
        }

        private static void AssertUnchanged(IPreparedItem item, Snapshot before, Func<object> readState)
        {
            AssertStableIdentityAndQuality(item, before);
            Assert.That(item.IsFinished, Is.EqualTo(before.IsFinished));
            if (readState != null) { Assert.That(readState(), Is.EqualTo(before.State)); }
        }

        private static void AssertStableIdentityAndQuality(IPreparedItem item, Snapshot before)
        {
            Assert.That(item.Kind, Is.EqualTo(before.Kind));
            Assert.That(item.BoundItem, Is.EqualTo(before.Binding));
            Assert.That(item.Quality, Is.EqualTo(before.Quality));
        }

        private readonly struct Snapshot
        {
            public ItemKind Kind { get; }
            public OrderItemRef Binding { get; }
            public int Quality { get; }
            public bool IsFinished { get; }
            public object State { get; }
            public Snapshot(IPreparedItem item, Func<object> readState)
            {
                Kind = item.Kind;
                Binding = item.BoundItem;
                Quality = item.Quality;
                IsFinished = item.IsFinished;
                State = readState?.Invoke();
            }
        }
    }
}

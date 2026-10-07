using System;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    /// <summary>Every preparation module runs this fixture with a finished, not-yet-Ready item.</summary>
    public static class PreparedItemContractAssertions
    {
        public static void AssertLifecycle(IPreparedItem item, Action finish, Func<object> readState, object expectedReadyState, Func<int> readEventCount)
        {
            if (finish == null) { throw new ArgumentNullException(nameof(finish)); }
            AssertUnfinishedTransition(item, readState, readEventCount);
            finish();
            AssertReadyTransition(item, readState, expectedReadyState, readEventCount);
        }

        public static void AssertReadyTransition(IPreparedItem item, Func<object> readState, object expectedReadyState, Func<int> readEventCount)
        {
            RequireObservers(readState, readEventCount);
            Assert.That(item.IsFinished, Is.True);
            var initial = new Snapshot(item, readState, readEventCount);
            Assert.That(item.MarkReady().IsSuccess, Is.True);
            AssertStableIdentityAndQuality(item, initial);
            Assert.That(item.IsFinished, Is.True, "Final-step completion remains true after Ready.");
            AssertNoEvents(initial, readEventCount);
            Assert.That(readState(), Is.EqualTo(expectedReadyState));
            var ready = new Snapshot(item, readState, readEventCount);
            Assert.That(item.MarkReady().ReasonKey, Is.EqualTo("ready.already_ready"));
            AssertUnchanged(item, ready, readState, readEventCount);
        }

        public static void AssertUnfinishedTransition(IPreparedItem item, Func<object> readState, Func<int> readEventCount)
        {
            RequireObservers(readState, readEventCount);
            Assert.That(item.IsFinished, Is.False);
            var before = new Snapshot(item, readState, readEventCount);
            // Repeat the rejection: a hidden Ready latch must not change the next result.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Assert.That(item.MarkReady().ReasonKey, Is.EqualTo("ready.not_finished"));
                AssertUnchanged(item, before, readState, readEventCount);
            }
        }

        private static void AssertUnchanged(IPreparedItem item, Snapshot before, Func<object> readState, Func<int> readEventCount)
        {
            AssertStableIdentityAndQuality(item, before);
            Assert.That(item.IsFinished, Is.EqualTo(before.IsFinished));
            Assert.That(readState(), Is.EqualTo(before.State));
            AssertNoEvents(before, readEventCount);
        }

        private static void AssertNoEvents(Snapshot before, Func<int> readEventCount)
        {
            Assert.That(readEventCount(), Is.EqualTo(before.EventCount), "MarkReady must not publish before the shelf transaction commits.");
        }

        private static void RequireObservers(Func<object> readState, Func<int> readEventCount)
        {
            if (readState == null) { throw new ArgumentNullException(nameof(readState)); }
            if (readEventCount == null) { throw new ArgumentNullException(nameof(readEventCount)); }
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
            public int EventCount { get; }
            public Snapshot(IPreparedItem item, Func<object> readState, Func<int> readEventCount)
            {
                Kind = item.Kind;
                Binding = item.BoundItem;
                Quality = item.Quality;
                IsFinished = item.IsFinished;
                State = readState();
                EventCount = readEventCount();
            }
        }
    }
}

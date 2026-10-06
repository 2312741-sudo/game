using System;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Interaction;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Interaction
{
    public sealed class InteractionDriverTests
    {
        private ManualClock _clock;
        private EventBus _events;
        private InteractionContext _context;
        private InteractionActionDriver _driver;
        private Target _target;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock();
            _events = new EventBus();
            _context = new InteractionContext(new ActorRef(1), ActorRole.Lobby | ActorRole.Stall, new HeldItemSlot(), _clock, _events);
            _driver = new InteractionActionDriver(_context);
            _target = new Target();
        }
        [TearDown]
        public void TearDown() => _events.Dispose();

        [Test]
        public void TC_INT_001_QueryHasAvailableBlockedAndHiddenStates()
        {
            foreach (Availability value in new[] { Availability.Available, Availability.Blocked("blocked"), Availability.Hidden })
            {
                _target.Availability = value;
                Assert.That(_target.Query(_context).Availability.Status, Is.EqualTo(value.Status));
            }
        }
        [Test]
        public void TC_INT_002_PressExecutesOnceAndBlockedPressPublishesReason()
        {
            _driver.Begin(_target);
            Assert.That(_target.Executions, Is.EqualTo(1));
            ActionBlocked blocked = default;
            using (_events.Subscribe<ActionBlocked>(e => blocked = e))
            {
                _target.Availability = Availability.Blocked("preview.blocked");
                _driver.Begin(_target);
                Assert.That(_target.Executions, Is.EqualTo(1));
                Assert.That(blocked.ReasonKey, Is.EqualTo("preview.blocked"));
            }
        }
        [Test]
        public void TC_INT_003_HoldCompletesOnlyAfterDurationAndPauseFreezesProgress()
        {
            _target.Kind = InteractionKind.Hold;
            _driver.Begin(_target);
            Assert.That(_target.Starts, Is.EqualTo(1));
            _clock.Advance(0.5);
            _driver.Tick(_target);
            Assert.That(_driver.Progress, Is.EqualTo(0.5f));
            Assert.That(_target.Executions, Is.Zero);
            _clock.Pause();
            _clock.Advance(10);
            _driver.Tick(_target);
            Assert.That(_driver.Progress, Is.EqualTo(0.5f));
            _clock.Resume();
            _clock.Advance(0.5);
            _driver.Tick(_target);
            Assert.That(_target.Executions, Is.EqualTo(1));
            _driver.Tick(_target);
            Assert.That(_target.Executions, Is.EqualTo(1));
        }
        [TestCase("release")]
        [TestCase("focus")]
        [TestCase("blocked")]
        public void TC_INT_003_HoldCancelsOnReleaseFocusLossOrAvailabilityChange(string reason)
        {
            _target.Kind = InteractionKind.Hold;
            _driver.Begin(_target);
            if (reason == "release")
            {
                _driver.Release();
            }
            else
            {
                if (reason == "blocked")
                {
                    _target.Availability = Availability.Blocked("blocked");
                }
                _driver.Tick(reason == "focus" ? null : _target);
            }
            Assert.That(_target.Cancels, Is.EqualTo(1));
            Assert.That(_driver.IsRunning, Is.False);
            Assert.That(_target.Executions, Is.Zero);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void TC_INT_008_ContinuousReportsElapsedClockTimeOnReleaseOrFocusLoss(bool loseFocus)
        {
            _target.Kind = InteractionKind.Continuous;
            _driver.Begin(_target);
            _clock.Advance(0.75);
            _clock.Pause();
            _clock.Advance(20);
            _clock.Resume();
            if (loseFocus)
            {
                _driver.Tick(null);
            }
            else
            {
                _driver.Release();
            }
            Assert.That(_target.HeldSeconds, Is.EqualTo(0.75f));
            Assert.That(_driver.IsRunning, Is.False);
        }
        [Test]
        public void TC_INT_007_QueryAllocatesZeroManagedBytes()
        {
            _target.Query(_context);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                _target.Query(_context);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }
        [Test]
        public void TC_INT_003_InvalidHoldDurationIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InteractionQuery(Availability.Available, "hold", InteractionKind.Hold, 0f));
        }
        [Test]
        public void TC_INT_004_HandsRejectSecondItemAndReleaseFirst()
        {
            var first = new Item();
            var second = new Item();
            Assert.That(_context.Hands.TryPickUp(first), Is.True);
            Assert.That(_context.Hands.TryPickUp(second), Is.False);
            Assert.That(_context.Hands.Current, Is.SameAs(first));
            Assert.That(_context.Hands.TryRelease(), Is.True);
            Assert.That(_context.Hands.Current, Is.Null);
            Assert.That(first.Released, Is.True);
        }
        private sealed class Item : IHoldable
        {
            public Transform HandGrip => null;
            public bool Released;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() => Released = true;
        }
        private sealed class Target : IInteractable
        {
            public InteractableId Id => new InteractableId(1);
            public Transform InteractionPoint => null;
            public Availability Availability = Availability.Available;
            public InteractionKind Kind;
            public int Executions;
            public int Starts;
            public int Cancels;
            public float HeldSeconds;
            public InteractionQuery Query(InteractionContext context) => new InteractionQuery(Availability, "preview.inspect", Kind, 1f);
            public void Execute(InteractionContext context)
            {
                if (Query(context).Availability.IsAvailable)
                {
                    Executions++;
                }
            }
            public void OnHoldStarted(InteractionContext context) => Starts++;
            public void OnHoldCancelled(InteractionContext context) => Cancels++;
            public void OnContinuousReleased(InteractionContext context, float seconds) => HeldSeconds = seconds;
        }
    }
}

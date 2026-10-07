using System;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Interaction;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Interaction
{
    public sealed class HeldUseDriverTests
    {
        private ManualClock _clock;
        private EventBus _events;
        private HeldItemSlot _hands;
        private InteractionContext _context;
        private InteractionActionDriver _driver;
        private HeldAction _item;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock();
            _events = new EventBus();
            _hands = new HeldItemSlot();
            _context = new InteractionContext(new ActorRef(1), ActorRole.Stall, _hands, _clock, _events);
            _driver = new InteractionActionDriver(_context);
            _item = new HeldAction();
            _hands.TryPickUp(_item);
        }

        [TearDown]
        public void TearDown() => _events.Dispose();

        [Test]
        public void TC_INT_009_HeldPressRunsWithoutTarget()
        {
            _driver.BeginHeld();
            Assert.That(_item.Executions, Is.EqualTo(1));
            Assert.That(_driver.IsRunning, Is.False);
        }

        [Test]
        public void TC_INT_009_HeldHoldCompletesExactlyOnceWithoutFocus()
        {
            _item.Kind = InteractionKind.Hold;
            _driver.BeginHeld();
            _clock.Advance(0.5d);
            _driver.Tick(null);
            Assert.That(_driver.Progress, Is.EqualTo(0.5f));
            Assert.That(_item.Executions, Is.Zero);
            _clock.Advance(0.5d);
            _driver.Tick(null);
            _driver.Tick(null);
            Assert.That(_item.Executions, Is.EqualTo(1));
            Assert.That(_item.Starts, Is.EqualTo(1));
        }

        [TestCase("release")]
        [TestCase("replace")]
        [TestCase("blocked")]
        [TestCase("action")]
        public void TC_INT_009_HeldHoldCancellationResetsWithoutExecuting(string reason)
        {
            _item.Kind = InteractionKind.Hold;
            _driver.BeginHeld();
            _clock.Advance(0.5d);
            if (reason == "release")
            {
                _driver.Release();
            }
            else
            {
                if (reason == "replace")
                {
                    _hands.TryRelease();
                    _hands.TryPickUp(new HeldAction());
                }
                else if (reason == "blocked")
                {
                    _item.Availability = Availability.Blocked("blocked");
                }
                else
                {
                    _item.PromptKey = "different.action";
                }
                _driver.Tick(null);
            }
            Assert.That(_driver.IsRunning, Is.False);
            Assert.That(_driver.Progress, Is.Zero);
            Assert.That(_item.Cancels, Is.EqualTo(1));
            Assert.That(_item.Executions, Is.Zero);
        }

        [Test]
        public void TC_INT_009_PauseFreezesHeldHoldAndResumeContinues()
        {
            _item.Kind = InteractionKind.Hold;
            _driver.BeginHeld();
            _clock.Advance(0.5d);
            _driver.Tick(null);
            _clock.Pause();
            _item.Availability = Availability.Blocked("paused");
            _clock.Advance(20d);
            _driver.Tick(null);
            Assert.That(_driver.Progress, Is.EqualTo(0.5f));
            Assert.That(_driver.IsRunning, Is.True);
            _clock.Resume();
            _item.Availability = Availability.Available;
            _clock.Advance(0.5d);
            _driver.Tick(null);
            Assert.That(_item.Executions, Is.EqualTo(1));
        }

        [Test]
        public void TC_INT_009_ReleaseWhilePausedCancelsHeldHold()
        {
            _item.Kind = InteractionKind.Hold;
            _driver.BeginHeld();
            _clock.Pause();
            _driver.Release();
            _clock.Resume();
            _clock.Advance(2d);
            _driver.Tick(null);
            Assert.That(_item.Executions, Is.Zero);
            Assert.That(_item.Cancels, Is.EqualTo(1));
        }

        [Test]
        public void TC_INT_009_TargetAndHeldUseShareOneActionSlot()
        {
            _item.Kind = InteractionKind.Hold;
            _driver.BeginHeld();
            var target = new Target();
            _driver.Begin(target);
            Assert.That(target.Executions, Is.Zero);
            _driver.Release();
            target.Kind = InteractionKind.Hold;
            _driver.Begin(target);
            _driver.BeginHeld();
            Assert.That(_item.Starts, Is.EqualTo(1));
        }

        [Test]
        public void TC_INT_009_BlockedHeldActionReportsReasonAndNeverStarts()
        {
            _item.Availability = Availability.Blocked("need.ice");
            ActionBlocked blocked = default;
            using (_events.Subscribe<ActionBlocked>(e => blocked = e))
            {
                _driver.BeginHeld();
            }
            Assert.That(blocked.ReasonKey, Is.EqualTo("need.ice"));
            Assert.That(_item.Starts, Is.Zero);
            Assert.That(_item.Executions, Is.Zero);
        }

        [Test]
        public void TC_INT_009_TargetHoldCancelsWhenHandIdentityChanges()
        {
            var target = new Target { Kind = InteractionKind.Hold };
            _driver.Begin(target);
            _hands.TryRelease();
            _hands.TryPickUp(new HeldAction());
            _clock.Advance(1d);
            _driver.Tick(target);
            Assert.That(target.Executions, Is.Zero);
            Assert.That(target.Cancels, Is.EqualTo(1));
        }

        [Test]
        public void TC_INT_007_HeldQueryAndDriverTickAllocateNothing()
        {
            _item.Kind = InteractionKind.Hold;
            _driver.BeginHeld();
            _driver.Tick(null);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                _item.QueryUse(_context);
                _driver.Tick(null);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test]
        public void TC_INT_009_HeldActionDoesNotRequireTargetContractOrTargetId()
        {
            _hands.TryRelease();
            var held = new HeldOnlyAction();
            _hands.TryPickUp(held);
            _driver.BeginHeld();
            Assert.That(held.Executions, Is.EqualTo(1));
            var prompt = new InteractionPromptChanged(default, held.QueryUse(_context), 0f, true);
            Assert.That(prompt.IsVisible, Is.True);
        }

        [Test]
        public void TC_INT_009_TargetHoldIgnoresPausedFocusLossAndRechecksAfterResume()
        {
            var target = new Target { Kind = InteractionKind.Hold };
            _driver.Begin(target);
            _clock.Advance(0.5d);
            _driver.Tick(target);
            _clock.Pause();
            _driver.Tick(null);
            Assert.That(_driver.Progress, Is.EqualTo(0.5f));
            Assert.That(target.Cancels, Is.Zero);
            _clock.Resume();
            _driver.Tick(null);
            Assert.That(target.Cancels, Is.EqualTo(1));
        }

        private sealed class HeldOnlyAction : IHoldable, IHeldItemAction
        {
            public Transform HandGrip => null;
            public int Executions;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
            public InteractionQuery QueryUse(InteractionContext context) => new InteractionQuery(Availability.Available, "held.only");
            public void ExecuteUse(InteractionContext context) => Executions++;
        }

        private sealed class HeldAction : IHoldable, IHeldItemAction, IInteractable
        {
            public Transform HandGrip => null;
            public Availability Availability = Availability.Available;
            public InteractionKind Kind;
            public string PromptKey = "use.held";
            public int Starts;
            public int Cancels;
            public int Executions;
            public void OnPickedUp(IHeldItemSlot hands) { }
            public void OnReleased() { }
            public InteractionQuery QueryUse(InteractionContext context) => new InteractionQuery(Availability, PromptKey, Kind, 1f);
            public void ExecuteUse(InteractionContext context) => Executions++;
            public InteractableId Id => new InteractableId(3);
            public Transform InteractionPoint => null;
            public InteractionQuery Query(InteractionContext context) => QueryUse(context);
            public void Execute(InteractionContext context) => ExecuteUse(context);
            public void OnHoldStarted(InteractionContext context) => Starts++;
            public void OnHoldCancelled(InteractionContext context) => Cancels++;
        }

        private sealed class Target : IInteractable
        {
            public InteractableId Id => new InteractableId(2);
            public Transform InteractionPoint => null;
            public InteractionKind Kind;
            public int Executions;
            public int Cancels;
            public InteractionQuery Query(InteractionContext context) => new InteractionQuery(Availability.Available, "target", Kind, 1f);
            public void Execute(InteractionContext context) => Executions++;
            public void OnHoldCancelled(InteractionContext context) => Cancels++;
        }
    }
}

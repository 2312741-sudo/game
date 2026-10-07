using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Orders;
using TramChanh.Tests.EditMode.Orders;

namespace TramChanh.Tests.EditMode.Drinks
{
    public sealed class DrinkPreparationTests
    {
        private static readonly string[] Actions = { "TryPickUp", "Open", "AddCoconutJelly", "AddLemonJelly", "AddIce", "Shake", "Wipe", "MarkReady" };
        private static readonly string[] States = { "Stored", "PickedUp", "Opened", "CoconutJellyAdded", "LemonJellyAdded", "IceAdded", "Shaken", "Wiped", "Ready" };

        [Test]
        public void GT_007_OnlyCanonicalStatesExistWithoutMeasurePourOrSeal()
        {
            CollectionAssert.AreEqual(States, Enum.GetNames(typeof(TeaBagState)));
        }

        [Test]
        public void GT_007_ExactDrinkSequenceSucceedsAndRepeatingEachStepIsBlocked()
        {
            var bag = new DrinkPreparation(new PreparationId(1));
            for (int i = 0; i < Actions.Length; i++)
            {
                Assert.That(Invoke(bag, Actions[i]).IsSuccess, Is.True, Actions[i]);
                Assert.That(bag.State.ToString(), Is.EqualTo(States[i + 1]));
                Assert.That(Invoke(bag, Actions[i]).IsSuccess, Is.False, Actions[i] + " repeated");
                Assert.That(bag.State.ToString(), Is.EqualTo(States[i + 1]));
            }
        }

        [Test]
        public void TC_DRINK_007_EveryOutOfOrderActionLeavesStateUnchanged()
        {
            for (int state = 0; state < States.Length; state++)
            {
                for (int action = 0; action < Actions.Length; action++)
                {
                    var bag = new DrinkPreparation(new PreparationId(1));
                    for (int step = 0; step < state; step++)
                    {
                        Assert.That(Invoke(bag, Actions[step]).IsSuccess, Is.True);
                    }
                    TeaBagState before = bag.State;
                    Result result = Invoke(bag, Actions[action]);
                    Assert.That(result.IsSuccess, Is.EqualTo(action == state), States[state] + " -> " + Actions[action]);
                    if (action != state)
                    {
                        Assert.That(bag.State, Is.EqualTo(before));
                        Assert.That(result.ReasonKey, Is.Not.Null.And.Not.Empty);
                    }
                }
            }
        }

        [TestCase(5)]
        [TestCase(6)]
        public void TC_DRINK_003_004_CannotReadyBeforeShakeAndWipe(int completedSteps)
        {
            var bag = new DrinkPreparation(new PreparationId(1));
            for (int i = 0; i < completedSteps; i++)
            {
                Assert.That(Invoke(bag, Actions[i]).IsSuccess, Is.True);
            }
            TeaBagState before = bag.State;
            Assert.That(Invoke(bag, "MarkReady").ReasonKey, Is.EqualTo("ready.not_finished"));
            Assert.That(bag.State, Is.EqualTo(before));
        }

        [Test]
        public void TC_DRINK_005_D1CommitsBindingAndStockBeforeHandNotification()
        {
            using var events = new EventBus();
            var rack = new TeaRackInventory(2, 2, new SequentialIdGenerator());
            var queue = new TicketQueue();
            int published = 0;
            using var subscription = events.Subscribe<DrinkStepCompleted>(_ => published++);
            Assert.That(rack.TryTakeBag(true, queue, index =>
            {
                Assert.That(rack.BagAt(index).State, Is.EqualTo(TeaBagState.PickedUp));
                Assert.That(rack.BagAt(index).BoundItem, Is.EqualTo(queue.Binding));
                Assert.That(rack.Stock, Is.EqualTo(1));
                return true;
            }).IsSuccess, Is.True);
            Assert.That(published, Is.Zero);
            Assert.That(queue.Claims, Is.EqualTo(1));
        }

        [Test]
        public void TC_DRINK_008_RefusedHandTransferRollsBackBagBindingTicketAndStockWithoutEvent()
        {
            using var events = new EventBus();
            var rack = new TeaRackInventory(1, 1, new SequentialIdGenerator());
            var queue = new TicketQueue();
            int published = 0;
            using var subscription = events.Subscribe<DrinkStepCompleted>(_ => published++);
            Assert.That(rack.TryTakeBag(true, queue, _ => false).ReasonKey, Is.EqualTo("hands.full"));
            Assert.That(rack.Stock, Is.EqualTo(1));
            Assert.That(rack.BagAt(0).State, Is.EqualTo(TeaBagState.Stored));
            Assert.That(rack.BagAt(0).BoundItem.IsValid, Is.False);
            Assert.That(queue.HasPending(ItemKind.Drink), Is.True);
            Assert.That(queue.Releases, Is.EqualTo(1));
            Assert.That(published, Is.Zero);
        }

        [Test]
        public void TC_DRINK_008_BlockedPickupNeverClaimsOrTransfers()
        {
            var queue = new TicketQueue();
            var rack = new TeaRackInventory(1, 1);
            Assert.That(rack.TryTakeBag(false, queue, _ => throw new Exception("Blocked pickup transferred.")).ReasonKey, Is.EqualTo("hands.full"));
            Assert.That(new TeaRackInventory(1, 0).TryTakeBag(true, queue, _ => true).ReasonKey, Is.EqualTo("drink.rack.empty"));
            queue.Pending = false;
            Assert.That(rack.TryTakeBag(true, queue, _ => true).ReasonKey, Is.EqualTo("stall.no_ticket.drink"));
            Assert.That(queue.Claims, Is.Zero);
            Assert.That(rack.Stock, Is.EqualTo(1));
        }

        [Test]
        public void TC_DRINK_008_FailedOrReleasedOrderUnbindsBagButFinishingRemainsPossible()
        {
            var rack = new TeaRackInventory(1, 1);
            var queue = new TicketQueue();
            Assert.That(rack.TryTakeBag(true, queue, _ => true).IsSuccess, Is.True);
            DrinkPreparation bag = rack.BagAt(0);
            Assert.That(bag.BoundItem.IsValid, Is.True);
            Assert.That(queue.Release(bag.BoundItem).IsSuccess, Is.True);
            Assert.That(bag.BoundItem.IsValid, Is.False);
            for (int step = 1; step < 7; step++)
            {
                Assert.That(Invoke(bag, Actions[step]).IsSuccess, Is.True);
            }
            Assert.That(bag.IsFinished, Is.True);
        }

        [Test]
        public void TC_DRINK_007_OnlyStrictModeIsImplemented()
        {
            Assert.Throws<NotSupportedException>(() => new DrinkPreparation(new PreparationId(1), SequenceMode.FreeWithScoring));
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void TC_DRINK_003_004_PreparedDomainContractPreservesIdentityStateAndEvents(int completedSteps)
        {
            using var events = new EventBus();
            var rack = new TeaRackInventory(1, 1, new SequentialIdGenerator());
            var queue = new TicketQueue();
            int count = 0;
            using var subscription = events.Subscribe<DrinkStepCompleted>(_ => count++);
            Assert.That(rack.TryTakeBag(true, queue, _ => true).IsSuccess, Is.True);
            DrinkPreparation bag = rack.BagAt(0);
            for (int step = 1; step < completedSteps; step++)
            {
                Assert.That(Invoke(bag, Actions[step]).IsSuccess, Is.True);
            }
            if (bag.IsFinished)
            {
                PreparedItemContractAssertions.AssertReadyTransition(bag, () => bag.State, TeaBagState.Ready, () => count);
            }
            else
            {
                PreparedItemContractAssertions.AssertUnfinishedTransition(bag, () => bag.State, () => count);
            }
        }

        [Test]
        public void TC_DRINK_015_DomainUsesSameInstanceUnfinishedFinishReadyLifecycle()
        {
            using var events = new EventBus(); int count = 0;
            using var step = events.Subscribe<DrinkStepCompleted>(_ => count++);
            using var ready = events.Subscribe<OrderItemStatusChanged>(_ => count++);
            var queue = new TicketQueue(); var rack = new TeaRackInventory(1, 1);
            rack.TryTakeBag(true, queue, _ => true); var bag = rack.BagAt(0);
            bag.Open(); bag.AddCoconutJelly(); bag.AddLemonJelly(); bag.AddIce();
            PreparedItemContractAssertions.AssertLifecycle(bag, () => { bag.Shake(); bag.Wipe(); }, () => bag.State, TeaBagState.Ready, () => count);
            Assert.That(count, Is.Zero); Assert.That(bag.BoundItem.IsValid, Is.True);
        }

        private static Result Invoke(DrinkPreparation bag, string action)
        {
            return action switch
            {
                "TryPickUp" => bag.TryPickUp(),
                "Open" => bag.Open(),
                "AddCoconutJelly" => bag.AddCoconutJelly(),
                "AddLemonJelly" => bag.AddLemonJelly(),
                "AddIce" => bag.AddIce(),
                "Shake" => bag.Shake(),
                "Wipe" => bag.Wipe(),
                "MarkReady" => bag.MarkReady(),
                _ => throw new ArgumentException(nameof(action))
            };
        }

        private sealed class TicketQueue : IStallTicketQueue
        {
            public bool Pending { get; set; } = true;
            public int Claims { get; private set; }
            public int Releases { get; private set; }
            public OrderItemRef Binding { get; private set; }
            public IReadOnlyList<OrderId> Tickets => Array.Empty<OrderId>();
            public bool HasPending(ItemKind kind) => Pending && kind == ItemKind.Drink;
            public Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId preparationId)
            {
                if (!HasPending(kind))
                {
                    return Result<OrderItemRef>.Fail("stall.no_ticket.drink");
                }
                Claims++;
                Pending = false;
                Binding = new OrderItemRef(new OrderId(1), new OrderItemId(1), preparationId);
                return Result<OrderItemRef>.Success(Binding);
            }
            public Result Release(OrderItemRef item)
            {
                if (!IsBound(item))
                {
                    return Result.Fail("stall.ticket.not_bound");
                }
                Releases++;
                Binding = default;
                Pending = true;
                return Result.Success();
            }
            public bool IsBound(OrderItemRef item) => item.IsValid && item == Binding;
        }
    }
}

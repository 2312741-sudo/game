using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Drinks
{
    /// <summary>
    /// MAIN-102 scenario 1/5: a held drink whose order failed (customer left / debug cancel) must not block the
    /// hands forever. Mirrors cake.discard_orphan: a held-use discard offered only while the bound item is not live,
    /// publishing nothing order-related itself.
    /// </summary>
    public sealed class DrinkOrphanDiscardTests
    {
        private DrinkReliabilityKit _kit;

        [SetUp]
        public void SetUp() => _kit = new DrinkReliabilityKit();

        [TearDown]
        public void TearDown() => _kit.Dispose();

        [Test]
        public void MAIN_102_LiveBoundBagNeverOffersDiscard()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            _kit.SendOrder(1, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            Assert.That(bag.Preparation.IsOrphaned, Is.False);
            Assert.That(bag.QueryUse(_kit.Context).PromptKey, Is.EqualTo("drink.bag.open"));
            Finish(bag);
            Assert.That(bag.QueryUse(_kit.Context).PromptKey, Is.Not.EqualTo(TeaBagItem.DiscardOrphanPromptKey));
        }

        [Test]
        public void MAIN_102_UnclaimedFreeBagIsNotOrphaned()
        {
            var preparation = new DrinkPreparation(new PreparationId(77));
            Assert.That(preparation.TryPickUp().IsSuccess, Is.True);
            Assert.That(preparation.IsOrphaned, Is.False, "Only a bag that lost its claim is orphaned.");
            Assert.That(preparation.Retire().IsSuccess, Is.False);
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(6)]
        public void MAIN_102_FailedOrderLetsTheHeldBagBeDiscardedWithoutOrderEvents(int stepsDone)
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            OrderId order = _kit.SendOrder(1, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            Advance(bag, stepsDone);
            Assert.That(_kit.Orders.Fail(order, FailureReason.CustomerLeft).IsSuccess, Is.True);

            Assert.That(bag.Preparation.IsOrphaned, Is.True);
            InteractionQuery query = bag.QueryUse(_kit.Context);
            Assert.That(query.PromptKey, Is.EqualTo(TeaBagItem.DiscardOrphanPromptKey));
            Assert.That(query.Availability.IsAvailable, Is.True);
            Assert.That(query.Kind, Is.EqualTo(InteractionKind.Press));

            int before = _kit.OrderEvents;
            GameObject bagObject = bag.gameObject;
            bag.ExecuteUse(_kit.Context);
            Assert.That(_kit.OrderEvents, Is.EqualTo(before), "The discard publishes nothing order-related.");
            Assert.That(_kit.Hands.Current, Is.Null);
            Assert.That(bagObject == null, Is.True, "The orphaned bag leaves play.");
            Assert.That(_kit.Blocked, Is.Empty);
        }

        [Test]
        public void MAIN_102_OrphanedBagIsBlockedAtStationsAndTheReadyShelf()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            OrderId order = _kit.SendOrder(1, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            bag.Preparation.Open();
            _kit.Orders.Fail(order, FailureReason.CancelledByDebug);

            var coconutObject = new GameObject("Coconut");
            _kit.Track(coconutObject);
            var coconut = coconutObject.AddComponent<ToppingBin>();
            DrinkReliabilityKit.Set(coconut, "_id", 11);
            Assert.That(coconut.Query(_kit.Context).BlockedReasonKey, Is.EqualTo("ready.no_order"));
            coconut.Execute(_kit.Context);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Opened), "No step advances an orphaned bag.");

            DrinkPreparation p = bag.Preparation;
            Assert.That(p.AddCoconutJelly().IsSuccess && p.AddLemonJelly().IsSuccess && p.AddIce().IsSuccess
                && p.Shake().IsSuccess && p.Wipe().IsSuccess, Is.True, "Domain steps alone are not order-aware.");
            Assert.That(_kit.Shelf.CanPlace(bag).ReasonKey, Is.EqualTo("ready.no_order"));
        }

        [Test]
        public void MAIN_102_ShakeHoldInProgressIsCancelledWhenTheOrderFails()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            OrderId order = _kit.SendOrder(1, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            Advance(bag, 4);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.IceAdded));
            var driver = new InteractionActionDriver(_kit.Context);
            driver.BeginHeld();
            Assert.That(driver.IsRunning, Is.True);
            _kit.Clock.Advance(1d);
            _kit.Orders.Fail(order, FailureReason.CustomerLeft);
            driver.Tick(null);
            Assert.That(driver.IsRunning, Is.False, "The changed held query releases the hold.");
            Assert.That(bag.State, Is.EqualTo(TeaBagState.IceAdded), "No shake completes for a failed order.");

            driver.BeginHeld();
            Assert.That(_kit.Hands.Current, Is.Null, "Re-beginning the held action now discards.");
        }

        [Test]
        public void MAIN_102_DiscardIsRoleAndPauseGuarded()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            OrderId order = _kit.SendOrder(1, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            _kit.Orders.Fail(order, FailureReason.CustomerLeft);
            var lobbyOnly = new InteractionContext(new ActorRef(2), ActorRole.Lobby, _kit.Hands, _kit.Clock, _kit.Events);
            InteractionQuery query = bag.QueryUse(lobbyOnly);
            Assert.That(query.PromptKey, Is.EqualTo(TeaBagItem.DiscardOrphanPromptKey));
            Assert.That(query.BlockedReasonKey, Is.EqualTo("interaction.stall_role_required"));
            bag.ExecuteUse(lobbyOnly);
            Assert.That(_kit.Hands.Current, Is.SameAs(bag));
        }

        private static void Advance(TeaBagItem bag, int steps)
        {
            DrinkPreparation p = bag.Preparation;
            if (steps > 0) { Assert.That(p.Open().IsSuccess, Is.True); }
            if (steps > 1) { Assert.That(p.AddCoconutJelly().IsSuccess, Is.True); }
            if (steps > 2) { Assert.That(p.AddLemonJelly().IsSuccess, Is.True); }
            if (steps > 3) { Assert.That(p.AddIce().IsSuccess, Is.True); }
            if (steps > 4) { Assert.That(p.Shake().IsSuccess, Is.True); }
            if (steps > 5) { Assert.That(p.Wipe().IsSuccess, Is.True); }
        }

        private static void Finish(TeaBagItem bag) => DrinkReliabilityKit.Finish(bag);
    }
}

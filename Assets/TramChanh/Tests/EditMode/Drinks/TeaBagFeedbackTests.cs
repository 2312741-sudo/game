using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Drinks
{
    /// <summary>ACCEL-01 contract (MAIN-103 request): TeaBagItem implements IPreparationFeedback for the HUD.</summary>
    public sealed class TeaBagFeedbackTests
    {
        private DrinkReliabilityKit _kit;

        [SetUp]
        public void SetUp() => _kit = new DrinkReliabilityKit();

        [TearDown]
        public void TearDown() => _kit.Dispose();

        [Test]
        public void MAIN_102_TeaBagReportsStateAndNextActionThroughTheWholeFlow()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            OrderId order = _kit.SendOrder(1, "drink");
            TeaBagItem stored = rack.GetComponentsInChildren<TeaBagItem>(true)[1];
            Assert.That(stored, Is.InstanceOf<IPreparationFeedback>());
            Assert.That(stored.PreparationStateKey, Is.EqualTo("drink.state.stored"));
            Assert.That(stored.NextActionKey, Is.EqualTo("drink.bag.open"));

            TeaBagItem bag = _kit.TakeBag(rack);
            DrinkPreparation p = bag.Preparation;
            Expect(bag, "pickedup", "drink.bag.open");
            p.Open(); Expect(bag, "opened", "drink.add_coconut");
            p.AddCoconutJelly(); Expect(bag, "coconutjellyadded", "drink.add_lemon");
            p.AddLemonJelly(); Expect(bag, "lemonjellyadded", "drink.add_ice");
            p.AddIce(); Expect(bag, "iceadded", "drink.bag.shake");
            p.Shake(); Expect(bag, "shaken", "drink.wipe");
            p.Wipe(); Expect(bag, "wiped", "ready.place_item");
            Assert.That(_kit.Shelf.PlaceReady(bag).IsSuccess, Is.True);
            Expect(bag, "ready", "ready.pick_up_order");
            Assert.That(_kit.Orders.Get(order).Status, Is.EqualTo(OrderStatus.Ready));
        }

        [Test]
        public void MAIN_102_OrphanedBagPointsAtItsDiscard()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            OrderId order = _kit.SendOrder(1, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            bag.Preparation.Open();
            _kit.Orders.Fail(order, FailureReason.CustomerLeft);
            Assert.That(bag.PreparationStateKey, Is.EqualTo("drink.state.opened"));
            Assert.That(bag.NextActionKey, Is.EqualTo(TeaBagItem.DiscardOrphanPromptKey));
        }

        [Test]
        public void MAIN_102_EveryTeaBagStateHasAComposedStateKey()
        {
            foreach (TeaBagState state in System.Enum.GetValues(typeof(TeaBagState)))
            {
                Assert.That("drink.state." + state.ToString().ToLowerInvariant(), Does.Match("^drink\\.state\\.[a-z]+$"));
            }
        }

        private static void Expect(TeaBagItem bag, string state, string next)
        {
            Assert.That(bag.PreparationStateKey, Is.EqualTo("drink.state." + state));
            Assert.That(bag.NextActionKey, Is.EqualTo(next));
        }
    }
}

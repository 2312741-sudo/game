using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.Stall.Runtime;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Drinks
{
    /// <summary>MAIN-102 scenarios 4, 9, 13 and 14 for the drink flow, against the real order owner and adapters.</summary>
    public sealed class DrinkReliabilityScenarioTests
    {
        private DrinkReliabilityKit _kit;

        [SetUp]
        public void SetUp() => _kit = new DrinkReliabilityKit();

        [TearDown]
        public void TearDown() => _kit.Dispose();

        [Test]
        public void MAIN_102_S4_HeldBagAtLobbyOrReadyPointsIsBlockedAndNothingMutates()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            var lobbyObject = new GameObject("Lobby");
            _kit.Track(lobbyObject);
            var controller = lobbyObject.AddComponent<LobbyOrderController>();
            controller.Initialize(_kit.Orders, _kit.Events);
            var tableObject = new GameObject("Table2");
            _kit.Track(tableObject);
            var table = tableObject.AddComponent<TableOrderPoint>();
            table.Initialize(31, tableObject.transform, _kit.Orders, controller, new TableId(2));

            OrderId first = _kit.SendOrder(1, "drink");
            OrderId waiting = table.RequestCustomerService(new CustomerId(2), new[] { new ItemRequest("drink", 1) }).Value;
            TeaBagItem bag = _kit.TakeBag(rack);

            Assert.That(table.Query(_kit.Context).BlockedReasonKey, Is.EqualTo("hands.full"), "No order entry while holding a bag.");
            table.Execute(_kit.Context);
            Assert.That(_kit.Orders.Get(waiting).Status, Is.EqualTo(OrderStatus.WaitingForLobby));

            var pickupObject = new GameObject("Pickup");
            _kit.Track(pickupObject);
            var pickup = pickupObject.AddComponent<ReadyOrderPickupPoint>();
            pickup.Initialize(_kit.Shelf, _kit.Orders, _kit.Events);
            Assert.That(pickup.Query(_kit.Context).BlockedReasonKey, Is.EqualTo("hands.full"));

            var counterObject = new GameObject("Counter");
            _kit.Track(counterObject);
            var counter = counterObject.AddComponent<ReadyCounterPoint>();
            var drinkPlace = new GameObject("DrinkPlacement").transform;
            drinkPlace.SetParent(counterObject.transform, false);
            var cakePlace = new GameObject("CakePlacement").transform;
            cakePlace.SetParent(counterObject.transform, false);
            DrinkReliabilityKit.Set(counter, "_id", 41);
            DrinkReliabilityKit.Set(counter, "_drinkPlacementPoint", drinkPlace);
            DrinkReliabilityKit.Set(counter, "_cakePlacementPoint", cakePlace);
            counter.Initialize(_kit.Shelf, _kit.Events);
            Assert.That(counter.Query(_kit.Context).BlockedReasonKey, Is.EqualTo("ready.not_finished"), "Right place, unfinished item.");
            counter.Execute(_kit.Context);
            Assert.That(_kit.Hands.Current, Is.SameAs(bag));
            Assert.That(_kit.Orders.Get(first).Status, Is.EqualTo(OrderStatus.InPreparation));
        }

        [Test]
        public void MAIN_102_S9_SecondPickupWhileHoldingIsRefusedAndClaimsNothing()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            _kit.SendOrder(1, "drink");
            OrderId second = _kit.SendOrder(2, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            int stock = rack.Stock;
            Assert.That(rack.Query(_kit.Context).BlockedReasonKey, Is.EqualTo("hands.full"));
            rack.Execute(_kit.Context);
            Assert.That(_kit.Hands.Current, Is.SameAs(bag));
            Assert.That(rack.Stock, Is.EqualTo(stock));
            Assert.That(_kit.Orders.Get(second).Items[0].Status, Is.EqualTo(OrderItemStatus.Pending));
        }

        [Test]
        public void MAIN_102_S13_DuplicateFinishedDrinkCannotBePlacedOrClaimTwice()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            OrderId order = _kit.SendOrder(1, "drink", "cake");
            TeaBagItem bag = _kit.TakeBag(rack);
            DrinkReliabilityKit.Finish(bag);
            Assert.That(_kit.Shelf.PlaceReady(bag).IsSuccess, Is.True);
            Assert.That(_kit.Shelf.PlaceReady(bag).ReasonKey, Is.EqualTo("ready.already_ready"));
            Assert.That(bag.MarkReady().ReasonKey, Is.EqualTo("ready.already_ready"));
            _kit.Hands.TryRelease();
            Assert.That(rack.Query(_kit.Context).BlockedReasonKey, Is.EqualTo("stall.no_ticket.drink"), "No second drink for a one-drink order.");
            OrderId other = _kit.SendOrder(2, "drink");
            Assert.That(_kit.Queue.ClaimNext(ItemKind.Drink, bag.PreparationId).ReasonKey, Is.EqualTo("stall.ticket.preparation_bound"),
                "A finished drink's identity can never claim a second ticket.");
            Assert.That(_kit.Orders.Get(other).Items[0].Status, Is.EqualTo(OrderItemStatus.Pending));
            Assert.That(_kit.Orders.Get(order).Status, Is.EqualTo(OrderStatus.InPreparation));
        }

        [Test]
        public void MAIN_102_S14_ReleasedShakeRestartsFromZeroAndNeverCompletesEarly()
        {
            TeaRackController rack = _kit.CreateRack(2, 2);
            _kit.SendOrder(1, "drink");
            TeaBagItem bag = _kit.TakeBag(rack);
            DrinkPreparation p = bag.Preparation;
            Assert.That(p.Open().IsSuccess && p.AddCoconutJelly().IsSuccess && p.AddLemonJelly().IsSuccess && p.AddIce().IsSuccess, Is.True);
            float duration = bag.QueryUse(_kit.Context).HoldDuration;
            var driver = new InteractionActionDriver(_kit.Context);
            _kit.Clock.Advance(5d);

            driver.BeginHeld();
            _kit.Clock.Advance(duration * 0.75f);
            driver.Tick(null);
            driver.Release();
            Assert.That(bag.State, Is.EqualTo(TeaBagState.IceAdded));

            driver.BeginHeld();
            Assert.That(driver.Progress, Is.Zero);
            _kit.Clock.Advance(duration * 0.75f);
            driver.Tick(null);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.IceAdded), "Progress from the released attempt is not carried over.");
            _kit.Clock.Advance(duration * 0.3f);
            driver.Tick(null);
            Assert.That(bag.State, Is.EqualTo(TeaBagState.Shaken));
        }
    }
}

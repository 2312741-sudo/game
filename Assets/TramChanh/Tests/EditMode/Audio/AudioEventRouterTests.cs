using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Audio;
using TramChanh.Cakes;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Audio
{
    public sealed class AudioEventRouterTests
    {
        private sealed class Recorder : IAudioCueSink
        {
            public readonly List<string> Calls = new List<string>();
            public void Play(AudioCue cue) { Calls.Add("play:" + cue); }
            public void Stop(AudioCue cue) { Calls.Add("stop:" + cue); }
        }

        private EventBus _bus;
        private Recorder _rec;
        private AudioEventRouter _router;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _rec = new Recorder();
            _router = new AudioEventRouter(_bus, _rec);
        }

        [TearDown]
        public void TearDown()
        {
            _router.Dispose();
            _bus.Dispose();
        }

        [Test]
        public void OrderStatuses_MapToCues()
        {
            var id = new OrderId(1);
            _bus.Publish(new OrderStatusChanged(id, OrderStatus.WaitingForLobby));
            _bus.Publish(new OrderStatusChanged(id, OrderStatus.SentToStall));
            _bus.Publish(new OrderStatusChanged(id, OrderStatus.Ready));
            _bus.Publish(new OrderStatusChanged(id, OrderStatus.Completed));
            _bus.Publish(new OrderStatusChanged(id, OrderStatus.Failed));
            CollectionAssert.AreEqual(
                new[] { "play:OrderReceived", "play:TicketBell", "play:OrderServed", "play:OrderFailed" },
                _rec.Calls);
        }

        [Test]
        public void ActionBlocked_PlaysWrongAction()
        {
            _bus.Publish(new ActionBlocked(new InteractableId(3), "x"));
            CollectionAssert.AreEqual(new[] { "play:WrongAction" }, _rec.Calls);
        }

        [Test]
        public void DrinkSteps_MapToPourAndIce()
        {
            var p = new PreparationId(1);
            _bus.Publish(new DrinkStepCompleted(p, TeaBagState.LemonJellyAdded));
            _bus.Publish(new DrinkStepCompleted(p, TeaBagState.IceAdded));
            _bus.Publish(new DrinkStepCompleted(p, TeaBagState.Shaken));
            CollectionAssert.AreEqual(new[] { "play:Pour", "play:Ice" }, _rec.Calls);
        }

        [Test]
        public void Sizzle_StartsWithFirstCakeAndStopsWithLast()
        {
            var a = new PreparationId(1);
            var b = new PreparationId(2);
            _bus.Publish(new CakeStepCompleted(a, CakeState.Cooking));
            _bus.Publish(new CakeStepCompleted(b, CakeState.Cooking));
            _bus.Publish(new CakeStepCompleted(a, CakeState.Flipped));
            _bus.Publish(new CakeStepCompleted(a, CakeState.Cooked));
            _bus.Publish(new CakeStepCompleted(b, CakeState.Ruined));
            CollectionAssert.AreEqual(
                new[] { "play:GrillSizzleLoop", "play:CakeFlip", "stop:GrillSizzleLoop" },
                _rec.Calls);
        }

        [Test]
        public void BatterPoured_PlaysPour()
        {
            _bus.Publish(new CakeStepCompleted(new PreparationId(1), CakeState.BatterPoured));
            CollectionAssert.AreEqual(new[] { "play:Pour" }, _rec.Calls);
        }

        [Test]
        public void Dispose_StopsListening()
        {
            _router.Dispose();
            _bus.Publish(new OrderStatusChanged(new OrderId(1), OrderStatus.Ready));
            Assert.IsEmpty(_rec.Calls);
        }
    }
}

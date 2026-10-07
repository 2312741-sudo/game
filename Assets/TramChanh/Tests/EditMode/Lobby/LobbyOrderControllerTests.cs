using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using UnityEngine;
using UnityEngine.TestTools;

namespace TramChanh.Tests.EditMode.Lobby
{
    public sealed class LobbyOrderControllerTests
    {
        private LobbyTestKit _kit;
        private TableOrderPoint _table;
        private OrderId _order;

        [SetUp]
        public void SetUp()
        {
            _kit = new LobbyTestKit();
            _table = _kit.Table(interactableId: 11, tableId: 1);
            _order = _kit.RequestAt(_table);
            _table.Execute(_kit.Context());
            _kit.Service.Calls.Clear();
        }

        [TearDown] public void TearDown() => _kit.Dispose();

        [Test]
        public void Enter_ForwardsThePrefilledItemsOnceAndMovesTheOrderToEntered()
        {
            _kit.Events.Publish(new OrderEntryConfirmed(_order, _kit.Requested[0].RequestedItems));
            Assert.That(_kit.Service.Calls, Is.EqualTo(new[] { "Enter" }));
            Assert.That(_kit.Service.Entered[0].Count, Is.EqualTo(1));
            Assert.That(_kit.Service.Entered[0][0].ItemDefinitionId, Is.EqualTo("drink.test"));
            Assert.That(_kit.Service.Get(_order).Status, Is.EqualTo(OrderStatus.Entered));
            Assert.That(_kit.Controller.HasEntrySession, Is.True, "Send is still to come.");
        }

        [Test]
        public void Send_IsASeparateConfirmationThatEnqueuesAndClosesTheSession()
        {
            _kit.Events.Publish(new OrderSendRequested(_order));
            Assert.That(_kit.Service.Calls, Is.EqualTo(new[] { "SendToStall" }));
            Assert.That(_kit.Service.Get(_order).Status, Is.EqualTo(OrderStatus.TakingOrder), "Not entered yet, so the service rejects.");
            Assert.That(_kit.Blocked[_kit.Blocked.Count - 1].ReasonKey, Is.EqualTo("order.invalid_transition"));
            Assert.That(_kit.Controller.HasEntrySession, Is.True);
            _kit.Events.Publish(new OrderEntryConfirmed(_order, _kit.Requested[0].RequestedItems));
            _kit.Events.Publish(new OrderSendRequested(_order));
            Assert.That(_kit.Service.Get(_order).Status, Is.EqualTo(OrderStatus.SentToStall));
            Assert.That(_kit.Controller.HasEntrySession, Is.False);
        }

        [Test]
        public void AuthorizedSessionAcceptsEnterAndSendWhileTheClockIsPaused()
        {
            _kit.Clock.Pause();
            _kit.Events.Publish(new OrderEntryConfirmed(_order, _kit.Requested[0].RequestedItems));
            _kit.Events.Publish(new OrderSendRequested(_order));
            Assert.That(_kit.Service.Get(_order).Status, Is.EqualTo(OrderStatus.SentToStall));
            Assert.That(_kit.Blocked, Is.Empty);
        }

        [Test]
        public void NewPointInteractionStaysBlockedWhileThePausedClockHoldsTheSession()
        {
            TableOrderPoint other = _kit.Table(interactableId: 12, tableId: 2);
            OrderId otherOrder = _kit.RequestAt(other, 2);
            _kit.Service.Calls.Clear();
            _kit.Clock.Pause();
            other.Execute(_kit.Context());
            Assert.That(_kit.Service.Calls, Is.Empty);
            Assert.That(_kit.Service.Get(otherOrder).Status, Is.EqualTo(OrderStatus.WaitingForLobby));
            Assert.That(_kit.Blocked[_kit.Blocked.Count - 1].ReasonKey, Is.EqualTo("interaction.paused"));
            Assert.That(_kit.Controller.EntryOrder, Is.EqualTo(_order));
        }

        [Test]
        public void EventsForAnotherOrAnInvalidOrderAreIgnored()
        {
            TableOrderPoint other = _kit.Table(interactableId: 12, tableId: 2);
            OrderId otherOrder = _kit.RequestAt(other, 2);
            _kit.Service.Calls.Clear();
            _kit.Events.Publish(new OrderEntryConfirmed(otherOrder, LobbyTestKit.Items()));
            _kit.Events.Publish(new OrderSendRequested(otherOrder));
            _kit.Events.Publish(new OrderEntryConfirmed(default, LobbyTestKit.Items()));
            _kit.Events.Publish(new OrderSendRequested(default));
            Assert.That(_kit.Service.Calls, Is.Empty);
            Assert.That(_kit.Blocked, Is.Empty);
            Assert.That(_kit.Controller.EntryOrder, Is.EqualTo(_order));
        }

        [Test]
        public void EventsWithoutAnOpenedSessionAreIgnored()
        {
            _kit.Controller.CloseEntry();
            _kit.Events.Publish(new OrderEntryConfirmed(_order, LobbyTestKit.Items()));
            _kit.Events.Publish(new OrderSendRequested(_order));
            Assert.That(_kit.Service.Calls, Is.Empty);
            Assert.That(_kit.Service.Get(_order).Status, Is.EqualTo(OrderStatus.TakingOrder));
        }

        [Test]
        public void EnterFailureIsReportedOnThePointAndKeepsTheSessionOpen()
        {
            _kit.Service.EnterFailure = "order.enter.items_differ";
            _kit.Events.Publish(new OrderEntryConfirmed(_order, LobbyTestKit.Items("drink.other")));
            Assert.That(_kit.Blocked.Count, Is.EqualTo(1));
            Assert.That(_kit.Blocked[0].ReasonKey, Is.EqualTo("order.enter.items_differ"));
            Assert.That(_kit.Blocked[0].InteractableId, Is.EqualTo(new InteractableId(11)));
            Assert.That(_kit.Controller.HasEntrySession, Is.True);
            Assert.That(_kit.Service.Get(_order).Status, Is.EqualTo(OrderStatus.TakingOrder));
        }

        [Test]
        public void SendFailureIsReportedAndKeepsTheSessionOpen()
        {
            _kit.Events.Publish(new OrderEntryConfirmed(_order, _kit.Requested[0].RequestedItems));
            _kit.Service.SendFailure = "stall.queue.rejected";
            _kit.Events.Publish(new OrderSendRequested(_order));
            Assert.That(_kit.Blocked[0].ReasonKey, Is.EqualTo("stall.queue.rejected"));
            Assert.That(_kit.Controller.HasEntrySession, Is.True);
            Assert.That(_kit.Service.Get(_order).Status, Is.EqualTo(OrderStatus.Entered));
        }

        [Test]
        public void SessionClosesWhenTheOrderLeavesTheEntryStatuses()
        {
            _kit.Service.SetStatus(_order, OrderStatus.Failed);
            Assert.That(_kit.Controller.HasEntrySession, Is.False);
            _kit.Service.Calls.Clear();
            _kit.Events.Publish(new OrderEntryConfirmed(_order, LobbyTestKit.Items()));
            Assert.That(_kit.Service.Calls, Is.Empty);
        }

        [Test]
        public void OpenEntryRejectsAnOrderFromAnotherPointOrAnUnknownOrder()
        {
            TableOrderPoint other = _kit.Table(interactableId: 12, tableId: 2);
            OrderId otherOrder = _kit.RequestAt(other, 2);
            _kit.Controller.CloseEntry();
            _kit.Service.Calls.Clear();
            Result wrong = _kit.Controller.OpenEntry(other.Id, otherOrder, LobbyTestKit.Player, _table.Origin);
            Assert.That(wrong.ReasonKey, Is.EqualTo("order.wrong_point"));
            Assert.That(_kit.Controller.OpenEntry(other.Id, new OrderId(999), LobbyTestKit.Player, other.Origin).ReasonKey, Is.EqualTo("order.not_found"));
            Assert.That(_kit.Controller.OpenEntry(other.Id, default, LobbyTestKit.Player, other.Origin).ReasonKey, Is.EqualTo("order.not_found"));
            Assert.That(_kit.Controller.OpenEntry(other.Id, otherOrder, LobbyTestKit.Player, default).ReasonKey, Is.EqualTo("order.wrong_point"));
            Assert.That(_kit.Service.Calls, Is.Empty);
            Assert.That(_kit.Controller.HasEntrySession, Is.False);
        }

        [Test]
        public void OpenEntryRefusesOrdersPastEntryAndSupportsTheEnteredReopen()
        {
            _kit.Events.Publish(new OrderEntryConfirmed(_order, _kit.Requested[0].RequestedItems));
            _kit.Controller.CloseEntry();
            Assert.That(_kit.Controller.OpenEntry(_table.Id, _order, LobbyTestKit.Player, _table.Origin).IsSuccess, Is.True, "Entered orders can be reopened to send.");
            Assert.That(_kit.Service.Calls, Is.EqualTo(new[] { "Enter" }), "No second BeginTaking.");
            _kit.Service.SetStatus(_order, OrderStatus.InPreparation);
            Assert.That(_kit.Controller.OpenEntry(_table.Id, _order, LobbyTestKit.Player, _table.Origin).ReasonKey, Is.EqualTo("order.entry.not_available"));
        }

        [Test]
        public void ObserverFaultAfterTheCommitDoesNotChangeTheResult()
        {
            _kit.Controller.CloseEntry();
            _kit.Events.Subscribe<OrderEntryRequested>(_ => throw new InvalidOperationException("listener fault"));
            LogAssert.Expect(LogType.Exception, new Regex("listener fault"));
            Result result = _kit.Controller.OpenEntry(_table.Id, _order, LobbyTestKit.Player, _table.Origin);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_kit.Controller.HasEntrySession, Is.True);
        }

        [Test]
        public void InitializeTwiceDoesNotDoubleHandleAndDisconnectStopsHandling()
        {
            _kit.Controller.Initialize(_kit.Service, _kit.Events);
            _kit.Controller.OpenEntry(_table.Id, _order, LobbyTestKit.Player, _table.Origin);
            _kit.Service.Calls.Clear();
            _kit.Events.Publish(new OrderEntryConfirmed(_order, _kit.Requested[0].RequestedItems));
            Assert.That(_kit.Service.Calls, Is.EqualTo(new[] { "Enter" }));
            _kit.Controller.Disconnect();
            Assert.That(_kit.Controller.HasEntrySession, Is.False);
            _kit.Service.Calls.Clear();
            _kit.Events.Publish(new OrderSendRequested(_order));
            Assert.That(_kit.Service.Calls, Is.Empty);
        }

        [Test]
        public void ControllerRequiresServicesAndRefusesToOpenBeforeInitialization()
        {
            Assert.Throws<ArgumentNullException>(() => _kit.Controller.Initialize(null, _kit.Events));
            Assert.Throws<ArgumentNullException>(() => _kit.Controller.Initialize(_kit.Service, null));
            var host = new GameObject("Raw");
            try
            {
                var raw = host.AddComponent<LobbyOrderController>();
                Assert.That(raw.OpenEntry(_table.Id, _order, LobbyTestKit.Player, _table.Origin).ReasonKey, Is.EqualTo("order.lobby.not_configured"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ContractBoundary_PointsExposeNoDeliveryOrCompletionAndLobbyDoesNotReferenceUi()
        {
            Assert.That(typeof(OrderPoint).GetMethod("Deliver"), Is.Null);
            Assert.That(typeof(LobbyOrderController).GetMethod("Deliver"), Is.Null);
            Assert.That(typeof(LobbyOrderController).GetMethod("Complete"), Is.Null);
            foreach (var reference in typeof(LobbyOrderController).Assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name, Is.Not.EqualTo("TramChanh.UI"));
                Assert.That(reference.Name, Is.Not.EqualTo("TramChanh.Stall"));
            }
        }
    }
}

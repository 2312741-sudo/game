using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using TramChanh.UI.Orders;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class OrderEntryModelTests
    {
        private EventBus _events;
        private OrderEntryModel _model;
        private readonly List<OrderEntryConfirmed> _confirmed = new List<OrderEntryConfirmed>();
        private readonly List<OrderSendRequested> _send = new List<OrderSendRequested>();
        private int _changes;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _confirmed.Clear();
            _send.Clear();
            _changes = 0;
            _events.Subscribe<OrderEntryConfirmed>(_confirmed.Add);
            _events.Subscribe<OrderSendRequested>(_send.Add);
            _model = new OrderEntryModel(_events);
            _model.Changed += () => _changes++;
        }

        [TearDown]
        public void TearDown()
        {
            _model.Dispose();
            _events.Dispose();
        }

        private static ItemRequest[] Items() => new[] { new ItemRequest("drink.a", 2), new ItemRequest("drink.b", 1) };

        [Test]
        public void RequestedEntryOpensPrefilledFromTheRequestSnapshot()
        {
            var source = Items();
            _events.Publish(new OrderEntryRequested(new OrderId(5), source));
            source[0] = new ItemRequest("changed", 9);
            Assert.That(_model.IsOpen, Is.True);
            Assert.That(_model.OrderId, Is.EqualTo(new OrderId(5)));
            Assert.That(_model.Items[0].ItemDefinitionId, Is.EqualTo("drink.a"));
            Assert.That(_model.Items[0].Quantity, Is.EqualTo(2));
            Assert.That(_changes, Is.EqualTo(1));
        }

        [Test]
        public void EnterAndSendAreSeparateTypedEventsForTheOpenOrder()
        {
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            Assert.That(_model.Enter(), Is.True);
            Assert.That(_confirmed.Count, Is.EqualTo(1));
            Assert.That(_send, Is.Empty, "Enter must not send.");
            Assert.That(_confirmed[0].OrderId, Is.EqualTo(new OrderId(5)));
            Assert.That(_confirmed[0].Items.Count, Is.EqualTo(2));
            Assert.That(_confirmed[0].Items[1].ItemDefinitionId, Is.EqualTo("drink.b"));
            Assert.That(_model.Send(), Is.True);
            Assert.That(_send.Count, Is.EqualTo(1));
            Assert.That(_send[0].OrderId, Is.EqualTo(new OrderId(5)));
            Assert.That(_confirmed.Count, Is.EqualTo(1), "Send must not re-enter.");
        }

        [Test]
        public void EnterAndSendDoNothingWhileClosed()
        {
            Assert.That(_model.Enter(), Is.False);
            Assert.That(_model.Send(), Is.False);
            Assert.That(_confirmed, Is.Empty);
            Assert.That(_send, Is.Empty);
        }

        [Test]
        public void CloseHidesWithoutPublishingAndIsIdempotent()
        {
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            _model.Close();
            _model.Close();
            Assert.That(_model.IsOpen, Is.False);
            Assert.That(_confirmed, Is.Empty);
            Assert.That(_send, Is.Empty);
            Assert.That(_changes, Is.EqualTo(2), "Open then one close; the second close is a no-op.");
            Assert.That(_model.Enter(), Is.False);
        }

        [Test]
        public void StatusPastEntryOrFailureClosesOnlyTheOpenOrder()
        {
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            _events.Publish(new OrderStatusChanged(new OrderId(6), OrderStatus.SentToStall));
            _events.Publish(new OrderStatusChanged(new OrderId(5), OrderStatus.TakingOrder));
            _events.Publish(new OrderStatusChanged(new OrderId(5), OrderStatus.Entered));
            Assert.That(_model.IsOpen, Is.True);
            _events.Publish(new OrderStatusChanged(new OrderId(5), OrderStatus.SentToStall));
            Assert.That(_model.IsOpen, Is.False);
            _events.Publish(new OrderEntryRequested(new OrderId(7), Items()));
            _events.Publish(new OrderStatusChanged(new OrderId(7), OrderStatus.Failed));
            Assert.That(_model.IsOpen, Is.False);
        }

        [Test]
        public void ActionBlockedShowsAReasonWhileOpenAndEnterClearsIt()
        {
            _events.Publish(new ActionBlocked(new InteractableId(1), "ignored.while.closed"));
            Assert.That(_model.BlockedReasonKey, Is.Null);
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            _events.Publish(new ActionBlocked(new InteractableId(1), "order.enter.items_differ"));
            Assert.That(_model.BlockedReasonKey, Is.EqualTo("order.enter.items_differ"));
            _model.Enter();
            Assert.That(_model.BlockedReasonKey, Is.Null);
        }

        [Test]
        public void SuccessfulEnterAfterAFailedSendRefreshesTheUiWhenTheReasonIsCleared()
        {
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            _events.Publish(new ActionBlocked(new InteractableId(1), "order.transition.invalid"));
            int before = _changes;
            string reasonSeenByTheRefresh = "unset";
            _model.Changed += () => reasonSeenByTheRefresh = _model.BlockedReasonKey;
            Assert.That(_model.Enter(), Is.True);
            Assert.That(_changes, Is.EqualTo(before + 1), "Clearing the reason must notify the view.");
            Assert.That(reasonSeenByTheRefresh, Is.Null);
            Assert.That(_model.BlockedReasonKey, Is.Null);
            Assert.That(_confirmed.Count, Is.EqualTo(1));
        }

        [Test]
        public void SendClearsAStaleReasonAndNotifiesOnlyWhenThereWasOne()
        {
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            int afterOpen = _changes;
            _model.Send();
            Assert.That(_changes, Is.EqualTo(afterOpen), "No reason to clear, so no refresh.");
            _events.Publish(new ActionBlocked(new InteractableId(1), "order.transition.invalid"));
            int afterBlocked = _changes;
            _model.Send();
            Assert.That(_changes, Is.EqualTo(afterBlocked + 1));
            Assert.That(_model.BlockedReasonKey, Is.Null);
        }

        [Test]
        public void FailureReportedWhileSendingIsShownAfterTheClear()
        {
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            _events.Publish(new ActionBlocked(new InteractableId(1), "order.items.request_mismatch"));
            _events.Subscribe<OrderSendRequested>(_ => _events.Publish(new ActionBlocked(new InteractableId(1), "order.transition.invalid")));
            _model.Send();
            Assert.That(_model.BlockedReasonKey, Is.EqualTo("order.transition.invalid"));
        }

        [Test]
        public void SecondRequestReplacesTheOpenOrderAndInvalidRequestsAreIgnored()
        {
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            _events.Publish(new OrderEntryRequested(new OrderId(8), new[] { new ItemRequest("drink.c", 1) }));
            Assert.That(_model.OrderId, Is.EqualTo(new OrderId(8)));
            Assert.That(_model.Items.Count, Is.EqualTo(1));
            _events.Publish(default(OrderEntryRequested));
            Assert.That(_model.OrderId, Is.EqualTo(new OrderId(8)));
        }

        [Test]
        public void DisposeStopsListeningAndRequiresAnEventBus()
        {
            _model.Dispose();
            _events.Publish(new OrderEntryRequested(new OrderId(5), Items()));
            Assert.That(_model.IsOpen, Is.False);
            Assert.Throws<ArgumentNullException>(() => new OrderEntryModel(null));
        }

        [Test]
        public void UiOnlyTalksThroughTypedEventsAndNeverReferencesLobbyOrStall()
        {
            foreach (var reference in typeof(OrderEntryModel).Assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name, Is.Not.EqualTo("TramChanh.Lobby"));
                Assert.That(reference.Name, Is.Not.EqualTo("TramChanh.Stall"));
            }
        }
    }
}

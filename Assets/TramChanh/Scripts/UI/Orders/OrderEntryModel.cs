using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;

namespace TramChanh.UI.Orders
{
    /// <summary>
    /// Engine-free state of the order-entry panel. It only listens to and publishes typed events;
    /// the UI assembly never references Lobby. Enter (T3) and Send (T4) are separate confirmations.
    /// </summary>
    internal sealed class OrderEntryModel : IDisposable
    {
        private readonly IEventBus _events;
        private readonly IDisposable _requested;
        private readonly IDisposable _status;
        private readonly IDisposable _blocked;
        private bool _disposed;

        public bool IsOpen { get; private set; }
        public OrderId OrderId { get; private set; }
        public IReadOnlyList<ItemRequest> Items { get; private set; } = Array.AsReadOnly(Array.Empty<ItemRequest>());
        public string BlockedReasonKey { get; private set; }
        public event Action Changed;

        public OrderEntryModel(IEventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _requested = events.Subscribe<OrderEntryRequested>(OnRequested);
            _status = events.Subscribe<OrderStatusChanged>(OnStatusChanged);
            _blocked = events.Subscribe<ActionBlocked>(OnBlocked);
        }

        public bool Enter()
        {
            if (!IsOpen) { return false; }
            ClearReason();
            _events.Publish(new OrderEntryConfirmed(OrderId, Items));
            return true;
        }

        public bool Send()
        {
            if (!IsOpen) { return false; }
            ClearReason();
            _events.Publish(new OrderSendRequested(OrderId));
            return true;
        }

        // A retry hides the previous failure at once; a failure published by the service while this
        // call runs sets the key again afterwards and renders through the ActionBlocked handler.
        private void ClearReason()
        {
            if (BlockedReasonKey == null) { return; }
            BlockedReasonKey = null;
            Changed?.Invoke();
        }

        /// <summary>Hides the panel; the order stays as it is (TakingOrder remains TakingOrder).</summary>
        public void Close()
        {
            if (!IsOpen) { return; }
            IsOpen = false;
            BlockedReasonKey = null;
            Changed?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) { return; }
            _disposed = true;
            _requested.Dispose();
            _status.Dispose();
            _blocked.Dispose();
            IsOpen = false;
        }

        private void OnRequested(OrderEntryRequested message)
        {
            if (!message.OrderId.IsValid || message.RequestedItems == null) { return; }
            OrderId = message.OrderId;
            Items = message.RequestedItems;
            BlockedReasonKey = null;
            IsOpen = true;
            Changed?.Invoke();
        }

        private void OnStatusChanged(OrderStatusChanged message)
        {
            if (!IsOpen || message.OrderId != OrderId) { return; }
            if (message.Status != OrderStatus.WaitingForLobby && message.Status != OrderStatus.TakingOrder && message.Status != OrderStatus.Entered)
            {
                Close();
            }
        }

        private void OnBlocked(ActionBlocked message)
        {
            if (!IsOpen) { return; }
            BlockedReasonKey = message.ReasonKey;
            Changed?.Invoke();
        }
    }
}

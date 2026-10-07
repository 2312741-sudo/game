using System;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Lobby
{
    /// <summary>
    /// Opens the order-entry flow after T2 and performs T3/T4 from typed UI events.
    /// The UI never references Lobby: it only publishes <see cref="OrderEntryConfirmed"/> and
    /// <see cref="OrderSendRequested"/>. An entry session is authorized once, when a point opens it;
    /// confirmations inside that session do not re-check the clock, so the UI may pause the game.
    /// Delivery and completion are out of scope for this task.
    /// </summary>
    public sealed class LobbyOrderController : MonoBehaviour
    {
        private readonly struct Session
        {
            public InteractableId PointId { get; }
            public OrderId OrderId { get; }
            public Session(InteractableId pointId, OrderId orderId)
            {
                PointId = pointId;
                OrderId = orderId;
            }
        }

        private IOrderService _orders;
        private IEventBus _events;
        private IDisposable _confirmed;
        private IDisposable _sendRequested;
        private IDisposable _statusChanged;
        private Session _session;
        private bool _hasSession;

        public bool HasEntrySession => _hasSession;
        public OrderId EntryOrder => _hasSession ? _session.OrderId : default;

        public void Initialize(IOrderService orders, IEventBus events)
        {
            if (orders == null) { throw new ArgumentNullException(nameof(orders)); }
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            Disconnect();
            _orders = orders;
            _events = events;
            Subscribe();
        }

        /// <summary>T2 (when needed) and opening the entry UI for an order at its own point.</summary>
        public Result OpenEntry(InteractableId pointId, OrderId orderId, ActorRef actor, OrderOrigin point)
        {
            if (_orders == null || _events == null) { return Result.Fail("order.lobby.not_configured"); }
            IReadOnlyOrder order = orderId.IsValid ? _orders.Get(orderId) : null;
            if (order == null) { return Result.Fail("order.not_found"); }
            if (!point.IsValid || !order.Origin.Equals(point)) { return Result.Fail("order.wrong_point"); }
            switch (order.Status)
            {
                case OrderStatus.WaitingForLobby:
                    Result begin = _orders.BeginTaking(orderId, actor, point);
                    if (!begin.IsSuccess) { return begin; }
                    break;
                case OrderStatus.TakingOrder:
                case OrderStatus.Entered:
                    break;
                default:
                    return Result.Fail("order.entry.not_available");
            }
            _session = new Session(pointId, orderId);
            _hasSession = true;
            Notify(new OrderEntryRequested(orderId, order.RequestedItems));
            return Result.Success();
        }

        /// <summary>The entry UI was cancelled elsewhere or the host wants to forget the session.</summary>
        public void CloseEntry()
        {
            _hasSession = false;
            _session = default;
        }

        public void Disconnect()
        {
            _confirmed?.Dispose();
            _sendRequested?.Dispose();
            _statusChanged?.Dispose();
            _confirmed = null;
            _sendRequested = null;
            _statusChanged = null;
            CloseEntry();
        }

        private void Subscribe()
        {
            if (_confirmed != null || _events == null) { return; }
            _confirmed = _events.Subscribe<OrderEntryConfirmed>(OnConfirmed);
            _sendRequested = _events.Subscribe<OrderSendRequested>(OnSendRequested);
            _statusChanged = _events.Subscribe<OrderStatusChanged>(OnStatusChanged);
        }

        private void OnConfirmed(OrderEntryConfirmed message)
        {
            if (!IsSessionOrder(message.OrderId)) { return; }
            if (message.Items == null || message.Items.Count == 0)
            {
                Reject("order.entry.empty");
                return;
            }
            Result result = _orders.Enter(_session.OrderId, message.Items);
            if (!result.IsSuccess) { Reject(result.ReasonKey); }
        }

        private void OnSendRequested(OrderSendRequested message)
        {
            if (!IsSessionOrder(message.OrderId)) { return; }
            Result result = _orders.SendToStall(_session.OrderId);
            if (!result.IsSuccess)
            {
                Reject(result.ReasonKey);
                return;
            }
            CloseEntry();
        }

        private void OnStatusChanged(OrderStatusChanged message)
        {
            if (!IsSessionOrder(message.OrderId)) { return; }
            if (message.Status != OrderStatus.WaitingForLobby && message.Status != OrderStatus.TakingOrder && message.Status != OrderStatus.Entered)
            {
                CloseEntry();
            }
        }

        private bool IsSessionOrder(OrderId id) => _hasSession && id.IsValid && id == _session.OrderId;

        private void Reject(string reasonKey) => Notify(new ActionBlocked(_session.PointId, reasonKey));

        // The order service has already committed; an observer fault must not change the result.
        private void Notify<TEvent>(TEvent message) where TEvent : struct
        {
            try
            {
                _events.Publish(message);
            }
            catch (Exception fault)
            {
                Debug.LogException(fault);
            }
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Disconnect();
        private void OnDestroy() => Disconnect();
    }
}

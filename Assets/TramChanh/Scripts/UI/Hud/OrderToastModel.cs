using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.UI.Hud
{
    /// <summary>
    /// Short order milestones (sent, ready, completed, failed, wrong delivery target). The newest message
    /// replaces the previous one and hides after <see cref="Duration"/> seconds of the supplied clock.
    /// </summary>
    public sealed class OrderToastModel : IDisposable
    {
        public const double DefaultDuration = 3d;
        private readonly IOrderService _orders;
        private readonly Func<double> _now;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private double _shownAt;
        private bool _disposed;

        public OrderToastModel(IOrderService orders, IEventBus events, Func<double> now, double duration = DefaultDuration)
        {
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            _now = now ?? throw new ArgumentNullException(nameof(now));
            Duration = duration > 0d ? duration : DefaultDuration;
            _subscriptions.Add(events.Subscribe<OrderStatusChanged>(OnStatus));
            _subscriptions.Add(events.Subscribe<DeliveryRejected>(OnRejected));
        }

        public double Duration { get; }
        public HudText Current { get; private set; }
        public bool IsVisible => !Current.IsEmpty;
        /// <summary>True for a successful milestone (completed/ready/sent); false for failures.</summary>
        public bool IsPositive { get; private set; }
        public int Version { get; private set; }
        public Exception LastFault { get; private set; }
        public event Action Changed;

        /// <summary>Hides an expired toast. Returns true when visibility changed.</summary>
        public bool Tick()
        {
            if (!IsVisible || _now() - _shownAt < Duration) { return false; }
            Current = HudText.Empty;
            Version++;
            Changed?.Invoke();
            return true;
        }

        public void Dispose()
        {
            if (_disposed) { return; }
            _disposed = true;
            foreach (IDisposable subscription in _subscriptions) { subscription.Dispose(); }
            _subscriptions.Clear();
        }

        /// <summary>Toast key for an order status, or null when the status is not announced.</summary>
        public static string KeyFor(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.SentToStall: return HudKeys.ToastSent;
                case OrderStatus.Ready: return HudKeys.ToastReady;
                case OrderStatus.Completed: return HudKeys.ToastCompleted;
                case OrderStatus.Failed: return HudKeys.ToastFailed;
                default: return null;
            }
        }

        private void OnStatus(OrderStatusChanged change)
        {
            string key = KeyFor(change.Status);
            if (key == null) { return; }
            Show(key, change.OrderId, change.Status != OrderStatus.Failed);
        }

        private void OnRejected(DeliveryRejected rejected) => Show(HudKeys.ToastWrongTarget, rejected.Order, false);

        private void Show(string key, OrderId id, bool positive)
        {
            if (_disposed) { return; }
            try
            {
                IReadOnlyOrder order = _orders.Get(id);
                // Completed/Failed orders leave Active but stay queryable by id.
                HudText origin = order != null ? HudKeys.Origin(order.Origin) : HudText.Of(HudKeys.HeldUnknown);
                Current = HudText.Of(key, origin);
                IsPositive = positive;
                _shownAt = _now();
                Version++;
            }
            catch (Exception fault)
            {
                LastFault = fault;
                return;
            }
            Changed?.Invoke();
        }
    }
}
